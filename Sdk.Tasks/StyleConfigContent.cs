// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Tasks;

using System;
using System.Globalization;
using System.Text;

/// <summary>
/// Pure transformations applied to a style/config file before it is written to a consumer
/// repository. Kept free of MSBuild and file-system types so the behaviour can be unit tested
/// directly.
/// </summary>
internal static class StyleConfigContent
{
	/// <summary>
	/// The .editorconfig key whose value carries the file header each consumer repository stamps
	/// onto its source files.
	/// </summary>
	internal const string HeaderKey = "file_header_template";

	/// <summary>
	/// The longest mutex scope key retained. Mutex names are length-limited on some platforms, and a
	/// truncated collision only over-serializes, which is harmless.
	/// </summary>
	private const int MaxScopeKeyLength = 180;

	/// <summary>
	/// Builds the <c>file_header_template</c> line for a consumer's copyright text.
	/// </summary>
	/// <param name="copyrightText">The contents of the consumer's COPYRIGHT.md, or null/blank.</param>
	/// <returns>The full key/value line, or <see langword="null"/> when there is no copyright text.</returns>
	/// <remarks>
	/// .editorconfig has no multi-line values, so real newlines are encoded as the two characters
	/// <c>\n</c>. Line endings are normalized first so a CRLF file does not produce <c>\r\n</c>.
	/// </remarks>
	public static string? BuildHeaderLine(string? copyrightText) =>
		string.IsNullOrWhiteSpace(copyrightText)
			? null
			: HeaderKey + " = " + copyrightText!
				.Replace("\r\n", "\n")
				.Replace("\r", "\n")
				.Replace("\n", "\\n");

	/// <summary>
	/// Replaces every <c>file_header_template</c> assignment in <paramref name="content"/> with
	/// <paramref name="headerLine"/>, preserving each line's original indentation and terminator.
	/// </summary>
	/// <param name="content">The packaged .editorconfig content.</param>
	/// <param name="headerLine">The replacement line, or null to leave the content untouched.</param>
	/// <returns>The rewritten content.</returns>
	/// <remarks>
	/// The whole assignment line is replaced wherever it appears, rather than matching a known
	/// default value: the packaged default is free to change, and a value-based match would
	/// silently leave a stale header behind when it does.
	/// </remarks>
	public static string ApplyHeader(string content, string? headerLine)
	{
		if (headerLine is null || content.Length == 0)
		{
			return content;
		}

		StringBuilder builder = new(content.Length);
		int cursor = 0;

		while (cursor < content.Length)
		{
			int lineFeed = content.IndexOf('\n', cursor);
			int next = lineFeed < 0 ? content.Length : lineFeed + 1;
			string line = content.Substring(cursor, next - cursor);
			cursor = next;

			string body = line.TrimEnd('\r', '\n');
			string terminator = line.Substring(body.Length);
			string keyed = body.TrimStart();

			if (IsHeaderAssignment(keyed))
			{
				string indent = body.Substring(0, body.Length - keyed.Length);
				builder.Append(indent).Append(headerLine).Append(terminator);
			}
			else
			{
				builder.Append(line);
			}
		}

		return builder.ToString();
	}

	/// <summary>
	/// Derives a stable, path-based key for the cross-process sync mutex.
	/// </summary>
	/// <param name="scope">The solution directory the sync is scoped to.</param>
	/// <returns>A lower-case key containing only letters, digits and underscores.</returns>
	/// <remarks>
	/// Distinct solutions get distinct locks. Non-alphanumeric characters are folded to underscores
	/// because a mutex name may not contain a path separator, and the key is trimmed from the left
	/// so the most specific part of a long path is what survives.
	/// </remarks>
	public static string BuildScopeKey(string? scope)
	{
		char[] chars = (scope ?? string.Empty).ToLowerInvariant().ToCharArray();

		for (int i = 0; i < chars.Length; i++)
		{
			if (!char.IsLetterOrDigit(chars[i]))
			{
				chars[i] = '_';
			}
		}

		string key = new(chars);

		return key.Length > MaxScopeKeyLength
			? key.Substring(key.Length - MaxScopeKeyLength)
			: key;
	}

	/// <summary>
	/// Determines whether a leading-trimmed line assigns the header key.
	/// </summary>
	/// <param name="trimmedLine">The line with leading whitespace removed.</param>
	/// <returns><see langword="true"/> when the line is a <c>file_header_template</c> assignment.</returns>
	private static bool IsHeaderAssignment(string trimmedLine)
	{
		if (!trimmedLine.StartsWith(HeaderKey, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string remainder = trimmedLine.Substring(HeaderKey.Length).TrimStart();

		return remainder.StartsWith("=", StringComparison.Ordinal);
	}

	/// <summary>
	/// Builds the system-wide mutex name for a sync scope.
	/// </summary>
	/// <param name="scope">The solution directory the sync is scoped to.</param>
	/// <returns>The mutex name.</returns>
	public static string BuildMutexName(string? scope) =>
		string.Format(CultureInfo.InvariantCulture, @"Global\ktsu-sdk-style-sync-{0}", BuildScopeKey(scope));
}
