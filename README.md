# ktsu.Sdk

A comprehensive MSBuild-based SDK for .NET projects that standardizes configuration, metadata management, and package workflows. Supports multiple .NET versions (.NET 5.0+, .NET Standard 2.0/2.1) with optimizations for .NET 9.0.

## Features

-   **Multi-Target Support**: Supports .NET 5.0+, .NET Standard 2.0/2.1, with optimizations for .NET 9.0
-   **MSBuildSdk Packaging Support**: Properly configured for MSBuild SDK project packaging
-   **Standardized Project Structure**: Enforces consistent organization across solution components
-   **Metadata Management**: Automatically handles project metadata from markdown files
-   **Package Publishing**: Streamlined NuGet package creation with proper metadata inclusion
-   **Documentation**: Automated inclusion of documentation in packages
-   **GitHub Integration**: Built-in support for GitHub workflows and CI/CD
-   **Cross-Platform Support**: Compatible with Windows, macOS, and Linux
-   **Smart Project Detection**: Automatic detection of primary, console, GUI, and test projects

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

## Key Features

### Automatic Project References

Projects automatically reference the primary project and use appropriate namespaces.

### Standardized Package Creation

Library projects are automatically configured for NuGet packaging with proper metadata.

### Cross-Platform Compatibility

Projects are configured with multiple runtime identifiers for Windows, macOS, and Linux.

### Testing Support

Internals are automatically exposed to test projects, and testing configurations are applied.

## Requirements

-   .NET SDK 5.0 or later (optimized for .NET SDK 9.0)

## License

See the [LICENSE.md](LICENSE.md) file for license information.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
