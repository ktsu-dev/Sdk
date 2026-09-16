namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Verifies the game-engine SDKs resolve the framework, output shape and project-type detection
/// flag a consuming project needs, and that each one puts back the core SDK default that would
/// otherwise break the engine's own conventions.
/// </summary>
[TestClass]
public sealed class EngineSdkResolutionTests
{
    /// <param name="demo">The demo directory under examples/demos.</param>
    /// <param name="project">The consuming project, relative to the demo directory.</param>
    /// <param name="framework">Expected TargetFramework.</param>
    /// <param name="flag">The project-type detection flag expected to be 'true'.</param>
    [TestMethod]
    [DataRow("Unity", "Demo.Unity/Demo.Unity.csproj", "netstandard2.1", "IsUnityProject", DisplayName = "ktsu.Sdk.Unity")]
    [DataRow("Godot", "Demo.Godot/Demo.Godot.csproj", TargetFrameworks.Latest, "IsGodotProject", DisplayName = "ktsu.Sdk.Godot")]
    public void EngineSdk_ResolvesExpectedProperties(string demo, string project, string framework, string flag)
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo(demo));

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            project, "TargetFramework", "TargetFrameworks", "OutputType", "RuntimeIdentifiers", flag);

        Assert.AreEqual(framework, props["TargetFramework"], "TargetFramework");
        Assert.AreEqual(string.Empty, props["TargetFrameworks"], "TargetFrameworks");
        Assert.AreEqual("Library", props["OutputType"], "OutputType");

        // Neither engine builds from a .NET runtime identifier: Unity builds each player itself,
        // and Godot's export pipeline supplies the single RID it is exporting for.
        Assert.AreEqual(string.Empty, props["RuntimeIdentifiers"], "RuntimeIdentifiers");
        Assert.AreEqual("true", props[flag], flag);
    }

    /// <summary>
    /// Godot loads the game assembly out of <c>.godot/mono/temp/bin/$(Configuration)/</c> and
    /// resolves it by the file name recorded in <c>project.godot</c>. Godot.NET.Sdk sets both up;
    /// the core SDK's <c>AppendTargetFrameworkToOutputPath</c> and its fully-qualified
    /// <c>AssemblyName</c> each move the assembly out from under the engine, so ktsu.Sdk.Godot
    /// puts both back. The demo's AUTHORS.md is what makes the assembly name diverge at all:
    /// without an authors namespace the core SDK's derived name and the project name are the
    /// same string and the assertion would pass either way.
    /// </summary>
    [TestMethod]
    public void GodotSdk_KeepsTheAssemblyWhereTheEngineLooksForIt()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Godot"));

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            "Demo.Godot/Demo.Godot.csproj",
            "AppendTargetFrameworkToOutputPath", "AssemblyName", "RootNamespace", "EnableDynamicLoading");

        Assert.AreEqual("false", props["AppendTargetFrameworkToOutputPath"], "AppendTargetFrameworkToOutputPath");
        Assert.AreEqual("Demo.Godot", props["AssemblyName"], "AssemblyName");

        // The ktsu namespace is still applied to the code; only the assembly file name is pinned.
        Assert.AreEqual("ktsu.Demo.Godot", props["RootNamespace"], "RootNamespace");

        // Produces the runtimeconfig.json Godot needs to load the assembly into a collectible
        // load context, which is what makes editor assembly reloading work.
        Assert.AreEqual("true", props["EnableDynamicLoading"], "EnableDynamicLoading");
    }

    /// <summary>
    /// KTSU1003: Unity's scripting runtime implements .NET Standard and .NET Framework, so a
    /// netX.0 assembly fails to import. Without the guard the project builds clean and the
    /// failure only shows up in the Unity console.
    /// </summary>
    [TestMethod]
    public void UnitySdk_RejectsAFrameworkUnityCannotLoad()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Unity"));

        // System.Memory is required by KTSU0001 on .NET Standard and redundant (NU1510) on
        // net10.0, so the repin drops it along the way.
        string projectPath = Path.Combine(workspace.Root, "Demo.Unity", "Demo.Unity.csproj");
        string repinned = File.ReadAllText(projectPath)
            .Replace("""<Sdk Name="ktsu.Sdk.Unity" />""", $"""<Sdk Name="ktsu.Sdk.Unity" /><PropertyGroup><TargetFramework>{TargetFrameworks.Latest}</TargetFramework></PropertyGroup>""", StringComparison.Ordinal)
            .Replace("""<PackageReference Include="System.Memory" />""", string.Empty, StringComparison.Ordinal);
        File.WriteAllText(projectPath, repinned);

        CliResult result = workspace.Build("Demo.Unity/Demo.Unity.csproj");

        Assert.IsFalse(result.Succeeded, $"Expected the repinned Unity demo to fail.{Environment.NewLine}{result.Output}");
        StringAssert.Contains(result.Output, "KTSU1003", $"Expected KTSU1003.{Environment.NewLine}{result.Output}");
    }
}
