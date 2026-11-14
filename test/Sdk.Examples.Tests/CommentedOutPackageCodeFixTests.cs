namespace Sdk.Examples.Tests;

using ktsu.Sdk.Analyzers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The KTSU0007 and KTSU0008 code fixes, and the diagnostic location both rules report, when a
/// commented-out copy of the package line sits above the live one (ktsu-dev/Sdk#108).
/// </summary>
/// <remarks>
/// Each used to take the first line naming the package, which was the comment. The fix then edited
/// the comment, left the live element unchanged, and the diagnostic came back on the next build.
/// </remarks>
[TestClass]
public sealed class CommentedOutPackageCodeFixTests
{
    private const string ProjectWithCommentedReference =
        "<Project Sdk=\"ktsu.Sdk\">\n"
        + "  <ItemGroup>\n"
        + "    <!-- <PackageReference Include=\"Polyfill\" Version=\"1.0.0\" /> -->\n"
        + "    <PackageReference Include=\"Polyfill\" Version=\"2.0.0\" />\n"
        + "  </ItemGroup>\n"
        + "</Project>\n";

    private const string PropsWithCommentedVersion =
        "<Project>\n"
        + "  <ItemGroup>\n"
        + "    <!-- <PackageVersion Include=\"System.Text.Json\" Version=\"8.0.5\" /> -->\n"
        + "    <PackageVersion Include=\"System.Text.Json\" Version=\"6.0.0\" />\n"
        + "  </ItemGroup>\n"
        + "</Project>\n";

    [TestMethod]
    public void Ktsu0007FixMakesTheLiveReferencePrivate_NotTheCommentedOutCopy()
    {
        SourceText? fixedText = NonPrivateBuildTimePackageCodeFixProvider.MakeReferencePrivate(
            SourceText.From(ProjectWithCommentedReference), "Polyfill");

        Assert.IsNotNull(fixedText);
        Assert.AreEqual(
            ProjectWithCommentedReference.Replace(
                "<PackageReference Include=\"Polyfill\" Version=\"2.0.0\" />",
                "<PackageReference Include=\"Polyfill\" Version=\"2.0.0\" PrivateAssets=\"all\" />"),
            fixedText.ToString());
    }

    [TestMethod]
    public void Ktsu0008FixPinsTheLivePackageVersion_NotTheCommentedOutCopy()
    {
        SourceText fixedText = FrameworkOverridingPackageCodeFixProvider.SetVersionAttribute(
            SourceText.From(PropsWithCommentedVersion), "PackageVersion", "System.Text.Json", "10.0.0", out bool changed);

        Assert.IsTrue(changed);
        Assert.AreEqual(
            PropsWithCommentedVersion.Replace(
                "<PackageVersion Include=\"System.Text.Json\" Version=\"6.0.0\" />",
                "<PackageVersion Include=\"System.Text.Json\" Version=\"10.0.0\" />"),
            fixedText.ToString());
    }

    [TestMethod]
    public void Ktsu0008FixAddsNoWarnToTheLiveReference_NotTheCommentedOutCopy()
    {
        SourceText fixedText = FrameworkOverridingPackageCodeFixProvider.AddNoWarn(
            SourceText.From(ProjectWithCommentedReference), "Polyfill", "NU1510");

        Assert.AreEqual(
            ProjectWithCommentedReference.Replace(
                "<PackageReference Include=\"Polyfill\" Version=\"2.0.0\" />",
                "<PackageReference Include=\"Polyfill\" Version=\"2.0.0\" NoWarn=\"NU1510\"/>"),
            fixedText.ToString());
    }

    [TestMethod]
    public void DiagnosticLineIsTheLiveDeclaration_NotTheCommentedOutCopy()
    {
        SourceText text = SourceText.From(ProjectWithCommentedReference);

        TextLine? line = BuildFileLookup.DeclaringLine(text, "PackageReference", "Polyfill");

        Assert.AreEqual<int?>(3, line?.LineNumber);
    }

    [TestMethod]
    public void ACommentedOutCopyAloneDeclaresNothing()
    {
        SourceText text = SourceText.From(
            "<Project>\n  <ItemGroup>\n    <!-- <PackageReference Include=\"Polyfill\" /> -->\n  </ItemGroup>\n</Project>\n");

        Assert.IsNull(BuildFileLookup.DeclaringLine(text, "PackageReference", "Polyfill"));
        Assert.IsNull(NonPrivateBuildTimePackageCodeFixProvider.MakeReferencePrivate(text, "Polyfill"));
    }

    [TestMethod]
    public void AFileThatIsNotWellFormedXmlStillFindsTheFirstMatchingLine()
    {
        SourceText text = SourceText.From(
            "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"Polyfill\" Version=\"2.0.0\" />\n  </ItemGroup>\n");

        TextLine? line = BuildFileLookup.DeclaringLine(text, "PackageReference", "Polyfill");

        Assert.AreEqual<int?>(2, line?.LineNumber);
    }
}
