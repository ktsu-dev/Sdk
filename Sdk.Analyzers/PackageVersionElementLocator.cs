// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

/// <summary>
/// Locates <c>PackageVersion</c> elements in the text of a <c>Directory.Packages.props</c> file.
/// </summary>
/// <remarks>
/// The orphan list is produced by <c>XmlPeek</c> over the parsed XML, so any valid layout of the
/// element can be reported: attributes wrapped across lines, an element with a body, or a live entry
/// that follows a commented-out copy. A line-by-line substring test handles only the single-line
/// layout, and deleting one line of a multi-line element leaves the file malformed. This type reads
/// the file as XML so the whole element is found and comments are skipped. It uses only the BCL and
/// no Roslyn types, so it can be unit tested directly.
/// </remarks>
internal static class PackageVersionElementLocator
{
	private const string PackageVersionElementName = "PackageVersion";
	private const string IncludeAttributeName = "Include";

	/// <summary>
	/// Finds every live (not commented-out) <c>PackageVersion</c> element whose <c>Include</c> names
	/// <paramref name="packageId"/>.
	/// </summary>
	/// <param name="text">The file content.</param>
	/// <param name="packageId">The package identifier, compared case-insensitively as NuGet does.</param>
	/// <param name="elements">
	/// The matching elements as (start, length) spans from the opening <c>&lt;</c> to the closing
	/// <c>&gt;</c>, in document order.
	/// </param>
	/// <returns><see langword="false"/> when the text is not well-formed XML.</returns>
	public static bool TryFindElements(string text, string packageId, out IReadOnlyList<(int Start, int Length)> elements)
	{
		List<(int Start, int Length)> found = [];
		elements = found;

		int[] lineStarts = ComputeLineStarts(text);
		XmlReaderSettings settings = new()
		{
			DtdProcessing = DtdProcessing.Prohibit,
			XmlResolver = null,
			IgnoreComments = true,
			IgnoreProcessingInstructions = true,
		};

		try
		{
			using StringReader stringReader = new(text);
			using XmlReader reader = XmlReader.Create(stringReader, settings);
			IXmlLineInfo lineInfo = (IXmlLineInfo)reader;

			int matchStart = -1;
			int matchDepth = -1;

			while (reader.Read())
			{
				bool located = matchStart < 0
					? TryStartMatch(reader, lineInfo, text, lineStarts, packageId, found, ref matchStart, ref matchDepth)
					: TryEndMatch(reader, lineInfo, text, lineStarts, matchDepth, found, ref matchStart);

				if (!located)
				{
					found.Clear();
					return false;
				}
			}
		}
		catch (XmlException)
		{
			found.Clear();
			return false;
		}

		return true;
	}

	/// <summary>
	/// Finds the text to delete to remove the <c>PackageVersion</c> for <paramref name="packageId"/>.
	/// </summary>
	/// <param name="text">The file content.</param>
	/// <param name="packageId">The package identifier.</param>
	/// <param name="start">The start of the span to delete.</param>
	/// <param name="length">The length of the span to delete.</param>
	/// <returns>
	/// <see langword="true"/> only when the file is well-formed and exactly one live element matches.
	/// Anything else is ambiguous, and a partial or wrong edit is worse than no edit.
	/// </returns>
	/// <remarks>
	/// When the element is alone on its lines, the whole lines are removed, including the indentation
	/// before it and the line break after it, so no blank line is left behind.
	/// </remarks>
	public static bool TryFindRemovalSpan(string text, string packageId, out int start, out int length)
	{
		start = 0;
		length = 0;

		if (!TryFindElements(text, packageId, out IReadOnlyList<(int Start, int Length)> elements) || elements.Count != 1)
		{
			return false;
		}

		(start, length) = ExtendToWholeLines(text, elements[0].Start, elements[0].Start + elements[0].Length);
		return true;
	}

	/// <summary>
	/// Widens <c>[start, end)</c> to whole lines, including the indentation before it and the line
	/// break after it, when nothing else shares those lines. Otherwise returns it unchanged.
	/// </summary>
	private static (int Start, int Length) ExtendToWholeLines(string text, int start, int end)
	{
		int lineStart = start;
		while (lineStart > 0 && IsInlineWhitespace(text[lineStart - 1]))
		{
			lineStart--;
		}

		int lineEnd = end;
		while (lineEnd < text.Length && IsInlineWhitespace(text[lineEnd]))
		{
			lineEnd++;
		}

		bool aloneAtStart = lineStart == 0 || text[lineStart - 1] is '\n' or '\r';
		bool aloneAtEnd = lineEnd == text.Length || text[lineEnd] is '\n' or '\r';
		if (!aloneAtStart || !aloneAtEnd)
		{
			return (start, end - start);
		}

		if (lineEnd < text.Length && text[lineEnd] == '\r')
		{
			lineEnd++;
		}

		if (lineEnd < text.Length && text[lineEnd] == '\n')
		{
			lineEnd++;
		}

		return (lineStart, lineEnd - lineStart);
	}

