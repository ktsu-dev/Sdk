# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This is an MSBuild SDK package (`ktsu.Sdk`) that provides standardized configuration, metadata management, and build workflows for .NET projects. The SDK automatically discovers solution structures, generates namespaces from directory paths, and manages project metadata through markdown files.

## Build Commands

### Building the Solution
```powershell
dotnet build --configuration Release --verbosity normal --no-incremental
```

### Testing
```powershell
dotnet test -m:1 --configuration Release --verbosity normal --no-build
```

### Packaging
```powershell
dotnet pack --configuration Release --output ./staging
```

### Publishing Applications
```powershell
dotnet publish <project>.csproj --no-build --configuration Release --framework net9.0 --output ./output/<project>
```

## Version Management

Version management is handled through PowerShell scripts in the `scripts/` directory using the PSBuild module:

- **make-version.ps1**: Calculates semantic version from git history
- **make-license.ps1**: Generates LICENSE.md from template
- **make-changelog.ps1**: Generates CHANGELOG.md from git commits
- **commit-metadata.ps1**: Commits metadata changes with proper attribution

Version calculation rules:
- `[major]` tag in commit: major version increment (breaking changes)
- `[minor]` tag or public API changes: minor version increment
- `[patch]` tag or code changes: patch version increment
- `[pre]` tag or minimal changes: prerelease increment

The PSBuild module automatically detects public API changes by analyzing diffs for modifications to public classes, interfaces, methods, properties, etc.

## Project Structure

The SDK consists of multiple sub-SDKs:

