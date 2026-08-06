# Plan: Distribute Applications as .NET Tools (`ktsu.Sdk.Tool`)

## Summary

Add a `ktsu.Sdk.Tool` sub-SDK so a project can be distributed as a global or
local .NET tool (`dotnet tool install`) with the same zero-configuration
conventions the SDK already provides for libraries, console apps, and GUI apps.

A tool project produces **one RID-agnostic, framework-dependent `.nupkg`** and
nothing else. It is pushed to nuget.org / GitHub Packages / packages.ktsu.dev by
the existing release pipeline with no change to that pipeline. Users installing
the tool need the .NET 10 runtime; consumers who want a standalone binary keep
using a separate `ktsu.Sdk.ConsoleApp` project alongside.

## Background / Current State

The only tool-related logic in the SDK today is one clause in
`Sdk/Sdk.targets:14`:

```xml
<IsPackable Condition="'$(IsLibrary)' == 'true' Or '$(PackAsTool)' == 'true'">true</IsPackable>
```

So a consumer who hand-sets `PackAsTool=true` on a `ktsu.Sdk.ConsoleApp` project
does get a `.nupkg`. Everything else about that package is wrong or unspecified:

- **The command name would be unusable.** `Sdk/Sdk.props:396` sets
  `<AssemblyName>$(RootNamespace)</AssemblyName>` unconditionally, so
  `AssemblyName` is `ktsu.KtsuBuild.CLI`. `ToolCommandName` defaults to
  `AssemblyName`, meaning users would type `ktsu.KtsuBuild.CLI` at the shell.
- **Library-oriented pack settings apply.** `IncludeSource=true`
  (`Sdk/Sdk.props:431`) and `EnablePackageValidation=true`
  (`Sdk/Sdk.props:444`) are global. A `DotnetTool` package has no public API
  contract and carries its entire dependency closure under `tools/`, so
  validation is noise at best and a pack failure at worst.
- **The project stays publishable.** `Sdk/Sdk.targets:13` sets
  `IsPublishable=true` for anything with `OutputType=Exe`, overriding the
  `false` default at `Sdk/Sdk.props:465`. A tool has nothing to publish.
- **Nothing prevents multi-targeting.** The base `ktsu.Sdk` multi-targets eight
  TFMs; a tool package needs exactly one.
- **There is no project type.** No `IsToolProject` alongside `IsCliProject` /
  `IsAppProject` (`Sdk/Sdk.props:289-292`), no name-convention discovery block,
  and no sub-SDK. Tool-ness is an undiscoverable raw MSBuild property.

Two existing defaults are already correct for tools and need no change:
`RollForward=LatestMinor` (`Sdk/Sdk.props:471`) and, in `Sdk.ConsoleApp`, a
single pinned `TargetFramework`.

### CI

Release orchestration is **KtsuBuild** (`C:/dev/ktsu-dev/KtsuBuild`), not the
legacy `scripts/PSBuild.psm1` still present in this repo.

- `DotNetService.PackAsync` (`KtsuBuild/DotNet/DotNetService.cs:181`) packs every
  non-test project and reconstructs `SolutionDir` / `SolutionName` by hand
  because ktsu.Sdk derives package metadata from them. Once `IsPackable` is
  true, a tool package flows to all three feeds with no further change.
- `DotNetService.IsExecutableProject` (`:511`) selects projects for
  self-contained RID publishing. It matches `Sdk="….App/"`, `Sdk="….Ios/"`, or a
  literal `<OutputType>Exe|WinExe</OutputType>` **in the csproj text**. A project
  declaring `<Sdk Name="ktsu.Sdk.Tool" />` matches none of these, so it is packed
  and skipped for RID publishing — the desired behaviour, but incidental rather
  than designed, so it must be pinned by a test.

## Design

### 1. New `ktsu.Sdk.Tool` sub-SDK

`Sdk.Tool/` mirrors `Sdk.ConsoleApp/`:

| File | Contents |
| --- | --- |
| `Sdk.Tool/Sdk.Tool.csproj` | Copy of `Sdk.ConsoleApp.csproj`: `PackageType=MSBuildSdk`, multi-targeted package build, the five shared `Sdk.Common.*` imports |
| `Sdk.Tool/Sdk.props` | Consuming-project shape |
| `Sdk.Tool/Sdk.targets` | Overrides that must run after the base SDK |

