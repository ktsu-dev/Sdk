# Plan: Add Platform-Specific App Targets (iOS, Android, Windows, Linux, macOS)

## Summary

Add MSBuild SDK sub-projects that let consuming solutions build apps for
specific platforms with the same zero-configuration conventions the SDK already
provides for console and desktop GUI apps (`ktsu.Sdk.ConsoleApp`,
`ktsu.Sdk.App`):

- **Mobile (new TFMs + workloads):** `ktsu.Sdk.iOS`, `ktsu.Sdk.Android`
- **Desktop (RID specialization of existing app SDKs):** `ktsu.Sdk.Windows`,
  `ktsu.Sdk.Linux`, `ktsu.Sdk.macOS`

The two groups are **fundamentally different** and the plan treats them
differently:

- iOS/Android use platform-specific framework monikers (`net10.0-ios`,
  `net10.0-android`) that depend on .NET **workloads** and (for iOS) a **macOS**
  build host.
- Windows/Linux/macOS desktop apps already build today via `ktsu.Sdk.App` /
  `ktsu.Sdk.ConsoleApp` using **runtime identifiers** on the base `net10.0`
  runtime. The desktop sub-SDKs are therefore thin RID/publish presets, not new
  runtimes — except where a project wants Windows-only UI APIs
  (`net10.0-windows` for WPF/WinForms) or Mac Catalyst (`net10.0-maccatalyst`).

## Background / Current State

The SDK is modular. Each app-type sub-SDK is a thin MSBuild SDK package that:

- Imports the shared `Sdk.Common.*` props/targets from the repo root.
- Sets `PackageType=MSBuildSdk` and multi-targets the package build itself.
- Ships a `Sdk.props` that pins the *consuming* project to a single
  `TargetFramework` and an `OutputType`.

For example `Sdk.App/Sdk.props` sets `TargetFramework=net10.0`, clears
`TargetFrameworks`, sets `OutputType=Exe`, and switches to `WinExe` on Windows.

Core conventions live in `Sdk/Sdk.props`:

- Project-type detection by name (`IsAppProject`, `IsCliProject`,
  `IsTestProject`, `IsPrimaryProject`) — `Sdk/Sdk.props` lines ~72-187.
- Default `RuntimeIdentifiers` = `win-x64;win-x86;win-arm64;osx-x64;linux-x64;osx-arm64;linux-arm64`
  (`Sdk/Sdk.props` line ~346) — these are the **desktop** RIDs.
- Analyzer KTSU0001 enforces required standard packages (SourceLink, Polyfill,
  System.Memory, System.Threading.Tasks.Extensions), fed framework facts via
  `CompilerVisibleProperty`.

So today, "run on Windows/Linux/macOS" is already possible by publishing
`ktsu.Sdk.App`/`ktsu.Sdk.ConsoleApp` with a desktop RID. What's missing is a
named, convention-driven shortcut per desktop OS (and correct handling of the
Windows-only UI TFM and Mac Catalyst).

iOS/Android differ in ways that drive this plan:

- **TFMs are platform-specific:** `net10.0-ios`, `net10.0-android`.
- **Workloads required:** `dotnet workload install ios android maui`. Not
  present on a stock `dotnet` install or the current CI image.
- **Build host constraints:** Android builds on Linux/macOS/Windows; iOS
  device/simulator builds require a **macOS** host with Xcode.
- **Different RIDs:** iOS uses `ios-arm64`, `iossimulator-x64`,
  `iossimulator-arm64`; Android RIDs are managed by the workload.
- **Different required properties:** `SupportedOSPlatformVersion`,
  `ApplicationId`, `ApplicationDisplayName`, `ApplicationVersion`, signing, etc.

## Goals

- Provide `ktsu.Sdk.iOS`, `ktsu.Sdk.Android`, `ktsu.Sdk.Windows`,
  `ktsu.Sdk.Linux`, and `ktsu.Sdk.macOS` packages following the existing
  sub-SDK pattern.
- A consuming project referencing one of these SDKs builds/publishes for the
  correct platform with sensible defaults and minimal boilerplate.
- Auto-detect `{Solution}.iOS` / `.Android` / `.Windows` / `.Linux` / `.macOS`
  projects so metadata, namespaces, and packaging conventions apply
  consistently.
- Keep analyzer enforcement meaningful (not spuriously failing) on each TFM.
- Document usage and prerequisites (mobile workloads, macOS-for-iOS).

## Non-Goals

- Shipping a full MAUI cross-platform single-project SDK (one project, many
  platforms). Possible future follow-up.
