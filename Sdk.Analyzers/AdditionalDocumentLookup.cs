// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

/// <summary>
/// Locates the build files that the SDK supplies to analyzers as additional files, so that code
/// fixes can edit them. The SDK adds the project file and <c>Directory.Packages.props</c> to
/// <c>AdditionalFiles</c> in <c>Sdk.targets</c>.
/// </summary>
internal static class AdditionalDocumentLookup
{
	/// <summary>
	/// Finds the project file among the additional documents, preferring an exact path match with
	/// the project itself before falling back to any <c>.csproj</c>.
	/// </summary>
	/// <param name="project">The project whose additional documents are searched.</param>
	/// <returns>The project file document, or <see langword="null"/> when the SDK did not supply it.</returns>
	public static TextDocument? FindProjectFile(Project project) =>
		project.AdditionalDocuments.FirstOrDefault(
			d => !string.IsNullOrEmpty(project.FilePath) && string.Equals(d.FilePath, project.FilePath, StringComparison.OrdinalIgnoreCase))
		?? project.AdditionalDocuments.FirstOrDefault(
			d => d.FilePath is not null && d.FilePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Finds the Central Package Management props file among the additional documents.
	/// </summary>
	/// <param name="project">The project whose additional documents are searched.</param>
	/// <returns>The props file document, or <see langword="null"/> when the SDK did not supply it.</returns>
	public static TextDocument? FindDirectoryPackagesProps(Project project) =>
		project.AdditionalDocuments.FirstOrDefault(
			d => d.FilePath is not null && string.Equals(
				System.IO.Path.GetFileName(d.FilePath),
				OrphanedPackageVersionAnalyzer.DirectoryPackagesPropsFileName,
				StringComparison.OrdinalIgnoreCase));
}
