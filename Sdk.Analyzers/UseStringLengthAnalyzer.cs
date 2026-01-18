using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ktsu.Sdk.Analyzers;

/// <summary>
/// Analyzer that suggests using string.Length instead of string.Count()
/// This is a sample analyzer to demonstrate the analyzer infrastructure.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseStringLengthAnalyzer : KtsuAnalyzerBase
{
	/// <summary>
	/// Diagnostic ID for this analyzer
	/// </summary>
	public const string DiagnosticId = "KTSU0001";

	private static readonly LocalizableString Title = "Use string.Length instead of Count()";
	private static readonly LocalizableString MessageFormat = "Consider using '{0}.Length' instead of '{0}.Count()' for better performance";
	private static readonly LocalizableString Description = "Using the Length property is more efficient than calling the Count() extension method on strings.";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		Title,
		MessageFormat,
		Category,
		DiagnosticSeverity.Info,
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
		var invocation = (InvocationExpressionSyntax)context.Node;

		// Check if this is a Count() method call
		if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
		{
			return;
		}

		if (memberAccess.Name.Identifier.ValueText != "Count")
		{
			return;
		}

		// Check if the target is a string
		var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess.Expression);
		if (symbolInfo.Symbol is not ILocalSymbol and not IParameterSymbol and not IPropertySymbol and not IFieldSymbol)
		{
			return;
		}

		var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
		if (typeInfo.Type?.SpecialType != SpecialType.System_String)
		{
			return;
		}

		// Check if this is the LINQ Count() extension method
		var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
		if (methodSymbol?.ContainingType.ToString() != "System.Linq.Enumerable")
		{
			return;
		}

		// Report diagnostic
		var diagnostic = Diagnostic.Create(
			Rule,
			invocation.GetLocation(),
			memberAccess.Expression.ToString());

		context.ReportDiagnostic(diagnostic);
	}
}