- Provisioning Apple/Google signing or store-upload pipelines.
- Deprecating or changing default `ktsu.Sdk.App` / `ktsu.Sdk.ConsoleApp`
  behavior. The desktop sub-SDKs are additive conveniences.

## Proposed Changes

### A. Mobile sub-SDKs (new TFM + workload)

#### A1. `Sdk.iOS/`

Files mirror `Sdk.App/`:

- `Sdk.iOS/Sdk.iOS.csproj` — copy of `Sdk.App/Sdk.App.csproj`, renamed; keeps
  the multi-targeted package build, `PackageType=MSBuildSdk`, and the shared
  `Sdk.Common.*` imports.
- `Sdk.iOS/Sdk.props`:
  ```xml
  <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <TargetFramework>net10.0-ios</TargetFramework>
      <TargetFrameworks></TargetFrameworks>
      <OutputType>Exe</OutputType>
      <SupportedOSPlatformVersion>15.0</SupportedOSPlatformVersion>
      <RuntimeIdentifiers>ios-arm64;iossimulator-x64;iossimulator-arm64</RuntimeIdentifiers>
    </PropertyGroup>
  </Project>
  ```
- `Sdk.iOS/Sdk.targets` — empty placeholder.

#### A2. `Sdk.Android/`

- `Sdk.Android/Sdk.Android.csproj` — renamed copy.
- `Sdk.Android/Sdk.props`:
  ```xml
  <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <TargetFramework>net10.0-android</TargetFramework>
      <TargetFrameworks></TargetFrameworks>
      <OutputType>Exe</OutputType>
      <SupportedOSPlatformVersion>21.0</SupportedOSPlatformVersion>
      <RuntimeIdentifiers></RuntimeIdentifiers>
    </PropertyGroup>
  </Project>
  ```
- `Sdk.Android/Sdk.targets` — empty placeholder.

### B. Desktop sub-SDKs (RID specialization)

These produce self-contained, single-OS publish presets on top of the base
`net10.0` runtime. They default `RuntimeIdentifier` to that OS and narrow the
inherited `RuntimeIdentifiers` list so `dotnet publish` "just works" per OS.
They keep `OutputType` behavior consistent with `Sdk.App` (GUI); a console
variant can reuse `Sdk.ConsoleApp` semantics later if needed.

#### B1. `Sdk.Windows/`

- `Sdk.Windows/Sdk.props`:
  ```xml
  <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <!-- Plain net10.0 unless Windows-only UI APIs (WPF/WinForms) are needed,
           in which case consumers opt into net10.0-windows. -->
      <TargetFramework>net10.0</TargetFramework>
      <TargetFrameworks></TargetFrameworks>
      <OutputType>WinExe</OutputType>
      <RuntimeIdentifiers>win-x64;win-x86;win-arm64</RuntimeIdentifiers>
      <RuntimeIdentifier Condition="'$(RuntimeIdentifier)' == ''">win-x64</RuntimeIdentifier>
    </PropertyGroup>
  </Project>
  ```
  > Open question: ship a second TFM/variant (`net10.0-windows`) or a
  > `UseWindowsForms`/`UseWPF` switch for desktop UI frameworks.

#### B2. `Sdk.Linux/`

- `Sdk.Linux/Sdk.props`:
  ```xml
  <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <TargetFrameworks></TargetFrameworks>
      <OutputType>Exe</OutputType>
      <RuntimeIdentifiers>linux-x64;linux-arm64;linux-musl-x64;linux-musl-arm64</RuntimeIdentifiers>
      <RuntimeIdentifier Condition="'$(RuntimeIdentifier)' == ''">linux-x64</RuntimeIdentifier>
    </PropertyGroup>
  </Project>
  ```
  > Linux has **no** platform-specific TFM; it is purely RID-driven. Consider
  > `musl` RIDs for Alpine/containers.

#### B3. `Sdk.macOS/`

- `Sdk.macOS/Sdk.props`:
  ```xml
  <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <!-- Plain net10.0 for CLI/desktop binaries. Native AppKit UI or Mac
           Catalyst would instead use net10.0-macos / net10.0-maccatalyst,
           which require the macOS/Catalyst workloads + a macOS host. -->
      <TargetFramework>net10.0</TargetFramework>
      <TargetFrameworks></TargetFrameworks>
      <OutputType>Exe</OutputType>
      <RuntimeIdentifiers>osx-x64;osx-arm64</RuntimeIdentifiers>
      <RuntimeIdentifier Condition="'$(RuntimeIdentifier)' == ''">osx-arm64</RuntimeIdentifier>
    </PropertyGroup>
  </Project>
  ```
  > Open question: whether to also offer `net10.0-macos`/`-maccatalyst`
  > variants (workload + macOS host, like iOS) for native Cocoa UI apps.

