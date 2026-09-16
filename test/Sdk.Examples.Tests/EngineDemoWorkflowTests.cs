namespace Sdk.Examples.Tests;

using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Checks that the engine demos produce what their READMEs say they produce. The resolution
/// tests assert the properties; these assert the artifacts those properties are there to
/// arrange, which is the half that would still break if an engine changed its conventions.
/// Neither test needs the engine installed.
/// </summary>
[TestClass]
public sealed partial class EngineDemoWorkflowTests
{
    /// <summary>
    /// Godot resolves the game assembly by the name in <c>project.godot</c>, out of
    /// <c>.godot/mono/temp/bin/$(Configuration)/</c>. The name is read back from the demo's own
    /// project.godot rather than hardcoded, so the assertion is the engine's contract and not a
    /// restatement of the SDK's property.
    /// </summary>
    [TestMethod]
    public void GodotDemo_ProducesTheAssemblyProjectGodotAsksFor()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Godot"));

        CliResult result = workspace.Build("Demo.Godot/Demo.Godot.csproj");
        Assert.IsTrue(result.Succeeded, $"Expected the Godot demo to build.{Environment.NewLine}{result.Output}");

        string projectGodot = Path.Combine(workspace.Root, "Demo.Godot", "project.godot");
        Match match = AssemblyNameEntry().Match(File.ReadAllText(projectGodot));
        Assert.IsTrue(match.Success, $"'{projectGodot}' has no dotnet/project/assembly_name entry.");

        string assemblyName = match.Groups["name"].Value;
        string godotBin = Path.Combine(workspace.Root, "Demo.Godot", ".godot", "mono", "temp", "bin", "Release");

        Assert.IsTrue(
            File.Exists(Path.Combine(godotBin, assemblyName + ".dll")),
            $"Godot loads '{assemblyName}.dll' from its bin directory, but it is not there. " +
            $"Present: {Describe(godotBin)}");

        // EnableDynamicLoading, end to end: without the runtimeconfig.json Godot cannot spin up
        // the collectible load context the assembly is loaded into.
        Assert.IsTrue(
            File.Exists(Path.Combine(godotBin, assemblyName + ".runtimeconfig.json")),
            $"Expected a runtimeconfig.json alongside the assembly. Present: {Describe(godotBin)}");
    }

    /// <summary>
    /// Unity resolves nothing and builds nothing of yours outside <c>Assets/</c>: the plug-in
    /// gets there by being copied. The demo's DeployToUnityProject target is the copy, and this
    /// is the check that the two halves of the demo are actually wired to each other.
    /// </summary>
    [TestMethod]
    public void UnityDemo_DeploysThePluginIntoTheUnityProject()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Unity"));

        CliResult result = workspace.Build("Demo.Unity/Demo.Unity.csproj");
        Assert.IsTrue(result.Succeeded, $"Expected the Unity demo to build.{Environment.NewLine}{result.Output}");

        string plugins = Path.Combine(workspace.Root, "UnityProject", "Assets", "Plugins");

        Assert.IsTrue(
            File.Exists(Path.Combine(plugins, "Demo.Unity.dll")),
            $"Expected the plug-in in the Unity project's Assets/Plugins. Present: {Describe(plugins)}");
    }

    private static string Describe(string directory) => Directory.Exists(directory)
        ? string.Join(", ", Directory.GetFiles(directory).Select(Path.GetFileName))
        : $"'{directory}' does not exist";

    [GeneratedRegex(@"^project/assembly_name\s*=\s*""(?<name>[^""]+)""", RegexOptions.Multiline)]
    private static partial Regex AssemblyNameEntry();
}
