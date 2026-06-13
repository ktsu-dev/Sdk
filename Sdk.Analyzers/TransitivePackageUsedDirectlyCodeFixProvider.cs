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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Code fix provider that adds a direct <c>PackageReference</c> for a package that is currently
/// only available transitively. When Central Package Management is in use, a matching
/// <c>PackageVersion</c> is also added to <c>Directory.Packages.props</c> when one is missing.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TransitivePackageUsedDirectlyCodeFixProvider))]
[Shared]
public class TransitivePackageUsedDirectlyCodeFixProvider : CodeFixProvider
{
	/// <inheritdoc/>
	public override ImmutableArray<string> FixableDiagnosticIds => [TransitivePackageUsedDirectlyAnalyzer.DiagnosticId];

	/// <inheritdoc/>
	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <inheritdoc/>
	public override Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		Diagnostic diagnostic = context.Diagnostics.First();

		if (!diagnostic.Properties.TryGetValue(TransitivePackageUsedDirectlyAnalyzer.PackageIdProperty, out string? packageId) ||
			string.IsNullOrEmpty(packageId))
		{
			return Task.CompletedTask;
		}

		diagnostic.Properties.TryGetValue(TransitivePackageUsedDirectlyAnalyzer.PackageVersionProperty, out string? packageVersion);

		context.RegisterCodeFix(
			CodeAction.Create(
				title: $"Add PackageReference to '{packageId}'",
				createChangedSolution: ct => AddPackageReferenceAsync(context.Document, packageId!, packageVersion, ct),
				equivalenceKey: nameof(TransitivePackageUsedDirectlyCodeFixProvider)),
			diagnostic);

		return Task.CompletedTask;
	}

	private static async Task<Solution> AddPackageReferenceAsync(
		Document document,
		string packageId,
		string? packageVersion,
		CancellationToken cancellationToken)
	{
		Project project = document.Project;
		Solution solution = project.Solution;

		bool centrallyManaged = IsCentrallyManaged(project);

		TextDocument? projectFile = FindProjectFile(project);
		if (projectFile is null)
		{
			return solution;
		}

		SourceText? projectText = await projectFile.GetTextAsync(cancellationToken).ConfigureAwait(false);
		if (projectText is null)
		{
			return solution;
		}

		// Under Central Package Management the version lives in Directory.Packages.props, so the
		// PackageReference carries no Version attribute.
		string? versionForReference = centrallyManaged ? null : packageVersion;
		SourceText newProjectText = InsertElement(
			projectText,
			BuildPackageReference(packageId, versionForReference));
		solution = solution.WithAdditionalDocumentText(projectFile.Id, newProjectText);

		if (centrallyManaged)
		{
			TextDocument? packagesProps = FindDirectoryPackagesProps(project);
			SourceText? propsText = packagesProps is null ? null : await packagesProps.GetTextAsync(cancellationToken).ConfigureAwait(false);

			if (packagesProps is not null && propsText is not null && !ContainsInclude(propsText, packageId))
			{
				SourceText newPropsText = InsertElement(
					propsText,
					BuildPackageVersion(packageId, packageVersion));
				solution = solution.WithAdditionalDocumentText(packagesProps.Id, newPropsText);
			}
		}

		return solution;
	}

	private static bool IsCentrallyManaged(Project project)
	{
		AnalyzerConfigOptions options = project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions;
		return options.TryGetValue("build_property.ManagePackageVersionsCentrally", out string? value)
			&& string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
	}

	private static TextDocument? FindProjectFile(Project project) =>
		project.AdditionalDocuments.FirstOrDefault(
			d => !string.IsNullOrEmpty(project.FilePath) && string.Equals(d.FilePath, project.FilePath, StringComparison.OrdinalIgnoreCase))
		?? project.AdditionalDocuments.FirstOrDefault(
			d => d.FilePath is not null && d.FilePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

	private static TextDocument? FindDirectoryPackagesProps(Project project) =>
		project.AdditionalDocuments.FirstOrDefault(
			d => d.FilePath is not null && string.Equals(
				System.IO.Path.GetFileName(d.FilePath),
				OrphanedPackageVersionAnalyzer.DirectoryPackagesPropsFileName,
				StringComparison.OrdinalIgnoreCase));

	private static string BuildPackageReference(string packageId, string? version) =>
		version is { Length: > 0 }
			? $"<PackageReference Include=\"{packageId}\" Version=\"{version}\" />"
			: $"<PackageReference Include=\"{packageId}\" />";

	private static string BuildPackageVersion(string packageId, string? version) =>
		$"<PackageVersion Include=\"{packageId}\" Version=\"{version}\" />";

	private static bool ContainsInclude(SourceText text, string packageId)
	{
		string content = text.ToString();
		return content.IndexOf("Include=\"" + packageId + "\"", StringComparison.OrdinalIgnoreCase) >= 0
			|| content.IndexOf("Include='" + packageId + "'", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	/// <summary>
	/// Inserts an item element next to an existing item of the same kind, or in a new ItemGroup
	/// before the closing Project tag when no suitable anchor exists.
	/// </summary>
	private static SourceText InsertElement(SourceText text, string element)
	{
		string content = text.ToString();
		string newLine = content.Contains("\r\n") ? "\r\n" : "\n";

		// The element word ("PackageReference" / "PackageVersion") drives anchor detection.
		string elementName = element.Substring(1, element.IndexOf(' ') - 1);
		string anchor = "<" + elementName;

		int anchorIndex = content.IndexOf(anchor, StringComparison.Ordinal);
		if (anchorIndex >= 0)
		{
			int lineStart = content.LastIndexOf('\n', anchorIndex) + 1;
			string indent = content.Substring(lineStart, anchorIndex - lineStart);
			string insertion = indent + element + newLine;
			return SourceText.From(content.Insert(lineStart, insertion));
		}

		// No existing element of this kind: add a fresh ItemGroup before </Project>.
		int closeProject = content.LastIndexOf("</Project>", StringComparison.OrdinalIgnoreCase);
		string block = "  <ItemGroup>" + newLine
			+ "    " + element + newLine
			+ "  </ItemGroup>" + newLine;

		return closeProject >= 0
			? SourceText.From(content.Insert(closeProject, block))
			: SourceText.From(content + newLine + block);
	}
}
