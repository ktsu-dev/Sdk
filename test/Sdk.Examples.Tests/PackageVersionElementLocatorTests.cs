namespace Sdk.Examples.Tests;

using System.Xml.Linq;
using ktsu.Sdk.Analyzers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit tests for <see cref="PackageVersionElementLocator"/>, which the KTSU0005 analyzer and code
/// fix use to find an orphaned <c>PackageVersion</c> element.
/// </summary>
/// <remarks>
/// The fix used to delete the first line naming the package. For any layout other than a single
/// line that left <c>Directory.Packages.props</c> malformed (MSB4025 in every project), or deleted a
/// commented-out copy and left the live entry. Each layout test asserts that the edited file still
/// parses as XML and no longer declares the package.
/// </remarks>
[TestClass]
public sealed class PackageVersionElementLocatorTests
{
    private const string Header = "<Project>\n  <ItemGroup>\n";
    private const string Footer = "    <PackageVersion Include=\"Bar\" Version=\"2.0.0\" />\n  </ItemGroup>\n</Project>\n";

    [TestMethod]
    public void RemovesASingleLineElementAndItsLine()
    {
        string text = Header + "    <PackageVersion Include=\"Foo\" Version=\"1.2.3\" />\n" + Footer;

        Assert.AreEqual(Header + Footer, Remove(text, "Foo"));
    }

    [TestMethod]
    public void RemovesAnElementWhoseAttributesWrapAcrossLines()
    {
        string text = Header
            + "    <PackageVersion Include=\"Foo\"\n"
            + "                    Version=\"1.2.3\" />\n"
            + Footer;

        Assert.AreEqual(Header + Footer, Remove(text, "Foo"));
    }

    [TestMethod]
    public void RemovesAnElementWithABody()
    {
        string text = Header
            + "    <PackageVersion Include=\"Foo\">\n"
            + "      <Version>1.2.3</Version>\n"
            + "    </PackageVersion>\n"
            + Footer;

        Assert.AreEqual(Header + Footer, Remove(text, "Foo"));
    }

    [TestMethod]
    public void RemovesTheLiveEntry_NotACommentedOutCopy()
    {
        const string comment = "    <!-- <PackageVersion Include=\"Foo\" Version=\"1.0.0\" /> -->\n";
        string text = Header + comment + "    <PackageVersion Include=\"Foo\" Version=\"1.2.3\" />\n" + Footer;

        Assert.AreEqual(Header + comment + Footer, Remove(text, "Foo"));
    }

    [TestMethod]
    public void PreservesCrlfLineEndings()
    {
        string text = (Header + "    <PackageVersion Include=\"Foo\"\n        Version=\"1.2.3\" />\n" + Footer).Replace("\n", "\r\n", StringComparison.Ordinal);

        Assert.AreEqual((Header + Footer).Replace("\n", "\r\n", StringComparison.Ordinal), Remove(text, "Foo"));
    }

    [TestMethod]
    public void MatchesThePackageIdCaseInsensitively_AndEitherQuoteStyle()
    {
        string text = Header + "    <PackageVersion Include='foo' Version='1.2.3' />\n" + Footer;

        Assert.AreEqual(Header + Footer, Remove(text, "Foo"));
    }

    [TestMethod]
    public void IgnoresAGreaterThanInsideAnAttributeValue()
    {
        string text = Header + "    <PackageVersion Include=\"Foo\" Version=\"1.2.3\" Condition=\"'$(X)' > '1'\" />\n" + Footer;

        Assert.AreEqual(Header + Footer, Remove(text, "Foo"));
    }

    [TestMethod]
    public void RemovesOnlyTheElement_WhenItSharesItsLine()
    {
        string text = Header + "    <PackageVersion Include=\"Foo\" Version=\"1.2.3\" /><PackageVersion Include=\"Baz\" Version=\"3.0.0\" />\n" + Footer;

        Assert.AreEqual(Header + "    <PackageVersion Include=\"Baz\" Version=\"3.0.0\" />\n" + Footer, Remove(text, "Foo"));
    }

    [TestMethod]
    public void DoesNotMatchAPackageWhoseIdOnlySharesAPrefix()
    {
        string text = Header + "    <PackageVersion Include=\"Foo.Extensions\" Version=\"1.2.3\" />\n" + Footer;

        Assert.IsFalse(PackageVersionElementLocator.TryFindRemovalSpan(text, "Foo", out _, out _));
    }

    /// <summary>Two live entries are ambiguous, so no edit is offered rather than a guess.</summary>
    [TestMethod]
    public void OffersNoRemoval_WhenThePackageIsDeclaredTwice()
    {
        string text = Header
            + "    <PackageVersion Include=\"Foo\" Version=\"1.2.3\" />\n"
            + "    <PackageVersion Include=\"Foo\" Version=\"1.2.4\" />\n"
            + Footer;

        Assert.IsFalse(PackageVersionElementLocator.TryFindRemovalSpan(text, "Foo", out _, out _));
    }

    [TestMethod]
    public void OffersNoRemoval_WhenTheFileIsNotWellFormed()
    {
        string text = Header + "    <PackageVersion Include=\"Foo\" Version=\"1.2.3\" />\n  </ItemGroup>\n";

        Assert.IsFalse(PackageVersionElementLocator.TryFindRemovalSpan(text, "Foo", out _, out _));
        Assert.IsFalse(PackageVersionElementLocator.TryFindElements(text, "Foo", out _));
    }

    /// <summary>
    /// The analyzer anchors its diagnostic to the element, so a layout with <c>PackageVersion</c>
    /// and <c>Include</c> on different lines must be found, where the line test dropped it.
    /// </summary>
    [TestMethod]
    public void FindsAnElementWhoseIncludeIsOnALaterLine()
    {
        const string element = "<PackageVersion\n      Include=\"Foo\"\n      Version=\"1.2.3\" />";
        string text = Header + "    " + element + "\n" + Footer;

        Assert.IsTrue(PackageVersionElementLocator.TryFindElements(text, "Foo", out IReadOnlyList<(int Start, int Length)> elements));
        Assert.AreEqual(1, elements.Count);
        Assert.AreEqual(element, text.Substring(elements[0].Start, elements[0].Length));
    }

    private static string Remove(string text, string packageId)
    {
        Assert.IsTrue(
            PackageVersionElementLocator.TryFindRemovalSpan(text, packageId, out int start, out int length),
            "Expected the element to be located.");

        string result = text.Remove(start, length);

        XDocument document = XDocument.Parse(result);
        Assert.IsFalse(
            document.Descendants("PackageVersion").Any(e => string.Equals((string?)e.Attribute("Include"), packageId, StringComparison.OrdinalIgnoreCase)),
            $"'{packageId}' is still declared:{Environment.NewLine}{result}");

        return result;
    }
}