`Sdk.Tool/Sdk.props`:

```xml
<TargetFramework>net10.0</TargetFramework>
<TargetFrameworks></TargetFrameworks>
<OutputType>Exe</OutputType>
<PackAsTool>true</PackAsTool>
```

`Sdk.Tool/Sdk.targets` carries everything that depends on values the base SDK
computes. `<Sdk Name="…" />` elements import props at the top of the project and
targets at the bottom, both in declaration order, so a consumer listing
`ktsu.Sdk` before `ktsu.Sdk.Tool` gets `Sdk.Tool/Sdk.targets` evaluated after
`Sdk/Sdk.targets`:

- `ToolCommandName` default (see below).
- `IsPublishable=false`, undoing `Sdk/Sdk.targets:13`.
- `EnablePackageValidation=false`, `ApiCompatValidateAssemblies=false`,
  `IncludeSource=false`.
- A `BeforeTargets="Build;Pack"` `Error` when `TargetFrameworks` is non-empty,
  since a tool package cannot multi-target. This catches a consumer who
  re-declares `TargetFrameworks` in their own `PropertyGroup`.

`IsPackable` needs no change — `Sdk/Sdk.targets:14` already honours
`PackAsTool`. `PackageId` stays `$(AssemblyName)`, so `KtsuBuild.Tool` in
solution `KtsuBuild` publishes as package `ktsu.KtsuBuild.Tool` providing
command `ktsubuild`.

### 2. `ToolCommandName` derivation

```
ToolCommandName = lowercase($(SolutionName))
```

Falling back, when `SolutionName` is empty, to the project name with a `.Tool`
or `.CLI` suffix stripped. Spaces are removed. Always overridable by an explicit
`ToolCommandName` in the consuming project, and projects with short or generic
solution names are expected to set it.

### 3. Project-type detection

Add to `Sdk/Sdk.props`:

- A `ToolProjectName` / `ToolProjectFileName` / `ToolProjectPath` /
  `ToolProjectExists` discovery chain matching the existing `CliProject*` and
  `AppProject*` blocks.
- `IsToolProject`, set next to `IsCliProject` / `IsAppProject` at
  `Sdk/Sdk.props:289-292`.

Conventions: `{Solution}.Tool` and `{Solution}Tool` **only**. `.CLI` is
deliberately excluded so that no existing console project becomes a tool package
implicitly.

### 4. CI

No change to KtsuBuild's artifact selection is required — see Background. One
item needs empirical verification:

> `PackAsync` packs with `--no-build` (`DotNetService.cs:228`), and tool packing
> does publish-shaped work at pack time. Verify that `dotnet pack --no-build`
> against a previously-built tool project produces a valid tool package.

If it does not, the fix belongs in KtsuBuild (drop `--no-build` for tool
projects), which is a **separate repository and out of scope for this plan**.
Record the finding and open an issue there.

Within this repo, the new package must be registered wherever sub-SDKs are
enumerated:

- `examples/global.json` (`msbuild-sdks`)
- `test/Sdk.Examples.Tests/Infrastructure/RepoLayout.cs:31-32`
- `test/Sdk.Examples.Tests/Infrastructure/ExampleWorkspace.cs:87`

### 5. Examples and tests

`examples/demos/Tool/` mirroring `examples/demos/ConsoleApp/`: a minimal project
declaring `ktsu.Sdk` + `ktsu.Sdk.Tool` with a trivial `Program.cs`.

Tests in `test/Sdk.Examples.Tests`:

1. **Property resolution** (modelled on `PlatformSdkResolutionTests.cs`):
   `PackAsTool=true`, `ToolCommandName` equals the lowercased solution name,
   `IsPackable=true`, `IsPublishable=false`, `TargetFrameworks` empty.
2. **Build**, via the existing `DemoBuildTests.cs` mechanism.
3. **Pack** — over both pack paths (plain and `--no-build`, the one CI uses): assert the produced
   `.nupkg` contains `tools/net10.0/any/` with `DotnetToolSettings.xml` naming the expected command,
   the entry-point assembly, and the runtimeconfig; then **install the tool and run it**. Manifest
   presence alone is not proof — a package whose publish was suppressed still has the manifest.
