namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Guards scalar package metadata a consumer sets in <c>Directory.Build.props</c>, which is
/// evaluated before <c>Sdk.props</c>. The SDK assigned these unconditionally, so with no
/// <c>DESCRIPTION.md</c>, <c>TAGS.md</c> or <c>PROJECT.url</c> the consumer's value was replaced by an
/// empty one (or NuGet's "Package Description" placeholder), and
/// <c>ContinuousIntegrationBuild=true</c> was thrown away on any CI but GitHub Actions and Azure
/// Pipelines. See ktsu-dev/Sdk#91. The Namespaces demo carries none of those metadata files.
/// </summary>
[TestClass]
public sealed class ConsumerMetadataOverrideTests
{
    private const string Project = "Testing/Testing.csproj";

    /// <summary>Not on GitHub Actions or Azure Pipelines, whatever the machine running the test is.</summary>
    private static readonly string[] OffKnownCi = ["-p:GITHUB_ACTIONS=false", "-p:TF_BUILD=false"];

    /// <summary>
    /// Each value set in <c>Directory.Build.props</c> survives when its metadata file is absent.
    /// </summary>
    [TestMethod]
    public void ValuesSetInDirectoryBuildProps_SurviveWithoutMetadataFiles()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));
        WriteDirectoryBuildProps(workspace,
            """
            <PackageProjectUrl>https://example.com/foo</PackageProjectUrl>
            <PackageDescription>Foo library</PackageDescription>
            <PackageTags>foo;bar</PackageTags>
            <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
            """);

        IReadOnlyDictionary<string, string> props = workspace.EvaluateWith(
            Project, OffKnownCi, "PackageProjectUrl", "PackageDescription", "PackageTags", "ContinuousIntegrationBuild");

        Assert.AreEqual("https://example.com/foo", props["PackageProjectUrl"], "PackageProjectUrl");
        Assert.AreEqual("Foo library", props["PackageDescription"], "PackageDescription");
        Assert.AreEqual("foo;bar", props["PackageTags"], "PackageTags");
        Assert.AreEqual("true", props["ContinuousIntegrationBuild"], "ContinuousIntegrationBuild");
    }

    /// <summary>
    /// The metadata files still win where they exist: they are how ktsu repositories carry the
    /// values, and the change only stops an absent file from erasing a value.
    /// </summary>
    [TestMethod]
    public void MetadataFiles_StillSetThePackageValues()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));
        workspace.WriteFile("DESCRIPTION.md", "From the file" + Environment.NewLine);
        workspace.WriteFile("TAGS.md", "file;tags" + Environment.NewLine);
        workspace.WriteFile("PROJECT.url", "[InternetShortcut]" + Environment.NewLine + "URL=https://example.com/file" + Environment.NewLine);

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            Project, "PackageProjectUrl", "PackageDescription", "PackageTags");

        Assert.AreEqual("https://example.com/file", props["PackageProjectUrl"], "PackageProjectUrl");
        Assert.AreEqual("From the file", props["PackageDescription"], "PackageDescription");
        Assert.AreEqual("file;tags", props["PackageTags"], "PackageTags");
    }

    /// <summary>GitHub Actions and Azure Pipelines are still detected when nothing was set.</summary>
    [TestMethod]
    public void ContinuousIntegrationBuild_DefaultsFromKnownCi()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));

        Assert.AreEqual("true", workspace.EvaluateWith(
            Project, ["-p:GITHUB_ACTIONS=true", "-p:TF_BUILD=false"], "ContinuousIntegrationBuild")["ContinuousIntegrationBuild"], "GitHub Actions");
        Assert.AreEqual("true", workspace.EvaluateWith(
            Project, ["-p:GITHUB_ACTIONS=false", "-p:TF_BUILD=true"], "ContinuousIntegrationBuild")["ContinuousIntegrationBuild"], "Azure Pipelines");
        Assert.AreEqual("false", workspace.EvaluateWith(
            Project, OffKnownCi, "ContinuousIntegrationBuild")["ContinuousIntegrationBuild"], "elsewhere");
    }

    /// <summary>A consumer's explicit false is kept even on GitHub Actions.</summary>
    [TestMethod]
    public void ContinuousIntegrationBuild_ExplicitFalse_IsKeptOnKnownCi()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));
        WriteDirectoryBuildProps(workspace, "<ContinuousIntegrationBuild>false</ContinuousIntegrationBuild>");

        IReadOnlyDictionary<string, string> props = workspace.EvaluateWith(
            Project, ["-p:GITHUB_ACTIONS=true"], "ContinuousIntegrationBuild");

        Assert.AreEqual("false", props["ContinuousIntegrationBuild"], "ContinuousIntegrationBuild");
    }

    private static void WriteDirectoryBuildProps(ExampleWorkspace workspace, string properties) =>
        workspace.WriteFile("Directory.Build.props",
            $"""
            <Project>
              <PropertyGroup>
                {properties}
              </PropertyGroup>
            </Project>
            """);
}
