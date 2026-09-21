namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Guards the <c>AuthorsNamespace</c> derivation in <c>Sdk.props</c>, which turns the prose in
/// <c>AUTHORS.md</c> into the organisation prefix on <c>RootNamespace</c>, <c>AssemblyName</c> and
/// <c>PackageId</c>. Every ktsu repository carries a single-line <c>AUTHORS.md</c> reading
/// "ktsu.dev contributors", so the cases that break - a markdown heading, a contributor list, a
/// comma in a company name - only ever appeared outside the organisation, and appeared as an
/// MSB3030 in the copy step or an NU5123 at pack time rather than as anything naming AUTHORS.md.
/// </summary>
[TestClass]
public sealed class AuthorsNamespaceTests
{
    /// <summary>The ktsu convention itself: the prefix has to keep coming out as <c>ktsu</c>.</summary>
    [TestMethod]
    public void Authors_WithKtsuConvention_YieldsTheOrganisationPrefix() =>
        AssertIdentity("ktsu.dev contributors" + Environment.NewLine, "ktsu", "ktsu.Testing");

    /// <summary>
    /// A comma and a trailing period are legal in a company name and illegal in a package ID.
    /// Before the fix this built clean as <c>Contoso,Inc.Testing</c> and only failed at
    /// <c>dotnet pack</c> with NU5123.
    /// </summary>
    [TestMethod]
    public void Authors_WithPunctuation_HasThePunctuationRemoved() =>
        AssertIdentity("Contoso, Inc." + Environment.NewLine, "ContosoInc", "ContosoInc.Testing");

    /// <summary>An apostrophe is dropped rather than carried into the identifier.</summary>
    [TestMethod]
    public void Authors_WithApostrophe_HasTheApostropheRemoved() =>
        AssertIdentity("O'Reilly Media" + Environment.NewLine, "OReillyMedia", "OReillyMedia.Testing");

    /// <summary>An ampersand is dropped the same way, along with the spaces around it.</summary>
    [TestMethod]
    public void Authors_WithAmpersand_HasTheAmpersandRemoved() =>
        AssertIdentity("Foo & Bar Ltd" + Environment.NewLine, "FooBarLtd", "FooBarLtd.Testing");

    /// <summary>
    /// A name starting with a digit cannot begin a C# identifier, so the prefix is dropped
    /// entirely rather than mangled into something that compiles by accident.
    /// </summary>
    [TestMethod]
    public void Authors_StartingWithADigit_YieldsNoPrefix() =>
        AssertIdentity("3M Company" + Environment.NewLine, string.Empty, "Testing");

    /// <summary>
    /// The conventional shape of an <c>AUTHORS.md</c>: a heading over a contributor list. The
    /// heading is skipped and the first real line is a person, which yields nothing usable as an
    /// organisation prefix - so the project keeps its own name. Before the fix the whole file,
    /// heading and newlines included, became the assembly name and the build died in the copy step.
    /// </summary>
    [TestMethod]
    public void Authors_AsHeadingOverContributorList_YieldsNoPrefix() =>
        AssertIdentity(
            "# Authors" + Environment.NewLine +
            Environment.NewLine +
            "- Jane Doe <jane@example.com>" + Environment.NewLine +
            "- John Smith <john@example.com>" + Environment.NewLine,
            string.Empty,
            "Testing");

    /// <summary>
    /// A heading over a single organisation name still yields that organisation: the heading is
    /// skipped, not the whole file.
    /// </summary>
    [TestMethod]
    public void Authors_AsHeadingOverSingleOrganisation_YieldsThatOrganisation() =>
        AssertIdentity(
            "# Authors" + Environment.NewLine +
            Environment.NewLine +
            "Contoso Ltd" + Environment.NewLine,
            "ContosoLtd",
            "ContosoLtd.Testing");

