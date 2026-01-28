// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Analyzer that suggests using Ensure.NotNull instead of ArgumentNullException.ThrowIfNull
/// for better framework compatibility
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PreferEnsureNotNullAnalyzer : KtsuAnalyzerBase
{

	/// <summary>
	/// Diagnostic ID for this analyzer
	/// </summary>
	public const string DiagnosticId = "KTSU0003";

	private static readonly LocalizableString Title = "Use Ensure.NotNull over ArgumentNullException.ThrowIfNull";
	private static readonly LocalizableString MessageFormat = "Use 'Ensure.NotNull({0})' instead of 'ArgumentNullException.ThrowIfNull({0})' for better framework compatibility";
	private static readonly LocalizableString Description = "ArgumentNullException.ThrowIfNull was introduced in .NET 6. Use Ensure.NotNull from the Polyfill package instead to maintain compatibility with older target frameworks.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		Title,
		MessageFormat,
		Category,
		DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: Description);

	/// <inheritdoc/>
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	/// <inheritdoc/>
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
	}

	private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not InvocationExpressionSyntax invocation)
		{
			return;
		}

		// Check if this is a call to ArgumentNullException.ThrowIfNull
		if (!IsArgumentNullExceptionThrowIfNull(invocation, context.SemanticModel))
		{
			return;
		}

		// Get the argument name for the diagnostic message
		string argumentName = GetFirstArgumentName(invocation);

		Diagnostic diagnostic = Diagnostic.Create(
			Rule,
			invocation.GetLocation(),
			argumentName);

		context.ReportDiagnostic(diagnostic);
	}

	private static bool IsArgumentNullExceptionThrowIfNull(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
	{
		// Check if the expression is a member access (e.g., ArgumentNullException.ThrowIfNull)
		if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
		{
			return false;
		}

		// Check if the method name is ThrowIfNull
		if (memberAccess.Name.Identifier.Text != "ThrowIfNull")
		{
			return false;
		}

		// Get the symbol for the invocation to verify it's actually ArgumentNullException.ThrowIfNull
		if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
		{
			return false;
		}

		// Verify the containing type is System.ArgumentNullException
		if (methodSymbol.ContainingType?.ToDisplayString() != "System.ArgumentNullException")
		{
			return false;
		}

		return true;
	}

	private static string GetFirstArgumentName(InvocationExpressionSyntax invocation)
	{
		if (invocation.ArgumentList.Arguments.Count > 0)
		{
			ArgumentSyntax firstArg = invocation.ArgumentList.Arguments[0];
			return firstArg.Expression.ToString();
		}

		return "argument";
	}
}
