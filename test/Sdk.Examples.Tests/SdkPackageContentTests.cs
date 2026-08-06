namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Verifies the packed MSBuild SDK package carries distributable repository configuration files.
/// </summary>
[TestClass]
public sealed class SdkPackageContentTests
{
    [TestMethod]
    public void CoreSdkPackage_ContainsStyleConfigFiles()
    {
        string nupkg = Path.Combine(SdkFeed.FeedDir, $"ktsu.Sdk.{SdkFeed.Version}.nupkg");
        Assert.IsTrue(File.Exists(nupkg), $"Expected packed core SDK package at '{nupkg}'.");

        List<string> entries = [.. ExampleWorkspace.NupkgEntries(nupkg)];

        CollectionAssert.Contains(entries, "_PackageData/editorconfig", "Missing packaged .editorconfig content.");
        CollectionAssert.Contains(entries, "_PackageData/gitattributes", "Missing packaged .gitattributes content.");
        CollectionAssert.Contains(entries, "_PackageData/gitignore", "Missing packaged .gitignore content.");

        string expectedEditorConfig = File.ReadAllText(Path.Combine(RepoLayout.Root, ".editorconfig"));
        string expectedGitAttributes = File.ReadAllText(Path.Combine(RepoLayout.Root, ".gitattributes"));
        string expectedGitIgnore = File.ReadAllText(Path.Combine(RepoLayout.Root, ".gitignore"));

        Assert.AreEqual(expectedEditorConfig, ExampleWorkspace.ReadNupkgEntry(nupkg, "_PackageData/editorconfig"));
        Assert.AreEqual(expectedGitAttributes, ExampleWorkspace.ReadNupkgEntry(nupkg, "_PackageData/gitattributes"));
        Assert.AreEqual(expectedGitIgnore, ExampleWorkspace.ReadNupkgEntry(nupkg, "_PackageData/gitignore"));
    }
}
