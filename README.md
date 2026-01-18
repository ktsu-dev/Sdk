# ktsu.Sdk

A comprehensive, robust MSBuild-based SDK for .NET projects that standardizes configuration, metadata management, and package workflows. Features intelligent project structure detection, hierarchical solution discovery, and path-based namespace generation. Supports multiple .NET versions (.NET 5.0+, .NET Standard 2.0/2.1) with optimizations for .NET 9.0.

## Quick Start

### Installation

Add the SDK to your global.json (recommended):

```json
{
  "sdk": {
    "version": "9.0.0",
    "rollForward": "latestMinor"
  },
  "msbuild-sdks": {
    "ktsu.Sdk": "1.0.0",
    "ktsu.Sdk.ConsoleApp": "1.0.0",
    "ktsu.Sdk.App": "1.0.0"
  }
}
```

Or reference directly in your project file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="ktsu.Sdk" Version="1.0.0" />

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

- **Multi-Target Support**: .NET 9.0, 8.0, 7.0, 6.0, 5.0, .NET Standard 2.0/2.1
- **MSBuildSdk Packaging**: Properly configured for MSBuild SDK project packaging
- **Automatic Metadata Integration**: Seamlessly includes markdown files in package metadata
- **Package Validation**: Built-in API compatibility and package validation
- **Source Link Integration**: Automatic GitHub and Azure Repos source linking for debugging
- **Central Package Management**: Requires and works with Directory.Packages.props

### 🔧 **Development Workflow**

- **Analyzer-Enforced Requirements**: Roslyn analyzers (KTSU0001/KTSU0002) ensure proper package dependencies and internals visibility with helpful diagnostics and code fixers
- **Internals Visibility**: Code fixer to easily add InternalsVisibleTo attributes for test projects
- **GitHub Integration**: Built-in support for GitHub workflows and CI/CD
- **Cross-Platform Support**: Compatible with Windows, macOS, and Linux
- **Documentation Generation**: Automated XML documentation file generation
- **Strict Code Quality**: Nullable enabled, warnings as errors, latest analyzer rules

## SDK Components

This repository contains three SDK packages:

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
- Single target framework (net9.0)
- Cross-platform console optimizations

### **ktsu.Sdk.App**

Extension SDK for GUI applications (ImGui, WinForms, WPF, etc.). Adds:

- `OutputType=WinExe` on Windows (no console window)
- `OutputType=Exe` on other platforms
- Single target framework (net9.0)
- Platform-specific runtime configurations

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
   - `DESCRIPTION.md` - Package description
   - `CHANGELOG.md` - Release notes
   - `LICENSE.md` - License information
   - `COPYRIGHT.md` - Copyright notice
   - `TAGS.md` - NuGet package tags
   - `README.md` - Package documentation
   - `AUTHORS.url` - URL to author/organization
   - `PROJECT_URL.url` - URL to project repository

2. **icon.png**: Optional package icon at solution root

### Overriding Defaults

The SDK provides sensible defaults, but you can override any property:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="ktsu.Sdk" />

  <PropertyGroup>
    <!-- Override target frameworks -->
    <TargetFrameworks>net9.0;net8.0</TargetFrameworks>

    <!-- Override namespace -->
    <RootNamespace>MyCompany.MyProject</RootNamespace>

    <!-- Disable nullable if needed -->
    <Nullable>disable</Nullable>

    <!-- Allow warnings in test projects -->
    <TreatWarningsAsErrors Condition="$(IsTestProject) == 'true'">false</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

### Project Type Detection

The SDK automatically detects different project types in your solution:

- **Primary Project**: The main project of your solution (YourSolution, YourSolution.Core)
- **Console Projects**: Command-line interface projects (YourSolution.ConsoleApp, YourSolutionConsoleApp, YourSolution.CLI, YourSolutionCLI)
- **GUI App Projects**: Application projects (YourSolution.App, YourSolutionApp, YourSolution.WinApp, YourSolutionWinApp, YourSolution.ImGuiApp, YourSolutionImGuiApp)
- **Test Projects**: Test projects (YourSolution.Test, YourSolution.Tests, YourSolutionTest, YourSolutionTests, YourSolution.WinTest, YourSolutionWinTest)

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

The SDK includes Roslyn analyzers that enforce proper project configuration with helpful diagnostics and code fixers:

**KTSU0001 (Error)**: Projects must include required standard packages
- Enforces SourceLink packages (GitHub, Azure Repos)
- Enforces Polyfill package for non-test projects
- Enforces compatibility packages (System.Memory, System.Threading.Tasks.Extensions) based on target framework
- Diagnostic message includes package name and version number

**KTSU0002 (Error)**: Projects must expose internals to test projects
- Code fixer automatically adds `[assembly: InternalsVisibleTo(...)]` attribute
- Use Ctrl+. (Quick Actions) to apply the fix

These analyzers ensure consistent project structure while giving you explicit control over dependencies.

### Available Properties

The SDK makes these properties available for conditional logic in your project files:

**Project Type Detection:**

- `IsPrimaryProject` - True if this is the main library project
- `IsCliProject` - True if this is a console application
- `IsAppProject` - True if this is a GUI application
- `IsTestProject` - True if this is a test project

**Project Type Existence:**

- `PrimaryProjectExists` - True if primary project was found
- `CliProjectExists` - True if CLI project was found
- `AppProjectExists` - True if app project was found
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

- `IsPackable` - True for library projects
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

The SDK automatically includes these NuGet packages in all projects:

### Source Link Support (All Projects)

- **Microsoft.SourceLink.GitHub** (8.0.0) - GitHub source linking for debugging
- **Microsoft.SourceLink.AzureRepos.Git** (8.0.0) - Azure Repos source linking

### Polyfills (Non-Test Projects)

- **Polyfill** (8.8.0) - Modern language feature support for older frameworks

### Compatibility Packages (Framework-Specific)

- **System.Memory** (4.6.3) - For .NET Standard and .NET Framework
- **System.Threading.Tasks.Extensions** (4.6.3) - For netstandard2.0, netcoreapp2.0, and .NET Framework

## Code Quality Defaults

The SDK enforces strict code quality standards by default:

### Compiler Settings

- **LangVersion**: `latest` - Use latest C# language features
- **Nullable**: `enable` - Nullable reference types enabled
- **TreatWarningsAsErrors**: `true` - All warnings treated as errors
- **ImplicitUsings**: `enable` - Implicit global usings enabled

### Code Analysis

- **AnalysisLevel**: `latest-all` - All latest analyzer rules enabled
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
- The namespace format is: `{FirstPartOfAuthors}.{PathToProject}.{ProjectName}`
- You can always override with `<RootNamespace>` in your project file

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
  <TargetFramework>net9.0</TargetFramework>
  <TargetFrameworks></TargetFrameworks>
</PropertyGroup>
```

### Solution Not Found

**Problem**: SDK reports it cannot find a solution file

**Solution**: The SDK searches up to 5 directory levels. Ensure your project is within 5 levels of your .sln file, or manually set `<SolutionDir>` in your project.

## Requirements

- .NET SDK 5.0 or later (optimized for .NET SDK 9.0)
- Central Package Management (Directory.Packages.props)

## License

See the [LICENSE.md](LICENSE.md) file for license information.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
