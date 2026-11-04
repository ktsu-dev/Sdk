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
    /// A project file sitting directly in a scaffolding folder (a flat <c>src/Flat.csproj</c>
    /// layout) has the bare segment <c>src</c> as its whole relative path, with no trailing dot.
    /// It must be stripped all the same: before the fix this project resolved to <c>src.Flat</c>.
    /// See ktsu-dev/Sdk#61.
    /// </summary>
    [TestMethod]
    public void Project_DirectlyInScaffoldingFolder_DropsTheFolderFromItsNamespace()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            "src/Flat.csproj", "AssemblyName", "PackageId");

        Assert.AreEqual("Flat", props["AssemblyName"], "AssemblyName");
        Assert.AreEqual("Flat", props["PackageId"], "PackageId");
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

    /// <summary>
    /// A folder and project whose names contain a space get the same identity as one without:
    /// the space is stripped from the path segment as it is from the project name, and
    /// Microsoft.NET.Sdk's underscored <c>RootNamespace</c> default (<c>My_Lib</c>) is not taken for
    /// a consumer override. Before the fix this resolved to <c>My Lib.MyLib</c> and <c>My_Lib</c>.
    /// See ktsu-dev/Sdk#73.
    /// </summary>
    [TestMethod]
    public void Project_WithSpaceInFolderAndProjectName_StripsTheSpaceOnce()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            "My Lib/My Lib.csproj", "ProjectNamespace", "RootNamespace", "AssemblyName", "PackageId");

        Assert.AreEqual("MyLib", props["ProjectNamespace"], "ProjectNamespace");
        Assert.AreEqual("MyLib", props["RootNamespace"], "RootNamespace");
        Assert.AreEqual("MyLib", props["AssemblyName"], "AssemblyName");
        Assert.AreEqual("MyLib", props["PackageId"], "PackageId");
    }
}
