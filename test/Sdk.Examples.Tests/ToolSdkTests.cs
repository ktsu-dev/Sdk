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
        Assert.AreEqual("true", props["IsToolProject"], "IsToolProject");

        // IsPublishable must stay true: PackAsTool builds the tools/ payload from a publish, and
        // Microsoft.NET.Sdk gates the Publish target on it. See ToolSdk_PacksDotnetToolPayload.
        Assert.AreEqual("true", props["IsPublishable"], "IsPublishable");
    }

    /// <summary>
    /// Proves the package actually works as a tool. Asserting only that DotnetToolSettings.xml
    /// exists is not enough: a package whose publish was suppressed still contains that file, but
    /// none of the assemblies it points at, so it installs and then fails at run time.
    /// </summary>
    /// <param name="noBuild">
    /// Whether to pack with <c>--no-build</c>, which is the path KtsuBuild's release pipeline uses.
    /// </param>
    [TestMethod]
    [DataRow(false, DisplayName = "dotnet pack")]
    [DataRow(true, DisplayName = "dotnet pack --no-build (the CI path)")]
    public void ToolSdk_PacksDotnetToolPayload(bool noBuild)
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Tool"));

        if (noBuild)
        {
            CliResult build = workspace.Build(Project);
            Assert.IsTrue(build.Succeeded, $"Expected the tool demo to build.{Environment.NewLine}{build.Output}");
        }

        (CliResult result, string outputDir) = workspace.Pack(Project, noBuild ? ["--no-build"] : []);

        Assert.IsTrue(result.Succeeded, $"Expected the tool demo to pack successfully.{Environment.NewLine}{result.Output}");

        string nupkg = Directory.GetFiles(outputDir, "*.nupkg")
            .Single(f => !f.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));

        List<string> entries = [.. ExampleWorkspace.NupkgEntries(nupkg)];
        string listing = string.Join(Environment.NewLine, entries);

        // The runnable payload, not just the manifest: the entry-point assembly and the
        // runtimeconfig the shim needs to launch it.
        const string toolsDir = "tools/net10.0/any/";
        foreach (string required in new[]
                 {
                     toolsDir + "DotnetToolSettings.xml",
                     toolsDir + "Demo.Tool.dll",
                     toolsDir + "Demo.Tool.runtimeconfig.json",
                 })
        {
            CollectionAssert.Contains(entries, required, $"Missing '{required}'.{Environment.NewLine}Entries:{Environment.NewLine}{listing}");
        }

        string settings = ExampleWorkspace.ReadNupkgEntry(nupkg, toolsDir + "DotnetToolSettings.xml");
        StringAssert.Contains(
            settings,
            $"Name=\"{ExpectedCommand}\"",
            $"Expected the derived command name in DotnetToolSettings.xml.{Environment.NewLine}{settings}");

        // The manifest's EntryPoint must actually be in the package - the exact pairing that a
        // suppressed publish breaks.
        StringAssert.Contains(
            settings,
            "EntryPoint=\"Demo.Tool.dll\"",
            $"Unexpected entry point.{Environment.NewLine}{settings}");

        // Install it and run it. Package contents can look right and still not work; this is the
        // only assertion that exercises what a user actually does.
        CliResult run = workspace.InstallAndRunTool(outputDir, "Demo.Tool", "1.0.0", ExpectedCommand);

        Assert.IsTrue(run.Succeeded, $"Expected `{ExpectedCommand}` to install and run.{Environment.NewLine}{run.Output}");
        StringAssert.Contains(
            run.Output,
            "Hello from Demo.Tool",
            $"Expected the tool to produce its output.{Environment.NewLine}{run.Output}");
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
