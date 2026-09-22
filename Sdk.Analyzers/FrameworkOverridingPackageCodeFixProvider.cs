// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Code fix provider that pins a framework-overriding <c>PackageReference</c> down to the version
/// the shared framework supplies, and suppresses NuGet's NU1510 on that item.
/// </summary>
/// <remarks>
/// The <c>NoWarn</c> is not incidental. Once the version is pinned to the floor the reference
/// becomes prunable, and NuGet reports NU1510 - "the package is provided by the framework, remove
/// the reference" - which is the opposite of what KTSU0006 demands. The two rules can only be
/// satisfied together with that scoped suppression, so the fix that pins the version adds it in the
/// same edit rather than trading one build error for another.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FrameworkOverridingPackageCodeFixProvider))]
[Shared]
public class FrameworkOverridingPackageCodeFixProvider : CodeFixProvider
{
	/// <summary>
	/// The NuGet warning that fires once a framework-supplied reference is pinned to the floor.
	/// </summary>
	internal const string PruningWarning = "NU1510";

	/// <inheritdoc/>
	public override ImmutableArray<string> FixableDiagnosticIds => [FrameworkOverridingPackageAnalyzer.DiagnosticId];

	/// <inheritdoc/>
	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <inheritdoc/>
	public override Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		Diagnostic diagnostic = context.Diagnostics.First();

		if (!diagnostic.Properties.TryGetValue(FrameworkOverridingPackageAnalyzer.PackageIdProperty, out string? packageId) ||
			string.IsNullOrEmpty(packageId))
		{
			return Task.CompletedTask;
		}

		if (!diagnostic.Properties.TryGetValue(FrameworkOverridingPackageAnalyzer.FrameworkVersionProperty, out string? frameworkVersion) ||
			string.IsNullOrEmpty(frameworkVersion))
		{
			return Task.CompletedTask;
		}

		context.RegisterCodeFix(
			CodeAction.Create(
				title: $"Pin '{packageId}' to {frameworkVersion} and suppress {PruningWarning}",
				createChangedSolution: ct => PinToFrameworkVersionAsync(context.Document, packageId!, frameworkVersion!, ct),
				equivalenceKey: nameof(FrameworkOverridingPackageCodeFixProvider)),
			diagnostic);

