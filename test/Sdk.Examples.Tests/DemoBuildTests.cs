namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Smoke tests proving the buildable SDK demos compile end-to-end against the locally packed
/// ktsu.Sdk packages. Platform SDKs that require another OS or a workload (Windows, macOS, iOS,
/// Android) are covered by <see cref="PlatformSdkResolutionTests"/> via property evaluation.
/// </summary>
[TestClass]
public sealed class DemoBuildTests
{
    /// <summary>Each demo that fully builds on a Linux CI runner.</summary>
    /// <param name="demo">The demo directory under examples/demos.</param>
    /// <param name="project">The project to build, relative to the demo directory.</param>
    [TestMethod]
    [DataRow("Library", "Library/Library.csproj", DisplayName = "ktsu.Sdk (library)")]
    [DataRow("ConsoleApp", "ConsoleApp/ConsoleApp.csproj", DisplayName = "ktsu.Sdk.ConsoleApp")]
    [DataRow("App", "App/App.csproj", DisplayName = "ktsu.Sdk.App")]
    [DataRow("Linux", "Linux/Linux.csproj", DisplayName = "ktsu.Sdk.Linux")]
    public void Demo_Builds(string demo, string project)
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo(demo));

        CliResult result = workspace.Build(project);

        Assert.IsTrue(result.Succeeded, $"Expected '{demo}' to build successfully.{Environment.NewLine}{result.Output}");
    }

    /// <summary>The test-project demo: a library plus its MSTest project (test-project detection + InternalsVisibleTo).</summary>
    [TestMethod]
    public void TestProjectDemo_Builds()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Test"));

        // Building the test project also builds the referenced Calculator library.
        CliResult result = workspace.Build("Calculator.Test/Calculator.Test.csproj");

        Assert.IsTrue(result.Succeeded, $"Expected the test-project demo to build successfully.{Environment.NewLine}{result.Output}");
    }
}
