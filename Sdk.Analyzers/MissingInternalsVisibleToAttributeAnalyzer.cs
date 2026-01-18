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
					return assemblyName == testNamespace;
				}
			}
			return false;
		});

		if (!hasTestReference)
		{
			// Report diagnostic at the first syntax tree location (project-level diagnostic)

			Location location = context.Compilation.SyntaxTrees.FirstOrDefault()?.GetRoot().GetLocation() ?? Location.None;

			Diagnostic diagnostic = Diagnostic.Create(
				Rule,
				location,
				testNamespace);

			context.ReportDiagnostic(diagnostic);
		}
	}
}
