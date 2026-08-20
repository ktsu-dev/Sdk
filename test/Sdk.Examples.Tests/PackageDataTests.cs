namespace Sdk.Examples.Tests;

using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Guards how the SDK turns the repository metadata files (README.md, PROJECT_URL.url and the
/// rest) into package properties and package content. Both behaviours here were regressions that
/// reached published packages, so they are covered end-to-end against a real pack/publish rather
/// than by evaluating properties.
/// </summary>
[TestClass]
public sealed class PackageDataTests
{
    /// <summary>
    /// No package-data file may be copied to the consumer's output directory.
    /// </summary>
    /// <remarks>
    /// Every project used to copy its metadata to the same relative output path
    /// (<c>_PackageData\README.md</c> and nine siblings), so any consumer whose graph contained
    /// two ktsu packages failed the publish with NETSDK1152. Nothing reads those files from the
    /// output directory, so the copy was dropped; this test is what stops it coming back.
    /// <para>
    /// The demo gives Alpha and Beta their own README.md and DESCRIPTION.md, and that detail is
    /// load-bearing. Those three files are project-scoped first (<c>ReadmeFilePathProject</c> wins
    /// over <c>ReadmeFilePathSolution</c>), so per-project copies make the item <em>sources</em>
    /// differ while the link path stays identical - which is what MSBuild rejects. With only
    /// solution-level metadata every project contributes the same source file, MSBuild dedupes it,
    /// and the bug does not reproduce. That is why it went unnoticed until ktsu-dev/ImGuiApp#316.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void PackageDataItems_AreNotCopiedToOutput()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("PackageData"));

        IReadOnlyList<IReadOnlyDictionary<string, string>> nones = workspace.EvaluateItems("Alpha/Alpha.csproj", "None");

        List<IReadOnlyDictionary<string, string>> packageData =
        [
            .. nones.Where(i => i.TryGetValue("Link", out string? link)
                && link.StartsWith("_PackageData", StringComparison.Ordinal))
        ];

        Assert.AreNotEqual(0, packageData.Count, "Expected the SDK to contribute _PackageData items; the demo metadata may not be wired up.");

        foreach (IReadOnlyDictionary<string, string> item in packageData)
        {
            string copy = item.TryGetValue("CopyToOutputDirectory", out string? c) ? c : string.Empty;
            Assert.AreEqual(
                string.Empty,
                copy,
                $"'{item.GetValueOrDefault("Link")}' is copied to the output directory. The link path is fixed per "
                + "file name, so two ktsu packages in one publish graph both write it and fail with NETSDK1152.");
        }
    }

    /// <summary>
    /// <c>PROJECT_URL.url</c> is an InternetShortcut INI file, so the packed
    /// <c>&lt;projectUrl&gt;</c> must be the URL it contains, not the whole file.
    /// </summary>
    /// <remarks>
    /// Reading the file whole produced a multi-line value that NuGet dropped, leaving every
    /// published ktsu package with no <c>projectUrl</c> at all.
    /// </remarks>
    [TestMethod]
    public void Pack_ReadsProjectUrlFromInternetShortcutFile()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("PackageData"));

        (CliResult result, string outputDir) = workspace.Pack("Alpha/Alpha.csproj");
        Assert.IsTrue(result.Succeeded, $"Expected the pack to succeed.{Environment.NewLine}{result.Output}");

        string nupkg = Directory.EnumerateFiles(outputDir, "*.nupkg")
            .Single(f => !f.EndsWith(".symbols.nupkg", StringComparison.Ordinal));

        string nuspecEntry = ExampleWorkspace.NupkgEntries(nupkg)
            .Single(e => e.EndsWith(".nuspec", StringComparison.Ordinal));

        XDocument nuspec = XDocument.Parse(ExampleWorkspace.ReadNupkgEntry(nupkg, nuspecEntry));
        XElement? projectUrl = nuspec.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "projectUrl");

        Assert.IsNotNull(projectUrl, $"Packed nuspec has no <projectUrl>.{Environment.NewLine}{nuspec}");
        Assert.AreEqual("https://github.com/ktsu-dev/Sdk", projectUrl.Value.Trim());
    }
}