    /// <summary>A repository with no <c>AUTHORS.md</c> at all builds with no prefix.</summary>
    [TestMethod]
    public void Authors_Absent_YieldsNoPrefix()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));

        AssertIdentity(workspace, string.Empty, "Testing");
    }

    /// <summary>
    /// The supported escape hatch for a repository whose <c>AUTHORS.md</c> cannot yield a sensible
    /// prefix: name it in <c>Directory.Build.props</c>, which is evaluated before the SDK.
    /// </summary>
    [TestMethod]
    public void AuthorsNamespace_SetInDirectoryBuildProps_WinsOverTheDerivedValue()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));
        workspace.WriteFile("AUTHORS.md", "Contoso, Inc." + Environment.NewLine);
        WriteDirectoryBuildProps(workspace, "<AuthorsNamespace>Contoso</AuthorsNamespace>");

        AssertIdentity(workspace, "Contoso", "Contoso.Testing");
    }

    /// <summary>
    /// The override the README advertises. It used to move the namespace and leave
    /// <c>AssemblyName</c> and <c>PackageId</c> on the derived value, because
    /// <c>Microsoft.NET.Sdk.props</c> has already defaulted <c>RootNamespace</c> to the project
    /// name by the time this SDK runs, so the old <c>== ''</c> guard never held.
    /// </summary>
    [TestMethod]
    public void RootNamespace_SetInDirectoryBuildProps_AlsoMovesAssemblyNameAndPackageId()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));
        workspace.WriteFile("AUTHORS.md", "Contoso, Inc." + Environment.NewLine);
        WriteDirectoryBuildProps(workspace, "<RootNamespace>Contoso.Widgets</RootNamespace>");

        IReadOnlyDictionary<string, string> props = Evaluate(workspace);

        Assert.AreEqual("Contoso.Widgets", props["RootNamespace"], "RootNamespace");
        Assert.AreEqual("Contoso.Widgets", props["AssemblyName"], "AssemblyName");
        Assert.AreEqual("Contoso.Widgets", props["PackageId"], "PackageId");
    }

    /// <summary>
    /// <c>AssemblyName</c> can be named on its own, without moving the namespace with it.
    /// </summary>
    [TestMethod]
    public void AssemblyName_SetInDirectoryBuildProps_IsNotOverwritten()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));
        workspace.WriteFile("AUTHORS.md", "ktsu.dev contributors" + Environment.NewLine);
        WriteDirectoryBuildProps(workspace, "<AssemblyName>Contoso.Widgets</AssemblyName>");

        IReadOnlyDictionary<string, string> props = Evaluate(workspace);

        Assert.AreEqual("Contoso.Widgets", props["AssemblyName"], "AssemblyName");
    }

    private static void AssertIdentity(string authorsFileContent, string expectedNamespace, string expectedIdentity)
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));
        workspace.WriteFile("AUTHORS.md", authorsFileContent);

        AssertIdentity(workspace, expectedNamespace, expectedIdentity);
    }

    private static void AssertIdentity(ExampleWorkspace workspace, string expectedNamespace, string expectedIdentity)
    {
        IReadOnlyDictionary<string, string> props = Evaluate(workspace);

        Assert.AreEqual(expectedNamespace, props["AuthorsNamespace"], "AuthorsNamespace");
        Assert.AreEqual(expectedIdentity, props["AssemblyName"], "AssemblyName");
        Assert.AreEqual(expectedIdentity, props["PackageId"], "PackageId");
    }

    private static IReadOnlyDictionary<string, string> Evaluate(ExampleWorkspace workspace) =>
        workspace.Evaluate(
            "Testing/Testing.csproj", "AuthorsNamespace", "RootNamespace", "AssemblyName", "PackageId");

    private static void WriteDirectoryBuildProps(ExampleWorkspace workspace, string property) =>
        workspace.WriteFile("Directory.Build.props",
            $"""
            <Project>
              <PropertyGroup>
                {property}
              </PropertyGroup>
            </Project>
            """);
}
