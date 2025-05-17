# ktsu.Sdk

A comprehensive .NET SDK that simplifies project configuration, metadata management, and package creation for .NET applications targeting .NET 8 and .NET 9.

## Features

- **Multi-Target Framework Support**: Built for .NET 8.0 and .NET 9.0
- **Standardized Project Structure**: Enforces consistent organization across solution components
- **Metadata Management**: Automatically handles project metadata from markdown files
- **Package Publishing**: Streamlined NuGet package creation with proper metadata inclusion
- **Documentation**: Automated inclusion of documentation in packages
- **GitHub Integration**: Built-in support for GitHub workflows and CI/CD

## Project Structure

- **Sdk**: Core SDK implementation with MSBuild props and targets
- **Sdk.App**: Application-specific SDK extensions
- **Sdk.Lib**: Library-focused SDK components
- **Sdk.CLI**: Command-line application SDK support
- **Sdk.Test**: Testing infrastructure and configuration

## Usage

To use this SDK in your project:

```xml
<Project Sdk="ktsu.Sdk">
  <PropertyGroup>
    <!-- Your project-specific properties here -->
  </PropertyGroup>
</Project>
```

## Metadata Files

This SDK supports the following metadata files at the solution root:

- `README.md`: Project documentation (this file)
- `AUTHORS.md`: Project authors and contributors
- `LICENSE.md`: Project license information
- `CHANGELOG.md`: Version history and changes
- `DESCRIPTION.md`: Detailed project description
- `VERSION.md`: Current version information
- `TAGS.md`: Project tags for NuGet packages
- `COPYRIGHT.md`: Copyright information

## Requirements

- .NET SDK 8.0 or later

## License

See the [LICENSE.md](LICENSE.md) file for license information.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
