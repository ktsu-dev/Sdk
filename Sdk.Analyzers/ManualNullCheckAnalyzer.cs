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
/// Analyzer that suggests using Ensure.NotNull instead of manual null checks with ArgumentNullException.
/// Detects patterns like:
/// - if (x == null) throw new ArgumentNullException(...)
/// - if (x is null) throw new ArgumentNullException(...)
/// - x ?? throw new ArgumentNullException(...)
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ManualNullCheckAnalyzer : KtsuAnalyzerBase
{
	/// <summary>
	/// Diagnostic ID for this analyzer.
	/// </summary>
	public const string DiagnosticId = "KTSU0004";

	/// <summary>
	/// Property key for the argument name to be used in the code fix.
	/// </summary>
	public const string ArgumentNameProperty = "ArgumentName";

	private static readonly LocalizableString Title = "Use Ensure.NotNull instead of manual null check";
	private static readonly LocalizableString MessageFormat = "Use 'Ensure.NotNull({0})' instead of manual null check with ArgumentNullException";
	private static readonly LocalizableString Description = "Manual null checks with ArgumentNullException should be replaced with Ensure.NotNull from the Polyfill package for consistency and better framework compatibility.";

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

		// Register for if statements to detect: if (x == null) throw new ArgumentNullException(...)
		context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);

		// Register for coalesce expressions to detect: x ?? throw new ArgumentNullException(...)
		context.RegisterSyntaxNodeAction(AnalyzeCoalesceExpression, SyntaxKind.CoalesceExpression);
	}

	private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not IfStatementSyntax ifStatement)
		{
			return;
		}

		// Check if the condition is a null check
		string? argumentName = GetNullCheckArgumentName(ifStatement.Condition);
		if (argumentName is null)
		{
			return;
		}

		// Check if the statement throws ArgumentNullException
		if (!ThrowsArgumentNullException(ifStatement.Statement, context.SemanticModel))
		{
			return;
		}

		ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArgumentNameProperty, argumentName);

		Diagnostic diagnostic = Diagnostic.Create(
			Rule,
			ifStatement.GetLocation(),
			properties,
			argumentName);

		context.ReportDiagnostic(diagnostic);
	}

	private static void AnalyzeCoalesceExpression(SyntaxNodeAnalysisContext context)
	{
		if (context.Node is not BinaryExpressionSyntax coalesceExpr)
		{
			return;
		}

		// Check if the right side is a throw expression with ArgumentNullException
		if (coalesceExpr.Right is not ThrowExpressionSyntax throwExpr)
		{
			return;
		}

		if (!IsArgumentNullExceptionCreation(throwExpr.Expression, context.SemanticModel))
		{
			return;
		}

		// Get the argument name from the left side
		string argumentName = coalesceExpr.Left.ToString();

		ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArgumentNameProperty, argumentName);

		Diagnostic diagnostic = Diagnostic.Create(
			Rule,
			coalesceExpr.GetLocation(),
			properties,
			argumentName);

		context.ReportDiagnostic(diagnostic);
	}

	private static string? GetNullCheckArgumentName(ExpressionSyntax condition)
	{
		// Handle: x == null or null == x
		if (condition is BinaryExpressionSyntax binaryExpr)
		{
			if (binaryExpr.IsKind(SyntaxKind.EqualsExpression))
			{
				if (binaryExpr.Right.IsKind(SyntaxKind.NullLiteralExpression))
				{
					return binaryExpr.Left.ToString();
				}

				if (binaryExpr.Left.IsKind(SyntaxKind.NullLiteralExpression))
				{
					return binaryExpr.Right.ToString();
				}
			}
		}

		// Handle: x is null
		if (condition is IsPatternExpressionSyntax isPatternExpr)
		{
			if (isPatternExpr.Pattern is ConstantPatternSyntax constantPattern &&
				constantPattern.Expression.IsKind(SyntaxKind.NullLiteralExpression))
			{
				return isPatternExpr.Expression.ToString();
			}
		}

		return null;
	}

	private static bool ThrowsArgumentNullException(StatementSyntax statement, SemanticModel semanticModel)
	{
		// Handle direct throw statement
		if (statement is ThrowStatementSyntax throwStatement)
		{
			return IsArgumentNullExceptionCreation(throwStatement.Expression, semanticModel);
		}

		// Handle block with single throw statement
		if (statement is BlockSyntax block && block.Statements.Count == 1)
		{
			if (block.Statements[0] is ThrowStatementSyntax blockThrowStatement)
			{
				return IsArgumentNullExceptionCreation(blockThrowStatement.Expression, semanticModel);
			}
		}

		return false;
	}

	private static bool IsArgumentNullExceptionCreation(ExpressionSyntax? expression, SemanticModel semanticModel)
	{
		if (expression is null)
		{
			return false;
		}

		// Check if this is a new ArgumentNullException(...)
		if (expression is ObjectCreationExpressionSyntax objectCreation)
		{
			ISymbol? symbol = semanticModel.GetSymbolInfo(objectCreation.Type).Symbol;
			if (symbol is INamedTypeSymbol typeSymbol)
			{
				return typeSymbol.ToDisplayString() == "System.ArgumentNullException";
			}
		}

		// Handle implicit object creation: throw new("paramName")
		if (expression is ImplicitObjectCreationExpressionSyntax implicitCreation)
		{
			TypeInfo typeInfo = semanticModel.GetTypeInfo(implicitCreation);
			if (typeInfo.Type is INamedTypeSymbol typeSymbol)
			{
				return typeSymbol.ToDisplayString() == "System.ArgumentNullException";
			}
		}

		return false;
	}
}
