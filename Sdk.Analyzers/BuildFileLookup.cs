// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Finds the build files the SDK supplies as <see cref="AdditionalText"/>, and locates the line
/// inside one that declares a given package.
/// </summary>
/// <remarks>
/// Several of the package-hygiene rules need the same two things: pick a build-generated input out
/// of <c>AdditionalFiles</c> by name, and point a diagnostic at the line a reader has to edit.
/// Each rule grew its own copy, so the lookup lived three times over and the line match twice with
/// slightly different spellings. They live here once instead.
/// </remarks>
internal static class BuildFileLookup
{
	/// <summary>
	/// Finds an additional file by file name, ignoring the directory it was written to.
	/// </summary>
	/// <param name="files">The additional files supplied to the compilation.</param>
	/// <param name="fileName">The file name to match.</param>
	/// <returns>The matching file, or <see langword="null"/> when the SDK did not supply it.</returns>
	public static AdditionalText? ByName(ImmutableArray<AdditionalText> files, string fileName)
	{
		foreach (AdditionalText file in files)
		{
			if (string.Equals(Path.GetFileName(file.Path), fileName, StringComparison.OrdinalIgnoreCase))
			{
				return file;
			}
		}

		return null;
	}

	/// <summary>
	/// Finds the project file among the additional files.
	/// </summary>
	/// <param name="files">The additional files supplied to the compilation.</param>
	/// <returns>The project file, or <see langword="null"/> when the SDK did not supply it.</returns>
	public static AdditionalText? ProjectFile(ImmutableArray<AdditionalText> files) =>
		files.FirstOrDefault(f => f.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Determines whether a line declares the named MSBuild item for a package identifier.
	/// </summary>
	/// <param name="lineText">The line to inspect.</param>
	/// <param name="elementName">The item name, for example <c>PackageReference</c> or <c>PackageVersion</c>.</param>
	/// <param name="packageId">The package identifier to match.</param>
	/// <returns><see langword="true"/> when the line declares that package.</returns>
	/// <remarks>
	/// The element name is matched loosely rather than as <c>&lt;PackageReference</c>, which is the
	/// spelling this has always shipped with. It is unambiguous in practice because no line carries
	/// two of these item names, and because <c>PackageVersion</c> is not a substring of
	/// <c>PackageReference</c> or the other way round. NuGet package identifiers are
	/// case-insensitive, and both quoting styles are accepted.
	/// </remarks>
	public static bool DeclaresPackage(string lineText, string elementName, string packageId) =>
		lineText.IndexOf(elementName, StringComparison.OrdinalIgnoreCase) >= 0
		&& (lineText.IndexOf($"\"{packageId}\"", StringComparison.OrdinalIgnoreCase) >= 0
			|| lineText.IndexOf($"'{packageId}'", StringComparison.OrdinalIgnoreCase) >= 0);

	/// <summary>
	/// Locates the line in a build file that declares a package, as a diagnostic location.
	/// </summary>
	/// <param name="file">The build file to search, which may be <see langword="null"/>.</param>
	/// <param name="elementName">The item name to match.</param>
	/// <param name="packageId">The package identifier to match.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The line's location, or <see cref="Location.None"/> when it cannot be found.</returns>
	/// <remarks>
	/// A build-file location is used in preference to a syntax-tree one throughout these rules, for
	/// the reason recorded on <see cref="ProjectSourceLocation"/>: package compile items are
	/// prepended to the compilation, so the first syntax tree is usually generated code, and a
	/// diagnostic located in generated code is dropped under
	/// <see cref="Microsoft.CodeAnalysis.Diagnostics.GeneratedCodeAnalysisFlags.None"/>. The build
	/// file is also the more useful place to point: it is what has to change.
	/// </remarks>
	public static Location DeclarationLocation(
		AdditionalText? file,
		string elementName,
		string packageId,
		CancellationToken cancellationToken)
	{
		SourceText? text = file?.GetText(cancellationToken);

		if (file is null || text is null)
		{
			return Location.None;
		}

		foreach (TextLine line in text.Lines)
		{
			if (DeclaresPackage(line.ToString(), elementName, packageId))
			{
				return Location.Create(file.Path, line.Span, text.Lines.GetLinePositionSpan(line.Span));
			}
		}

		return Location.None;
	}
}
