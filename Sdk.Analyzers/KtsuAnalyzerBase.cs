// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.Sdk.Analyzers;

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
}
