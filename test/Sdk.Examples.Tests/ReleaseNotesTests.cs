namespace Sdk.Examples.Tests;

using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Coverage for how the SDK turns a solution's changelog into <c>PackageReleaseNotes</c>.
/// </summary>
/// <remarks>
/// nuget.org rejects a push with <c>400 (A nuget package's ReleaseNotes property may not be more
/// than 35000 characters long.)</c>, and a repository only discovers that at publish time, after a
/// successful build, pack and release commit. The changelog grows with every release, so the cap has
/// to hold for every project the SDK builds.
/// </remarks>
[TestClass]
public sealed class ReleaseNotesTests
{
    /// <summary>The hard limit nuget.org enforces on the ReleaseNotes metadata value.</summary>
    private const int NuGetLimit = 35000;

    [TestMethod]
    public void OversizedChangelog_IsTruncatedBelowNuGetLimit()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));
        workspace.WriteFile("CHANGELOG.md", Changelog(entries: 2000));

        string notes = workspace.Evaluate("Library/Library.csproj", "PackageReleaseNotes")["PackageReleaseNotes"];

        Assert.IsTrue(
            notes.Length <= NuGetLimit,
            $"Release notes are {notes.Length} characters and must fit NuGet's {NuGetLimit} character limit.");
        StringAssert.StartsWith(notes, "## v9.9.9", "Truncation must keep the newest entries, which head the changelog.");
        StringAssert.EndsWith(notes, "(truncated due to NuGet length limits)", "A truncated value should say so.");
    }

    [TestMethod]
    public void ChangelogWithinLimit_IsUsedVerbatim()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));
        string changelog = Changelog(entries: 20);
        workspace.WriteFile("CHANGELOG.md", changelog);

        string notes = workspace.Evaluate("Library/Library.csproj", "PackageReleaseNotes")["PackageReleaseNotes"];

        Assert.AreEqual(Normalize(changelog), Normalize(notes), "A changelog under the limit must not be altered.");
    }

    /// <param name="absolute">Whether to pass the file as an absolute path (as CI does) or relative to the solution.</param>
    [TestMethod]
    [DataRow(true, DisplayName = "absolute path")]
    [DataRow(false, DisplayName = "relative to solution directory")]
    public void PackageReleaseNotesFile_ReplacesFullChangelog(bool absolute)
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));
        workspace.WriteFile("CHANGELOG.md", Changelog(entries: 2000));

        const string latest = "## v9.9.9 (minor)\n\n- the only change worth shipping notes for\n";
        workspace.WriteFile("LATEST_CHANGELOG.md", latest);

        string path = absolute ? Path.Combine(workspace.Root, "LATEST_CHANGELOG.md") : "LATEST_CHANGELOG.md";
        string notes = workspace.EvaluateWith(
            "Library/Library.csproj",
            ["-p:PackageReleaseNotesFile=" + path],
            "PackageReleaseNotes")["PackageReleaseNotes"];

        Assert.AreEqual(Normalize(latest), Normalize(notes), "An explicit release notes file must win over the full changelog.");
    }

    /// <summary>
    /// The SDK's own packages are built from <c>Sdk.Common.PackageProperties.props</c> rather than
    /// from the SDK it ships, so the cap has to be enforced there too. Missing it there is what made
    /// a release of this repository fail at <c>dotnet nuget push</c> once CHANGELOG.md passed 35000
    /// characters, long after the build and the release commit had succeeded.
    /// </summary>
    [TestMethod]
    public void SdkOwnPackages_CapReleaseNotes()
    {
        string oversized = Path.Combine(Path.GetTempPath(), "ktsu-sdk-release-notes-" + Guid.NewGuid().ToString("N") + ".md");
        File.WriteAllText(oversized, Changelog(entries: 2000));

        try
        {
            CliResult result = Cli.Dotnet(
                RepoLayout.Root,
                "msbuild", Path.Combine("Sdk", "Sdk.csproj"),
                "-getProperty:PackageReleaseNotes",
                "-p:PackageReleaseNotesFile=" + oversized);

            Assert.IsTrue(result.Succeeded, $"Evaluating the core SDK project should succeed.{Environment.NewLine}{result.Output}");

            string notes = result.Output.Trim();
            Assert.IsTrue(
                notes.Length <= NuGetLimit,
                $"Release notes are {notes.Length} characters and must fit NuGet's {NuGetLimit} character limit.");
            StringAssert.EndsWith(notes, "(truncated due to NuGet length limits)", "A truncated value should say so.");
        }
        finally
        {
            File.Delete(oversized);
        }
    }

    /// <summary>
    /// A newest-first changelog, matching what <c>make-changelog.ps1</c> writes. The body is kept
    /// free of braces so a single-property evaluation is not mistaken for JSON output.
    /// </summary>
    private static string Changelog(int entries)
    {
        StringBuilder builder = new();
        builder.AppendLine("## v9.9.9 (minor)").AppendLine();
        for (int i = entries; i > 0; i--)
        {
            builder.AppendLine($"- change number {i} by someone in the project history");
        }

        return builder.ToString();
    }

    private static string Normalize(string value) => value.ReplaceLineEndings("\n").Trim();
}
