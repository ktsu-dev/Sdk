// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Base class for all ktsu.Sdk analyzers
/// </summary>
public abstract class KtsuAnalyzerBase : DiagnosticAnalyzer
{

	/// <summary>
	/// Category for ktsu.Sdk analyzers
	/// </summary>
	protected const string Category = "ktsu.Sdk";

	/// <summary>
	/// Builds a rule descriptor carrying the settings every ktsu.Sdk rule shares.
	/// </summary>
	/// <param name="id">The diagnostic identifier.</param>
	/// <param name="title">The rule's title.</param>
	/// <param name="messageFormat">The reported message's format string.</param>
	/// <param name="description">The rule's longer description.</param>
	/// <param name="reportedAtCompilationEnd">
	/// <see langword="true"/> for a rule reported from a compilation action rather than from a
	/// syntax node, which needs the <c>CompilationEnd</c> tag so the compiler does not skip it when
	/// replaying cached results.
	/// </param>
	/// <returns>The descriptor.</returns>
	/// <remarks>
	/// Every rule here is an error, enabled by default, and in the one category. Those four facts
	/// were previously spelled out in each analyzer, which made them look like per-rule choices
	/// rather than the house rule they are - and left six identical eight-argument constructions
	/// for a reader to diff by eye. Stating them once makes the invariant the thing that is written
	/// down, and leaves each analyzer declaring only what is actually its own.
	/// </remarks>
	protected static DiagnosticDescriptor CreateRule(
		string id,
		LocalizableString title,
		LocalizableString messageFormat,
		LocalizableString description,
		bool reportedAtCompilationEnd = true) =>
		new(
			id,
			title,
			messageFormat,
			Category,
			DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: description,
			customTags: reportedAtCompilationEnd ? ["CompilationEnd"] : []);
}
