namespace Sdk.Examples.Tests;

using ktsu.Sdk.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Unit coverage for the pure parts of the style-config sync task.
/// </summary>
/// <remarks>
/// This logic used to live in an inline <c>RoslynCodeTaskFactory</c> fragment inside
/// <c>Sdk.targets</c>, where the only way to exercise it was a full end-to-end build of an example
/// workspace (see <see cref="StyleConfigSyncTests"/>, whose parallel-sync case alone takes about
/// fifteen minutes). Moving it into a compiled task assembly makes the edge cases directly
/// testable in milliseconds; the end-to-end tests still cover the file-system and locking
/// behaviour.
/// </remarks>
[TestClass]
public sealed class StyleConfigContentTests
{
    [TestMethod]
    public void BuildHeaderLine_EncodesNewlinesForEditorConfig()
    {
        string? line = StyleConfigContent.BuildHeaderLine("Copyright (c) contoso\nAll rights reserved.");

        Assert.AreEqual(@"file_header_template = Copyright (c) contoso\nAll rights reserved.", line);
    }

    /// <summary>A CRLF copyright file must not produce a stray <c>\r</c> in the encoded value.</summary>
    [TestMethod]
    public void BuildHeaderLine_NormalizesCarriageReturns()
    {
        string? line = StyleConfigContent.BuildHeaderLine("first\r\nsecond\rthird");

        Assert.AreEqual(@"file_header_template = first\nsecond\nthird", line);
    }

    /// <param name="copyright">A copyright value that carries no content.</param>
    [TestMethod]
    [DataRow(null, DisplayName = "null")]
    [DataRow("", DisplayName = "empty")]
    [DataRow("   \r\n  ", DisplayName = "whitespace")]
    public void BuildHeaderLine_ReturnsNull_WhenThereIsNoCopyrightText(string? copyright) =>
        Assert.IsNull(StyleConfigContent.BuildHeaderLine(copyright));

    [TestMethod]
    public void ApplyHeader_ReplacesTheAssignment_PreservingIndentAndLineEnding()
    {
        const string content = "[*]\r\n    file_header_template = old value\r\nindent_style = tab\r\n";

        string result = StyleConfigContent.ApplyHeader(content, "file_header_template = new value");

        Assert.AreEqual("[*]\r\n    file_header_template = new value\r\nindent_style = tab\r\n", result);
    }

    /// <summary>
    /// The final line of a file with no trailing newline must still be rewritten, and must not gain
    /// a terminator it did not have.
    /// </summary>
    [TestMethod]
    public void ApplyHeader_HandlesFinalLineWithoutTrailingNewline()
    {
        const string content = "[*]\nfile_header_template = old";

        string result = StyleConfigContent.ApplyHeader(content, "file_header_template = new");

        Assert.AreEqual("[*]\nfile_header_template = new", result);
    }

    [TestMethod]
    public void ApplyHeader_ReplacesEveryOccurrence()
    {
        const string content = "file_header_template = a\n[*.cs]\nfile_header_template = b\n";

        string result = StyleConfigContent.ApplyHeader(content, "file_header_template = z");

        Assert.AreEqual("file_header_template = z\n[*.cs]\nfile_header_template = z\n", result);
    }

    /// <summary>A key that merely starts with the header key is a different setting.</summary>
    [TestMethod]
    public void ApplyHeader_IgnoresKeysThatOnlyShareAPrefix()
    {
        const string content = "file_header_template_extra = keep me\n";

        string result = StyleConfigContent.ApplyHeader(content, "file_header_template = new");

        Assert.AreEqual(content, result);
    }

    [TestMethod]
    public void ApplyHeader_LeavesContentAlone_WhenThereIsNoHeaderLine()
    {
        const string content = "file_header_template = keep me\n";

        Assert.AreEqual(content, StyleConfigContent.ApplyHeader(content, headerLine: null));
    }

    [TestMethod]
    public void BuildScopeKey_FoldsPathCharactersAndLowercases()
    {
        string key = StyleConfigContent.BuildScopeKey(@"C:\dev\My-Solution\");

        Assert.AreEqual("c__dev_my_solution_", key);
    }

    /// <summary>
    /// A long path is trimmed from the left, so the most specific part of the path is what
    /// distinguishes two scopes.
    /// </summary>
    [TestMethod]
    public void BuildScopeKey_TrimsLongPathsFromTheLeft()
    {
        string scope = new string('a', 200) + "distinctive";

        string key = StyleConfigContent.BuildScopeKey(scope);

        Assert.AreEqual(180, key.Length);
        StringAssert.EndsWith(key, "distinctive");
    }

    /// <summary>Distinct solution directories must not share a lock.</summary>
    [TestMethod]
    public void BuildMutexName_DiffersPerScope()
    {
        string first = StyleConfigContent.BuildMutexName(@"C:\dev\one");
        string second = StyleConfigContent.BuildMutexName(@"C:\dev\two");

        StringAssert.StartsWith(first, @"Global\ktsu-sdk-style-sync-");
        Assert.AreNotEqual(first, second);
    }

    /// <summary>Casing differences in a path must not produce two locks for one directory.</summary>
    [TestMethod]
    public void BuildMutexName_IsCaseInsensitive() =>
        Assert.AreEqual(
            StyleConfigContent.BuildMutexName(@"C:\Dev\Solution"),
            StyleConfigContent.BuildMutexName(@"c:\dev\solution"));
}