	/// <summary>
	/// Handles a node while no element is open. A matching empty element is recorded at once; a
	/// matching element with a body is held open until its end tag.
	/// </summary>
	/// <returns><see langword="false"/> when a matching element's tag cannot be delimited.</returns>
	private static bool TryStartMatch(
		XmlReader reader,
		IXmlLineInfo lineInfo,
		string text,
		int[] lineStarts,
		string packageId,
		List<(int Start, int Length)> found,
		ref int matchStart,
		ref int matchDepth)
	{
		if (reader.NodeType != XmlNodeType.Element
			|| !string.Equals(reader.LocalName, PackageVersionElementName, StringComparison.OrdinalIgnoreCase)
			|| !IncludeNames(reader, packageId))
		{
			return true;
		}

		// Line info for an element points at the first character of its name.
		int start = ToOffset(lineStarts, lineInfo.LineNumber, lineInfo.LinePosition) - 1;
		int openTagEnd = FindTagEnd(text, start);
		if (openTagEnd < 0)
		{
			return false;
		}

		if (reader.IsEmptyElement)
		{
			found.Add((start, openTagEnd - start));
		}
		else
		{
			matchStart = start;
			matchDepth = reader.Depth;
		}

		return true;
	}

	/// <summary>
	/// Handles a node while a matching element is open, recording it when its end tag is reached.
	/// </summary>
	/// <returns><see langword="false"/> when the end tag cannot be delimited.</returns>
	private static bool TryEndMatch(
		XmlReader reader,
		IXmlLineInfo lineInfo,
		string text,
		int[] lineStarts,
		int matchDepth,
		List<(int Start, int Length)> found,
		ref int matchStart)
	{
		if (reader.NodeType != XmlNodeType.EndElement || reader.Depth != matchDepth)
		{
			return true;
		}

		// Line info for an end element points at the first character of its name, after "</".
		int endTagStart = ToOffset(lineStarts, lineInfo.LineNumber, lineInfo.LinePosition) - 2;
		int endTagEnd = FindTagEnd(text, endTagStart);
		if (endTagEnd < 0)
		{
			return false;
		}

		found.Add((matchStart, endTagEnd - matchStart));
		matchStart = -1;
		return true;
	}

	private static bool IncludeNames(XmlReader reader, string packageId)
	{
		for (bool more = reader.MoveToFirstAttribute(); more; more = reader.MoveToNextAttribute())
		{
			if (string.Equals(reader.LocalName, IncludeAttributeName, StringComparison.OrdinalIgnoreCase))
			{
				bool matches = string.Equals(reader.Value.Trim(), packageId, StringComparison.OrdinalIgnoreCase);
				_ = reader.MoveToElement();
				return matches;
			}
		}

		_ = reader.MoveToElement();
		return false;
	}

	/// <summary>
	/// Returns the offset just past the <c>&gt;</c> that closes the tag starting at
	/// <paramref name="tagStart"/>, skipping any <c>&gt;</c> inside a quoted attribute value.
	/// </summary>
	private static int FindTagEnd(string text, int tagStart)
	{
		if (tagStart < 0 || tagStart >= text.Length || text[tagStart] != '<')
		{
			return -1;
		}

		char quote = '\0';
		for (int i = tagStart + 1; i < text.Length; i++)
		{
			char c = text[i];
			if (quote != '\0')
			{
				if (c == quote)
				{
					quote = '\0';
				}
			}
			else if (c is '"' or '\'')
			{
				quote = c;
			}
			else if (c == '>')
			{
				return i + 1;
			}
		}

		return -1;
	}

	/// <summary>
	/// Computes the offset of each line's first character, breaking lines the way the XML reader
	/// counts them: at CRLF, CR and LF.
	/// </summary>
	private static int[] ComputeLineStarts(string text)
	{
		List<int> starts = [0];
		int i = 0;
		while (i < text.Length)
		{
			char c = text[i];
			i++;

			if (c == '\r' && i < text.Length && text[i] == '\n')
			{
				i++;
			}

			if (c is '\r' or '\n')
			{
				starts.Add(i);
			}
		}

		return [.. starts];
	}

	private static int ToOffset(int[] lineStarts, int lineNumber, int linePosition) =>
		lineNumber < 1 || lineNumber > lineStarts.Length
			? -1
			: lineStarts[lineNumber - 1] + linePosition - 1;

	private static bool IsInlineWhitespace(char c) => c is ' ' or '\t';
}
