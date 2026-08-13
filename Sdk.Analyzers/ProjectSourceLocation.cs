// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Picks a location for a project-level diagnostic that the project itself owns.
/// </summary>
/// <remarks>
/// Simply taking the compilation's first syntax tree is wrong. Package compile items are prepended
/// to the compilation, so with a source-embedding package such as Polyfill the first tree is a file
/// from that package, carrying an <c>auto-generated</c> header. Diagnostics located in generated
/// code are dropped under <see cref="GeneratedCodeAnalysisFlags.None"/>, which silently loses the
/// diagnostic - the root cause of ktsu-dev/Sdk#12 / #8 / #11. Restricting the choice to files under
/// the project directory, excluding the intermediate output, also guarantees that any accompanying
/// code fix edits a file the user actually owns rather than one in the NuGet cache.
/// </remarks>
internal static class ProjectSourceLocation
{
	/// <summary>
	/// Finds a location in a source file the project owns.
	/// </summary>
	/// <param name="compilation">The compilation being analyzed.</param>
	/// <param name="options">The global analyzer config options, used to read <c>ProjectDir</c>.</param>
	/// <returns>A location in a project-owned file, or <see cref="Location.None"/> when there is none.</returns>
	public static Location Find(Compilation compilation, AnalyzerConfigOptions options)
	{
		options.TryGetValue("build_property.ProjectDir", out string? projectDir);

		SyntaxTree? tree = null;

		if (!string.IsNullOrEmpty(projectDir))
		{
			tree = compilation.SyntaxTrees.FirstOrDefault(t => IsProjectOwned(t.FilePath, projectDir!));
		}

		tree ??= compilation.SyntaxTrees.FirstOrDefault();

		return tree?.GetRoot().GetLocation() ?? Location.None;
	}

	/// <summary>
	/// Determines whether a source file belongs to the project rather than to a package or the
	/// intermediate output.
	/// </summary>
	/// <param name="filePath">The source file path.</param>
	/// <param name="projectDir">The project directory, with a trailing separator.</param>
	/// <returns><see langword="true"/> when the file is project-owned source.</returns>
	private static bool IsProjectOwned(string filePath, string projectDir)
	{
		if (string.IsNullOrEmpty(filePath) ||
			!filePath.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string relative = filePath.Substring(projectDir.Length);

		return !relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
			&& !relative.StartsWith("obj/", StringComparison.OrdinalIgnoreCase);
	}
}
