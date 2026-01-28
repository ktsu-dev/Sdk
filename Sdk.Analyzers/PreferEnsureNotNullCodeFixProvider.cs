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
/// Code fix provider that replaces ArgumentNullException.ThrowIfNull with Ensure.NotNull
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferEnsureNotNullCodeFixProvider))]
[Shared]
public class PreferEnsureNotNullCodeFixProvider : CodeFixProvider
{

	/// <inheritdoc/>
	public override ImmutableArray<string> FixableDiagnosticIds => [PreferEnsureNotNullAnalyzer.DiagnosticId];

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

		// Find the invocation expression
		InvocationExpressionSyntax? invocation = root.FindToken(diagnosticSpan.Start)
			.Parent?
			.AncestorsAndSelf()
			.OfType<InvocationExpressionSyntax>()
			.FirstOrDefault();

		if (invocation is null)
		{
			return;
		}

		context.RegisterCodeFix(
			CodeAction.Create(
				title: "Replace with Ensure.NotNull",
				createChangedDocument: ct => ReplaceWithEnsureNotNullAsync(context.Document, invocation, ct),
				equivalenceKey: nameof(PreferEnsureNotNullCodeFixProvider)),
			diagnostic);
	}

	private static async Task<Document> ReplaceWithEnsureNotNullAsync(
		Document document,
		InvocationExpressionSyntax invocation,
		CancellationToken cancellationToken)
	{
		SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
		{
			return document;
		}

		// Get the arguments from the original invocation
		ArgumentListSyntax originalArgs = invocation.ArgumentList;

		// Create the new invocation: Ensure.NotNull(...)
		MemberAccessExpressionSyntax ensureNotNull = SyntaxFactory.MemberAccessExpression(
			SyntaxKind.SimpleMemberAccessExpression,
			SyntaxFactory.IdentifierName("Ensure"),
			SyntaxFactory.IdentifierName("NotNull"));

		InvocationExpressionSyntax newInvocation = SyntaxFactory.InvocationExpression(ensureNotNull, originalArgs)
			.WithLeadingTrivia(invocation.GetLeadingTrivia())
			.WithTrailingTrivia(invocation.GetTrailingTrivia());

		// Replace the old invocation with the new one
		SyntaxNode newRoot = root.ReplaceNode(invocation, newInvocation);

		return document.WithSyntaxRoot(newRoot);
	}
}
