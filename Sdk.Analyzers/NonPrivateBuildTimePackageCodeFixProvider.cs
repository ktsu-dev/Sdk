// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Code fix provider that makes a build-time-only <c>PackageReference</c> fully private by setting
/// <c>PrivateAssets="all"</c> on it. The edit is applied as targeted text replacement so the rest of
/// the project file keeps its original formatting.
/// </summary>
/// <remarks>
/// No fix is offered when the reference is declared outside the project file (for example in a
/// <c>Directory.Build.props</c>), because only the project file is supplied to the analyzer as an
/// additional document. The diagnostic still reports in that case.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NonPrivateBuildTimePackageCodeFixProvider))]
[Shared]
public class NonPrivateBuildTimePackageCodeFixProvider : CodeFixProvider
{
	private const string PrivateAssetsValue = "all";

	private static readonly Regex PrivateAssetsAttribute = new(
		"PrivateAssets\\s*=\\s*(\"[^\"]*\"|'[^']*')",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	private static readonly Regex PrivateAssetsElement = new(
		"<PrivateAssets\\s*>.*?</PrivateAssets\\s*>",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);

	/// <inheritdoc/>
	public override ImmutableArray<string> FixableDiagnosticIds => [NonPrivateBuildTimePackageAnalyzer.DiagnosticId];

	/// <inheritdoc/>
	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <inheritdoc/>
	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		Diagnostic diagnostic = context.Diagnostics.First();

		if (!diagnostic.Properties.TryGetValue(NonPrivateBuildTimePackageAnalyzer.PackageIdProperty, out string? packageId) ||
			string.IsNullOrEmpty(packageId))
		{
			return;
		}

		Project project = context.Document.Project;
		TextDocument? projectFile = AdditionalDocumentLookup.FindProjectFile(project);

		if (projectFile is null)
		{
			return;
		}

		SourceText? projectText = await projectFile.GetTextAsync(context.CancellationToken).ConfigureAwait(false);

		if (projectText is null)
		{
			return;
		}

		// The reference may be declared in a file this fix cannot reach, so the edit is computed up
		// front and the fix is only offered when there is something to change.
		SourceText? updatedText = MakeReferencePrivate(projectText, packageId!);

		if (updatedText is null)
		{
			return;
		}

		Solution updatedSolution = project.Solution.WithAdditionalDocumentText(projectFile.Id, updatedText);

		context.RegisterCodeFix(
			CodeAction.Create(
				title: $"Set PrivateAssets=\"all\" on '{packageId}'",
				createChangedSolution: _ => Task.FromResult(updatedSolution),
				equivalenceKey: nameof(NonPrivateBuildTimePackageCodeFixProvider)),
			diagnostic);
	}

	/// <summary>
	/// Rewrites the <c>PackageReference</c> for <paramref name="packageId"/> so that it is fully
	/// private, whether <c>PrivateAssets</c> is currently absent, an attribute, or a child element.
	/// </summary>
	/// <param name="text">The project file text.</param>
	/// <param name="packageId">The package identifier to update.</param>
	/// <returns>The updated text, or <see langword="null"/> when the reference was not found.</returns>
	private static SourceText? MakeReferencePrivate(SourceText text, string packageId)
	{
		string content = text.ToString();

		if (!TryFindOpenTag(content, packageId, out int tagStart, out int tagEnd))
		{
			return null;
		}

		string tag = content.Substring(tagStart, tagEnd - tagStart + 1);

		// An existing attribute is authoritative over anything else in the element.
		Match attribute = PrivateAssetsAttribute.Match(tag);

		if (attribute.Success)
		{
			string quote = attribute.Groups[1].Value[0].ToString();
			string replacement = $"PrivateAssets={quote}{PrivateAssetsValue}{quote}";
			return SourceText.From(content.Remove(attribute.Index + tagStart, attribute.Length)
				.Insert(attribute.Index + tagStart, replacement));
		}

		bool selfClosing = tag.EndsWith("/>", StringComparison.Ordinal);

		// A child element wins over an attribute in MSBuild, so an existing one must be rewritten
		// rather than shadowed by a newly added attribute.
		if (!selfClosing)
		{
			int closeIndex = content.IndexOf("</PackageReference", tagEnd, StringComparison.OrdinalIgnoreCase);

			if (closeIndex >= 0)
			{
				string body = content.Substring(tagEnd + 1, closeIndex - tagEnd - 1);
				Match element = PrivateAssetsElement.Match(body);

				if (element.Success)
				{
					int elementStart = tagEnd + 1 + element.Index;
					return SourceText.From(content.Remove(elementStart, element.Length)
						.Insert(elementStart, $"<PrivateAssets>{PrivateAssetsValue}</PrivateAssets>"));
				}
			}
		}

		// No PrivateAssets anywhere: add the attribute to the open tag. Rebuilding the tag end
		// normalizes whitespace only within the tag being changed.
		string withoutClose = tag.Substring(0, tag.Length - (selfClosing ? 2 : 1)).TrimEnd();
		string newTag = withoutClose + $" PrivateAssets=\"{PrivateAssetsValue}\"" + (selfClosing ? " />" : ">");

		return SourceText.From(content.Remove(tagStart, tag.Length).Insert(tagStart, newTag));
	}

	/// <summary>
	/// Locates the <c>PackageReference</c> open tag whose <c>Include</c> names <paramref name="packageId"/>.
	/// </summary>
	/// <param name="content">The project file text.</param>
	/// <param name="packageId">The package identifier to match, case-insensitively.</param>
	/// <param name="tagStart">The index of the opening angle bracket.</param>
	/// <param name="tagEnd">The index of the closing angle bracket.</param>
	/// <returns><see langword="true"/> when a matching tag was found.</returns>
	private static bool TryFindOpenTag(string content, string packageId, out int tagStart, out int tagEnd)
	{
		tagStart = -1;
		tagEnd = -1;

		int searchFrom = 0;

		while (true)
		{
			int candidate = content.IndexOf("<PackageReference", searchFrom, StringComparison.OrdinalIgnoreCase);

			if (candidate < 0)
			{
				return false;
			}

			int candidateEnd = content.IndexOf('>', candidate);

			if (candidateEnd < 0)
			{
				return false;
			}

			string tag = content.Substring(candidate, candidateEnd - candidate + 1);

			if (NamesPackage(tag, packageId))
			{
				tagStart = candidate;
				tagEnd = candidateEnd;
				return true;
			}

			searchFrom = candidateEnd + 1;
		}
	}

	/// <summary>
	/// Determines whether a <c>PackageReference</c> open tag includes the given package.
	/// </summary>
	/// <param name="tag">The open tag text.</param>
	/// <param name="packageId">The package identifier to match.</param>
	/// <returns><see langword="true"/> when the tag's <c>Include</c> names the package.</returns>
	private static bool NamesPackage(string tag, string packageId) =>
		tag.IndexOf($"Include=\"{packageId}\"", StringComparison.OrdinalIgnoreCase) >= 0
		|| tag.IndexOf($"Include='{packageId}'", StringComparison.OrdinalIgnoreCase) >= 0;
}
