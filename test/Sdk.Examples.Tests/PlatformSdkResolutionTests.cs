namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Verifies the platform app SDKs resolve the expected framework, output type, runtime
/// identifiers and project-type detection flag for a consuming project. Property evaluation
/// does not build the consumer, so these run on any host without mobile/desktop workloads.
/// </summary>
[TestClass]
public sealed class PlatformSdkResolutionTests
{
    /// <param name="demo">The demo directory under examples/demos.</param>
    /// <param name="project">The consuming project, relative to the demo directory.</param>
    /// <param name="tfm">Expected TargetFramework.</param>
    /// <param name="outputType">Expected OutputType.</param>
    /// <param name="rids">Expected RuntimeIdentifiers.</param>
    /// <param name="flag">The project-type detection flag expected to be 'true'.</param>
    [TestMethod]
    [DataRow("Windows", "Windows/Windows.csproj", "net10.0", "WinExe", "win-x64;win-x86;win-arm64", "IsWindowsProject", DisplayName = "ktsu.Sdk.Windows")]
    [DataRow("macOS", "macOS/macOS.csproj", "net10.0", "Exe", "osx-x64;osx-arm64", "IsMacProject", DisplayName = "ktsu.Sdk.macOS")]
    [DataRow("iOS", "iOS/iOS.csproj", "net10.0-ios", "Exe", "ios-arm64;iossimulator-x64;iossimulator-arm64", "IsIosProject", DisplayName = "ktsu.Sdk.iOS")]
    [DataRow("Android", "Android/Android.csproj", "net10.0-android", "Exe", "", "IsAndroidProject", DisplayName = "ktsu.Sdk.Android")]
    public void PlatformSdk_ResolvesExpectedProperties(
        string demo, string project, string tfm, string outputType, string rids, string flag)
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo(demo));

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            project, "TargetFramework", "OutputType", "RuntimeIdentifiers", flag);

        Assert.AreEqual(tfm, props["TargetFramework"], "TargetFramework");
        Assert.AreEqual(outputType, props["OutputType"], "OutputType");
        Assert.AreEqual(rids, props["RuntimeIdentifiers"], "RuntimeIdentifiers");
        Assert.AreEqual("true", props[flag], flag);
    }
}
