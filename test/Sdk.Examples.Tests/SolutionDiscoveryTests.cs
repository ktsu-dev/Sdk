namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Guards the choice of solution when the solution directory holds several solution files.
/// <c>Directory.GetFiles</c> guarantees no order, so taking its first entry made
/// <c>SolutionName</c>, and the project-type detection, <c>ToolCommandName</c> and package identity
/// keyed on it, depend on the file system. See ktsu-dev/Sdk#92.
/// </summary>
[TestClass]
public sealed class SolutionDiscoveryTests
{
    private const string ToolProject = """
        <Project Sdk="Microsoft.NET.Sdk">
          <Sdk Name="ktsu.Sdk" />
          <Sdk Name="ktsu.Sdk.Tool" />
          <ItemGroup>
            <PackageReference Include="Polyfill" PrivateAssets="all" />
          </ItemGroup>
        </Project>
        """;

    /// <summary>
    /// The solution named after its directory wins over the others, whatever order they were
    /// created in: the issue's repro, where ext4 listing order picked <c>Demo.Samples</c> and the
    /// tool project stopped being detected as one.
    /// </summary>
    [TestMethod]
    public void SeveralSolutions_PreferTheOneNamedAfterTheDirectory()
    {
        string[][] creationOrders =
        [
            ["Aaa.sln", "Demo.Samples.sln", "Demo.sln"],
            ["Demo.Samples.sln", "Demo.sln", "Aaa.sln"],
            ["Demo.sln", "Aaa.sln", "Demo.Samples.sln"],
        ];

        foreach (string[] order in creationOrders)
        {
            using ExampleWorkspace workspace = CreateWithSolutions("Demo", order);

            IReadOnlyDictionary<string, string> props = workspace.Evaluate(
                "Demo/Demo.Tool/Demo.Tool.csproj",
                "SolutionName", "IsToolProject", "ToolCommandName", "_KtsuAmbiguousSolution");

            string label = string.Join(", ", order);
            Assert.AreEqual("Demo", props["SolutionName"], $"SolutionName ({label})");
            Assert.AreEqual("true", props["IsToolProject"], $"IsToolProject ({label})");
            Assert.AreEqual("demo", props["ToolCommandName"], $"ToolCommandName ({label})");
            Assert.AreEqual("false", props["_KtsuAmbiguousSolution"], $"_KtsuAmbiguousSolution ({label})");
        }
    }

    /// <summary>
    /// With no solution named after the directory, the ordinally first one is taken regardless of
    /// creation order, so the result is at least the same on every machine.
    /// </summary>
    [TestMethod]
    public void SeveralSolutions_NoneNamedAfterTheDirectory_TakeTheOrdinallyFirst()
    {
        string[][] creationOrders =
        [
            ["Zeta.sln", "Demo.sln", "Beta.sln", "Alpha.sln"],
            ["Alpha.sln", "Beta.sln", "Demo.sln", "Zeta.sln"],
            ["Demo.sln", "Zeta.sln", "Alpha.sln", "Beta.sln"],
        ];

        foreach (string[] order in creationOrders)
        {
            using ExampleWorkspace workspace = CreateWithSolutions("Repo", order);

            IReadOnlyDictionary<string, string> props = workspace.Evaluate(
                "Repo/Demo.Tool/Demo.Tool.csproj", "SolutionName", "_KtsuAmbiguousSolution");

            string label = string.Join(", ", order);
            Assert.AreEqual("Alpha", props["SolutionName"], $"SolutionName ({label})");
            Assert.AreEqual("true", props["_KtsuAmbiguousSolution"], $"_KtsuAmbiguousSolution ({label})");
        }
    }

    /// <summary>
    /// An ambiguous pick raises KTSU1006, which names the remedy.
    /// </summary>
    [TestMethod]
    public void SeveralSolutions_NoneNamedAfterTheDirectory_WarnKtsu1006()
    {
        using ExampleWorkspace workspace = CreateWithSolutions("Repo", ["Demo.sln", "Alpha.sln"]);

        CliResult result = Cli.Dotnet(
            workspace.Root, "msbuild", "Repo/Demo.Tool/Demo.Tool.csproj", "-t:_KtsuWarnAmbiguousSolution", "-nologo");

        Assert.IsTrue(result.Succeeded, result.Output);
        StringAssert.Contains(result.Output, "KTSU1006", result.Output);
        StringAssert.Contains(result.Output, "Set SolutionName in Directory.Build.props", result.Output);
    }

    /// <summary>
    /// A consumer who has already chosen with <c>SolutionName</c> is not warned.
    /// </summary>
    [TestMethod]
    public void SeveralSolutions_SolutionNameSet_DoNotWarn()
    {
        using ExampleWorkspace workspace = CreateWithSolutions("Repo", ["Demo.sln", "Alpha.sln"]);

        IReadOnlyDictionary<string, string> props = workspace.EvaluateWith(
            "Repo/Demo.Tool/Demo.Tool.csproj", ["-p:SolutionName=Demo"], "SolutionName", "_KtsuAmbiguousSolution");

        Assert.AreEqual("Demo", props["SolutionName"], "SolutionName");
        Assert.AreEqual("false", props["_KtsuAmbiguousSolution"], "_KtsuAmbiguousSolution");
    }

    /// <summary>
    /// A single solution is used as before, and is never ambiguous.
    /// </summary>
    [TestMethod]
    public void OneSolution_IsUsedWithoutWarning()
    {
        using ExampleWorkspace workspace = CreateWithSolutions("Repo", ["Demo.sln"]);

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            "Repo/Demo.Tool/Demo.Tool.csproj", "SolutionName", "IsToolProject", "_KtsuAmbiguousSolution");

        Assert.AreEqual("Demo", props["SolutionName"], "SolutionName");
        Assert.AreEqual("true", props["IsToolProject"], "IsToolProject");
        Assert.AreEqual("false", props["_KtsuAmbiguousSolution"], "_KtsuAmbiguousSolution");
    }

    /// <summary>
    /// Builds a solution directory named <paramref name="directory"/> inside the Tool demo's
    /// workspace, holding <c>Demo.Tool</c> and the given solution files, written in the order given.
    /// The workspace root keeps the demo's packages props, which the nested project inherits.
    /// </summary>
    private static ExampleWorkspace CreateWithSolutions(string directory, string[] solutionFiles)
    {
        ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Tool"));
        workspace.WriteFile($"{directory}/Demo.Tool/Demo.Tool.csproj", ToolProject);
        foreach (string solution in solutionFiles)
        {
            workspace.WriteFile($"{directory}/{solution}", string.Empty);
        }

        return workspace;
    }
}
