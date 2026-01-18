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
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Code fix provider that adds InternalsVisibleToAttribute to expose internals to test projects
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddInternalsVisibleToAttributeCodeFixProvider))]
[Shared]
public class AddInternalsVisibleToAttributeCodeFixProvider : CodeFixProvider
{

	/// <inheritdoc/>
	public override ImmutableArray<string> FixableDiagnosticIds => [MissingInternalsVisibleToAttributeAnalyzer.DiagnosticId];

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

		context.RegisterCodeFix(
			CodeAction.Create(
				title: "Add [assembly: InternalsVisibleTo(...)]",
				createChangedDocument: ct => AddInternalsVisibleToAttributeAsync(context.Document, diagnostic, ct),
				equivalenceKey: nameof(AddInternalsVisibleToAttributeCodeFixProvider)),
			diagnostic);
	}

	private static async Task<Document> AddInternalsVisibleToAttributeAsync(
		Document document,
		Diagnostic diagnostic,
		CancellationToken cancellationToken)
	{
		SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is not CompilationUnitSyntax compilationUnit)
		{
			return document;
		}

		// Get test project namespace from analyzer config options

		AnalyzerConfigOptions options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions;
		if (!options.TryGetValue("build_property.TestProjectNamespace", out string? testNamespace) || string.IsNullOrWhiteSpace(testNamespace))
		{
			return document;
		}

		// Check if using directive already exists

		bool hasUsing = compilationUnit.Usings.Any(u =>
			u.Name?.ToString() == "System.Runtime.CompilerServices");

		// Create the using directive if needed

		SyntaxList<UsingDirectiveSyntax> newUsings = compilationUnit.Usings;
		if (!hasUsing)
		{
			UsingDirectiveSyntax usingDirective = SyntaxFactory.UsingDirective(
				SyntaxFactory.ParseName("System.Runtime.CompilerServices"))
				.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
			newUsings = newUsings.Add(usingDirective);
		}

		// Create the InternalsVisibleTo attribute

		AttributeArgumentSyntax attributeArgument = SyntaxFactory.AttributeArgument(
			SyntaxFactory.LiteralExpression(
				SyntaxKind.StringLiteralExpression,
				SyntaxFactory.Literal(testNamespace)));

		AttributeSyntax attribute = SyntaxFactory.Attribute(
			SyntaxFactory.ParseName("assembly: System.Runtime.CompilerServices.InternalsVisibleTo"),
			SyntaxFactory.AttributeArgumentList(
				SyntaxFactory.SingletonSeparatedList(attributeArgument)));

		AttributeListSyntax attributeList = SyntaxFactory.AttributeList(
			SyntaxFactory.SingletonSeparatedList(attribute))
			.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

		// Add the attribute to the compilation unit

		CompilationUnitSyntax newCompilationUnit = compilationUnit
			.WithUsings(newUsings)
			.AddAttributeLists(attributeList)
			.WithLeadingTrivia(compilationUnit.GetLeadingTrivia())
			.WithTrailingTrivia(compilationUnit.GetTrailingTrivia());

		return document.WithSyntaxRoot(newCompilationUnit);
	}
}
