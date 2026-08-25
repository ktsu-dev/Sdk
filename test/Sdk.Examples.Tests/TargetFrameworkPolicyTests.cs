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
}
