// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

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

		if (PrivateAssets.IsFullyPrivate(privateAssets))
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
	private static Location FindReferenceLocation(CompilationAnalysisContext context, string packageId) =>
		BuildFileLookup.DeclarationLocation(
			BuildFileLookup.ProjectFile(context.Options.AdditionalFiles),
			"PackageReference",
			packageId,
			context.CancellationToken);
}
