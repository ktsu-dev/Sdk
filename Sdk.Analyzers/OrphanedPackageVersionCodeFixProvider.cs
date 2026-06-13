// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

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
/// Code fix provider that removes an orphaned <c>PackageVersion</c> entry from
/// <c>Directory.Packages.props</c>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(OrphanedPackageVersionCodeFixProvider))]
[Shared]
public class OrphanedPackageVersionCodeFixProvider : CodeFixProvider
{
	/// <inheritdoc/>
	public override ImmutableArray<string> FixableDiagnosticIds => [OrphanedPackageVersionAnalyzer.DiagnosticId];

	/// <inheritdoc/>
	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <inheritdoc/>
	public override Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		// The diagnostic is reported against Directory.Packages.props, which is supplied to the
		// compiler as an additional file, so the fix targets the additional text document.
		TextDocument? document = context.TextDocument;
		if (document is null)
		{
			return Task.CompletedTask;
		}

		Diagnostic diagnostic = context.Diagnostics.First();
		if (!diagnostic.Properties.TryGetValue(OrphanedPackageVersionAnalyzer.PackageIdProperty, out string? packageId) ||
			string.IsNullOrEmpty(packageId))
		{
			return Task.CompletedTask;
		}

		context.RegisterCodeFix(
			CodeAction.Create(
				title: $"Remove orphaned PackageVersion '{packageId}'",
				createChangedSolution: ct => RemovePackageVersionAsync(document, packageId!, ct),
				equivalenceKey: nameof(OrphanedPackageVersionCodeFixProvider)),
			diagnostic);

		return Task.CompletedTask;
	}

	private static async Task<Solution> RemovePackageVersionAsync(
		TextDocument document,
		string packageId,
		CancellationToken cancellationToken)
	{
		SourceText? text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
		if (text is null)
		{
			return document.Project.Solution;
		}

		foreach (TextLine line in text.Lines)
		{
			if (!OrphanedPackageVersionAnalyzer.IsPackageVersionLineFor(line.ToString(), packageId))
			{
				continue;
			}

			// Remove the entire line, including its line break, so no blank line is left behind.
			SourceText newText = text.Replace(line.SpanIncludingLineBreak, string.Empty);
			return document.Project.Solution.WithAdditionalDocumentText(document.Id, newText);
		}

		return document.Project.Solution;
	}
}
