## v2.6.2-pre.5 (prerelease)

Changes since v2.6.2-pre.4:

- Sync COPYRIGHT.md ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v2.6.2-pre.4 (prerelease)

Changes since v2.6.2-pre.3:

- Merge remote-tracking branch 'refs/remotes/origin/main' ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\update-winget-manifests.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v2.6.2-pre.3 (prerelease)

Changes since v2.6.2-pre.2:

- Sync scripts\PSBuild.psm1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\update-winget-manifests.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v2.6.2-pre.2 (prerelease)

Changes since v2.6.2-pre.1:

- Merge remote-tracking branch 'refs/remotes/origin/main' ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\PSBuild.psm1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v2.6.2-pre.1 (prerelease)

Changes since v2.6.1:

- Sync scripts\PSBuild.psm1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v2.6.1 (patch)

Changes since v2.6.0:

- Bump version to 2.6.1-pre.1, update COPYRIGHT.md, and add changelog entry ([@matt-edmondson](https://github.com/matt-edmondson))
## v2.6.1-pre.1 (prerelease)

Incremental prerelease update.
## v2.6.0 (minor)

Changes since v2.5.0:

- Enhance SDK documentation and metadata handling ([@matt-edmondson](https://github.com/matt-edmondson))
## v2.5.1-pre.1 (prerelease)

Changes since v2.5.0:

- Sync scripts\PSBuild.psm1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\update-winget-manifests.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v2.5.0 (minor)

Changes since v2.4.0:

- Fix null check code fix provider to use non-nullable argument and update Sdk.targets for additional properties ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove .github\workflows\project.yml ([@matt-edmondson](https://github.com/matt-edmondson))
## v2.4.1 (patch)

Changes since v2.4.0:

- Remove .github\workflows\project.yml ([@matt-edmondson](https://github.com/matt-edmondson))
## v2.4.0 (minor)

Changes since v2.3.0:

- Add ManualNullCheckAnalyzer and CodeFixProvider to enforce Ensure.NotNull usage ([@matt-edmondson](https://github.com/matt-edmondson))
## v2.3.1-pre.1 (prerelease)

Changes since v2.3.0:

- Merge remote-tracking branch 'refs/remotes/origin/main' ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .gitignore ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v2.3.0 (minor)

Changes since v2.2.0:

- Add an analyzer to enforce usage of Ensure.NotNull ([@matt-edmondson](https://github.com/matt-edmondson))
- Add check to skip non-application projects in build workflow ([@matt-edmondson](https://github.com/matt-edmondson))
## v2.2.2-pre.4 (prerelease)

Changes since v2.2.2-pre.3:

- Merge remote-tracking branch 'refs/remotes/origin/main' ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\update-winget-manifests.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v2.2.2-pre.3 (prerelease)

Changes since v2.2.2-pre.2:

- Sync COPYRIGHT.md ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v2.2.2-pre.2 (prerelease)

Changes since v2.2.2-pre.1:

- Sync .gitignore ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync COPYRIGHT.md ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\PSBuild.psm1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v2.2.2-pre.1 (prerelease)

Changes since v2.2.1:

- Bump the microsoft group with 2 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v2.2.1 (patch)

Changes since v2.2.0:

- Add check to skip non-application projects in build workflow ([@matt-edmondson](https://github.com/matt-edmondson))
## v2.2.1-pre.1 (prerelease)

Incremental prerelease update.
## v2.2.0 (minor)

Changes since v2.1.0:

- Update analyzer package reference to use dynamic version ([@matt-edmondson](https://github.com/matt-edmondson))
- Upgrade .NET SDK version from 9.0 to 10.0 across project files and documentation ([@matt-edmondson](https://github.com/matt-edmondson))
## v2.1.0 (minor)

Changes since v2.0.0:

- Migrate PolyGuard to PolyEnsure ([@matt-edmondson](https://github.com/matt-edmondson))
## v2.0.0 (major)

Changes since v1.76.0:

- [major] Add Roslyn analyzers to enforce SDK requirements and remove UseStringLengthAnalyzer ([@matt-edmondson](https://github.com/matt-edmondson))
- Add ktsu.Sdk.Analyzers project with initial analyzer implementation and metadata files ([@matt-edmondson](https://github.com/matt-edmondson))
- Add PolyGuard and PolyNullability properties for non-test projects ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance assembly name check to support strong-named assemblies in MissingInternalsVisibleToAttributeAnalyzer ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance MissingStandardPackagesAnalyzer to validate additional package references via MSBuild properties ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix attribute syntax for InternalsVisibleTo in code fix provider ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix erroneous spaces in solution discovery ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix solution directory discovery logic for great-grandparent level ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor project files to import shared configuration before SDK settings ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor RequiresSystemMemory method to simplify null checks on targetFramework ([@matt-edmondson](https://github.com/matt-edmondson))
- Update Sdk.targets to replace {version} placeholder with actual SDK version in release script ([@matt-edmondson](https://github.com/matt-edmondson))
- Update SetPackageReferenceProperties target to run before GenerateMSBuildEditorConfigFile ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.76.0 (minor)

Changes since v1.75.0:

- Add CLAUDE.md for SDK guidance and build instructions ([@matt-edmondson](https://github.com/matt-edmondson))
- add manual triggering capability to the dotnet GitHub workflow ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance README.md with detailed installation instructions, usage examples, and advanced configuration features for ktsu.Sdk ([@matt-edmondson](https://github.com/matt-edmondson))
- Update package references to latest versions in Sdk.targets ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.75.0 (minor)

Changes since v1.74.0:

- Remove System.Runtime.Numerics package reference from Sdk.targets to simplify target framework support. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.74.0 (minor)

Changes since v1.73.0:

- Replace Microsoft.Bcl.Numerics with System.Runtime.Numerics package reference in Sdk.targets and remove SuppressTfmSupportBuildWarnings property to streamline target framework support. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.73.0 (minor)

Changes since v1.72.0:

- Add SuppressTfmSupportBuildWarnings property to Sdk.targets to suppress build warnings related to target framework support. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.72.0 (minor)

Changes since v1.71.0:

- Add Microsoft.Bcl.Numerics package reference with version override in Sdk.targets for .NETStandard and .NETFramework compatibility ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.71.0 (minor)

Changes since v1.70.0:

- Refine Sdk.targets by adding conditions to Polyfill package reference and PolyGuard/PolyNullability properties to ensure they are only applied to non-test projects. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.70.0 (minor)

Changes since v1.69.0:

- Update package references in Sdk.targets with version overrides and add new properties for PolyGuard and PolyNullability to enhance project configuration. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.69.0 (minor)

Changes since v1.68.0:

- Update line ending configurations in .editorconfig and .gitattributes, and add target framework properties to Sdk.props, Sdk.App.props, and Sdk.ConsoleApp.props for improved project consistency. Also, clean up .gitignore and update scripts for better functionality. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.68.0 (minor)

Changes since v1.67.0:

- Add NoWarn property for test projects in Sdk.targets to suppress specific warnings ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.67.0 (minor)

Changes since v1.66.0:

- Consolidate target framework settings across project files by adding TargetFrameworks property to Sdk.csproj, Sdk.App.csproj, and Sdk.ConsoleApp.csproj, while removing redundant entries from Directory.Build.props and Sdk.props. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.66.0 (minor)

Changes since v1.65.0:

- Remove net9.0 target framework from Directory.Build.props and clean up unused file links in Directory.Build.targets to streamline project configuration. ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove Sdk.Test project from solution to streamline project structure and eliminate obsolete configurations. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update target framework settings in project files and remove obsolete test project configurations. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.65.0 (minor)

Changes since v1.64.0:

- Enable target framework net9.0 in Sdk.props to support the latest .NET version. ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor Sdk solution by removing Sdk.WinApp and Sdk.WinTest projects, and adding Sdk.App project with multi-targeting support. Update Sdk.Test properties to explicitly include supported target frameworks. ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove code coverage collection configuration from Sdk.props to simplify test project setup. ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove commented-out test results configuration from Sdk.props to clean up project properties. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.64.0 (minor)

Changes since v1.63.0:

- Refactor Sdk.props and Sdk.targets to remove unused RunSettingsFile references and streamline package data inclusion for non-test projects. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.63.0 (minor)

Changes since v1.62.0:

- Add test results configuration and enable code coverage collection in Sdk.props ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.62.0 (minor)

Changes since v1.61.0:

- Remove coverage-related properties and dependencies from Sdk.props to simplify test project configuration. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.61.0 (minor)

Changes since v1.60.0:

- Refine project inclusion logic in Sdk.targets to show solution scoped files only in non-test projects, enhancing clarity and organization. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.60.0 (minor)

Changes since v1.59.0:

- Refactor SDK structure by removing Sdk.Lib and updating project files to streamline SDK usage. Enhance README.md with clearer project type descriptions and usage instructions. Remove outdated version update script from workflows. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.59.0 (minor)

Changes since v1.58.0:

- Enhance SDK documentation and features in DESCRIPTION.md and README.md. Improve project structure detection, error handling, and package management capabilities. Update Sdk.props and Sdk.targets for better namespace generation and project inclusion logic. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.58.0 (minor)

Changes since v1.57.0:

- Refine conditions in Sdk.targets to improve project file inclusion logic for primary and test projects. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.57.0 (minor)

Changes since v1.56.0:

- Add publishing step for libraries to ktsu.dev in GitHub Actions workflow and refine conditions in Sdk.targets ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.56.0 (minor)

Changes since v1.55.0:

- Update TargetFrameworks in Sdk.props to include additional frameworks for improved compatibility ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.55.0 (minor)

Changes since v1.54.0:

- Refactor SDK import statements in Sdk.props files and remove redundant imports from Sdk.targets files across multiple projects to enhance modularity and maintainability. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.54.0 (minor)

Changes since v1.53.0:

- Enhance SDK import statements in Sdk.targets files for multiple projects to improve modularity and maintainability. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.53.0 (minor)

Changes since v1.52.0:

- Refactor SDK import statements in Sdk.props files for ConsoleApp and ImGuiApp to enhance modularity and maintainability. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.52.0 (minor)

Changes since v1.51.0:

- Remove redundant SDK import statements from Sdk.targets files across multiple projects to streamline project configuration. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.51.0 (minor)

Changes since v1.50.0:

- Add SDK metadata and update project configurations ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.50.0 (minor)

Changes since v1.49.0:

- Remove deprecated files and update project configurations for multi-targeting support ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.49.0 (minor)

Changes since v1.48.0:

- Update project configuration and build scripts ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.48.0 (minor)

Changes since v1.47.0:

- Fix PackageReleaseNotes condition syntax in Sdk.props for NuGet compliance ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.47.0 (minor)

Changes since v1.46.0:

- Enhance PackageReleaseNotes handling in Sdk.props ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.46.0 (minor)

Changes since v1.45.0:

- Add coverage configuration to Sdk.props ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.45.0 (minor)

Changes since v1.44.0:

- Add Microsoft.Testing.Extensions.CodeCoverage package reference to Sdk.props ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.44.0 (minor)

Changes since v1.43.0:

- Add Microsoft.NET.Sdk to Sdk.props for enhanced project configuration ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.43.0 (minor)

Changes since v1.42.0:

- Enhance test project configuration in Sdk.props ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.42.0 (minor)

Changes since v1.41.0:

- Standardize library usage across projects in MSBuild SDK ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.41.0 (minor)

Changes since v1.40.0:

- Update SDK documentation and streamline scripts ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.40.0 (minor)

Changes since v1.39.0:

- [minor] New sdk version ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.39.1-pre.1 (prerelease)

Changes since v1.39.0:
## v1.39.0 (minor)

Changes since v1.38.0:

- Remove self-contained and single-file publish settings ([@matt-edmondson](https://github.com/matt-edmondson))
- Update configuration files and dependencies ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.38.0 (minor)

Changes since v1.37.0:

- Update SelfContained property in Sdk.props ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.37.0 (minor)

Changes since v1.36.0:

- Enable self-contained deployment in Sdk.props ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.36.0 (minor)

Changes since v1.35.0:

- Update runtime identifiers and publishing properties ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.35.0 (minor)

Changes since v1.34.0:

- Refactor project structure and update dependencies ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.34.0 (minor)

Changes since v1.33.0:

- Update SDK configurations and add centralized package management ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.33.0 (minor)

Changes since v1.32.0:

- Update project to .NET 9.0 and enhance configuration ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.32.0 (minor)

Changes since v1.31.0:

- Update PackageReferences in Sdk.targets ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.31.0 (minor)

Changes since v1.30.0:

- Update package references and manage versions centrally ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.30.0 (minor)

Changes since v1.29.0:

- Cleanup: Remove unused ktsu package references ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.29.0 (minor)

Changes since v1.28.0:

- Add WinApp adk ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.28.0 (minor)

Changes since v1.27.0:

- [minor] Remove duplicate CommandLineParser package reference ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.27.0 (minor)

Changes since v1.26.0:

- [minor] Fix typo in TestProjectNamespace ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.26.0 (minor)

Changes since v1.25.0:

- [minor] Fix InternalsVisibleTo for tests ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.25.0 (minor)

Changes since v1.24.0:

- [minor] Fix conditionals for internals ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.24.0 (minor)

Changes since v1.23.0:

- [minor] Fix test package namespace ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.23.0 (minor)

Changes since v1.22.0:

- [minor] Fix incorrect test package namespace ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.22.0 (minor)

Changes since v1.21.0:

- [minor] Upgrade DeepCLone ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.21.0 (minor)

Changes since v1.20.0:

- [minor] fix version ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.20.1-pre.1 (prerelease)

Changes since v1.20.0:
## v1.20.0 (minor)

Changes since v1.19.0:

- [minor] fix versions ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.19.1-pre.1 (prerelease)

Changes since v1.19.0:
## v1.19.0 (minor)

Changes since v1.18.0:

- [minor] Downgrade TestableIO package versions ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.18.0 (minor)

Changes since v1.17.0:

- [minor] Update TestableIO packages in Sdk.targets ([@matt-edmondson](https://github.com/matt-edmondson))
- [patch] fix package version ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.17.1 (patch)

Changes since v1.17.0:

- [patch] fix package version ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.17.1-pre.1 (prerelease)

Incremental prerelease update.
## v1.17.0 (minor)

Changes since v1.16.0:

- Refactor package references in Sdk.targets ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.16.0 (minor)

Changes since v1.15.0:

- Enhance project existence checks in Sdk.props ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.15.0 (minor)

Changes since v1.14.0:

- Enhance SDK documentation and features for .NET 9.0 ([@matt-edmondson](https://github.com/matt-edmondson))
- Update Sdk.targets to reference ktsu.Semantics instead of ktsu.SemanticString ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.14.1 (patch)

Changes since v1.14.0:

- Enhance SDK documentation and features for .NET 9.0 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.14.0 (minor)

Changes since v1.13.0:

- Update project structure and dependencies ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.13.0 (minor)

Changes since v1.12.0:

- Add MSBuildSdk packaging support to project files ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove multi-targeting for .NET frameworks ([@matt-edmondson](https://github.com/matt-edmondson))
- Update projects to target .NET 9.0 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.12.1 (patch)

Changes since v1.12.0:

- Add MSBuildSdk packaging support to project files ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.12.0 (minor)

Changes since v1.11.0:

- Remove TargetFramework properties from Sdk.props ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.11.1-pre.2 (prerelease)

Changes since v1.11.1-pre.1:

- Sync .editorconfig ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .gitattributes ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .gitignore ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .mailmap ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .runsettings ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.11.1-pre.1 (prerelease)

Changes since v1.11.0:

- Sync .editorconfig ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .gitattributes ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .gitignore ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .mailmap ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .runsettings ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.11.0 (minor)

Changes since v1.10.0:

- [patch] Fix tags file not being shown in the solution explorer ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor primary project handling in Sdk.props and Sdk.targets ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.10.2-pre.1 (prerelease)

Changes since v1.10.1:

- Update dotnet-sdk.yml ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.10.1 (patch)

Changes since v1.10.0:

- [patch] Fix tags file not being shown in the solution explorer ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.10.0 (minor)

Changes since v1.9.0:

- [minor] Refactor project structure for centralized settings ([@matt-edmondson](https://github.com/matt-edmondson))
- Add TAGS.md support and enhance project metadata ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance SDK documentation and add Sdk.CLI project ([@matt-edmondson](https://github.com/matt-edmondson))
- Update Sdk.props for output type configuration ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.9.0 (minor)

Changes since v1.8.0:

- [minor] Support primary project suffixes: Core, App, CLI ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.8.1-pre.5 (prerelease)

Changes since v1.8.1-pre.4:

- Sync .github\workflows\dependabot-merge.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .github\workflows\project.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.8.1-pre.4 (prerelease)

Changes since v1.8.1-pre.3:

- Refactor changelog generation logic in PowerShell script to implement more flexible commit filtering. Added progressive fallback mechanisms to ensure commits are captured for both regular and prerelease versions, improving the accuracy of the changelog output. ([@github-actions[bot]](https://github.com/github-actions[bot]))
## v1.8.1-pre.3 (prerelease)

Changes since v1.8.1-pre.2:

- Refactor changelog generation logic in PowerShell script to improve commit range determination. Enhanced handling for the newest version and added fallback for relaxed filters when no commits are found. Updated output messages for better clarity. ([@github-actions[bot]](https://github.com/github-actions[bot]))
## v1.8.1-pre.2 (prerelease)

Changes since v1.8.1-pre.1:

- Refactor changelog generation and versioning logic in PowerShell scripts. Updated commit range handling to include the latest changes since the last tag, improved output formatting, and added fallback logic for versioning when no tags are found. ([@github-actions[bot]](https://github.com/github-actions[bot]))
- Remove security analysis step from GitHub Actions workflow and update permissions accordingly. ([@github-actions[bot]](https://github.com/github-actions[bot]))
## v1.8.1-pre.1 (prerelease)

Changes since v1.8.0:

- Enhance GitHub Actions workflow by adding security analysis and dependency detection steps. Updated permissions to include security-events and id-token for improved security scanning and dependency submission. ([@github-actions[bot]](https://github.com/github-actions[bot]))
## v1.8.0 (minor)

Changes since v1.7.0:

- [minor] 1.8.0 ([@github-actions[bot]](https://github.com/github-actions[bot]))
- [minor] Update workflow and add Demo project ([@github-actions[bot]](https://github.com/github-actions[bot]))
- Remove Demo project and associated files ([@github-actions[bot]](https://github.com/github-actions[bot]))
- Update project configuration and versioning scripts ([@github-actions[bot]](https://github.com/github-actions[bot]))
## v1.7.1-pre.2 (prerelease)

Changes since v1.7.1-pre.1:

- [minor] Update workflow and add Demo project ([@github-actions[bot]](https://github.com/github-actions[bot]))
- Remove Demo project and associated files ([@github-actions[bot]](https://github.com/github-actions[bot]))
## v1.7.1-pre.1 (prerelease)

Changes since v1.7.0:

- Update project configuration and versioning scripts ([@github-actions[bot]](https://github.com/github-actions[bot]))
## v1.7.0 (minor)

Changes since v1.6.0:

- Update testing platform command line arguments ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.6.0 (minor)

Changes since v1.5.0:

- Add Sdk.Lib project and update SDK configurations ([@matt-edmondson](https://github.com/matt-edmondson))
- Update AssemblyName to use dynamic MSBuildProjectName ([@matt-edmondson](https://github.com/matt-edmondson))
- Update testing platform configuration in Sdk.props ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.5.1 (patch)

Changes since v1.5.0:

- Add Sdk.Lib project and update SDK configurations ([@matt-edmondson](https://github.com/matt-edmondson))
- Update AssemblyName to use dynamic MSBuildProjectName ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.5.0 (minor)

Changes since v1.4.0:

- Update SDK references in Sdk.props ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.4.0 (minor)

Changes since v1.3.0:

- Update SDK references for versioning consistency ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.3.0 (minor)

Changes since v1.2.0:

- Update project files to use new SDK references ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.2.0 (minor)

Changes since v1.1.0:

- Add Sdk.App and Sdk.Test projects to solution ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.1.0 (major)

- Initial commit ([@matt-edmondson](https://github.com/matt-edmondson))

