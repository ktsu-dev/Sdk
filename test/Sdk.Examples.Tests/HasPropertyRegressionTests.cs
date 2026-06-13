namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Regression coverage for ktsu-dev/Sdk#12 (follow-up to #8 / #11): the
/// <c>SetPackageReferenceProperties</c> target must populate the <c>build_property.Has*</c>
/// values before the editorconfig snapshot the analyzers read, otherwise KTSU0001 produces
/// false positives for projects that do reference the standard packages.
/// </summary>
[TestClass]
public sealed class HasPropertyRegressionTests
{
    /// <summary>
    /// A project that references all three standard packages must build clean (no KTSU0001) and
    /// expose <c>HasSourceLinkGitHub</c>/<c>HasSourceLinkAzureRepos</c>/<c>HasPolyfill</c> = true
    /// in the generated editorconfig.
    /// </summary>
    [TestMethod]
    public void StandardPackages_PopulateHasProperties_AndDoNotFalseTriggerKtsu0001()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));

        CliResult result = workspace.Build("Library/Library.csproj");

        Assert.IsTrue(result.Succeeded, $"Project referencing the standard packages should build clean.{Environment.NewLine}{result.Output}");
        CollectionAssert.DoesNotContain(result.KtsuDiagnostics().ToList(), "KTSU0001", "KTSU0001 should not fire when the standard packages are referenced.");

        string editorConfig = workspace.ReadGeneratedEditorConfig("Library", "Library");
        StringAssert.Contains(editorConfig, "build_property.HasSourceLinkGitHub = true", "HasSourceLinkGitHub should be populated and true.");
        StringAssert.Contains(editorConfig, "build_property.HasSourceLinkAzureRepos = true", "HasSourceLinkAzureRepos should be populated and true.");
        StringAssert.Contains(editorConfig, "build_property.HasPolyfill = true", "HasPolyfill should be populated and true.");
    }
}
