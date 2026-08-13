// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Tasks;

using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

/// <summary>
/// Syncs a consumer repository's style/config files from the defaults packaged in ktsu.Sdk.
/// </summary>
/// <remarks>
/// The destinations live at the solution root, but the invoking target runs once per inner build
/// (per TargetFramework) and per project, so many MSBuild nodes race for the same handful of files.
/// Two things keep that safe:
/// <list type="bullet">
/// <item>The desired content is computed first and compared against what is already on disk, so a
/// synced file is never rewritten. Steady-state builds do reads only.</item>
/// <item>The rare real write is serialized across processes by a named mutex scoped to the solution
/// directory, with a bounded retry as a fallback for hosts where a named mutex cannot be created.</item>
/// </list>
/// A failure here never fails the build: a style file is not worth breaking a consumer's build over.
/// </remarks>
public sealed class KtsuSyncStyleConfigFiles : Task
{
	/// <summary>Metadata name carrying the destination path for a source file.</summary>
	private const string DestinationMetadata = "Destination";

	/// <summary>Metadata name marking a file whose header line should be rewritten.</summary>
	private const string RewriteHeaderMetadata = "RewriteHeader";

	/// <summary>How long to wait for the cross-process sync lock before proceeding anyway.</summary>
	private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(120);

	/// <summary>Number of attempts made to write a destination that another node may hold open.</summary>
	private const int WriteAttempts = 5;

	/// <summary>
	/// The packaged default files to sync. Each carries a <c>Destination</c> metadata value, and
	/// optionally <c>RewriteHeader</c> to opt into copyright-header substitution.
	/// </summary>
	[Required]
	public ITaskItem[] SourceFiles { get; set; } = [];

	/// <summary>The solution directory, used to scope the cross-process lock.</summary>
	[Required]
	public string SyncScope { get; set; } = string.Empty;

	/// <summary>The consumer's copyright text, stamped into the .editorconfig file header.</summary>
	public string? CopyrightText { get; set; }

	/// <inheritdoc/>
	public override bool Execute()
	{
		string? headerLine = StyleConfigContent.BuildHeaderLine(CopyrightText);

		Mutex? mutex = null;
		bool held = false;

		try
		{
			(mutex, held) = AcquireLock();
			SyncAll(headerLine);
		}
		finally
		{
			if (mutex is not null)
			{
				if (held)
				{
					mutex.ReleaseMutex();
				}

				mutex.Dispose();
			}
		}

		return !Log.HasLoggedErrors;
	}

	/// <summary>
	/// Takes the cross-process sync lock, tolerating hosts where a named mutex is unavailable.
	/// </summary>
	/// <returns>The mutex (when one could be created) and whether it is held.</returns>
	private (Mutex? Mutex, bool Held) AcquireLock()
	{
		try
		{
			Mutex mutex = new(false, StyleConfigContent.BuildMutexName(SyncScope));
			return (mutex, mutex.WaitOne(LockTimeout));
		}
		catch (AbandonedMutexException)
		{
			// A node died holding the lock. The files are still consistent because every write is a
			// whole-file write, so take ownership and carry on. The mutex object is lost here, which
			// only means this process does not release it explicitly.
			return (null, false);
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or WaitHandleCannotBeOpenedException)
		{
			Log.LogMessage(MessageImportance.Low, "ktsu.Sdk: style config sync lock unavailable ({0}); relying on retries.", exception.Message);
			return (null, false);
		}
	}

	/// <summary>
	/// Writes every source file to its destination, skipping any that already match.
	/// </summary>
	/// <param name="headerLine">The replacement file-header line, or null to leave headers alone.</param>
	private void SyncAll(string? headerLine)
	{
		UTF8Encoding encoding = new(false);

		foreach (ITaskItem item in SourceFiles)
		{
			string source = item.GetMetadata("FullPath");
			string destination = item.GetMetadata(DestinationMetadata);

			// Only files the consumer already has are synced; the SDK never creates one.
			if (string.IsNullOrEmpty(destination) || !File.Exists(source) || !File.Exists(destination))
			{
				continue;
			}

			string desired = File.ReadAllText(source);

			if (string.Equals(item.GetMetadata(RewriteHeaderMetadata), "true", StringComparison.OrdinalIgnoreCase))
			{
				desired = StyleConfigContent.ApplyHeader(desired, headerLine);
			}

			Write(destination, desired, encoding);
		}
	}

	/// <summary>
	/// Writes <paramref name="desired"/> to <paramref name="destination"/> unless it already
	/// matches, retrying briefly while another node holds the file open.
	/// </summary>
	/// <param name="destination">The file to write.</param>
	/// <param name="desired">The content it should have.</param>
	/// <param name="encoding">The encoding to write with (UTF-8, no BOM).</param>
	private void Write(string destination, string desired, Encoding encoding)
	{
		for (int attempt = 0; attempt < WriteAttempts; attempt++)
		{
			try
			{
				if (string.Equals(File.ReadAllText(destination), desired, StringComparison.Ordinal))
				{
					return;
				}

				File.WriteAllText(destination, desired, encoding);
				Log.LogMessage(MessageImportance.Low, "ktsu.Sdk: synced {0} from SDK defaults.", destination);
				return;
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException && attempt < WriteAttempts - 1)
			{
				Thread.Sleep(100 * (attempt + 1));
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
			{
				// Never fail a build over a style file that another node is mid-write on.
				Log.LogMessage(MessageImportance.Low, "ktsu.Sdk: could not sync {0} ({1}).", destination, exception.Message);
				return;
			}
		}
	}
}
