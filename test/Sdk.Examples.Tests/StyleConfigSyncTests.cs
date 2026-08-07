namespace Sdk.Examples.Tests;

using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Verifies consumer style/config files are synced from the packaged SDK defaults, with opt-out.
/// </summary>
[TestClass]
public sealed class StyleConfigSyncTests
{
    private const string Project = "Library/Library.csproj";

    [TestMethod]
    public void Build_SyncsStyleConfigFilesByDefault()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));
        string solutionDir = workspace.Evaluate(Project, "SolutionDir")["SolutionDir"];

        SeedConsumerStyleFiles(solutionDir);

        CliResult result = workspace.Build(Project, "-p:EnforceCodeStyleInBuild=false");
        Assert.IsTrue(result.Succeeded, $"Expected demo build to succeed.{Environment.NewLine}{result.Output}");

        AssertStyleConfigFilesMatchSdkDefaults(solutionDir);
    }

    [TestMethod]
    public void Build_DoesNotSyncStyleConfigFiles_WhenOptedOut()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));
        string solutionDir = workspace.Evaluate(Project, "SolutionDir")["SolutionDir"];

        SeedConsumerStyleFiles(solutionDir);
        string editorConfigBefore = File.ReadAllText(Path.Combine(solutionDir, ".editorconfig"));
        string gitAttributesBefore = File.ReadAllText(Path.Combine(solutionDir, ".gitattributes"));
        string gitIgnoreBefore = File.ReadAllText(Path.Combine(solutionDir, ".gitignore"));
        string runSettingsBefore = File.ReadAllText(Path.Combine(solutionDir, ".runsettings"));

        CliResult result = workspace.Build(Project, "-p:EnforceCodeStyleInBuild=false", "-p:KtsuSyncStyleConfigFiles=false");
        Assert.IsTrue(result.Succeeded, $"Expected demo build to succeed.{Environment.NewLine}{result.Output}");

        Assert.AreEqual(editorConfigBefore, File.ReadAllText(Path.Combine(solutionDir, ".editorconfig")));
        Assert.AreEqual(gitAttributesBefore, File.ReadAllText(Path.Combine(solutionDir, ".gitattributes")));
        Assert.AreEqual(gitIgnoreBefore, File.ReadAllText(Path.Combine(solutionDir, ".gitignore")));
        Assert.AreEqual(runSettingsBefore, File.ReadAllText(Path.Combine(solutionDir, ".runsettings")));
    }

    [TestMethod]
    public void Build_UsesConsumerCopyrightFileContentInEditorConfigHeader()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));
        string solutionDir = workspace.Evaluate(Project, "SolutionDir")["SolutionDir"];

        SeedConsumerStyleFiles(solutionDir);
        const string consumerCopyright = "Copyright (c) 2020-2030 Contoso contributors\nAll rights reserved.\nLicensed under the Apache-2.0 license.";
        File.WriteAllText(Path.Combine(solutionDir, "COPYRIGHT.md"), consumerCopyright);
        File.WriteAllText(Path.Combine(solutionDir, ".editorconfig"), "stale editorconfig");
        // Cli joins captured output lines with Environment.NewLine, so a multi-line property
        // value comes back CRLF-terminated on Windows regardless of what MSBuild evaluated.
        Assert.AreEqual(
            consumerCopyright,
            workspace.Evaluate(Project, "Copyright")["Copyright"].Replace("\r\n", "\n", StringComparison.Ordinal));

        CliResult result = workspace.Build(Project, "-p:EnforceCodeStyleInBuild=false");
        Assert.IsTrue(result.Succeeded, $"Expected demo build to succeed.{Environment.NewLine}{result.Output}");

        string expectedEditorConfig = File.ReadAllText(Path.Combine(RepoLayout.Root, ".editorconfig"))
            .Replace(
                "file_header_template = Copyright (c) ktsu.dev\\nAll rights reserved.\\nLicensed under the MIT license.",
                $"file_header_template = {ToEditorConfigValue(consumerCopyright)}",
                StringComparison.Ordinal);

        Assert.AreEqual(
            NormalizeEditorConfig(expectedEditorConfig),
            NormalizeEditorConfig(File.ReadAllText(Path.Combine(solutionDir, ".editorconfig"))));
        Assert.AreEqual(File.ReadAllText(Path.Combine(RepoLayout.Root, ".gitattributes")), File.ReadAllText(Path.Combine(solutionDir, ".gitattributes")));
        Assert.AreEqual(File.ReadAllText(Path.Combine(RepoLayout.Root, ".gitignore")), File.ReadAllText(Path.Combine(solutionDir, ".gitignore")));
        Assert.AreEqual(File.ReadAllText(Path.Combine(RepoLayout.Root, ".runsettings")), File.ReadAllText(Path.Combine(solutionDir, ".runsettings")));
    }

    /// <summary>
    /// A file that already matches the SDK defaults must not be rewritten. Rewriting on every
    /// build is what put concurrent MSBuild nodes in contention over the solution-root files.
    /// </summary>
    [TestMethod]
    public void Build_DoesNotRewriteAlreadySyncedStyleConfigFiles()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));
        string solutionDir = workspace.Evaluate(Project, "SolutionDir")["SolutionDir"];

        SeedConsumerStyleFiles(solutionDir);

        CliResult first = workspace.Build(Project, "-p:EnforceCodeStyleInBuild=false");
        Assert.IsTrue(first.Succeeded, $"Expected the first demo build to succeed.{Environment.NewLine}{first.Output}");

        Dictionary<string, DateTime> writtenAt = SyncedFileNames
            .ToDictionary(name => name, name => File.GetLastWriteTimeUtc(Path.Combine(solutionDir, name)));

        CliResult second = workspace.Build(Project, "-p:EnforceCodeStyleInBuild=false");
        Assert.IsTrue(second.Succeeded, $"Expected the second demo build to succeed.{Environment.NewLine}{second.Output}");

        foreach (string name in SyncedFileNames)
        {
            Assert.AreEqual(
                writtenAt[name],
                File.GetLastWriteTimeUtc(Path.Combine(solutionDir, name)),
                $"'{name}' was rewritten by a build even though it already matched the SDK defaults.");
        }

        AssertStyleConfigFilesMatchSdkDefaults(solutionDir);
    }

    /// <summary>
    /// The sync target runs once per inner build, so a multi-targeting project has several
    /// MSBuild nodes reaching for the same solution-root files at once.
    /// </summary>
    [TestMethod]
    public void Build_SyncsStyleConfigFiles_WhenMultiTargetingInParallel()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));
        string solutionDir = workspace.Evaluate(Project, "SolutionDir")["SolutionDir"];

        SeedConsumerStyleFiles(solutionDir);
        MultiTargetDemoProject(solutionDir);

        CliResult result = workspace.Build(Project, "-p:EnforceCodeStyleInBuild=false", "-m");

        Assert.IsTrue(result.Succeeded, $"Expected the multi-targeting demo build to succeed.{Environment.NewLine}{result.Output}");
        AssertStyleConfigFilesMatchSdkDefaults(solutionDir);
    }

    private static readonly string[] SyncedFileNames = [".editorconfig", ".gitattributes", ".gitignore", ".runsettings"];

    /// <summary>
    /// Switches the demo project from its pinned single framework to several, so the build
    /// fans out into parallel inner builds. Editing the project file is the only way to do
    /// this: a global <c>-p:TargetFrameworks</c> cannot clear the project's own
    /// <c>TargetFramework</c>, and the SDK then rejects the combination (NETSDK1046).
    /// </summary>
    private static void MultiTargetDemoProject(string solutionDir)
    {
        string projectPath = Path.Combine(solutionDir, "Library", "Library.csproj");
        string original = File.ReadAllText(projectPath);
        string multiTargeted = original.Replace(
            "<TargetFramework>net10.0</TargetFramework>",
            "<TargetFramework></TargetFramework>",
            StringComparison.Ordinal)
            .Replace(
                "<TargetFrameworks></TargetFrameworks>",
                "<TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>",
                StringComparison.Ordinal);

        Assert.AreNotEqual(original, multiTargeted, "The demo project no longer has the expected framework properties.");
        File.WriteAllText(projectPath, multiTargeted);
    }

    private static void SeedConsumerStyleFiles(string solutionDir)
    {
        File.WriteAllText(Path.Combine(solutionDir, ".editorconfig"), "stale editorconfig");
        File.WriteAllText(Path.Combine(solutionDir, ".gitattributes"), "stale gitattributes");
        File.WriteAllText(Path.Combine(solutionDir, ".gitignore"), "stale gitignore");
        File.WriteAllText(Path.Combine(solutionDir, ".runsettings"), "stale runsettings");
    }

    private static void AssertStyleConfigFilesMatchSdkDefaults(string solutionDir)
    {
        string expectedEditorConfig = File.ReadAllText(Path.Combine(RepoLayout.Root, ".editorconfig"));
        string expectedGitAttributes = File.ReadAllText(Path.Combine(RepoLayout.Root, ".gitattributes"));
        string expectedGitIgnore = File.ReadAllText(Path.Combine(RepoLayout.Root, ".gitignore"));
        string expectedRunSettings = File.ReadAllText(Path.Combine(RepoLayout.Root, ".runsettings"));

        Assert.AreEqual(
            NormalizeEditorConfig(expectedEditorConfig),
            NormalizeEditorConfig(File.ReadAllText(Path.Combine(solutionDir, ".editorconfig"))));
        Assert.AreEqual(expectedGitAttributes, File.ReadAllText(Path.Combine(solutionDir, ".gitattributes")));
        Assert.AreEqual(expectedGitIgnore, File.ReadAllText(Path.Combine(solutionDir, ".gitignore")));
        Assert.AreEqual(expectedRunSettings, File.ReadAllText(Path.Combine(solutionDir, ".runsettings")));
    }

    private static string NormalizeEditorConfig(string content)
    {
        StringBuilder normalized = new();
        string[] lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        foreach (string line in lines)
        {
            if (line.Length == 0)
            {
                continue;
            }

            string canonicalLine = line.Replace("/n", "\\n", StringComparison.Ordinal);
            _ = normalized.AppendLine(canonicalLine);
        }

        return normalized.ToString();
    }

    private static string ToEditorConfigValue(string content) =>
        content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}
