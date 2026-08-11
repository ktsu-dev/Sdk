// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Analyzer that enforces <c>PrivateAssets="all"</c> on build-time-only package references.
/// NuGet only omits a dependency from the produced package altogether when every asset kind is
/// private, so a partial <c>PrivateAssets</c> value still leaks the dependency to consumers.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NonPrivateBuildTimePackageAnalyzer : KtsuAnalyzerBase
{
	/// <summary>
	/// Diagnostic ID for this analyzer
	/// </summary>
	public const string DiagnosticId = "KTSU0007";

	/// <summary>
	/// Diagnostic property carrying the package identifier the code fix should update
	/// </summary>
	public const string PackageIdProperty = "PackageId";

	/// <summary>
	/// The package identifier this rule governs
	/// </summary>
	public const string PolyfillPackageId = "Polyfill";

	private static readonly LocalizableString Title = "Build-time package reference is not private";
	private static readonly LocalizableString MessageFormat = "Package reference '{0}' must set PrivateAssets=\"all\". Without it this build-time-only package leaks into the dependency graph of every consumer.";
	private static readonly LocalizableString Description = "Build-time-only packages must not flow to consumers as transitive dependencies. Only a fully private reference is omitted from the produced package's dependencies.";

	/// <summary>
	/// The asset kinds that <c>PrivateAssets="all"</c> expands to. A spelled-out value covering
	/// every one of them is equivalent to <c>all</c> and is not reported.
	/// </summary>
	private static readonly ImmutableHashSet<string> AllAssetKinds = ImmutableHashSet.Create(
		StringComparer.OrdinalIgnoreCase,
		"compile",
		"runtime",
		"build",
		"buildMultitargeting",
		"buildTransitive",
		"contentFiles",
		"analyzers",
		"native");

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		Title,
		MessageFormat,
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: Description,
		customTags: "CompilationEnd");

	/// <inheritdoc/>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	/// <inheritdoc/>
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterCompilationAction(AnalyzeCompilation);
	}

	private static void AnalyzeCompilation(CompilationAnalysisContext context)
	{
		AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;

		options.TryGetValue("build_property.IsTestProject", out string? isTestProject);

		// Test projects are exempt from the standard-package rules, so they are exempt from this one.
		if (string.Equals(isTestProject, "true", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		// A missing reference is KTSU0001's concern; this rule only grades an existing one.
		options.TryGetValue("build_property.HasPolyfill", out string? hasPolyfill);

		if (!string.Equals(hasPolyfill, "true", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		// PrivateAssets metadata does not appear in the compilation references, so it arrives as an
		// SDK-computed property via CompilerVisibleProperty.
		options.TryGetValue("build_property.PolyfillPrivateAssets", out string? privateAssets);

		if (IsFullyPrivate(privateAssets))
		{
			return;
		}

		Diagnostic diagnostic = Diagnostic.Create(
			Rule,
			FindReferenceLocation(context, PolyfillPackageId),
			ImmutableDictionary<string, string?>.Empty.Add(PackageIdProperty, PolyfillPackageId),
			PolyfillPackageId);

		context.ReportDiagnostic(diagnostic);
	}

	/// <summary>
	/// Locates the <c>PackageReference</c> line in the project file, which the SDK supplies as an
	/// additional file.
	/// </summary>
	/// <param name="context">The compilation analysis context.</param>
	/// <param name="packageId">The package identifier to locate.</param>
	/// <returns>The line's location, or <see cref="Location.None"/> when it cannot be found.</returns>
	/// <remarks>
	/// A syntax-tree location is deliberately not used. Package compile items are prepended to the
	/// compilation, so the first syntax tree is usually a file from Polyfill itself, which counts as
	/// generated code. Diagnostics located in generated code are dropped under
	/// <see cref="GeneratedCodeAnalysisFlags.None"/>, which silently loses the diagnostic. The
	/// project file is also the more useful place to point: it is what has to change.
	/// </remarks>
	private static Location FindReferenceLocation(CompilationAnalysisContext context, string packageId)
	{
		AdditionalText? projectFile = context.Options.AdditionalFiles.FirstOrDefault(
			f => f.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

		SourceText? projectText = projectFile?.GetText(context.CancellationToken);

		if (projectFile is null || projectText is null)
		{
			return Location.None;
		}

		foreach (TextLine line in projectText.Lines)
		{
			if (IsPackageReferenceLineFor(line.ToString(), packageId))
			{
				return Location.Create(projectFile.Path, line.Span, projectText.Lines.GetLinePositionSpan(line.Span));
			}
		}

		return Location.None;
	}

	/// <summary>
	/// Determines whether a line of the project file declares a <c>PackageReference</c> for the
	/// supplied package identifier.
	/// </summary>
	/// <param name="lineText">The line to inspect.</param>
	/// <param name="packageId">The package identifier to match.</param>
	/// <returns><see langword="true"/> when the line references the package.</returns>
	internal static bool IsPackageReferenceLineFor(string lineText, string packageId) =>
		lineText.IndexOf("PackageReference", StringComparison.OrdinalIgnoreCase) >= 0
		&& (lineText.IndexOf($"\"{packageId}\"", StringComparison.OrdinalIgnoreCase) >= 0
			|| lineText.IndexOf($"'{packageId}'", StringComparison.OrdinalIgnoreCase) >= 0);

	/// <summary>
	/// Determines whether a <c>PrivateAssets</c> value makes the reference fully private.
	/// </summary>
	/// <param name="privateAssets">The semicolon-separated metadata value, which may be empty.</param>
	/// <returns><see langword="true"/> when the value is <c>all</c> or names every asset kind.</returns>
	private static bool IsFullyPrivate(string? privateAssets)
	{
		if (string.IsNullOrWhiteSpace(privateAssets))
		{
			return false;
		}

		HashSet<string> tokens = new(StringComparer.OrdinalIgnoreCase);

		foreach (string token in privateAssets!.Split(';'))
		{
			string trimmed = token.Trim();

			if (trimmed.Length > 0)
			{
				tokens.Add(trimmed);
			}
		}

		return tokens.Contains("all") || AllAssetKinds.All(tokens.Contains);
	}
}