Each desktop sub-SDK also gets a renamed-copy `.csproj` and an empty
`Sdk.targets`, identical in shape to `Sdk.App`.

### C. Core project-type detection (`Sdk/Sdk.props`)

Add detection blocks mirroring the existing `AppProject` block (lines ~103-132)
plus corresponding `Is*Project` flags:

- `IsIosProject` — `{Solution}.iOS`, `{Solution}iOS`, `{Solution}.Ios`
- `IsAndroidProject` — `{Solution}.Android`, `{Solution}Android`, `{Solution}.Droid`
- `IsWindowsProject` — `{Solution}.Windows`, `{Solution}.Win`, `{Solution}Win`
- `IsLinuxProject` — `{Solution}.Linux`, `{Solution}Linux`
- `IsMacProject` — `{Solution}.macOS`, `{Solution}.MacOS`, `{Solution}.Mac`

Wire into the same evaluation phase so namespace, metadata, and packaging
conventions apply, and into the `IsExecutable`/`IsPackable` derivation (treat as
executable, non-packable, like the other app types).

### D. Analyzer requirement adjustments (`Sdk.Analyzers`)

KTSU0001 requires SourceLink/Polyfill/System.Memory/System.Threading.Tasks.Extensions.

- On all modern TFMs here (`net10.0`, `net10.0-ios`, `net10.0-android`,
  `net10.0-windows`), Polyfill/System.Memory/System.Threading.Tasks.Extensions
  are unnecessary — the existing TFM-based relaxation should already cover the
  base `net10.0` desktop targets. Confirm it also keys off the platform TFM
  suffixes.
- SourceLink should still be encouraged across the board.

Action: review the requirement matrix; add explicit handling for `-ios` /
`-android` (and `-windows`/`-macos`/`-maccatalyst` if those variants are
adopted). Add/extend analyzer unit tests. Update analyzer release notes if
diagnostic behavior changes.

**Validation finding (resolved — no change required):** `net10.0-ios` and
`net10.0-android` both report `TargetFrameworkIdentifier = .NETCoreApp`, so the
existing legacy-TFM gates in `MissingStandardPackagesAnalyzer` already exclude
them from the `System.Memory` and `System.Threading.Tasks.Extensions`
requirements (those fire only for `.NETStandard` / `.NETFramework` /
`netcoreapp2` / `netstandard2.0`). `Polyfill` and SourceLink remain required on
all non-test projects across every TFM — and that is intentional: `Polyfill`'s
`PolyEnsure` source generator provides the `Ensure.NotNull()` API that
KTSU0003/KTSU0004 steer users toward, so it is needed regardless of TFM. The
analyzer therefore needs no mobile-specific change, and no diagnostic behavior
changed (so the analyzer release notes are untouched).

### E. Solution + packaging wiring

- Add all five new projects to `Sdk.sln`.
- Packaging is unchanged: `Sdk.Common.SdkContent.targets` places
  `Sdk.props`/`Sdk.targets` under `\Sdk` in each nupkg.
- Confirm `dotnet pack` produces `ktsu.Sdk.iOS`, `ktsu.Sdk.Android`,
  `ktsu.Sdk.Windows`, `ktsu.Sdk.Linux`, `ktsu.Sdk.macOS`.

### F. CI/CD (`.github/workflows/dotnet-sdk.yml`)

- **Desktop sub-SDKs** are MSBuild content packages — packing them needs no
  extra toolchain, so the existing Linux job builds/packs/publishes them.
- **Mobile sub-SDKs:** install workloads before build
  (`dotnet workload install android ios maui`). iOS cannot fully build on
  Linux/Windows; if iOS device builds are needed in CI, add a **macOS** job.
  Validate early whether *packing the SDK* (vs. a consumer building an app)
  requires the toolchain at all — likely not, since these are content packages.
- Ensure release/publish steps include all five new packages.

**Validation finding (resolved):** *packing the SDK requires no workloads* — the
sub-SDK `.csproj` files target plain `net*`/`netstandard*` (they are MSBuild
content packages); only consuming mobile *app* projects need the workloads.
Consequently:

- The new packages already flow through the existing `dotnet build` /
  `dotnet pack` / publish steps with no workload install and no macOS job. The
  publish glob (`staging/*.nupkg`) picks them up automatically.
- A dedicated `validate-platform-sdks` job (Ubuntu) runs
  `scripts/validate-platform-sdks.sh`, which packs the SDKs to a local feed and
  asserts that a consumer resolves the expected `TargetFramework`, `OutputType`,
  `RuntimeIdentifiers`, and detection flag for each platform — a regression gate
  on every PR. It needs no workloads because property evaluation does not build
  the consumer.