- **Sdk/**: Core SDK with MSBuild props and targets (all project types)
  - `Sdk.props`: Hierarchical solution discovery, metadata file loading, namespace generation, package configuration
  - `Sdk.targets`: Project type detection, automatic references, package inclusion logic

- **Sdk.ConsoleApp/**: Console application SDK
  - Sets `OutputType=Exe` and `TargetFramework=net9.0`

- **Sdk.App/**: GUI application SDK (ImGui/Windows apps)
  - Sets `OutputType=WinExe` on Windows, `Exe` on other platforms
  - Configures runtime identifiers for cross-platform GUI support

## Key SDK Features

### Hierarchical Solution Discovery

The SDK searches up to 5 directory levels from the project directory to find solution files. This enables nested project structures without manual configuration.

### Path-Based Namespace Generation

Namespaces are automatically generated from directory structure:
```
MySolution/src/Core/Utils/MyProject.csproj
→ ProjectNamespace: src.Core.Utils.MyProject
→ RootNamespace: {AuthorsNamespace}.src.Core.Utils.MyProject
```

The SDK intelligently handles cases where the directory name matches the project name to avoid duplication.

### Project Type Detection

The SDK automatically detects project types based on naming conventions:
- **Primary Project**: `{SolutionName}` or `{SolutionName}.Core`
- **Console Projects**: `{SolutionName}.CLI`, `{SolutionName}.ConsoleApp`, etc.
- **GUI Projects**: `{SolutionName}.App`, `{SolutionName}.WinApp`, `{SolutionName}.ImGuiApp`, etc.
- **Test Projects**: `{SolutionName}.Test`, `{SolutionName}.Tests`, etc.

Properties set based on detection: `IsPrimaryProject`, `IsCliProject`, `IsAppProject`, `IsTestProject`

### Analyzer-Enforced Requirements

The SDK uses Roslyn analyzers to enforce proper project configuration:

- **KTSU0001 (Error)**: Projects must include required standard packages (SourceLink, Polyfill, System.Memory, System.Threading.Tasks.Extensions). Requirements vary based on project type and target framework.
- **KTSU0002 (Error)**: Projects must expose internals to test projects using `[assembly: InternalsVisibleTo(...)]`. A code fixer is available to automatically add this attribute.

These properties are passed to analyzers via `CompilerVisibleProperty`: `IsTestProject`, `TestProjectExists`, `TestProjectNamespace`, `TargetFramework`, `TargetFrameworkIdentifier`.

### Metadata File Integration

The SDK reads markdown files from the solution root and uses them to populate package metadata:
- `AUTHORS.md` → Authors, AuthorsNamespace
- `VERSION.md` → Version, PackageVersion
- `DESCRIPTION.md` → Description, PackageDescription
- `CHANGELOG.md` → PackageReleaseNotes (truncated at 35KB if needed)
- `TAGS.md` → Tags, PackageTags
- `LICENSE.md` → PackageLicenseFile
- `README.md` → PackageReadmeFile
- `COPYRIGHT.md` → Copyright
- `PROJECT_URL.url` → ProjectUrl, PackageProjectUrl
- `AUTHORS.url` → AuthorsUrl
- `icon.png` → PackageIcon

All metadata files are automatically included in NuGet packages.

## Important MSBuild Properties

### Multi-Targeting
Default: `net9.0;net8.0;net7.0;net6.0;net5.0;netstandard2.0;netstandard2.1`

Individual SDK sub-projects can override `TargetFrameworks` to target a single framework.

### Code Quality
- `LangVersion=latest`
- `Nullable=enable`
- `TreatWarningsAsErrors=true`
- `AnalysisLevel=latest-all`
- `EnforceCodeStyleInBuild=true`

### Package Validation
- `EnablePackageValidation=true`
- `ApiCompatValidateAssemblies=true`
- `EnableStrictModeForBaselineValidation=true`

### Runtime Identifiers
Default RIDs: `win-x64;win-x86;win-arm64;osx-x64;linux-x64;osx-arm64;linux-arm64`

## CI/CD Workflow

The GitHub Actions workflow (`.github/workflows/dotnet-sdk.yml`) runs on:
- Push to `main` or `develop` branches
- Pull requests
- Nightly schedule (11 PM UTC)

Release process (only on main branch, non-fork):
1. Generate VERSION.md, LICENSE.md, CHANGELOG.md from git history
2. Commit metadata changes with bot attribution
3. Commit Sdk.props/Sdk.targets version updates
4. Build all projects
5. Run tests
6. Create NuGet packages
7. Publish to GitHub Packages, NuGet.org, and ktsu.dev package feeds
8. Create GitHub release with artifacts

## Common Development Tasks

### Adding a New SDK Sub-Project

1. Create directory: `Sdk.{Name}/`
2. Create `Sdk.{Name}.csproj` with appropriate `TargetFrameworks`
3. Create `Sdk.props` with project-type-specific property overrides
4. Create `Sdk.targets` if custom build logic needed
5. Package structure: SDK packages must include `Sdk/Sdk.props` and `Sdk/Sdk.targets` in the package

### Modifying Core SDK Logic

- **Solution/project discovery**: Edit `Sdk/Sdk.props` (lines 1-42)
- **Namespace generation**: Edit `Sdk/Sdk.props` (lines 237-266)
- **Project type detection**: Edit `Sdk/Sdk.props` (lines 72-187)
- **Automatic references**: Edit `Sdk/Sdk.targets` (lines 28-36)
- **Package validation**: Edit `Sdk/Sdk.props` (lines 308-318)

### Testing SDK Changes Locally

1. Build the SDK: `dotnet build --configuration Release`
2. Pack the SDK: `dotnet pack --configuration Release --output ./local-packages`
3. In consuming project, add local package source:
   ```xml
   <PropertyGroup>
     <RestoreAdditionalProjectSources>C:\dev\ktsu-dev\Sdk\local-packages</RestoreAdditionalProjectSources>
   </PropertyGroup>
   ```
4. Reference the local version in consuming project's csproj or global.json

## Architecture Notes

### MSBuild Evaluation Order

The SDK uses careful property evaluation to ensure correct values:
1. Early evaluation: Solution discovery, file path resolution
2. Mid evaluation: Metadata file reading, namespace calculation
3. Late evaluation: Derived properties (IsExecutable, IsPackable, etc.)

Properties are set conditionally to avoid overwriting user-specified values.

### Safe Array Operations

The SDK includes robust null/empty checks to prevent MSBuild failures:
- Solution file array access uses `.Split(';')[0]` with validation
- String operations check for null/empty before manipulation
- File existence validated before `File.ReadAllText()` calls

### Package Type

The core SDK sets `PackageType=MSBuildSdk` in `Directory.Build.props`, which is required for proper MSBuild SDK packaging. The `Directory.Build.targets` file includes the SDK props/targets files in the package at the correct paths.
