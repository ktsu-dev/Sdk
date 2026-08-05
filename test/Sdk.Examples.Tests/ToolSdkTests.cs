namespace Sdk.Examples.Tests;

using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Verifies ktsu.Sdk.Tool packs a consuming project as a usable dotnet tool: the right MSBuild
/// shape, a derived command name, and an actual DotnetTool payload in the produced package.
/// </summary>
[TestClass]
public sealed class ToolSdkTests
{
    private const string Project = "Demo.Tool/Demo.Tool.csproj";

    /// <summary>The demo solution is named Demo, so the derived command is its lowercased name.</summary>
    private const string ExpectedCommand = "demo";

    /// <summary>
    /// The tool shape: a single framework, PackAsTool on, packable, and explicitly NOT publishable
    /// (the core SDK would otherwise mark any OutputType=Exe project publishable).
    /// </summary>
    [TestMethod]
    public void ToolSdk_ResolvesExpectedProperties()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Tool"));

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            Project,
            "TargetFramework", "TargetFrameworks", "OutputType", "PackAsTool",
            "ToolCommandName", "IsPackable", "IsPublishable", "IsToolProject");

        Assert.AreEqual("net10.0", props["TargetFramework"], "TargetFramework");
        Assert.AreEqual(string.Empty, props["TargetFrameworks"], "TargetFrameworks");
        Assert.AreEqual("Exe", props["OutputType"], "OutputType");
        Assert.AreEqual("true", props["PackAsTool"], "PackAsTool");
        Assert.AreEqual(ExpectedCommand, props["ToolCommandName"], "ToolCommandName");
        Assert.AreEqual("true", props["IsPackable"], "IsPackable");
        Assert.AreEqual("false", props["IsPublishable"], "IsPublishable");
        Assert.AreEqual("true", props["IsToolProject"], "IsToolProject");
    }

    /// <summary>
    /// The only assertion that proves the package actually works as a tool: property checks alone
    /// pass against a package with no tools/ payload at all.
    /// </summary>
    [TestMethod]
    public void ToolSdk_PacksDotnetToolPayload()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Tool"));

        (CliResult result, string outputDir) = workspace.Pack(Project);

        Assert.IsTrue(result.Succeeded, $"Expected the tool demo to pack successfully.{Environment.NewLine}{result.Output}");

        string nupkg = Directory.GetFiles(outputDir, "*.nupkg")
            .Single(f => !f.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));

        IReadOnlyList<string> entries = ExampleWorkspace.NupkgEntries(nupkg);

        const string settingsPath = "tools/net10.0/any/DotnetToolSettings.xml";
        CollectionAssert.Contains(
            entries.ToList(),
            settingsPath,
            $"Expected a DotnetTool payload.{Environment.NewLine}Entries:{Environment.NewLine}{string.Join(Environment.NewLine, entries)}");

        string settings = ExampleWorkspace.ReadNupkgEntry(nupkg, settingsPath);
        StringAssert.Contains(
            settings,
            $"Name=\"{ExpectedCommand}\"",
            $"Expected the derived command name in DotnetToolSettings.xml.{Environment.NewLine}{settings}");
    }

    /// <summary>
    /// Pins the contract that keeps tool projects out of CI's self-contained RID publish path.
    /// KtsuBuild's IsExecutableProject selects projects by scanning the csproj text for a literal
    /// OutputType or an .App/.Ios SDK reference, none of which a Tool SDK project carries. If a
    /// future change adds one, tools would start emitting per-RID zips alongside the package.
    /// </summary>
    [TestMethod]
    public void ToolDemo_IsNotDetectedAsAnExecutableProjectByCi()
    {
        string csproj = File.ReadAllText(Path.Combine(RepoLayout.Demo("Tool"), Project.Replace('/', Path.DirectorySeparatorChar)));

        Assert.IsFalse(
            Regex.IsMatch(csproj, @"<OutputType>\s*(Exe|WinExe)\s*</OutputType>", RegexOptions.IgnoreCase),
            "A literal OutputType in the csproj makes CI publish the tool as self-contained RID zips.");
        Assert.IsFalse(
            Regex.IsMatch(csproj, @"Sdk=""[^""]*\.(App|Ios)[/""]", RegexOptions.IgnoreCase),
            "An .App/.Ios SDK attribute in the csproj makes CI publish the tool as self-contained RID zips.");
    }
}
