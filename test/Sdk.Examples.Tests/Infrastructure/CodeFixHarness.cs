namespace Sdk.Examples.Tests.Infrastructure;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Runs an analyzer and its code fix in-process against one source file, then compiles the result.
/// </summary>
/// <remarks>
/// The example builds in <c>AnalyzerTriggerTests</c> show that a diagnostic fires, but nothing there
/// applies a fix. This harness does, and reports whether the fixed code still compiles. The source is
/// compiled with a stand-in for Polyfill's <c>Ensure</c> class, whose parameters are named as
/// Polyfill names them (<c>argument</c>, <c>name</c>).
/// </remarks>
internal static class CodeFixHarness
{
    internal const string EnsureStub = """
        namespace Polyfills
        {
            internal static class Ensure
            {
                public static T NotNull<T>(T? argument, [System.Runtime.CompilerServices.CallerArgumentExpression("argument")] string? name = null)
                    where T : class
                    => argument ?? throw new System.ArgumentNullException(name);
            }
        }
        """;

    private static readonly ImmutableArray<MetadataReference> References =
        [
            .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Where(path => Path.GetFileName(path).StartsWith("System", StringComparison.Ordinal)
                    || Path.GetFileName(path) is "mscorlib.dll" or "netstandard.dll")
                .Select(path => MetadataReference.CreateFromFile(path)),
        ];

    /// <summary>
    /// Applies the first fix offered for the first diagnostic, and returns the fixed source with the
    /// compiler errors it has.
    /// </summary>
    internal static async Task<(string Source, ImmutableArray<Diagnostic> Errors)> ApplyFixAsync(
        DiagnosticAnalyzer analyzer,
        CodeFixProvider codeFix,
        string source)
    {
        using AdhocWorkspace workspace = new();
        Document document = CreateDocument(workspace, source);
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(analyzer, document).ConfigureAwait(false);
        Diagnostic diagnostic = diagnostics.Single();

        List<CodeAction> actions = [];
        CodeFixContext context = new(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await codeFix.RegisterCodeFixesAsync(context).ConfigureAwait(false);

        ImmutableArray<CodeActionOperation> operations = await actions.First().GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        Solution solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        Document fixedDocument = solution.GetDocument(document.Id)!;

        string fixedSource = (await fixedDocument.GetTextAsync().ConfigureAwait(false)).ToString();
        Compilation compilation = (await fixedDocument.Project.GetCompilationAsync().ConfigureAwait(false))!;
        ImmutableArray<Diagnostic> errors = [.. compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)];
        return (fixedSource, errors);
    }

    private static Document CreateDocument(AdhocWorkspace workspace, string source)
    {
        Project project = workspace.AddProject("Subject", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable))
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.Latest))
            .AddMetadataReferences(References);
        project = project.AddDocument("Ensure.cs", EnsureStub).Project;
        return project.AddDocument("Subject.cs", source);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(DiagnosticAnalyzer analyzer, Document document)
    {
        Compilation compilation = (await document.Project.GetCompilationAsync().ConfigureAwait(false))!;
        ImmutableArray<Diagnostic> compileErrors = [.. compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)];
        if (!compileErrors.IsEmpty)
        {
            throw new InvalidOperationException("The source under test does not compile: " + string.Join("; ", compileErrors));
        }

        return await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }
}
