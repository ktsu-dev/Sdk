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
dotnet publish <project>.csproj --no-build --configuration Release --framework net10.0 --output ./output/<project>
```

## Version Management

Version management for this repository is handled by standalone PowerShell scripts in `scripts/`, each
invoked directly from `.github/workflows/dotnet-sdk.yml`:

- **make-version.ps1**: Calculates semantic version from git history
- **make-license.ps1**: Generates LICENSE.md from template
- **make-changelog.ps1**: Generates CHANGELOG.md from git commits
- **commit-metadata.ps1**: Commits metadata changes with proper attribution

Version calculation rules:
- `[major]` tag in commit: major version increment (breaking changes)
- `[minor]` tag or source file changes: minor version increment
- `[patch]` tag or docs/metadata/CI-only changes: patch version increment
- `[pre]` tag or no qualifying commits: prerelease increment

With no marker present, `make-version.ps1` picks the increment from *which* files changed in the
commit range rather than from the content of the diff: any commit touching files outside the
markdown, text, solution, project, url, build, PowerShell and CI exclusion lists gives a minor bump,
any other qualifying commit gives a patch, and neither gives a prerelease bump.

The PSBuild PowerShell module that used to drive all of this has been removed. This repository uses
the standalone scripts above, and consuming repositories build with `ktsu.KtsuBuild.Tool`
(`ktsubuild`), which replaced the module.

## Project Structure

The SDK consists of multiple sub-SDKs:

- **Sdk/**: Core SDK with MSBuild props and targets (all project types)
  - `Sdk.props`: Hierarchical solution discovery, metadata file loading, namespace generation, package configuration
  - `Sdk.targets`: Project type detection, automatic references, package inclusion logic

- **Sdk.ConsoleApp/**: Console application SDK
  - Sets `OutputType=Exe` and `TargetFramework=net10.0`

- **Sdk.App/**: GUI application SDK (ImGui/Windows apps)
  - Sets `OutputType=WinExe` on Windows, `Exe` on other platforms
  - Sets `TargetFramework=net10.0`
  - Configures runtime identifiers for cross-platform GUI support

- **Sdk.Tool/**: .NET tool SDK (`dotnet tool install`)
  - Sets `PackAsTool=true`, `OutputType=Exe`, `TargetFramework=net10.0`
  - Clears `RuntimeIdentifiers`: under `PackAsTool` the .NET 10 SDK turns each RID in the
    inherited desktop list into a separate RID-specific tool package, so one `dotnet pack`
    emits seven packages racing over a single intermediate output directory. Tools here are
    framework-dependent and RID-agnostic — consumers need the .NET 10 runtime.
  - Derives `ToolCommandName` from the lowercased solution name (stripping a trailing
    `.tool`/`.cli`), because the default would be `AssemblyName`, which the core SDK forces to
    the fully-qualified namespace (`ktsu.KtsuBuild.Tool`). Derived in `Sdk.props`, not
    `Sdk.targets`, so the value is set before Microsoft.NET.Sdk defaults it.
  - Disables package validation and `IncludeSource`, which are library-oriented
  - Sets `IsPublishable=true` **in `Sdk.props`**. `PackAsTool` builds the `tools/` payload from a
    publish, which is gated on `IsPublishable`; without it the package contains only
    `DotnetToolSettings.xml` and none of the assemblies it points at — it installs, then fails at
    run time. The core SDK's `Sdk.targets` flip (false in props, true for `OutputType=Exe` in
    targets) is too late, for the same import-ordering reason as `ToolCommandName`. Tool projects
    stay out of CI's RID zip publishing by project *selection* (KtsuBuild scans the csproj text),
    not by this property.
  - Errors (KTSU1001) if `TargetFrameworks` is set: a tool package cannot multi-target

- **Sdk.Windows/**, **Sdk.Linux/**, **Sdk.macOS/**: Desktop per-OS app SDKs
  - RID-based presets on the base `net10.0` runtime (no extra prerequisites)
  - Narrow `RuntimeIdentifiers` to the target OS and default `RuntimeIdentifier`
  - Windows uses `OutputType=WinExe`; Linux/macOS use `Exe`

- **Sdk.iOS/**, **Sdk.Android/**: Mobile app SDKs (TFM + workload based)
  - Set `TargetFramework=net10.0-ios` / `net10.0-android` plus `SupportedOSPlatformVersion`
  - Consuming projects require the `ios`/`android` workloads
    (`dotnet workload install android ios maui`); iOS additionally needs a macOS host
  - The SDK packages themselves carry no workload dependency and pack on any host

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
- **Console Projects**: `{SolutionName}.CLI`, `{SolutionName}.Cli`, `{SolutionName}Cli`, `{SolutionName}CLI`, `{SolutionName}.ConsoleApp`, `{SolutionName}.Console`
- **GUI Projects**: `{SolutionName}.App`, `{SolutionName}App`, `{SolutionName}.WinApp`, `{SolutionName}WinApp`, `{SolutionName}.ImGuiApp`, `{SolutionName}ImGuiApp`
- **iOS Projects**: `{SolutionName}.iOS`, `{SolutionName}iOS`, `{SolutionName}.Ios`
- **Android Projects**: `{SolutionName}.Android`, `{SolutionName}Android`, `{SolutionName}.Droid`
- **Windows Projects**: `{SolutionName}.Windows`, `{SolutionName}Windows`, `{SolutionName}.Win`
- **Linux Projects**: `{SolutionName}.Linux`, `{SolutionName}Linux`
- **macOS Projects**: `{SolutionName}.macOS`, `{SolutionName}.MacOS`, `{SolutionName}.Mac`
- **Tool Projects**: `{SolutionName}.Tool`, `{SolutionName}Tool` — deliberately *not* `.CLI`, so no existing console project silently starts publishing itself as a tool package
- **Test Projects**: `{SolutionName}.Test`, `{SolutionName}.Tests`, `{SolutionName}Test`, `{SolutionName}Tests`

Properties set based on detection: `IsPrimaryProject`, `IsCliProject`, `IsAppProject`, `IsToolProject`, `IsIosProject`, `IsAndroidProject`, `IsWindowsProject`, `IsLinuxProject`, `IsMacProject`, `IsTestProject`

### Analyzer-Enforced Requirements

The SDK automatically includes the `ktsu.Sdk.Analyzers` package (with version synchronization via `{version}` placeholder) to enforce proper project configuration:

- **KTSU0001 (Error)**: Projects must include required standard packages (Polyfill, System.Memory, System.Threading.Tasks.Extensions). Requirements vary based on project type and target framework. SourceLink is intentionally **not** required: the .NET 8+ SDK bundles SourceLink and enables it implicitly, and an explicit `Microsoft.SourceLink.*` `PackageReference` re-enables the noisy "Source control information is not available" warning for any build without a usable remote. Consumers should not reference SourceLink packages directly.
- **KTSU0002 (Error)**: Projects must expose internals to test projects using `[assembly: InternalsVisibleTo(...)]`. A code fixer is available to automatically add this attribute. The diagnostic is anchored to a project-owned source file (under `ProjectDir`, excluding `obj`), not to the compilation's first syntax tree. With a source-embedding package such as Polyfill the first tree is a package file marked as generated code, and a diagnostic located in generated code is discarded under `GeneratedCodeAnalysisFlags.None`. That was the real cause of the intermittent behavior tracked in ktsu-dev/Sdk#12 / #8 / #11, and it is also why the code fix now always lands in a file the user owns.
- **KTSU0003 (Error)**: Use `Ensure.NotNull()` over `ArgumentNullException.ThrowIfNull()` for better framework compatibility. A code fixer is available to automatically replace the invocation.
- **KTSU0004 (Error)**: Use `Ensure.NotNull()` instead of manual null checks with ArgumentNullException. Detects patterns like `if (x == null) throw new ArgumentNullException(...)`, `if (x is null) throw ...`, and `x ?? throw ...`. A code fixer is available.
- **KTSU0005 (Error)**: Orphaned `PackageVersion` entries. Flags `PackageVersion` entries in `Directory.Packages.props` (Central Package Management) that no project in the solution references via `PackageReference`/`GlobalPackageReference`. A code fixer removes the orphaned entry. Disable with `<KtsuEnableOrphanedPackageVersionAnalysis>false</KtsuEnableOrphanedPackageVersionAnalysis>`. An ignore list (`Sdk.targets`) keeps SDK-governed packages from being flagged even without a direct `PackageReference`: the KTSU0001 standard packages (`Polyfill`, `System.Memory`, `System.Threading.Tasks.Extensions`) and the `Microsoft.Testing.Extensions.*` runner family that test SDKs (e.g. `MSTest.Sdk`) inject into test projects (which the scan skips). Consumers can extend it via `<KtsuOrphanedPackageVersionIgnore Include="..." />`.
- **KTSU0006 (Error)**: Transitive package used directly. Flags use of a type or member that originates from a transitive package dependency when the project does not declare a direct `PackageReference` to it. A code fixer adds the `PackageReference` (and, under Central Package Management, a matching `PackageVersion`). Disable with `<KtsuEnableTransitivePackageAnalysis>false</KtsuEnableTransitivePackageAnalysis>`.
- **KTSU0007 (Error)**: Build-time package reference is not private. Requires `PrivateAssets="all"` on the `Polyfill` reference in non-test projects. Polyfill embeds source at build time and has no runtime assembly, and NuGet only omits a dependency from the produced package when *every* asset kind is private, so a partial `PrivateAssets` value still leaks Polyfill into every downstream consumer's graph. Satisfied by `all` or by a value naming every asset kind. Deliberately scoped to Polyfill: the other KTSU0001 standard packages (`System.Memory`, `System.Threading.Tasks.Extensions`) are genuine runtime dependencies that must flow transitively. A code fixer sets the attribute, rewriting an existing `PrivateAssets` attribute or child element in place. No fix is offered when the reference is declared outside the project file (e.g. in a `Directory.Build.props`), since only the project file is supplied as an `AdditionalDocument`. The diagnostic still reports in that case. The diagnostic is reported at the `PackageReference` line in the project file rather than at a syntax-tree location: package compile items are prepended to the compilation, so the first syntax tree is usually a Polyfill source file, and a diagnostic located in generated code is dropped under `GeneratedCodeAnalysisFlags.None`.

These properties are passed to analyzers via `CompilerVisibleProperty`: `IsTestProject`, `TestProjectExists`, `TestProjectNamespace`, `TargetFramework`, `TargetFrameworkIdentifier`, `HasPolyfill`, `HasSystemMemory`, `HasSystemThreadingTasksExtensions`, `PolyfillPrivateAssets`, `ManagePackageVersionsCentrally`.

**Package-graph analyzer inputs**: KTSU0005 and KTSU0006 require solution-wide / post-restore facts that a per-project Roslyn analyzer cannot observe on its own. The SDK targets `_KtsuGenerateOrphanedPackageVersionInputs` and `_KtsuGenerateTransitivePackageInputs` (in `Sdk/Sdk.targets`) compute these facts at build time and surface them to the analyzers as `AdditionalFiles` (orphan list, assembly→package map, direct-package set), alongside the `Directory.Packages.props` that the KTSU0005/KTSU0006 fixers edit via `AdditionalDocuments`. The project file is added to `AdditionalFiles` by a static `ItemGroup` rather than by either target, so disabling one analyzer does not take the other's code fix (or KTSU0007's) with it.

**Polyfill Configuration**: For non-test projects, the SDK automatically sets:

- `PolyEnsure=true` - Enables ensure/guard clause polyfills
- `PolyNullability=true` - Enables nullability-related polyfills
- `PolyArgumentExceptions=true` - Enables argument exception polyfills
- `PolyStringInterpolation=true` - Enables string interpolation polyfills

### Metadata File Integration

The SDK reads markdown files from the solution root and uses them to populate package metadata:
- `AUTHORS.md` → Authors, AuthorsNamespace
- `VERSION.md` → Version, PackageVersion
- `DESCRIPTION.md` → Description, PackageDescription (checked in project directory first, then solution directory)
- `CHANGELOG.md` → PackageReleaseNotes (see below)
- `TAGS.md` → Tags, PackageTags (checked in project directory first, then solution directory)
- `LICENSE.md` → PackageLicenseFile
- `README.md` → PackageReadmeFile (checked in project directory first, then solution directory)
- `COPYRIGHT.md` → Copyright
- `PROJECT.url` → ProjectUrl, PackageProjectUrl
- `AUTHORS.url` → AuthorsUrl
- `icon.png` → PackageIcon

All metadata files are automatically included in NuGet packages.

### Release Notes

`PackageReleaseNotes` is resolved in this order, in `Sdk/Sdk.props` for consuming projects and in
`Sdk.Common.PackageProperties.props` for this repository's own SDK packages:

1. A value the project already set wins.
2. `PackageReleaseNotesFile`, if set and the file exists. A relative path resolves against the
   solution directory; an absolute path is used as given. KtsuBuild's pack step
   (`DotNetService.PackAsync` in `ktsu.KtsuBuild.Tool`) points this at the `LATEST_CHANGELOG.md` it
   generates, so a package carries only the notes for the version being released. The property is not
   an MSBuild or NuGet built-in, and the SDK is the only thing that gives it meaning, so the two sides
   have to stay in step.
3. The full `CHANGELOG.md`.

The result is capped at 34000 characters plus a truncation marker. nuget.org rejects a push with
`400 (A nuget package's ReleaseNotes property may not be more than 35000 characters long.)`, and a
repository only hits that at publish time, after the build, pack and release commit have all
succeeded. The changelog grows with every release, so every project needs the cap. Because
`CHANGELOG.md` is newest-first, truncation keeps the entries that matter. `ReleaseNotesTests` in
`test/Sdk.Examples.Tests` covers both code paths.

This repository is not built by KtsuBuild - its workflow runs `scripts/make-changelog.ps1` and
`dotnet pack` directly, and nothing generates a `LATEST_CHANGELOG.md` - so the SDK's own packages
always take the third path and carry a capped `CHANGELOG.md`. KtsuBuild also truncates the file it
writes, at 35000, so for consuming repositories the cap here is a second line of defense rather than
the only one.

## Important MSBuild Properties

### Multi-Targeting
Default: `net10.0;net9.0;net8.0;net7.0;net6.0;net5.0;netstandard2.0;netstandard2.1`

Individual SDK sub-projects (ConsoleApp, App) override `TargetFrameworks` to target a single framework (net10.0).

### Code Quality
- `LangVersion=latest`
- `Nullable=enable`
- `TreatWarningsAsErrors=true`
- `AnalysisLevel=latest-all`
- `EnforceCodeStyleInBuild=true`

### Package Validation
- `EnablePackageValidation=true`
- `ApiCompatValidateAssemblies=true`
- `EnableStrictModeForBaselineValidation=true` — real breaking changes vs a published baseline are caught
- `EnableStrictModeForCompatibleFrameworksInPackage=false` and `EnableStrictModeForCompatibleTfms=false` — strict cross-TFM validation is intentionally off. This SDK mandates Polyfill + broad multi-targeting, and Polyfill source-embeds framework shim types whose shape legitimately differs per TFM; strict mode reports those as false-positive breaking changes (CP0002/CP0014/CP0015/CP0016). Baseline validation stays on, and package consumers are unaffected (validation is producer-side only). Repos capture any residual non-strict compatible-framework diffs in a regenerable `CompatibilitySuppressions.xml` (`dotnet pack -p:ApiCompatGenerateSuppressionFile=true`).

### Runtime Identifiers
Default RIDs: `win-x64;win-x86;win-arm64;osx-x64;linux-x64;osx-arm64;linux-arm64`

## CI/CD Workflow

The GitHub Actions workflow (`.github/workflows/dotnet-sdk.yml`) runs on:
- Push to `main` or `develop` branches
- Pull requests
- Nightly schedule (11 PM UTC)
- Manual workflow dispatch

The workflow uses .NET SDK 10.0.

Release process (only on main branch, non-fork):
1. Generate VERSION.md, LICENSE.md, CHANGELOG.md from git history
2. Update analyzer releases with `make-analyzer-releases.ps1`
3. Commit metadata changes with bot attribution
4. Commit Sdk.props/Sdk.targets version updates
5. Build all projects
6. Run tests
7. Create NuGet packages
8. Publish to GitHub Packages, NuGet.org, and ktsu.dev package feeds
9. Create GitHub release with artifacts

## Common Development Tasks

### Adding a New SDK Sub-Project

1. Create directory: `Sdk.{Name}/`
2. Create `Sdk.{Name}.csproj` with appropriate `TargetFrameworks`
3. Create `Sdk.props` with project-type-specific property overrides
4. Create `Sdk.targets` if custom build logic needed
5. Package structure: SDK packages must include `Sdk/Sdk.props` and `Sdk/Sdk.targets` in the package

### Modifying Core SDK Logic

- **Solution/project discovery**: Edit `Sdk/Sdk.props` (lines 1-70)
- **Project type detection**: Edit `Sdk/Sdk.props` (lines 72-187)
- **Metadata file loading**: Edit `Sdk/Sdk.props` (lines 189-248)
- **Namespace generation**: Edit `Sdk/Sdk.props` (lines 249-287)
- **Package configuration**: Edit `Sdk/Sdk.props` (lines 289-330)
- **Package reference detection**: Edit `Sdk/Sdk.targets` (lines 29-53)
- **Polyfill configuration**: Edit `Sdk/Sdk.targets` (lines 76-82)

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

### Modular Structure

The SDK projects (Sdk, Sdk.ConsoleApp, Sdk.App) use a modular architecture with shared configuration files:
- **Sdk.Common.SolutionDiscovery.props**: Shared solution/project discovery logic
- **Sdk.Common.MetadataFiles.props**: Shared metadata file loading logic
- **Sdk.Common.PackageProperties.props**: Shared package configuration
- **Sdk.Common.SdkContent.targets**: Shared SDK content packaging logic
- **Sdk.Common.PackageContent.targets**: Shared package content inclusion logic

Each SDK project imports these modular files to avoid code duplication and ensure consistency.

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