4. **RID-publish exclusion** — assert the demo csproj text contains no literal
   `<OutputType>` and no `Sdk="….App/"`, pinning the KtsuBuild
   `IsExecutableProject` behaviour that keeps tools out of the zip publish path.

### 6. Documentation

- `README.md`: project-type table, properties list (`IsToolProject`),
  and a short "distributing as a dotnet tool" section.
- `CLAUDE.md`: sub-SDK list, project type detection list, and the
  `ToolCommandName` derivation rule.

## Out of Scope

- Converting `KtsuBuild.CLI` to a tool project.
- Any change to the KtsuBuild repository.
- Self-contained or RID-specific tool packages (.NET 10 per-RID tool
  packaging). Tools here are framework-dependent and require the .NET 10 runtime
  on the user's machine.
- Standalone binary distribution for tool projects; that remains the job of a
  separate `ktsu.Sdk.ConsoleApp` project.

## Implementation Notes

Two things the design did not anticipate, found while building it:

**RID-specific tool packages are opt-out, not opt-in.** The core SDK sets a seven-entry
`RuntimeIdentifiers` list (`Sdk/Sdk.props:474`). Under `PackAsTool` the .NET 10 SDK turns each
entry into a separate RID-specific tool package, so one `dotnet pack` emitted
`Demo.Tool.win-x64…`, `Demo.Tool.linux-x64…` and five more — building concurrently into a single
intermediate output directory, which also produced MSB4018 file-contention failures. Since this
plan chose RID-agnostic packages, `ktsu.Sdk.Tool` clears `RuntimeIdentifiers`.

**`ToolCommandName` must be derived in `Sdk.props`, not `Sdk.targets`.** Microsoft.NET.Sdk
defaults it to `AssemblyName` in its targets, so a conditional assignment in a `.targets` file is
too late. (When iterating on SDK content locally without bumping the version, the extracted
package in the global packages folder is stale — clear `~/.nuget/packages/ktsu.sdk.tool` between
runs or the old behaviour persists.)

**`IsPublishable` must be `true`, and must be set in props** (found while converting the first real
consumer, after the broken 2.16.0 shipped). `PackAsTool` builds the `tools/` payload from a publish,
which is gated on `IsPublishable`. Setting it false — as this plan specified, to keep CI from
zipping tool projects — produced a package containing only `DotnetToolSettings.xml` and none of the
assemblies it points at: it installs, then fails at run time. Tool projects are kept out of RID zip
publishing by project *selection* instead, which was always the actual mechanism (§4).

Simply removing the `false` is not enough. The core SDK defaults `IsPublishable` to false in
`Sdk/Sdk.props` and flips it true for `OutputType=Exe` in `Sdk/Sdk.targets`, and that flip is too
late — pack has already decided by then, the same import-ordering trap as `ToolCommandName`. The
Tool SDK sets `IsPublishable=true` in its own `Sdk.props`.

The test hole mattered as much as the bug: asserting `DotnetToolSettings.xml` exists passes against
a package with no payload at all. §5.3 now asserts the entry-point assembly and runtimeconfig are
present **and installs the tool and runs it**, over both pack paths.

**`dotnet pack --no-build` is fine.** The question flagged in §4 is resolved: with the publish
enabled, packing a previously-built tool project with `--no-build` produces a complete payload, so
KtsuBuild's `PackAsync` needs no change. The test covers both pack paths.

Packing also requires the metadata files the SDK declares (`LICENSE.md`, `README.md`, `icon.png`)
to exist in the solution directory, or pack fails NU5030/NU5039/NU5046. This is pre-existing
behaviour for every packable project, not tool-specific; the tool demo carries those files
because it is the only demo that packs.

## Risks

| Risk | Mitigation |
| --- | --- |
| `dotnet pack --no-build` produces an invalid tool package | Pack test (§5.3) catches it in this repo's CI; fix lands in KtsuBuild |
| A short solution name yields a poor command (`Sdk` → `sdk`) | `ToolCommandName` is always overridable; documented in README |
| Import ordering differs from expectation, so `Sdk.Tool/Sdk.targets` cannot see base-SDK values | Property-resolution test (§5.1) asserts final evaluated values, not the props in isolation |
| A future `.CLI` project is expected to be a tool | Explicitly excluded from detection; consumers opt in with the Tool SDK |
