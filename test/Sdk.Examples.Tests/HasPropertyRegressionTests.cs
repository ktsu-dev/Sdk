namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Regression coverage for ktsu-dev/Sdk#12 (follow-up to #8 / #11): the
/// <c>SetPackageReferenceProperties</c> target must make the <c>build_property.Has*</c> values
/// available to the analyzers, otherwise KTSU0001 produces false positives for projects that do
/// reference the standard packages.
/// </summary>
/// <remarks>
/// The user-visible symptom of #8 is a false KTSU0001 on a correctly-configured project, so the
/// assertion here is behavioral: a project that references all three standard packages must build
/// clean with no KTSU0001. (Inspecting the on-disk generated editorconfig is unreliable — the
/// snapshot written to obj does not always reflect the values the compiler is handed, so the
/// behavioral check is the meaningful one. The negative case — KTSU0001 firing when the packages
/// are absent — is covered by <see cref="AnalyzerTriggerTests"/>.)
/// </remarks>
[TestClass]
public sealed class HasPropertyRegressionTests
{
    [TestMethod]
    public void StandardPackages_DoNotFalseTriggerKtsu0001()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));

        CliResult result = workspace.Build("Library/Library.csproj");

        Assert.IsTrue(result.Succeeded, $"A project referencing the standard packages should build clean.{Environment.NewLine}{result.Output}");
        CollectionAssert.DoesNotContain(
            result.KtsuDiagnostics().ToList(),
            "KTSU0001",
            $"KTSU0001 must not fire when the standard packages are referenced.{Environment.NewLine}{result.Output}");
    }
}