- A macOS job + workloads would only be needed to add *consumer* mobile build
  smoke tests (building an actual `.app`/`.apk`), which remains a possible future
  enhancement.

### G. Documentation

- `README.md`: add all five sub-SDKs to the component list, with usage snippets
  and a **prerequisites** note (mobile workloads; iOS requires macOS + Xcode;
  desktop targets are RID-based and self-contained).
- `CLAUDE.md`: add the sub-projects under "Project Structure", document the new
  `Is*Project` detection names, and note which targets are TFM-based (mobile)
  vs. RID-based (desktop).

## Affected Files

| File | Change |
| --- | --- |
| `Sdk.iOS/`, `Sdk.Android/` (csproj/props/targets) | new (TFM + workload) |
| `Sdk.Windows/`, `Sdk.Linux/`, `Sdk.macOS/` (csproj/props/targets) | new (RID presets) |
| `Sdk/Sdk.props` | add iOS/Android/Windows/Linux/macOS detection + flags |
| `Sdk.Analyzers/*` | relax KTSU0001 for platform TFMs + tests |
| `Sdk.sln` | add five new projects |
| `.github/workflows/dotnet-sdk.yml` | workloads + optional macOS job; publish new packages |
| `README.md`, `CLAUDE.md` | docs |

## Implementation Phases

1. **Scaffold desktop sub-SDKs** (`Windows`/`Linux`/`macOS`) — lowest risk, no
   workloads; add to `Sdk.sln`; confirm `dotnet pack` emits the nupkgs and a
   sample consumer publishes self-contained per OS.
2. **Scaffold mobile sub-SDKs** (`iOS`/`Android`) — add to `Sdk.sln`; confirm
   pack; build a sample Android consumer on Linux and iOS consumer on macOS.
3. **Detection** — add all five `Is*Project` flags to `Sdk/Sdk.props`; wire into
   executable/packable derivation.
4. **Analyzer** — adjust KTSU0001 matrix for platform TFMs; add unit tests.
5. **Local validation** — throwaway consuming projects per platform against the
   locally packed SDKs via `RestoreAdditionalProjectSources`; confirm TFM,
   OutputType, RIDs, and no analyzer false-positives.
6. **CI** — workload install + optional macOS job; ensure publish covers all new
   packages.
7. **Docs** — update `README.md` and `CLAUDE.md`.

## Testing & Validation

- `dotnet pack` produces all five `ktsu.Sdk.*` nupkgs.
- Sample `*.Windows` / `*.Linux` / `*.macOS` projects publish self-contained for
  their OS RID and run.
- Sample `*.Android` builds on Linux (with `android` workload) → `.apk`/`.aab`.
- Sample `*.iOS` builds on macOS (with `ios` workload).
- Analyzer suite passes, including new TFM cases (no spurious KTSU0001).
- Existing `Sdk.App` / `Sdk.ConsoleApp` consumers unchanged (regression check).

## Risks & Open Questions

- **Desktop overlap:** Windows/Linux/macOS targets largely duplicate what
  `Sdk.App`/`Sdk.ConsoleApp` + a RID already do. Confirm the named convenience
  is worth the extra packages, or whether a single property switch on the
  existing app SDKs would be preferable.
- **UI frameworks:** Whether to offer `net10.0-windows` (WPF/WinForms) and
  `net10.0-macos`/`-maccatalyst` (Cocoa) variants — these add workload/host
  requirements similar to mobile.
- **iOS in CI:** Confirm whether packing the SDK needs the iOS toolchain at all
  (likely not — content package). macOS requirement may apply only to consumers.
- **Workload version drift:** Mobile workloads are versioned with the .NET SDK;
  pin/track to avoid CI breakage.
- **Default versions/RIDs:** `SupportedOSPlatformVersion` (iOS 15 / Android 21),
  default desktop RID per OS, and inclusion of `linux-musl-*` need confirmation.
- **Naming/casing:** Confirm preferred suffixes (`.iOS`/`.Android`/`.Windows`/
  `.Linux`/`.macOS` vs. `.Droid`/`.Win`/`.Mac`) before finalizing detection.
- **MAUI alternative:** A single multi-targeting MAUI SDK is a larger, separate
  design if cross-platform single-project is desired.

## Rollout

Ship via the normal release process. All packages are new and additive — no
breaking change to existing consumers. Announce prerequisites (mobile workloads,
macOS-for-iOS; desktop targets are self-contained RID publishes) prominently in
release notes.
