# ktsu.Sdk

A comprehensive, robust MSBuild-based SDK for .NET projects that standardizes configuration, metadata management, and package workflows. Features intelligent project structure detection, hierarchical solution discovery, and path-based namespace generation. Supports multiple .NET versions (.NET 5.0+, .NET Standard 2.0/2.1) with optimizations for .NET 9.0.

## Key Features

### 🏗️ **Intelligent Project Structure**
-   **Hierarchical Solution Discovery**: Automatically finds solution files up to 5 directory levels above the project
-   **Path-Based Namespace Generation**: Creates namespaces from directory structure between solution and project
-   **Smart Project Detection**: Automatically detects primary, console, GUI, and test project types
-   **Nested Project Support**: Works seamlessly with deeply nested project structures

### 🛡️ **Robust Error Handling**
-   **Safe Array Operations**: Prevents index-out-of-bounds errors in MSBuild expressions
-   **Null-Safe String Operations**: Comprehensive null/empty checks for all string manipulations  
-   **Graceful Fallbacks**: Provides sensible defaults when metadata files or properties are missing
-   **Comprehensive Validation**: Built-in validation for all file operations and property access

### 📦 **Advanced Package Management**
-   **Multi-Target Support**: .NET 9.0, 8.0, 7.0, 6.0, 5.0, .NET Standard 2.0/2.1
-   **MSBuildSdk Packaging**: Properly configured for MSBuild SDK project packaging
-   **Automatic Metadata Integration**: Seamlessly includes markdown files in package metadata
-   **Package Validation**: Built-in API compatibility and package validation
-   **Source Link Integration**: Automatic source linking for debugging

### 🔧 **Development Workflow**
-   **Automatic Project References**: Smart cross-project referencing based on project types
-   **Internals Visibility**: Automatic InternalsVisibleTo configuration for test projects
-   **GitHub Integration**: Built-in support for GitHub workflows and CI/CD
-   **Cross-Platform Support**: Compatible with Windows, macOS, and Linux
-   **Documentation Generation**: Automated XML documentation file generation

## Project Structure

-   **Sdk**: Core SDK implementation with MSBuild props and targets
-   **Sdk.Lib**: Library-focused SDK components for class libraries
-   **Sdk.ConsoleApp**: Console application SDK support with cross-platform console configurations
-   **Sdk.ImGuiApp**: ImGui application SDK for modern GUI applications
-   **Sdk.WinApp**: Windows application SDK for platform-specific GUI applications
-   **Sdk.Test**: Testing infrastructure and configuration for unit tests
-   **Sdk.WinTest**: Windows-specific testing infrastructure and configuration

## Usage

To use this SDK in your project:

```xml
<Project Sdk="ktsu.Sdk">
  <PropertyGroup>
    <!-- Your project-specific properties here -->
  </PropertyGroup>
</Project>
```

### Project Type Detection

The SDK automatically detects different project types in your solution:

-   **Primary Project**: The main project of your solution (YourSolution, YourSolution.Core)
-   **Console Projects**: Command-line interface projects (YourSolution.ConsoleApp, YourSolutionConsoleApp, YourSolution.CLI, YourSolutionCLI)
-   **GUI App Projects**: Application projects (YourSolution.App, YourSolutionApp, YourSolution.WinApp, YourSolutionWinApp, YourSolution.ImGuiApp, YourSolutionImGuiApp)
-   **Test Projects**: Test projects (YourSolution.Test, YourSolution.Tests, YourSolutionTest, YourSolutionTests, YourSolution.WinTest, YourSolutionWinTest)

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

## Metadata Files

This SDK supports the following metadata files at the solution root:

-   `README.md`: Project documentation (this file)
-   `AUTHORS.md`: Project authors and contributors
-   `LICENSE.md`: Project license information
-   `CHANGELOG.md`: Version history and changes
-   `DESCRIPTION.md`: Detailed project description
-   `VERSION.md`: Current version information
-   `TAGS.md`: Project tags for NuGet packages
-   `COPYRIGHT.md`: Copyright information
-   `AUTHORS.url`: URL to authors' information
-   `PROJECT_URL.url`: URL to project information

These files are automatically included in the NuGet package and used to populate package metadata.

## Advanced Configuration Features

### Automatic Project References

Projects automatically reference the primary project and expose internals to test projects. Cross-references are intelligently configured based on project types and naming conventions.

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
- **Multi-Platform Testing**: Test configurations for both standard and Windows-specific tests

## Requirements

-   .NET SDK 5.0 or later (optimized for .NET SDK 9.0)

## License

See the [LICENSE.md](LICENSE.md) file for license information.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
