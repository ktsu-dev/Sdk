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

