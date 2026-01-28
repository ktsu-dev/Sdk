; Shipped analyzer releases
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 2.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
KTSU0001 | ktsu.Sdk | Error | Missing required package reference
KTSU0002 | ktsu.Sdk | Error | Missing InternalsVisibleTo attribute for test project
KTSU0003 | ktsu.Sdk | Error | Use Ensure.NotNull over ArgumentNullException.ThrowIfNull for framework compatibility
KTSU0004 | ktsu.Sdk | Error | Use Ensure.NotNull instead of manual null check with ArgumentNullException
