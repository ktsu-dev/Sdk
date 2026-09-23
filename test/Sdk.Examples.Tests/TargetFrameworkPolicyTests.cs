namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Pins the framework policy: the rewrite that keeps the example projects in step with the
/// framework the SDK pins, and the default multi-target list the core SDK gives a library.
/// </summary>
[TestClass]
public sealed class TargetFrameworkPolicyTests
{
    /// <param name="projectXml">The project text to rewrite.</param>
    /// <param name="latest">The framework to repin to.</param>
    /// <param name="expected">The expected result.</param>
    [TestMethod]
    [DataRow(
        "<TargetFramework>net10.0</TargetFramework>", "net11.0",
        "<TargetFramework>net11.0</TargetFramework>",
        DisplayName = "a versioned pin is repinned")]
    [DataRow(
        "<TargetFramework>net10.0-ios</TargetFramework>", "net11.0",
        "<TargetFramework>net11.0-ios</TargetFramework>",
        DisplayName = "a platform pin keeps its platform")]
    [DataRow(
        "<TargetFramework>netstandard2.0</TargetFramework>", "net11.0",
        "<TargetFramework>netstandard2.0</TargetFramework>",
        DisplayName = "netstandard is left alone")]
    [DataRow(
        "<TargetFrameworks>net10.0;net9.0</TargetFrameworks>", "net11.0",
        "<TargetFrameworks>net10.0;net9.0</TargetFrameworks>",
        DisplayName = "the plural element is left alone")]
    [DataRow(
        "<OutputType>Exe</OutputType>", "net11.0",
        "<OutputType>Exe</OutputType>",
        DisplayName = "a project with no pin is unchanged")]
    public void RewritePins_RepinsOnlyVersionedFrameworks(string projectXml, string latest, string expected) =>
        Assert.AreEqual(expected, TargetFrameworks.RewritePins(projectXml, latest));

    /// <summary>
    /// The default multi-target list a library inherits. Frameworks follow the .NET support
    /// lifecycle: one enters when it ships and leaves when it goes out of support.
    /// netstandard2.0 and 2.1 are API standards rather than runtimes, have no support term, and
    /// stay as the fallback that keeps a consumer on a trimmed framework working.
    /// </summary>
    [TestMethod]
    public void CoreSdk_MultiTargetsTheSupportedFrameworks()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));

        // The demo pins a single framework to keep the smoke tests fast, and the project body is
        // evaluated after the SDK props, so both elements have to go before the SDK's own default
        // is observable.
        string projectPath = Path.Combine(workspace.Root, "Library", "Library.csproj");
        string original = File.ReadAllText(projectPath);
        string unpinned = original
            .Replace($"<TargetFramework>{TargetFrameworks.Latest}</TargetFramework>", string.Empty, StringComparison.Ordinal)
            .Replace("<TargetFrameworks></TargetFrameworks>", string.Empty, StringComparison.Ordinal);

        Assert.AreNotEqual(original, unpinned, "The Library demo no longer has the expected framework properties.");
        File.WriteAllText(projectPath, unpinned);

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            "Library/Library.csproj", "TargetFrameworks");

        Assert.AreEqual(TargetFrameworks.Library, props["TargetFrameworks"], "TargetFrameworks");
    }

    /// <summary>
    /// A test project that says nothing about frameworks single-targets the pinned one. The
    /// matching plural default is cleared so the project builds one leg directly rather than
    /// dispatching a one-leg cross-targeting build.
    /// </summary>
    [TestMethod]
    public void TestProject_SingleTargetsByDefault()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Test"));

        SetTestProjectFrameworks(workspace, string.Empty);

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            "Calculator.Test/Calculator.Test.csproj", "TargetFramework", "TargetFrameworks");

        Assert.AreEqual(TargetFrameworks.Latest, props["TargetFramework"], "TargetFramework");
        Assert.AreEqual(string.Empty, props["TargetFrameworks"], "TargetFrameworks");
    }

    /// <summary>
    /// A test project that asks for a framework matrix keeps it.
    /// </summary>
    /// <remarks>
    /// Sdk.targets is imported after the project body, and it clears TargetFrameworks for test
    /// projects to produce the single-target default above. Unconditionally, that also erased a
    /// list the project had set for itself: the project's own configuration was discarded with no
    /// opt-out and no diagnostic, and the collapse was invisible because a suite that still runs
    /// one leg still reports green.
    /// <para>
    /// The case is concrete rather than hypothetical. A library that multi-targets needs its tests
    /// to run on each framework's own shared framework to exercise that framework's in-box
    /// dependencies; compiled against many and run on one tests only compile compatibility. See
    /// ktsu-dev/JsonRequiredConditionally#25, whose matrix this made unrestorable.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void TestProject_KeepsAnExplicitMultiTargetList()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Test"));

        const string matrix = "net10.0;net9.0";
        SetTestProjectFrameworks(workspace, matrix);

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            "Calculator.Test/Calculator.Test.csproj", "TargetFrameworks");

        Assert.AreEqual(
            matrix,
            props["TargetFrameworks"],
            "A test project's explicit TargetFrameworks was discarded by the SDK.");
    }

    /// <summary>
    /// Replaces the framework properties in the Test demo's test project.
    /// </summary>
    /// <param name="workspace">The workspace holding the copied demo.</param>
    /// <param name="frameworks">
    /// The plural list to set, or empty to leave the project saying nothing about frameworks so
    /// the SDK's own defaults are observable.
    /// </param>
    private static void SetTestProjectFrameworks(ExampleWorkspace workspace, string frameworks)
    {
        string projectPath = Path.Combine(workspace.Root, "Calculator.Test", "Calculator.Test.csproj");
        string original = File.ReadAllText(projectPath);

        // The project body is evaluated after the SDK props, so both elements have to go before
        // either the SDK's default or a replacement of our own is what is being observed.
        string rewritten = original
            .Replace($"<TargetFramework>{TargetFrameworks.Latest}</TargetFramework>", string.Empty, StringComparison.Ordinal)
            .Replace("<TargetFrameworks></TargetFrameworks>", string.Empty, StringComparison.Ordinal);

        Assert.AreNotEqual(original, rewritten, "The Test demo no longer has the expected framework properties.");

        if (frameworks.Length > 0)
        {
            rewritten = rewritten.Replace(
                "<IsTestProject>true</IsTestProject>",
                $"<IsTestProject>true</IsTestProject>\n    <TargetFramework></TargetFramework>\n    <TargetFrameworks>{frameworks}</TargetFrameworks>",
                StringComparison.Ordinal);
        }

        File.WriteAllText(projectPath, rewritten);
    }
}
