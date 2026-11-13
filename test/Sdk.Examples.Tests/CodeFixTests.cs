namespace Sdk.Examples.Tests;

using System.Collections.Immutable;
using ktsu.Sdk.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Applies the analyzers' code fixes in-process and checks that the fixed code compiles.
/// </summary>
[TestClass]
public sealed class CodeFixTests
{
    private const string Usings = "using Polyfills;\n";

    /// <summary>
    /// KTSU0003's fix copied ThrowIfNull's argument list verbatim, so a <c>paramName:</c> label
    /// survived into <c>Ensure.NotNull</c>, whose parameter is <c>name</c>: CS1739 (ktsu-dev/Sdk#112).
    /// </summary>
    [TestMethod]
    [DataRow(
        "System.ArgumentNullException.ThrowIfNull(value, paramName: nameof(value));",
        "Ensure.NotNull(value, nameof(value));",
        DisplayName = "paramName label")]
    [DataRow(
        "System.ArgumentNullException.ThrowIfNull(argument: value, paramName: nameof(value));",
        "Ensure.NotNull(value, nameof(value));",
        DisplayName = "both labels")]
    [DataRow(
        "System.ArgumentNullException.ThrowIfNull(paramName: nameof(value), argument: value);",
        "Ensure.NotNull(value, nameof(value));",
        DisplayName = "labels out of order")]
    [DataRow(
        "System.ArgumentNullException.ThrowIfNull(value, nameof(value));",
        "Ensure.NotNull(value, nameof(value));",
        DisplayName = "positional")]
    [DataRow(
        "System.ArgumentNullException.ThrowIfNull(value);",
        "Ensure.NotNull(value);",
        DisplayName = "argument only")]
    public async Task KTSU0003_Fix_Compiles(string call, string expected)
    {
        string source = Usings + "public static class C { public static void M(string value) { " + call + " } }\n";

        (string fixedSource, ImmutableArray<Diagnostic> errors) = await CodeFixHarness.ApplyFixAsync(
            new PreferEnsureNotNullAnalyzer(),
            new PreferEnsureNotNullCodeFixProvider(),
            source).ConfigureAwait(false);

        StringAssert.Contains(fixedSource, "{ " + expected + " }");
        Assert.AreEqual(0, errors.Length, string.Join("\n", errors));
    }
}
