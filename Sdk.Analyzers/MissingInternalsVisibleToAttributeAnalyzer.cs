// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer that suggests exposing internals to test projects via InternalsVisibleToAttribute
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingInternalsVisibleToAttributeAnalyzer : KtsuAnalyzerBase
{

	/// <summary>
	/// Diagnostic ID for this analyzer
	/// </summary>
	public const string DiagnosticId = "KTSU0002";

	private static readonly LocalizableString Title = "Missing InternalsVisibleTo attribute for test project";
	private static readonly LocalizableString MessageFormat = "Consider exposing internals to test project '{0}'. Add '[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"{0}\")]' to a .cs file.";
	private static readonly LocalizableString Description = "Projects should expose their internal members to test projects using the InternalsVisibleToAttribute for comprehensive testing.";

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

		// Check if this is a non-test project

		if (!options.TryGetValue("build_property.IsTestProject", out string? isTest) || isTest != "false")
		{
			return;
		}

		// Check if a test project exists

		if (!options.TryGetValue("build_property.TestProjectExists", out string? exists) || exists != "true")
		{
			return;
		}

		// Get test project namespace

		if (!options.TryGetValue("build_property.TestProjectNamespace", out string? testNamespace) || string.IsNullOrWhiteSpace(testNamespace))
		{
			return;
		}

		// Check if InternalsVisibleToAttribute already exists for the test project

		IEnumerable<AttributeData> internalsVisibleToAttributes = context.Compilation.Assembly
			.GetAttributes()
			.Where(attr => attr.AttributeClass?.Name == "InternalsVisibleToAttribute" &&
						   attr.AttributeClass.ContainingNamespace?.ToDisplayString() == "System.Runtime.CompilerServices");

		bool hasTestReference = internalsVisibleToAttributes.Any(attr =>
		{
			if (attr.ConstructorArguments.Length > 0)
			{
				TypedConstant firstArg = attr.ConstructorArguments[0];
				if (firstArg.Kind == TypedConstantKind.Primitive && firstArg.Value is string assemblyName)
				{
					// Handle both non-strong-named ("MyTest") and strong-named ("MyTest, PublicKey=...") assemblies
					return assemblyName == testNamespace ||
						   assemblyName.StartsWith(testNamespace + ",", System.StringComparison.Ordinal);
				}
			}
			return false;
		});

		if (!hasTestReference)
		{
			Diagnostic diagnostic = Diagnostic.Create(
				Rule,
				FindProjectSourceLocation(context, options),
				testNamespace);

			context.ReportDiagnostic(diagnostic);
		}
	}

	/// <summary>
	/// Picks a syntax tree the project itself owns, to carry this project-level diagnostic.
	/// </summary>
	/// <param name="context">The compilation analysis context.</param>
	/// <param name="options">The global analyzer config options.</param>
	/// <returns>A location in a project-owned file, or <see cref="Location.None"/> when there is none.</returns>
	/// <remarks>
	/// Simply taking the first syntax tree is wrong. Package compile items are prepended to the
	/// compilation, so with a source-embedding package such as Polyfill the first tree is a file from
	/// that package, carrying an <c>auto-generated</c> header. Diagnostics located in generated code
	/// are dropped under <see cref="GeneratedCodeAnalysisFlags.None"/>, which is what made this rule
	/// appear to fire only intermittently (ktsu-dev/Sdk#12 / #8 / #11). Restricting the choice to
	/// files under the project directory, excluding the intermediate output, also guarantees the code
	/// fix edits a file the user actually owns rather than one in the NuGet cache.
	/// </remarks>
	private static Location FindProjectSourceLocation(CompilationAnalysisContext context, AnalyzerConfigOptions options)
	{
		options.TryGetValue("build_property.ProjectDir", out string? projectDir);

		SyntaxTree? tree = null;

		if (!string.IsNullOrEmpty(projectDir))
		{
			tree = context.Compilation.SyntaxTrees.FirstOrDefault(t => IsProjectOwned(t.FilePath, projectDir!));
		}

		tree ??= context.Compilation.SyntaxTrees.FirstOrDefault();

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
			!filePath.StartsWith(projectDir, System.StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string relative = filePath.Substring(projectDir.Length);

		return !relative.StartsWith("obj" + System.IO.Path.DirectorySeparatorChar, System.StringComparison.OrdinalIgnoreCase)
			&& !relative.StartsWith("obj/", System.StringComparison.OrdinalIgnoreCase);
	}
}
