namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Guards the package metadata file declarations in <c>Sdk.props</c>. <c>Sdk.targets</c> packs
/// <c>icon.png</c>, <c>README.md</c> and <c>LICENSE.md</c> through <c>None</c> items conditioned on
/// the file existing, so naming them unconditionally on the package promised NuGet content the
/// package does not contain: pack failed with NU5046 (readme), NU5047 (icon) or NU5030 (license).
/// Every ktsu repository carries all three, so this only ever bit consumers outside the
/// organisation - at <c>dotnet pack</c>, long after a build that appeared to work.
/// </summary>
[TestClass]
public sealed class PackageMetadataFileTests
{
    private const string Project = "Testing/Testing.csproj";

    /// <summary>
    /// A 1x1 PNG. NuGet reads the icon it is told to pack, so a placeholder of arbitrary bytes
    /// would fail validation for a reason unrelated to what this test covers.
    /// </summary>
    private static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    /// <summary>
    /// A repository carrying none of the three optional metadata files packs successfully. Before
    /// the fix this failed with NU5030, naming a LICENSE.md the repository never had.
    /// </summary>
    [TestMethod]
    public void Pack_WithoutIconReadmeOrLicense_Succeeds()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));

        (CliResult result, string outputDir) = workspace.Pack(Project);

        Assert.IsTrue(
            result.Succeeded,
            $"Expected a project with no icon, readme or license to pack.{Environment.NewLine}{result.Output}");

        string[] packages = [.. Directory.GetFiles(outputDir, "Testing.*.nupkg")
            .Where(f => !f.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))];

        Assert.AreEqual(1, packages.Length, $"Expected exactly one package: {string.Join(", ", packages)}");
    }

    /// <summary>
    /// The other half of the conditional: when the files are present they are still declared on the
    /// package, so making the declarations conditional did not quietly stop packaging them.
    /// </summary>
    [TestMethod]
    public void Pack_WithIconReadmeAndLicense_DeclaresThemOnThePackage()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));
        workspace.WriteFile("README.md", "# Testing" + Environment.NewLine);
        workspace.WriteFile("LICENSE.md", "MIT" + Environment.NewLine);
        File.WriteAllBytes(Path.Combine(workspace.Root, "icon.png"), OnePixelPng);

        (CliResult result, string outputDir) = workspace.Pack(Project);

        Assert.IsTrue(result.Succeeded, $"Expected the packed project to succeed.{Environment.NewLine}{result.Output}");

        string nupkg = Directory.GetFiles(outputDir, "Testing.*.nupkg")
            .Single(f => !f.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));

        string nuspec = ExampleWorkspace.ReadNupkgEntry(nupkg, "Testing.nuspec");

        StringAssert.Contains(nuspec, "<icon>icon.png</icon>", "icon");
        StringAssert.Contains(nuspec, "<readme>README.md</readme>", "readme");
        StringAssert.Contains(nuspec, "LICENSE.md", "license");
    }
}
