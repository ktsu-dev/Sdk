namespace Sdk.Examples.Tests;

using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Guards how <c>AUTHORS.md</c> becomes the published <c>Authors</c> (the nuspec's
/// <c>&lt;authors&gt;</c>) and <c>Company</c> (<c>AssemblyCompanyAttribute</c>). The SDK used to
/// publish the file whole, so a conventional heading-over-contributor-list file put the markdown and
/// every contributor's email address on nuget.org, where a published package can't be taken back.
/// See ktsu-dev/Sdk#90. The <c>AuthorsNamespace</c> derived from the same file is covered by
/// <see cref="AuthorsNamespaceTests"/>.
/// </summary>
[TestClass]
public sealed class AuthorsMetadataTests
{
    private static readonly string HeadingOverContributorList =
        "# Authors" + Environment.NewLine +
        Environment.NewLine +
        "- Jane Doe <jane@example.com>" + Environment.NewLine +
        "- John Smith <john@example.com>" + Environment.NewLine;

    /// <summary>
    /// The heading, the list markers and the email addresses are dropped, and the names are joined
    /// with NuGet's separator. Company reads the same names as a display string.
    /// </summary>
    [TestMethod]
    public void HeadingOverContributorList_PublishesOnlyTheNames() =>
        AssertAuthors(HeadingOverContributorList, "Jane Doe;John Smith", "Jane Doe, John Smith");

    /// <summary>
    /// The other list markers markdown allows are dropped the same way, and a list item without an
    /// email keeps its name.
    /// </summary>
    [TestMethod]
    public void OtherListMarkers_AreDroppedToo() =>
        AssertAuthors(
            "## Maintainers" + Environment.NewLine +
            "* Jane Doe <jane@example.com>" + Environment.NewLine +
            "+ John Smith" + Environment.NewLine +
            "1. Ada Lovelace <ada@example.com>" + Environment.NewLine +
            "2) Alan Turing" + Environment.NewLine,
            "Jane Doe;John Smith;Ada Lovelace;Alan Turing",
            "Jane Doe, John Smith, Ada Lovelace, Alan Turing");

    /// <summary>The ktsu convention, a single line, comes through exactly as written.</summary>
    [TestMethod]
    public void SingleLine_IsPublishedAsWritten() =>
        AssertAuthors("ktsu.dev contributors" + Environment.NewLine, "ktsu.dev contributors", "ktsu.dev contributors");

    /// <summary>
    /// Punctuation that belongs to a name is not list syntax: a hyphenated or numbered company
    /// name on one line stays whole.
    /// </summary>
    [TestMethod]
    public void SingleLineWithPunctuation_IsPublishedAsWritten() =>
        AssertAuthors("Contoso-Fabrikam, Inc." + Environment.NewLine, "Contoso-Fabrikam, Inc.", "Contoso-Fabrikam, Inc.");

    /// <summary>
    /// With no <c>AUTHORS.md</c>, an <c>Authors</c> the consumer set in <c>Directory.Build.props</c>
    /// is theirs and is not rewritten.
    /// </summary>
    [TestMethod]
    public void AuthorsSetInDirectoryBuildProps_WithoutAuthorsFile_IsLeftAlone()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));
        workspace.WriteFile("Directory.Build.props",
            """
            <Project>
              <PropertyGroup>
                <Authors>Jane Doe &lt;jane@example.com&gt;</Authors>
              </PropertyGroup>
            </Project>
            """);

        IReadOnlyDictionary<string, string> props = workspace.Evaluate("Testing/Testing.csproj", "Authors");

        Assert.AreEqual("Jane Doe <jane@example.com>", props["Authors"], "Authors");
    }

    /// <summary>
    /// End to end: what nuget.org would show, and what the assembly carries, for the conventional
    /// heading-over-contributor-list file. Neither may contain the markdown or an email address.
    /// </summary>
    [TestMethod]
    public void Pack_HeadingOverContributorList_WritesOnlyTheNames()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("PackageData"));
        workspace.WriteFile("AUTHORS.md", HeadingOverContributorList);

        (CliResult result, string outputDir) = workspace.Pack("Alpha/Alpha.csproj");
        Assert.IsTrue(result.Succeeded, $"Expected the pack to succeed.{Environment.NewLine}{result.Output}");

        string nupkg = Directory.EnumerateFiles(outputDir, "*.nupkg")
            .Single(f => !f.EndsWith(".symbols.nupkg", StringComparison.Ordinal));
        string nuspecEntry = ExampleWorkspace.NupkgEntries(nupkg)
            .Single(e => e.EndsWith(".nuspec", StringComparison.Ordinal));
        XDocument nuspec = XDocument.Parse(ExampleWorkspace.ReadNupkgEntry(nupkg, nuspecEntry));
        XElement? authors = nuspec.Descendants().FirstOrDefault(e => e.Name.LocalName == "authors");

        Assert.IsNotNull(authors, $"Packed nuspec has no <authors>.{Environment.NewLine}{nuspec}");
        Assert.AreEqual("Jane Doe,John Smith", authors.Value, "nuspec <authors>");

        string assemblyInfo = Directory.EnumerateFiles(Path.Combine(workspace.Root, "Alpha", "obj"), "*.AssemblyInfo.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .Single(text => text.Contains("AssemblyCompanyAttribute", StringComparison.Ordinal));

        StringAssert.Contains(assemblyInfo, "AssemblyCompanyAttribute(\"Jane Doe, John Smith\")", assemblyInfo);
        Assert.IsFalse(assemblyInfo.Contains("example.com", StringComparison.Ordinal), assemblyInfo);
    }

    private static void AssertAuthors(string authorsFileContent, string expectedAuthors, string expectedCompany)
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Namespaces"));
        workspace.WriteFile("AUTHORS.md", authorsFileContent);

        IReadOnlyDictionary<string, string> props = workspace.Evaluate("Testing/Testing.csproj", "Authors", "Company");

        Assert.AreEqual(expectedAuthors, props["Authors"], "Authors");
        Assert.AreEqual(expectedCompany, props["Company"], "Company");
    }
}
