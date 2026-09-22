# ktsu.Sdk

A comprehensive, robust MSBuild-based SDK for .NET projects that standardizes configuration, metadata management, and package workflows. Features intelligent project structure detection, hierarchical solution discovery, and path-based namespace generation. Supports multiple .NET versions (.NET 5.0+, .NET Standard 2.0/2.1) with optimizations for .NET 9.0+.

[![License](https://img.shields.io/github/license/ktsu-dev/Sdk.svg?label=License&logo=nuget)](LICENSE.md)
[![NuGet Version](https://img.shields.io/nuget/v/ktsu.Sdk?label=Stable&logo=nuget)](https://nuget.org/packages/ktsu.Sdk)
[![NuGet Version](https://img.shields.io/nuget/vpre/ktsu.Sdk?label=Latest&logo=nuget)](https://nuget.org/packages/ktsu.Sdk)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.Sdk?label=Downloads&logo=nuget)](https://nuget.org/packages/ktsu.Sdk)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/ktsu-dev/Sdk?label=Commits&logo=github)](https://github.com/ktsu-dev/Sdk/commits/main)
[![GitHub contributors](https://img.shields.io/github/contributors/ktsu-dev/Sdk?label=Contributors&logo=github)](https://github.com/ktsu-dev/Sdk/graphs/contributors)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/ktsu-dev/Sdk/dotnet-sdk.yml?branch=main&label=Build&logo=github)](https://github.com/ktsu-dev/Sdk/actions)

## Quick Start

### Installation

Add the SDK to your global.json (recommended):

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  },
  "msbuild-sdks": {
    "MSTest.Sdk": "4.3.2",
    "ktsu.Sdk": "2.26.1",
    "ktsu.Sdk.ConsoleApp": "2.26.1",
    "ktsu.Sdk.App": "2.26.1",
    "ktsu.Sdk.Tool": "2.26.1",
    "ktsu.Sdk.Web": "2.26.1",
    "ktsu.Sdk.Windows": "2.26.1",
    "ktsu.Sdk.Linux": "2.26.1",
    "ktsu.Sdk.macOS": "2.26.1",
    "ktsu.Sdk.iOS": "2.26.1",
    "ktsu.Sdk.Android": "2.26.1",
    "ktsu.Sdk.Unity": "2.26.1",
    "ktsu.Sdk.Godot": "2.26.1"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

And reference in your project file:

```xml
<Project>
  <Sdk Name="Microsoft.NET.Sdk" />
  <Sdk Name="ktsu.Sdk" />

  <PropertyGroup>
    <!-- Your project-specific properties -->
  </PropertyGroup>
</Project>
```

### Basic Usage

For a library project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="ktsu.Sdk" />
</Project>
```

For a console application:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="ktsu.Sdk" />
  <Sdk Name="ktsu.Sdk.ConsoleApp" />
</Project>
```

For a GUI application:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="ktsu.Sdk" />
  <Sdk Name="ktsu.Sdk.App" />
</Project>
```

For a .NET tool (distributed via `dotnet tool install`):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="ktsu.Sdk" />
  <Sdk Name="ktsu.Sdk.Tool" />
</Project>
```

For an ASP.NET Core web application or service:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <Sdk Name="ktsu.Sdk" />
  <Sdk Name="ktsu.Sdk.Web" />
</Project>
```

Note the outer SDK. An `<Sdk Name="..." />` element extends the base SDK rather than replacing
it, so `ktsu.Sdk.Web` cannot supply the ASP.NET Core framework reference itself. Use
`Microsoft.NET.Sdk.Web` as above, or keep `Microsoft.NET.Sdk` and add
`<FrameworkReference Include="Microsoft.AspNetCore.App" />` for a service that wants the runtime
without the Web SDK's static asset and Razor machinery. That second form does not bring the Web
SDK's implicit global usings, so write them yourself. Getting neither is **KTSU1005**.

For a platform-specific application (e.g. Linux), reference the matching SDK:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="ktsu.Sdk" />
  <Sdk Name="ktsu.Sdk.Linux" />
</Project>
```

The same pattern applies to `ktsu.Sdk.Windows`, `ktsu.Sdk.macOS`,
`ktsu.Sdk.iOS`, and `ktsu.Sdk.Android`. Mobile targets require the relevant .NET
workload (`dotnet workload install android ios maui`), and iOS additionally
requires a macOS host with Xcode.

For a Unity managed plug-in:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="ktsu.Sdk" />
  <Sdk Name="ktsu.Sdk.Unity" />
</Project>
```

For a Godot game assembly, `Godot.NET.Sdk` is the outer SDK:

```xml
<Project Sdk="Godot.NET.Sdk/4.7.2">
  <Sdk Name="ktsu.Sdk" />
  <Sdk Name="ktsu.Sdk.Godot" />
</Project>
```

Order matters in both cases: `ktsu.Sdk` first, then the extension SDK, which is
what lets the extension override the core defaults.

## Key Features

### 🏗️ **Intelligent Project Structure**

- **Hierarchical Solution Discovery**: Automatically finds solution files up to 5 directory levels above the project
- **Path-Based Namespace Generation**: Creates namespaces from directory structure between solution and project
- **Smart Project Detection**: Automatically detects primary, console, GUI, and test project types
- **Nested Project Support**: Works seamlessly with deeply nested project structures

### 🛡️ **Robust Error Handling**

- **Safe Array Operations**: Prevents index-out-of-bounds errors in MSBuild expressions
- **Null-Safe String Operations**: Comprehensive null/empty checks for all string manipulations
- **Graceful Fallbacks**: Provides sensible defaults when metadata files or properties are missing
- **Comprehensive Validation**: Built-in validation for all file operations and property access

### 📦 **Advanced Package Management**

- **Multi-Target Support**: .NET 10.0, 9.0, 8.0, .NET Standard 2.0/2.1 (default: net10.0).
  Frameworks follow the .NET support lifecycle: one enters the list when it ships and leaves
  when it goes out of support. .NET Standard 2.0/2.1 stay as the fallback, so a consumer on an
  older framework resolves the netstandard2.1 asset rather than being stranded.
- **MSBuildSdk Packaging**: Properly configured for MSBuild SDK project packaging
- **Automatic Metadata Integration**: Seamlessly includes markdown files in package metadata
- **Package Validation**: Built-in API compatibility and package validation
- **Source Link Integration**: Automatic GitHub and Azure Repos source linking for debugging
- **Central Package Management**: Requires and works with Directory.Packages.props

**Upgrading:** Moving to a `ktsu.Sdk` version that dropped a target framework from this list can fail
your next `dotnet pack` if your project carries a `CompatibilitySuppressions.xml`. See
[Package Validation Fails After a Framework Is Dropped](#package-validation-fails-after-a-framework-is-dropped)
below.

### 🔧 **Development Workflow**

- **Analyzer-Enforced Requirements**: Roslyn analyzers (KTSU0001/KTSU0002) ensure proper package dependencies and internals visibility with helpful diagnostics and code fixers
- **Internals Visibility**: Code fixer to easily add InternalsVisibleTo attributes for test projects
- **GitHub Integration**: Built-in support for GitHub workflows and CI/CD
- **Cross-Platform Support**: Compatible with Windows, macOS, and Linux
- **Documentation Generation**: Automated XML documentation file generation
- **Strict Code Quality**: Nullable enabled, warnings as errors, latest analyzer rules

## SDK Components

This repository contains the following SDK packages:

### **ktsu.Sdk** (Core)

The base SDK that all projects should reference. Provides:

- Solution and project discovery
- Namespace generation
- Metadata file integration
- Multi-target framework support
- Automatic project references
- Package configuration
- Code quality defaults

### **ktsu.Sdk.ConsoleApp**

Extension SDK for console applications. Adds:

- `OutputType=Exe` configuration
- Single target framework (net10.0)
- Cross-platform console optimizations

### **ktsu.Sdk.App**

Extension SDK for GUI applications (ImGui, WinForms, WPF, etc.). Adds:

- `OutputType=WinExe` on Windows (no console window)
- `OutputType=Exe` on other platforms
- Single target framework (net10.0)
- Platform-specific runtime configurations

### **ktsu.Sdk.Tool**

Extension SDK for applications distributed as .NET tools. Adds:

- `PackAsTool=true`, producing a `DotnetTool` package instead of an executable
- Single target framework (net10.0), since a tool package cannot multi-target
- `ToolCommandName` derived from the lowercased solution name — a solution named
  `KtsuBuild` installs as `ktsubuild`. Set `ToolCommandName` explicitly to override,
  which short or generic solution names should do.
- No runtime identifiers: a tool ships as one RID-agnostic, framework-dependent
  package, so consumers need the .NET 10 runtime. For a standalone binary, add a
  separate `ktsu.Sdk.ConsoleApp` project.

A project named `{Solution}.Tool` or `{Solution}Tool` also sets `IsToolProject`.
`{Solution}.CLI` deliberately does not — CLI projects stay console apps unless they
reference this SDK.

Note that packing any project requires the metadata files the SDK declares as package
metadata (`LICENSE.md`, `README.md`, `icon.png`) to exist in the solution directory.

### **ktsu.Sdk.Web**

Extension SDK for ASP.NET Core web applications and services. Adds:

- Single target framework (net10.0). ASP.NET Core publishes no netstandard surface, so the
  core SDK's multi-target default cannot stand: its netstandard inner builds fail inside the
  Web SDK's generated global usings, as a wall of CS0234 naming neither the framework nor the
  SDK responsible. **KTSU1004** catches a consumer putting `TargetFrameworks` back.
- `CS1591` suppressed, while `GenerateDocumentationFile` stays on. The core SDK pairs
  documentation generation with warnings-as-errors, which fails a build on the first
  undocumented public type. That is right for a library, whose public types are its product,
  and wrong for a service, whose public types are request and response shapes with no external
  consumer. The documentation file itself is kept because OpenAPI generators read it.
- Package validation, assembly API compatibility and `IncludeSource` disabled, as on
  `ktsu.Sdk.Tool`: a web application ships as a container image or a published directory, never
  as a package.
- `IsWebProject=true`, matching the `IsWindowsProject` / `IsLinuxProject` family so CI and
  tooling can tell a service from a library without parsing the project file.

`RuntimeIdentifiers` is deliberately left as the core SDK's desktop list. A service is
published with an explicit RID about as often as without one, the list permits values rather
than causing a build fan-out, and narrowing it would reject a legitimate `-r win-x64`.

This is the one SDK in the family whose outer SDK is not `Microsoft.NET.Sdk`. See the usage
example above, and **KTSU1005** for what happens when the ASP.NET Core framework reference is
missing entirely.

### Platform-Specific App SDKs

These extension SDKs target a single platform. They fall into two groups:

**Desktop (RID-based, no extra prerequisites):** build self-contained apps on
the base `net10.0` runtime with the runtime identifiers narrowed to one OS.

- **ktsu.Sdk.Windows** — `net10.0`, `OutputType=WinExe`, RIDs `win-x64;win-x86;win-arm64`
- **ktsu.Sdk.Linux** — `net10.0`, `OutputType=Exe`, RIDs `linux-x64;linux-arm64;linux-musl-x64;linux-musl-arm64`
- **ktsu.Sdk.macOS** — `net10.0`, `OutputType=Exe`, RIDs `osx-x64;osx-arm64`

**Mobile (TFM + workload):** use platform-specific target frameworks and require
the corresponding .NET workload.

- **ktsu.Sdk.iOS** — `net10.0-ios`, `SupportedOSPlatformVersion=15.0`. Requires the
  `ios` workload **and a macOS host with Xcode** to build/run consumers.
- **ktsu.Sdk.Android** — `net10.0-android`, `SupportedOSPlatformVersion=21.0`.
  Requires the `android` workload; builds on Linux, macOS, or Windows.

> **Prerequisites for mobile targets:** install the workloads once with
> `dotnet workload install android ios maui`. The SDK packages themselves carry no
> workload dependency — only consuming app projects do.

### Game Engine SDKs

Unity and Godot both host their own runtime and produce their own executable, so a
C# project for either is a **library** the engine loads, not an app the .NET SDK
publishes. Each of these extension SDKs pins the shape that engine can actually
load and puts back the core SDK defaults that would otherwise get in the way.
Neither needs the engine installed to build.

- **ktsu.Sdk.Unity** — `netstandard2.1`, `OutputType=Library`, no runtime
  identifiers. Builds a [managed plug-in](https://docs.unity3d.com/Manual/plug-ins-managed.html)
  to drop into a Unity project's `Assets/Plugins` (or to publish for NuGetForUnity).
- **ktsu.Sdk.Godot** — `net10.0`, `OutputType=Library`, `EnableDynamicLoading=true`,
  no runtime identifiers. Composes with `Godot.NET.Sdk`, which supplies the
  GodotSharp bindings, the source generators and the engine's output layout.

**Unity: why netstandard2.1.** Unity's scripting runtime is Mono or IL2CPP, not
.NET Core. Both API Compatibility Levels Unity offers (".NET Standard 2.1", the
default, and ".NET Framework") implement netstandard2.1, so it is the one pin that
loads in every supported configuration; a `netX.0` assembly fails to import
outright. Override to `netstandard2.0` for editors older than Unity 2021.2, or to a
`net4x` framework for a project fixed on the .NET Framework profile — **KTSU1003**
fails the build for anything else, rather than letting the failure surface later as
an import error in the Unity console. The assembly is compiled by your own .NET
SDK, so it may use language features newer than Unity's in-editor compiler accepts;
features that need a newer *runtime* (ref fields, static abstract interface members)
will still fail on Unity's.

**Godot: what the SDK puts back.** Godot.NET.Sdk deliberately sets no
`TargetFramework` — GodotSharp declares the minimum and the project chooses — so
`ktsu.Sdk.Godot` pins the same `net10.0` as the rest of this SDK family. It also
restores two values the core SDK overwrites, and only when `Godot.NET.Sdk` is the
outer SDK:

- `AppendTargetFrameworkToOutputPath=false`, because Godot loads the assembly from
  `.godot/mono/temp/bin/$(Configuration)/` with no framework folder. The core SDK
  sets it back to `true`, which moves the output one directory deeper and leaves
  the editor reporting a missing assembly.
- `AssemblyName=$(MSBuildProjectName)`, because `project.godot` records the
  assembly to load in `dotnet/project/assembly_name` and Godot writes the project
  name there. The core SDK sets `AssemblyName` to the fully-qualified namespace
  (e.g. `ktsu.MyGame.Godot`), which is no longer the file Godot looks for.
  `RootNamespace`, `PackageId` and `Title` keep the ktsu-namespaced value — only
  the assembly file name is pinned — and a project whose `project.godot` says
  otherwise can still set `AssemblyName` itself.

A project named `{Solution}.Unity`/`{Solution}Unity` or
`{Solution}.Godot`/`{Solution}Godot` also sets `IsUnityProject` / `IsGodotProject`.

Runnable demos for both, including the engine-side halves and the deployment step, are under
[`examples/demos/Unity`](examples/demos/Unity/README.md) and
[`examples/demos/Godot`](examples/demos/Godot/README.md).

The `.gitignore` the SDK syncs into consuming repositories gains Godot's `.godot/` cache, and
negates two of its own generic rules for Unity: `**/[Pp]ackages/*` (a NuGet restore folder, but
Unity's `Packages/` is project source) and `*.meta` (the Visual Studio C++ build artifact, but
Unity generates one `.meta` per asset carrying the GUID scenes and prefabs reference). Both
negations are scoped, so a Unity project keeps its source while the artifacts they were written
for stay ignored everywhere else. Unity's generated caches — `Library/`, `Temp/`, `Logs/` — are
deliberately *not* added: those names are only caches beside an `Assets/` folder, so they belong
in the Unity project's own `.gitignore`, as the demo shows.

## Detailed Usage

### Setup Requirements

1. **Central Package Management**: Create a `Directory.Packages.props` file at your solution root:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
```

1. **Metadata Files**: Create these optional markdown files at your solution root (they will be automatically included in NuGet packages):
   - `AUTHORS.md` - Used for namespace generation and package authors
   - `VERSION.md` - Version number (can be managed by build scripts)
   - `DESCRIPTION.md` - Package description (checked in project directory first, then solution directory for multi-package support)
   - `CHANGELOG.md` - Release notes
   - `LICENSE.md` - License information
   - `COPYRIGHT.md` - Copyright notice
   - `TAGS.md` - NuGet package tags (checked in project directory first, then solution directory for multi-package support)
   - `README.md` - Package documentation (checked in project directory first, then solution directory for multi-package support)
   - `AUTHORS.url` - URL to author/organization
   - `PROJECT.url` - URL to project repository (`PROJECT_URL.url` is also accepted)

2. **icon.png**: Optional package icon at solution root

   `icon.png`, `README.md` and `LICENSE.md` are declared on the package only when the file is
   actually present, so a repository without them still packs.

### Overriding Defaults

The SDK provides sensible defaults, but you can override any property:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="ktsu.Sdk" />

  <PropertyGroup>
    <!-- Override target frameworks -->
    <TargetFrameworks>net10.0;net9.0</TargetFrameworks>

    <!-- Override the namespace only: AssemblyName and PackageId are derived before the
         project body is evaluated, so setting RootNamespace here does not move them. To move
         the whole identity, set RootNamespace in Directory.Build.props instead. -->
    <RootNamespace>MyCompany.MyProject</RootNamespace>

    <!-- Disable nullable if needed -->
    <Nullable>disable</Nullable>

    <!-- Allow warnings in test projects -->
    <TreatWarningsAsErrors Condition="$(IsTestProject) == 'true'">false</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

#### Style/config file sync

By default, `ktsu.Sdk` syncs these files from the SDK package into the solution root during build:

- `.editorconfig`
- `.gitattributes`
- `.gitignore`
- `.runsettings`

The sync updates files that already exist in the solution root.

When `COPYRIGHT.md` exists in the consumer solution root, `.editorconfig` is still synced but its
`file_header_template` line is rewritten from the full contents of `COPYRIGHT.md`.

To opt out:

```xml
<PropertyGroup>
  <KtsuSyncStyleConfigFiles>false</KtsuSyncStyleConfigFiles>
</PropertyGroup>
```

#### Host-only runtime builds

By default, a project carries every runtime identifier its packages ship, and its output
holds one copy of native assets per runtime. Set `KtsuHostRuntimeOnly` to build for the
host's runtime only instead:

```xml
<PropertyGroup>
  <KtsuHostRuntimeOnly>true</KtsuHostRuntimeOnly>
</PropertyGroup>
```

or on the command line:

```bash
dotnet test -p:KtsuHostRuntimeOnly=true
```

This exists so a whole workspace can be tested with one `dotnet test` invocation, which
cannot take a `RuntimeIdentifier` global property directly (NETSDK1134), without every
test project copying native assets for every runtime its packages ship.

The flag is off by default and applies only when the project has not already resolved a
runtime identifier before `ktsu.Sdk.props` is imported (a command-line value or one set in
`Directory.Build.props`). A project that sets `RuntimeIdentifier` in its own project body
is not protected by that check: the flag's property group still runs, and the project body
simply overwrites the runtime identifier afterward because it evaluates later. Such a
project also keeps `SelfContained=false`, which the flag imposes before the project body
runs. So the project ends up with its own runtime identifier, but not because the flag
deferred to it.

The platform SDKs (`ktsu.Sdk.Linux`, `ktsu.Sdk.macOS`, `ktsu.Sdk.Windows`) set their
runtime identifier behind that same `== ''` check, which the flag has already satisfied by
the time those SDKs run. Under the flag, a `ktsu.Sdk.Linux` project built on a Windows host
resolves `RuntimeIdentifier` to `win-x64` while `RuntimeIdentifiers` still lists the Linux
set. Treat the flag as incompatible with the platform SDKs until this is fixed.

**Never combine this flag with packing.** `ktsu.Sdk.Tool` clears `RuntimeIdentifiers`
(plural) so a tool ships as one runtime-agnostic package, but it does not clear
`RuntimeIdentifier` (singular), and the base SDK that sets `RuntimeIdentifier` under the
flag is imported first. Packing a `PackAsTool` project under the flag moves the tool
payload from `tools/net10.0/any/` to a runtime-specific folder such as
`tools/net10.0/win-x64/`, producing a package that will not install on any other platform.
Four repositories use `ktsu.Sdk.Tool`, including KtsuBuild itself, the tool every ktsu
repository installs. This flag is for building and testing, never for packing or
releasing.

Deliberate consequence: `ktsu.Sdk.App` sets `OutputType` to `WinExe` when the runtime
identifier starts with `win`, and the base SDK that sets `RuntimeIdentifier` under this
flag is imported before it, so app projects build as `WinExe` during a run with this flag
set. That is accepted. It is harmless when the purpose of the run is to execute tests, and
it is the reason a build with this flag is not interchangeable with a release build.

### Project Type Detection

The SDK automatically detects different project types in your solution:

- **Primary Project**: The main project of your solution (YourSolution, YourSolution.Core)
- **Console Projects**: Command-line interface projects (YourSolution.CLI, YourSolution.Cli, YourSolutionCli, YourSolutionCLI, YourSolution.ConsoleApp, YourSolution.Console)
- **GUI App Projects**: Application projects (YourSolution.App, YourSolutionApp, YourSolution.WinApp, YourSolutionWinApp, YourSolution.ImGuiApp, YourSolutionImGuiApp)
- **Platform App Projects**: Per-OS app projects (YourSolution.Windows, YourSolution.Linux, YourSolution.macOS, YourSolution.iOS, YourSolution.Android, and their suffix-free and abbreviated forms)
- **Game Engine Projects**: Engine assemblies (YourSolution.Unity, YourSolutionUnity, YourSolution.Godot, YourSolutionGodot)
- **Tool Projects**: .NET tool projects (YourSolution.Tool, YourSolutionTool)
- **Test Projects**: Test projects (YourSolution.Test, YourSolution.Tests, YourSolutionTest, YourSolutionTests)

Each project type receives appropriate default settings, references, and output configurations (console apps vs. GUI apps).

### Intelligent Namespace Generation

The SDK creates intelligent namespaces based on your project's directory structure:

**Examples:**

```
MySolution/src/Core/Utils/MyProject.csproj
→ ProjectNamespace: src.Core.Utils.MyProject

MySolution/libs/MyLib/MyLib.csproj  
→ ProjectNamespace: libs.MyLib (already ends with project name)

MySolution/MyApp/MyApp.csproj
→ ProjectNamespace: MyApp (directory equals project name)
```

**Final Namespace Pattern:**
`{AuthorsNamespace}.{ProjectNamespace}` where AuthorsNamespace comes from AUTHORS.md

#### How AuthorsNamespace is derived

`AuthorsNamespace` is the organization prefix on `RootNamespace`, `AssemblyName` and `PackageId`,
so it has to be both a legal C# identifier and a legal NuGet package ID segment. It is derived from
the first line of `AUTHORS.md` that is neither blank nor a markdown heading:

1. Spaces are dropped and `-` becomes `.`, so `ktsu-dev` and `ktsu.dev` agree.
2. Only the first dot-separated segment is kept, so `ktsu.dev contributors` gives `ktsu`.
3. Every character that cannot appear in an identifier is dropped, so `Contoso, Inc.` gives
   `ContosoInc`.
4. What is left is rejected if it cannot *begin* an identifier - empty, or starting with a digit -
   and the prefix is dropped rather than mangled.

A conventional `AUTHORS.md` that lists individual contributors under a heading therefore yields no
prefix at all, deliberately: a person's name is not an organization namespace. The project keeps its
own name (`MyLib` rather than `SomeOrg.MyLib`), which is visible and correctable, instead of
producing an assembly and package nobody can publish.

#### Overriding the derived identity

Identity is computed while the SDK's props are imported, which happens before your project body is
evaluated. An override therefore has to come from somewhere evaluated earlier - most simply
`Directory.Build.props` at your solution root:

```xml
<Project>
  <PropertyGroup>
    <!-- Name the organization prefix directly, whatever AUTHORS.md says -->
    <AuthorsNamespace>Contoso</AuthorsNamespace>

    <!-- Or name the whole identity: RootNamespace, AssemblyName and PackageId all follow -->
    <RootNamespace>Contoso.Widgets</RootNamespace>
  </PropertyGroup>
</Project>
```

One identity cannot be requested this way: one that is exactly the project name, since that is
indistinguishable from the SDK's own default. An empty `AuthorsNamespace` is the supported way to
drop the organization prefix and keep the project name.

### Hierarchical Solution Discovery  

The SDK automatically searches for solution files up the directory hierarchy:

```
MyProject/                     ← Level 3: Check here  
├── MyProject.sln             ← Found! Use this directory
└── apps/                     ← Level 2: Check here
    └── frontend/             ← Level 1: Check here  
        └── src/              ← Level 0: Start here (project directory)
            └── MyApp.csproj
```

This enables the SDK to work with any nested project structure without configuration.

## Advanced Configuration Features

### Analyzer-Enforced Requirements

The SDK automatically includes the `ktsu.Sdk.Analyzers` package (with version synchronization) that enforces proper project configuration with helpful diagnostics and code fixers:

**KTSU0001 (Error)**: Projects must include required standard packages
- Enforces Polyfill package for non-test projects
- Enforces compatibility packages (System.Memory, System.Threading.Tasks.Extensions) based on target framework
- Diagnostic message includes package name and version number

**KTSU0002 (Error)**: Projects must expose internals to test projects
- Code fixer automatically adds `[assembly: InternalsVisibleTo(...)]` attribute
- Use Ctrl+. (Quick Actions) to apply the fix

**KTSU0003 (Error)**: Use Ensure.NotNull over ArgumentNullException.ThrowIfNull

- ArgumentNullException.ThrowIfNull was introduced in .NET 6
- Ensure.NotNull from the Polyfill package maintains compatibility with older frameworks
- Code fixer automatically replaces the invocation

**KTSU0004 (Error)**: Use Ensure.NotNull instead of manual null checks

- Detects patterns like `if (x == null) throw new ArgumentNullException(...)`
- Detects patterns like `if (x is null) throw new ArgumentNullException(...)`
- Detects patterns like `x ?? throw new ArgumentNullException(...)`
- Code fixer automatically replaces with Ensure.NotNull

**KTSU0005 (Error)**: Orphaned `PackageVersion` entry in `Directory.Packages.props`

- Flags a centrally-managed version that no project in the solution references
- Code fixer removes the entry
- Disable with `<KtsuEnableOrphanedPackageVersionAnalysis>false</KtsuEnableOrphanedPackageVersionAnalysis>`

**KTSU0006 (Error)**: Transitive package used directly

- Flags use of a type or member that comes from a transitive dependency with no direct `PackageReference`
- Code fixer adds the `PackageReference`, and a matching `PackageVersion` under Central Package Management
- Disable with `<KtsuEnableTransitivePackageAnalysis>false</KtsuEnableTransitivePackageAnalysis>`

**KTSU0007 (Error)**: Build-time package reference is not private

- Requires `PrivateAssets="all"` on the Polyfill reference in non-test projects
- Polyfill is a source-embedding, build-time-only package. NuGet only omits a dependency from the
  produced package when every asset kind is private, so a partial `PrivateAssets` value still
  leaks Polyfill to every downstream consumer
- Code fixer sets the attribute on the `PackageReference`
- The other standard packages are deliberately not covered: `System.Memory` and
  `System.Threading.Tasks.Extensions` are genuine runtime dependencies that must flow transitively

**KTSU0008 (Error)**: Package reference overrides the shared framework

- Flags a fully private `PackageReference` to a package that also ships in the target framework's
  shared framework (`System.Text.Json`, `System.Memory` and the rest of the targeting pack's
  `PackageOverrides.txt`) at a version above the one that framework supplies
- Such a reference resolves a real assembly instead of being pruned, so the project compiles against
  the higher assembly version. `PrivateAssets="all"` then keeps the dependency out of the produced
  package, and a consumer on that framework resolves nothing and fails with `FileNotFoundException` —
  assembly binds roll forward but never backward. The producing build and its nuspec are both silent
- Code fixer pins the version to what the framework ships and adds `NoWarn="NU1510"` on the item,
  because NuGet then reports the reference as prunable — the opposite of what KTSU0006 demands, and
  the two can only be satisfied together
- Reported per target framework: the `net10.0` inner build of a `net10.0;net9.0` project sees the
  reference pruned and stays silent, while the `net9.0` one sees the override and reports
- A reference that still flows to consumers is not reported — it carries the higher version with it
- Disable with `<KtsuEnableFrameworkOverrideAnalysis>false</KtsuEnableFrameworkOverrideAnalysis>`

**Polyfill Configuration**: For non-test projects, the SDK automatically enables:
- `PolyEnsure=true` - Enables ensure/guard clause polyfills
- `PolyNullability=true` - Enables nullability-related polyfills
- `PolyArgumentExceptions=true` - Enables argument exception polyfills
- `PolyStringInterpolation=true` - Enables string interpolation polyfills

These analyzers ensure consistent project structure while giving you explicit control over dependencies.

### Available Properties

The SDK makes these properties available for conditional logic in your project files:

**Project Type Detection:**

- `IsPrimaryProject` - True if this is the main library project
- `IsCliProject` - True if this is a console application
- `IsAppProject` - True if this is a GUI application
- `IsWindowsProject`, `IsLinuxProject`, `IsMacProject`, `IsIosProject`, `IsAndroidProject` - True for the matching per-OS app project
- `IsUnityProject` - True if this is a Unity managed plug-in project
- `IsGodotProject` - True if this is a Godot game assembly project
- `IsToolProject` - True if this is a .NET tool project
- `IsTestProject` - True if this is a test project

**Project Type Existence:**

- `PrimaryProjectExists` - True if primary project was found
- `CliProjectExists` - True if CLI project was found
- `AppProjectExists` - True if app project was found
- `ToolProjectExists` - True if tool project was found
- `TestProjectExists` - True if test project was found

**Project Paths:**

- `SolutionDir` - Path to solution directory
- `SolutionPath` - Full path to .sln file
- `SolutionName` - Solution name without extension
- `PrimaryProjectPath` - Path to primary project
- `TestProjectPath` - Path to test project

**Namespace Properties:**

- `AuthorsNamespace` - Namespace prefix from AUTHORS.md
- `ProjectNamespace` - Namespace from directory path
- `RootNamespace` - Final combined namespace
- `TestProjectNamespace` - Namespace for test project

**Package Properties:**

- `IsPackable` - True for library projects and projects with `PackAsTool`
- `IsPublishable` - True for executable projects
- `IsExecutable` - True if OutputType is Exe or WinExe
- `IsLibrary` - True if OutputType is Library and not a test project

Use these in your project files:

```xml
<PropertyGroup>
  <!-- Example: Only pack if not a prerelease -->
  <IsPackable Condition="$(IsPrerelease) == 'true'">false</IsPackable>

  <!-- Example: Different settings for test projects -->
  <SomeProperty Condition="$(IsTestProject) == 'true'">TestValue</SomeProperty>
</PropertyGroup>
```

### Robust Error Handling

The SDK includes comprehensive error handling to prevent common MSBuild failures:

- **Safe Array Access**: Prevents "index out of bounds" errors when accessing file lists or string arrays
- **Null Property Checks**: All string operations include null/empty validation  
- **File Existence Validation**: All file operations verify existence before processing
- **Graceful Degradation**: Missing metadata files don't cause build failures

### Standardized Package Creation

Library projects are automatically configured for NuGet packaging with:

- **Automatic Metadata Population**: Uses markdown files for package description, changelog, etc.
- **Source Link Integration**: Enables source code debugging for published packages
- **Package Validation**: Built-in API compatibility and package structure validation
- **Multi-Framework Support**: Targets multiple .NET versions simultaneously

### Cross-Platform Compatibility

Projects are configured with multiple runtime identifiers:

- **Windows**: `win-x64`, `win-x86`, `win-arm64`
- **macOS**: `osx-x64`, `osx-arm64`
- **Linux**: `linux-x64`, `linux-arm64`

### Advanced Testing Support

- **Automatic InternalsVisibleTo**: Test projects automatically access internal members
- **Test Project Detection**: Identifies and configures test projects with appropriate settings
- **Relaxed Warnings**: Test projects suppress documentation and code style warnings

## Automatic Package References

The SDK enforces (via analyzers) that projects include these NuGet packages:

### Polyfills (Non-Test Projects)

- **Polyfill** - Modern language feature support for older frameworks, with automatic configuration for PolyEnsure and PolyNullability source generators

### Compatibility Packages (Framework-Specific)

- **System.Memory** - For .NET Standard and .NET Framework
- **System.Threading.Tasks.Extensions** - For netstandard2.0, netcoreapp2.0, and .NET Framework

### Analyzer Package (Non-Test Projects)

- **ktsu.Sdk.Analyzers** - Automatically included with version synchronization to enforce SDK requirements

## Code Quality Defaults

The SDK enforces strict code quality standards by default:

### Compiler Settings

- **LangVersion**: `latest` - Use latest C# language features
- **Nullable**: `enable` - Nullable reference types enabled
- **TreatWarningsAsErrors**: `true` - All warnings treated as errors
- **ImplicitUsings**: `enable` - Implicit global usings enabled

### Code Analysis

- **AnalysisLevel**: `10.0-all` - All .NET 10 analyzer rules enabled. Pinned rather than `latest-all` so a .NET SDK update cannot turn newly-added rules into build errors (`TreatWarningsAsErrors` is on). Override with `<AnalysisLevel>latest-all</AnalysisLevel>` to track the newest rules.
- **EnableNETAnalyzers**: `true` - .NET code analyzers enabled
- **EnforceCodeStyleInBuild**: `true` - Code style rules enforced during build

### Suppressed Warnings

The following warnings are suppressed globally:

- **CA1724**: Type names should not match namespaces
- **CA1034**: Nested types should not be visible
- **CA1000**: Do not declare static members on generic types
- **CA2260**: Implement ISerializable correctly
- **CA1515**: Override methods should call base methods

Additional suppressions for test projects:

- **CS1591**: Missing XML comment
- **CA2225**: Operator overloads have named alternates
- **IDE0022**: Use expression body for methods
- **IDE0058**: Expression value is never used
- **CA1305**: Specify IFormatProvider
- **CA5394**: Do not use insecure randomness
- **CA1707**: Identifiers should not contain underscores

### Runtime Configuration

- **InvariantGlobalization**: `true` - Invariant culture for better performance
- **NeutralLanguage**: `en-US`

## Troubleshooting

### Package Restore Fails

**Problem**: NuGet restore fails with "ManagePackageVersionsCentrally is not enabled"

**Solution**: Ensure `Directory.Packages.props` exists at your solution root with `ManagePackageVersionsCentrally` enabled.

### Namespace Generation Issues

**Problem**: Generated namespace doesn't match expectations

**Solution**:

- Check that `AUTHORS.md` exists and contains valid content
- The namespace format is: `{AuthorsNamespace}.{PathToProject}.{ProjectName}`
- `AuthorsNamespace` may come out empty - a heading over a contributor list yields no organization
  prefix, and neither does a name that cannot begin an identifier. See
  [How AuthorsNamespace is derived](#how-authorsnamespace-is-derived).
- You can always override with `<RootNamespace>` in **`Directory.Build.props`**, not in the project
  file: identity is derived while the SDK's props are imported, which is before the project body is
  evaluated, so a `RootNamespace` set in the project moves only the namespace and leaves
  `AssemblyName` and `PackageId` on the derived value

### Build Warnings as Errors

**Problem**: Build fails due to warnings being treated as errors

**Solution**: Either fix the warnings or selectively disable warnings:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);CA1234;IDE5678</NoWarn>
</PropertyGroup>
```

### Multi-Target Framework Issues

**Problem**: Project builds for too many frameworks

**Solution**: Override `TargetFrameworks` for specific projects:

```xml
<PropertyGroup>
  <!-- Single target for applications -->
  <TargetFramework>net10.0</TargetFramework>
  <TargetFrameworks></TargetFrameworks>
</PropertyGroup>
```

### Package Validation Fails After a Framework Is Dropped

**Problem**: `dotnet pack` fails with `EnablePackageValidation=true` after you take a version of
ktsu.Sdk that drops a target framework. The errors are CP0001, CP0002, CP0008, CP0014, CP0015, or
CP0016 diagnostics naming the removed framework (for example net5.0, net6.0, or net7.0), even
though `PackageValidationBaselineVersion` isn't set anywhere in your project.

**Solution**: This isn't baseline validation, and setting `PackageValidationBaselineVersion` won't
fix it. The real cause is a committed `CompatibilitySuppressions.xml` file that still records
comparisons against the framework you dropped. The package validation tool reprocesses those
entries on every pack, and once a recorded comparison names a framework that no longer exists, the
suppression can never match a live comparison again. The default
`ApiCompatPermitUnnecessarySuppressions=false` then treats that stale, unmatched suppression as an
error instead of discarding it. Regenerate the file once, against the new SDK, and commit the
result:

```powershell
dotnet pack -p:ApiCompatGenerateSuppressionFile=true
```

This replaces the stale entries with ones for your current target frameworks. If your project has
no `CompatibilitySuppressions.xml`, this problem doesn't apply to you, and a package validation
failure has a different cause.

### Solution Not Found

**Problem**: SDK reports it cannot find a solution file

**Solution**: The SDK searches up to 5 directory levels. Ensure your project is within 5 levels of your .sln file, or manually set `<SolutionDir>` in your project.

## Requirements

- .NET SDK 5.0 or later (optimized for .NET SDK 10.0)
- Central Package Management (Directory.Packages.props)

## License

See the [LICENSE.md](LICENSE.md) file for license information.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
