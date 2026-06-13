; Unshipped analyzer releases
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
KTSU0005 | ktsu.Sdk | Error | Orphaned PackageVersion entry in Directory.Packages.props
KTSU0006 | ktsu.Sdk | Error | Transitive package used directly without a PackageReference
