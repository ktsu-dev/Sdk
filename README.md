# ktsu.Sdk

A comprehensive .NET SDK that simplifies project configuration, metadata management, and package creation for .NET applications targeting .NET 9.0.

## Features

-   **Focused on .NET 9.0**: Optimized for the latest .NET framework
-   **MSBuildSdk Packaging Support**: Properly configured for MSBuild SDK project packaging
-   **Standardized Project Structure**: Enforces consistent organization across solution components
-   **Metadata Management**: Automatically handles project metadata from markdown files
-   **Package Publishing**: Streamlined NuGet package creation with proper metadata inclusion
-   **Documentation**: Automated inclusion of documentation in packages
-   **GitHub Integration**: Built-in support for GitHub workflows and CI/CD
-   **Cross-Platform Support**: Compatible with Windows, macOS, and Linux
-   **Smart Project Detection**: Automatic detection of primary, CLI, App, and Test projects

## Project Structure

-   **Sdk**: Core SDK implementation with MSBuild props and targets
-   **Sdk.App**: Application-specific SDK extensions
-   **Sdk.Lib**: Library-focused SDK components
-   **Sdk.CLI**: Command-line application SDK support
-   **Sdk.Test**: Testing infrastructure and configuration

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
-   **CLI Project**: Command-line interface projects (YourSolution.CLI, YourSolutionCLI)
-   **App Project**: Application projects (YourSolution.App, YourSolutionApp)
-   **Test Project**: Test projects (YourSolution.Test, YourSolution.Tests)

Each project type receives appropriate default settings and references.

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
-   `PROJECT.url`: URL to project information

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

-   .NET SDK 9.0 or later

## License

See the [LICENSE.md](LICENSE.md) file for license information.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
