// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer that enforces required standard package references
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingStandardPackagesAnalyzer : KtsuAnalyzerBase
{

	/// <summary>
	/// Diagnostic ID for this analyzer
	/// </summary>
	public const string DiagnosticId = "KTSU0001";

	private static readonly LocalizableString Title = "Missing required package reference";
	private static readonly LocalizableString MessageFormat = "Project must reference package '{0}'. Add '<PackageReference Include=\"{0}\" />' to your .csproj file.";
	private static readonly LocalizableString Description = "Projects should include required standard packages for SourceLink, polyfills, and framework compatibility.";

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

		// Get project properties

		options.TryGetValue("build_property.IsTestProject", out string? isTestProject);
		options.TryGetValue("build_property.TargetFramework", out string? targetFramework);
		options.TryGetValue("build_property.TargetFrameworkIdentifier", out string? targetFrameworkIdentifier);

		// Get package reference properties (passed from MSBuild)

		options.TryGetValue("build_property.HasSourceLinkGitHub", out string? hasSourceLinkGitHub);
		options.TryGetValue("build_property.HasSourceLinkAzureRepos", out string? hasSourceLinkAzureRepos);
		options.TryGetValue("build_property.HasPolyfill", out string? hasPolyfill);
		options.TryGetValue("build_property.HasSystemMemory", out string? hasSystemMemory);
		options.TryGetValue("build_property.HasSystemThreadingTasksExtensions", out string? hasSystemThreadingTasksExtensions);

		// Check for Microsoft.SourceLink.GitHub (build-time-only package)

		CheckPackageProperty(context, "Microsoft.SourceLink.GitHub", hasSourceLinkGitHub);

		// Check for Microsoft.SourceLink.AzureRepos.Git (build-time-only package)

		CheckPackageProperty(context, "Microsoft.SourceLink.AzureRepos.Git", hasSourceLinkAzureRepos);

		// Check for Polyfill (build-time-only package, non-test projects only)

		if (isTestProject != "true")
		{
			CheckPackageProperty(context, "Polyfill", hasPolyfill);
		}

		// Check for System.Memory (conditional on target framework)

		if (RequiresSystemMemory(targetFramework, targetFrameworkIdentifier))
		{
			CheckPackageProperty(context, "System.Memory", hasSystemMemory);
		}

		// Check for System.Threading.Tasks.Extensions (conditional on target framework)

		if (RequiresTaskExtensions(targetFramework, targetFrameworkIdentifier))
		{
			CheckPackageProperty(context, "System.Threading.Tasks.Extensions", hasSystemThreadingTasksExtensions);
		}
	}

	private static void CheckPackageProperty(CompilationAnalysisContext context, string packageName, string? hasPackageProperty)
	{
		// Check if package reference exists via MSBuild property
		// Build-time-only packages (with PrivateAssets=all) don't appear in compilation references,
		// so we need to check MSBuild properties passed via CompilerVisibleProperty

		bool hasPackage = hasPackageProperty == "true";

		if (!hasPackage)
		{
			Location location = context.Compilation.SyntaxTrees.FirstOrDefault()?.GetRoot().GetLocation() ?? Location.None;

			Diagnostic diagnostic = Diagnostic.Create(
				Rule,
				location,
				packageName);

			context.ReportDiagnostic(diagnostic);
		}
	}

	private static bool RequiresSystemMemory(string? targetFramework, string? targetFrameworkIdentifier)
	{
		if (string.IsNullOrEmpty(targetFramework) || string.IsNullOrEmpty(targetFrameworkIdentifier))
		{
			return false;
		}

		// Condition: $(TargetFrameworkIdentifier) == '.NETStandard' or
		//            $(TargetFrameworkIdentifier) == '.NETFramework' or
		//            $(TargetFramework.StartsWith('netcoreapp2'))

		return targetFrameworkIdentifier == ".NETStandard" ||
			   targetFrameworkIdentifier == ".NETFramework" ||
			   targetFramework!.StartsWith("netcoreapp2", System.StringComparison.Ordinal);
	}

	private static bool RequiresTaskExtensions(string? targetFramework, string? targetFrameworkIdentifier)
	{
		if (string.IsNullOrEmpty(targetFramework))
		{
			return false;
		}

		// Condition: $(TargetFramework) == 'netstandard2.0' or
		//            $(TargetFramework) == 'netcoreapp2.0' or
		//            $(TargetFrameworkIdentifier) == '.NETFramework'

		return targetFramework == "netstandard2.0" ||
			   targetFramework == "netcoreapp2.0" ||
			   targetFrameworkIdentifier == ".NETFramework";
	}
}
