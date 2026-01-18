using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ktsu.Sdk.Analyzers;

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