		return Task.CompletedTask;
	}

	private static async Task<Solution> PinToFrameworkVersionAsync(
		Document document,
		string packageId,
		string frameworkVersion,
		CancellationToken cancellationToken)
	{
		Project project = document.Project;
		Solution solution = project.Solution;

		// Under Central Package Management the version lives in Directory.Packages.props and the
		// PackageReference carries no Version attribute, so the two edits land in different files.
		TextDocument? packagesProps = AdditionalDocumentLookup.FindDirectoryPackagesProps(project);
		SourceText? propsText = packagesProps is null
			? null
			: await packagesProps.GetTextAsync(cancellationToken).ConfigureAwait(false);

		bool pinnedCentrally = false;

		if (packagesProps is not null && propsText is not null)
		{
			SourceText updated = SetVersionAttribute(propsText, "PackageVersion", packageId, frameworkVersion, out pinnedCentrally);

			if (pinnedCentrally)
			{
				solution = solution.WithAdditionalDocumentText(packagesProps.Id, updated);
			}
		}

		TextDocument? projectFile = AdditionalDocumentLookup.FindProjectFile(project);
		SourceText? projectText = projectFile is null
			? null
			: await projectFile.GetTextAsync(cancellationToken).ConfigureAwait(false);

		if (projectFile is null || projectText is null)
		{
			return solution;
		}

		SourceText newProjectText = projectText;

		if (!pinnedCentrally)
		{
			newProjectText = SetVersionAttribute(newProjectText, "PackageReference", packageId, frameworkVersion, out _);
		}

		newProjectText = AddNoWarn(newProjectText, packageId, PruningWarning);

		return solution.WithAdditionalDocumentText(projectFile.Id, newProjectText);
	}

	/// <summary>
	/// Rewrites the <c>Version</c> attribute of the named element for a package, when that element
	/// declares one.
	/// </summary>
	/// <param name="text">The build file's text.</param>
	/// <param name="elementName">The element to find, <c>PackageVersion</c> or <c>PackageReference</c>.</param>
	/// <param name="packageId">The package identifier to match.</param>
	/// <param name="version">The version to pin to.</param>
	/// <param name="changed">Set when a version attribute was found and rewritten.</param>
	/// <returns>The updated text, or the original when there was nothing to change.</returns>
	internal static SourceText SetVersionAttribute(
		SourceText text,
		string elementName,
		string packageId,
		string version,
		out bool changed)
	{
		// A returned line equal to the original means the element carries no Version attribute,
		// which under central package management is the normal shape of a PackageReference. There
		// is nothing to pin there; the caller falls through to Directory.Packages.props.
		SourceText updated = RewriteDeclaringLine(
			text,
			elementName,
			packageId,
			lineText => ReplaceVersionAttribute(lineText, version));

		changed = !ReferenceEquals(updated, text);

		return updated;
	}

	/// <summary>
	/// Adds a <c>NoWarn</c> attribute carrying the pruning warning to a package's
	/// <c>PackageReference</c>, merging with any value already present.
	/// </summary>
	/// <param name="text">The project file's text.</param>
	/// <param name="packageId">The package identifier to match.</param>
	/// <param name="warning">The warning identifier to suppress.</param>
	/// <returns>The updated text, or the original when the reference already suppresses it.</returns>
	internal static SourceText AddNoWarn(SourceText text, string packageId, string warning) =>
		RewriteDeclaringLine(text, "PackageReference", packageId, lineText => AddNoWarnToLine(lineText, warning));

	/// <summary>
	/// Rewrites the first line that declares a package, splicing the result back into the file.
	/// </summary>
	/// <param name="text">The build file's text.</param>
	/// <param name="elementName">The item name to match.</param>
	/// <param name="packageId">The package identifier to match.</param>
	/// <param name="rewrite">Produces the replacement line, or the line it was given to decline.</param>
	/// <returns>The updated text, or the very same instance when nothing changed.</returns>
	/// <remarks>
	/// Returning the original instance rather than an equal one is what lets
	/// <see cref="SetVersionAttribute"/> report whether it did anything by reference, so a caller can
	/// tell "pinned it here" from "there was nothing here to pin" without a second search.
	/// </remarks>
	private static SourceText RewriteDeclaringLine(
		SourceText text,
		string elementName,
		string packageId,
		Func<string, string> rewrite)
	{
		string content = text.ToString();

		foreach (TextLine line in text.Lines)
		{
			string lineText = line.ToString();

			if (!BuildFileLookup.DeclaresPackage(lineText, elementName, packageId))
			{
				continue;
			}

			string rewritten = rewrite(lineText);

			return string.Equals(rewritten, lineText, StringComparison.Ordinal)
				? text
				: SourceText.From(
					content.Substring(0, line.Span.Start)
					+ rewritten
					+ content.Substring(line.Span.End));
		}

		return text;
	}

	/// <summary>
	/// Produces a line carrying the warning in its <c>NoWarn</c>, merging with any value present.
	/// </summary>
	/// <param name="lineText">The declaring line.</param>
	/// <param name="warning">The warning identifier to suppress.</param>
	/// <returns>The rewritten line, or <paramref name="lineText"/> when it already suppresses it.</returns>
	private static string AddNoWarnToLine(string lineText, string warning)
	{
		int noWarnIndex = lineText.IndexOf("NoWarn=\"", StringComparison.OrdinalIgnoreCase);

		if (noWarnIndex < 0)
		{
			// Insert before the element's own close so the attribute lands inside the tag, whether
			// it is self-closing or has a body.
			int selfClose = lineText.IndexOf("/>", StringComparison.Ordinal);
			int close = selfClose >= 0 ? selfClose : lineText.IndexOf('>');

			return close < 0
				? lineText
				: lineText.Substring(0, close).TrimEnd() + $" NoWarn=\"{warning}\"" + lineText.Substring(close);
		}

		int valueStart = noWarnIndex + "NoWarn=\"".Length;
		int valueEnd = lineText.IndexOf('"', valueStart);

		if (valueEnd < 0)
		{
			return lineText;
		}

		string existing = lineText.Substring(valueStart, valueEnd - valueStart);

		if (existing.Split(';').Any(token => string.Equals(token.Trim(), warning, StringComparison.OrdinalIgnoreCase)))
		{
			return lineText;
		}

		string merged = existing.Length == 0 ? warning : existing.TrimEnd(';') + ";" + warning;

		return lineText.Substring(0, valueStart) + merged + lineText.Substring(valueEnd);
	}

	private static string ReplaceVersionAttribute(string lineText, string version)
	{
		int versionIndex = lineText.IndexOf("Version=\"", StringComparison.OrdinalIgnoreCase);

		if (versionIndex < 0)
		{
			return lineText;
		}

		int valueStart = versionIndex + "Version=\"".Length;
		int valueEnd = lineText.IndexOf('"', valueStart);

		return valueEnd < 0
			? lineText
			: lineText.Substring(0, valueStart) + version + lineText.Substring(valueEnd);
	}
}
