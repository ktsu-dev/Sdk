namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Guards the namespace/assembly-name/package-ID derivation in <c>Sdk.props</c> against
/// scaffolding folder segments (<c>src</c>, <c>source</c>, <c>tests</c>, <c>test</c>,
/// <c>examples</c>, <c>samples</c>) leaking into the identity of a project nested beneath one.
/// See ktsu-dev/Sdk#37.
/// </summary>
[TestClass]
public sealed class ProjectNamespaceTests
{
    /// <summary>
    /// A project directly under a leading scaffolding folder loses that folder's name from its
    /// derived identity: before the fix this project resolved to <c>examples.Widget</c>.
    /// </summary>
    [TestMethod]
    public void Project_UnderLeadingScaffoldingFolder_DropsTheFolderFromItsNamespace()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            "examples/Widget/Widget.csproj", "AssemblyName", "PackageId");

        Assert.AreEqual("Widget", props["AssemblyName"], "AssemblyName");
        Assert.AreEqual("Widget", props["PackageId"], "PackageId");
    }

    /// <summary>
    /// A folder that merely contains one of the scaffolding words as a substring (<c>Testing</c>
    /// contains <c>test</c>) must not be stripped: the match has to be a whole path segment.
    /// </summary>
    [TestMethod]
    public void Project_UnderFolderContainingScaffoldingWordAsSubstring_IsUnaffected()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            "Testing/Testing.csproj", "AssemblyName", "PackageId");

        Assert.AreEqual("Testing", props["AssemblyName"], "AssemblyName");
        Assert.AreEqual("Testing", props["PackageId"], "PackageId");
    }
}
