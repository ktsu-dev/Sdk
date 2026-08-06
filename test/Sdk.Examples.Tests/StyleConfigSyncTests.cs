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

        CliResult result = workspace.Build(Project, "-p:EnforceCodeStyleInBuild=false", "-p:KtsuSyncStyleConfigFiles=false");
        Assert.IsTrue(result.Succeeded, $"Expected demo build to succeed.{Environment.NewLine}{result.Output}");

        Assert.AreEqual(editorConfigBefore, File.ReadAllText(Path.Combine(solutionDir, ".editorconfig")));
        Assert.AreEqual(gitAttributesBefore, File.ReadAllText(Path.Combine(solutionDir, ".gitattributes")));
        Assert.AreEqual(gitIgnoreBefore, File.ReadAllText(Path.Combine(solutionDir, ".gitignore")));
    }

    [TestMethod]
    public void Build_UsesConsumerCopyrightInEditorConfigHeader()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));
        string solutionDir = workspace.Evaluate(Project, "SolutionDir")["SolutionDir"];

        SeedConsumerStyleFiles(solutionDir);
        const string consumerCopyright = "Copyright (c) 2020-2030 Contoso contributors";
        File.WriteAllText(Path.Combine(solutionDir, "COPYRIGHT.md"), consumerCopyright);
        File.WriteAllText(Path.Combine(solutionDir, ".editorconfig"), "stale editorconfig");
        Assert.AreEqual(consumerCopyright, workspace.Evaluate(Project, "Copyright")["Copyright"]);

        CliResult result = workspace.Build(Project, "-p:EnforceCodeStyleInBuild=false");
        Assert.IsTrue(result.Succeeded, $"Expected demo build to succeed.{Environment.NewLine}{result.Output}");

        string expectedEditorConfig = File.ReadAllText(Path.Combine(RepoLayout.Root, ".editorconfig"))
            .Replace("Copyright (c) ktsu.dev", consumerCopyright, StringComparison.Ordinal);

        Assert.AreEqual(
            NormalizeEditorConfig(expectedEditorConfig),
            NormalizeEditorConfig(File.ReadAllText(Path.Combine(solutionDir, ".editorconfig"))));
        Assert.AreEqual(File.ReadAllText(Path.Combine(RepoLayout.Root, ".gitattributes")), File.ReadAllText(Path.Combine(solutionDir, ".gitattributes")));
        Assert.AreEqual(File.ReadAllText(Path.Combine(RepoLayout.Root, ".gitignore")), File.ReadAllText(Path.Combine(solutionDir, ".gitignore")));
    }

    private static void SeedConsumerStyleFiles(string solutionDir)
    {
        File.WriteAllText(Path.Combine(solutionDir, ".editorconfig"), "stale editorconfig");
        File.WriteAllText(Path.Combine(solutionDir, ".gitattributes"), "stale gitattributes");
        File.WriteAllText(Path.Combine(solutionDir, ".gitignore"), "stale gitignore");
    }

    private static void AssertStyleConfigFilesMatchSdkDefaults(string solutionDir)
    {
        string expectedEditorConfig = File.ReadAllText(Path.Combine(RepoLayout.Root, ".editorconfig"));
        string expectedGitAttributes = File.ReadAllText(Path.Combine(RepoLayout.Root, ".gitattributes"));
        string expectedGitIgnore = File.ReadAllText(Path.Combine(RepoLayout.Root, ".gitignore"));

        Assert.AreEqual(
            NormalizeEditorConfig(expectedEditorConfig),
            NormalizeEditorConfig(File.ReadAllText(Path.Combine(solutionDir, ".editorconfig"))));
        Assert.AreEqual(expectedGitAttributes, File.ReadAllText(Path.Combine(solutionDir, ".gitattributes")));
        Assert.AreEqual(expectedGitIgnore, File.ReadAllText(Path.Combine(solutionDir, ".gitignore")));
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
}
