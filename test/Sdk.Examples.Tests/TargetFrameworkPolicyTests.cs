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
}
