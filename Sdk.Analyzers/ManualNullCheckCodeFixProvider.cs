// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Code fix provider that replaces manual null checks with Ensure.NotNull.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ManualNullCheckCodeFixProvider))]
[Shared]
public class ManualNullCheckCodeFixProvider : CodeFixProvider
{
	/// <inheritdoc/>
	public override ImmutableArray<string> FixableDiagnosticIds => [ManualNullCheckAnalyzer.DiagnosticId];

	/// <inheritdoc/>
	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	/// <inheritdoc/>
	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
		if (root is null)
		{
			return;
		}

		Diagnostic diagnostic = context.Diagnostics.First();
		Microsoft.CodeAnalysis.Text.TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

		// Get the argument name from the diagnostic properties
		if (!diagnostic.Properties.TryGetValue(ManualNullCheckAnalyzer.ArgumentNameProperty, out string? argumentName) ||
			string.IsNullOrEmpty(argumentName))
		{
			return;
		}

		// Find the syntax node at the diagnostic location
		SyntaxNode? node = root.FindNode(diagnosticSpan);
		if (node is null)
		{
			return;
		}

		// Handle if statement
		if (node is IfStatementSyntax ifStatement)
		{
			context.RegisterCodeFix(
				CodeAction.Create(
					title: "Replace with Ensure.NotNull",
					createChangedDocument: ct => ReplaceIfStatementWithEnsureNotNullAsync(context.Document, ifStatement, argumentName!, ct),
					equivalenceKey: nameof(ManualNullCheckCodeFixProvider)),
				diagnostic);
			return;
		}

		// Handle coalesce expression
		BinaryExpressionSyntax? coalesceExpr = node.AncestorsAndSelf()
			.OfType<BinaryExpressionSyntax>()
			.FirstOrDefault(b => b.IsKind(SyntaxKind.CoalesceExpression));

		if (coalesceExpr is not null)
		{
			context.RegisterCodeFix(
				CodeAction.Create(
					title: "Replace with Ensure.NotNull",
					createChangedDocument: ct => ReplaceCoalesceWithEnsureNotNullAsync(context.Document, coalesceExpr, argumentName!, ct),
					equivalenceKey: nameof(ManualNullCheckCodeFixProvider)),
				diagnostic);
		}
	}

	private static async Task<Document> ReplaceIfStatementWithEnsureNotNullAsync(
		Document document,
		IfStatementSyntax ifStatement,
		string argumentName,
		CancellationToken cancellationToken)
	{
		SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
		{
			return document;
		}

		// Create: Ensure.NotNull(argumentName);
		ExpressionStatementSyntax ensureNotNullStatement = CreateEnsureNotNullStatement(argumentName)
			.WithLeadingTrivia(ifStatement.GetLeadingTrivia())
			.WithTrailingTrivia(ifStatement.GetTrailingTrivia());

		SyntaxNode newRoot = root.ReplaceNode(ifStatement, ensureNotNullStatement);

		return document.WithSyntaxRoot(newRoot);
	}

	private static async Task<Document> ReplaceCoalesceWithEnsureNotNullAsync(
		Document document,
		BinaryExpressionSyntax coalesceExpr,
		string argumentName,
		CancellationToken cancellationToken)
	{
		SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
		{
			return document;
		}

		// Determine what the coalesce expression is part of
		SyntaxNode? parent = coalesceExpr.Parent;

		// If it's a standalone expression statement like: _ = x ?? throw new ...;
		// Replace the whole statement with Ensure.NotNull(x);
		if (parent is AssignmentExpressionSyntax assignment &&
			assignment.Left is IdentifierNameSyntax identifier &&
			identifier.Identifier.Text == "_" &&
			assignment.Parent is ExpressionStatementSyntax expressionStatement)
		{
			ExpressionStatementSyntax ensureNotNullStatement = CreateEnsureNotNullStatement(argumentName)
				.WithLeadingTrivia(expressionStatement.GetLeadingTrivia())
				.WithTrailingTrivia(expressionStatement.GetTrailingTrivia());

			SyntaxNode newRoot = root.ReplaceNode(expressionStatement, ensureNotNullStatement);
			return document.WithSyntaxRoot(newRoot);
		}

		// For other cases (e.g., assignment to a variable), replace with just Ensure.NotNull(x)
		// Note: This changes semantics slightly as Ensure.NotNull returns the value
		InvocationExpressionSyntax ensureNotNullInvocation = CreateEnsureNotNullInvocation(argumentName)
			.WithLeadingTrivia(coalesceExpr.GetLeadingTrivia())
			.WithTrailingTrivia(coalesceExpr.GetTrailingTrivia());

		SyntaxNode newRootGeneral = root.ReplaceNode(coalesceExpr, ensureNotNullInvocation);
		return document.WithSyntaxRoot(newRootGeneral);
	}

	private static ExpressionStatementSyntax CreateEnsureNotNullStatement(string argumentName)
	{
		InvocationExpressionSyntax invocation = CreateEnsureNotNullInvocation(argumentName);
		return SyntaxFactory.ExpressionStatement(invocation);
	}

	private static InvocationExpressionSyntax CreateEnsureNotNullInvocation(string argumentName)
	{
		MemberAccessExpressionSyntax ensureNotNull = SyntaxFactory.MemberAccessExpression(
			SyntaxKind.SimpleMemberAccessExpression,
			SyntaxFactory.IdentifierName("Ensure"),
			SyntaxFactory.IdentifierName("NotNull"));

		ArgumentSyntax argument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(argumentName));
		ArgumentListSyntax argumentList = SyntaxFactory.ArgumentList(
			SyntaxFactory.SingletonSeparatedList(argument));

		return SyntaxFactory.InvocationExpression(ensureNotNull, argumentList);
	}
}
