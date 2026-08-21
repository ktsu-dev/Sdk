namespace Sdk.Examples.Tests;

using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Pins that the harness stops waiting once the build it started has exited.
/// </summary>
[TestClass]
public sealed class CliProcessLifetimeTests
{
    private const string Project = "Library/Library.csproj";

    /// <summary>
    /// Far above the ~30 seconds the build itself needs, and far below the 15 minute stall, so this
    /// separates the two without being sensitive to how fast the machine is.
    /// </summary>
    private static readonly TimeSpan Threshold = TimeSpan.FromMinutes(5);

    /// <summary>
    /// MSBuild worker nodes inherit the redirected stdout and stderr handles the harness hands their
    /// parent, and with node reuse left on they outlive the build by the reuse idle timeout — fifteen
    /// minutes. <c>Cli.Run</c> ends with an unbounded <c>WaitForExit()</c>, which waits for those
    /// streams to reach EOF rather than for the process to exit, so it sat for the full fifteen
    /// minutes with the CPU flat while the build itself had finished in about ten seconds.
    /// </summary>
    /// <remarks>
    /// A multi-targeting build under <c>-m</c> is what fans out into worker nodes, and
    /// <see cref="ExampleWorkspace.Build"/> shuts the build server down immediately beforehand, which
    /// forces the build to spawn fresh nodes instead of reusing existing ones. That is why this
    /// reproduces every time rather than only on a cold machine: nodes that already exist are
    /// connected to, and never inherit this process's handles.
    /// </remarks>
    [TestMethod]
    public void Build_ReturnsWhenTheBuildExits_RatherThanWaitingOutMsbuildNodeReuse()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));
        MultiTargetDemoProject(workspace.Root);

        Stopwatch elapsed = Stopwatch.StartNew();
        CliResult result = workspace.Build(Project, "-p:EnforceCodeStyleInBuild=false", "-m");
        elapsed.Stop();

        Assert.IsTrue(
            result.Succeeded,
            $"Expected the multi-targeting demo build to succeed.{Environment.NewLine}{result.Output}");

        Assert.IsTrue(
            elapsed.Elapsed < Threshold,
            $"The build call took {elapsed.Elapsed}, which is long enough that the harness was waiting on "
            + $"MSBuild node reuse rather than on the build. Child builds must run with node reuse disabled.");
    }

    /// <summary>
    /// Switches the demo from its pinned single framework to several so the build fans out into
    /// parallel inner builds, each of which reaches for a worker node.
    /// </summary>
    private static void MultiTargetDemoProject(string workspaceRoot)
    {
        string projectPath = Path.Combine(workspaceRoot, "Library", "Library.csproj");
        string original = File.ReadAllText(projectPath);
        string multiTargeted = original
            .Replace("<TargetFramework>net10.0</TargetFramework>", "<TargetFramework></TargetFramework>", StringComparison.Ordinal)
            .Replace("<TargetFrameworks></TargetFrameworks>", "<TargetFrameworks>net10.0;net9.0;net8.0</TargetFrameworks>", StringComparison.Ordinal);

        Assert.AreNotEqual(original, multiTargeted, "The demo project no longer has the expected framework properties.");
        File.WriteAllText(projectPath, multiTargeted);
    }
}
