# Plan: Add iOS and Android Apps as Supported Targets

## Summary

Add two new MSBuild SDK sub-projects — `ktsu.Sdk.iOS` and `ktsu.Sdk.Android` —
that let consuming solutions build native iOS and Android applications with the
same zero-configuration conventions the SDK already provides for console and
desktop GUI apps (`ktsu.Sdk.ConsoleApp`, `ktsu.Sdk.App`).

These sub-SDKs target the platform-specific framework monikers
(`net10.0-ios`, `net10.0-android`), which depend on the .NET `ios`, `android`,
and (optionally) `maui` workloads rather than the cross-platform base runtime.
Because of this, the plan also covers project-type detection, analyzer
requirement adjustments, CI runner changes, and documentation.

## Background / Current State

The SDK is modular. Each app-type sub-SDK is a thin MSBuild SDK package that:

- Imports the shared `Sdk.Common.*` props/targets from the repo root.
- Sets `PackageType=MSBuildSdk` and multi-targets the package build itself.
- Ships a `Sdk.props` that pins the *consuming* project to a single
  `TargetFramework` and an `OutputType`.

For example `Sdk.App/Sdk.props` sets `TargetFramework=net10.0`,
clears `TargetFrameworks`, and sets `OutputType=Exe` (`WinExe` on Windows).

Core conventions live in `Sdk/Sdk.props`:

- Project-type detection by name (`IsAppProject`, `IsCliProject`,
  `IsTestProject`, `IsPrimaryProject`) — `Sdk/Sdk.props` lines ~72-187.
- Default `RuntimeIdentifiers` = `win-x64;win-x86;win-arm64;osx-x64;linux-x64;osx-arm64;linux-arm64`
  (`Sdk/Sdk.props` line ~346) — note these are **desktop** RIDs and do not
  apply to mobile.
- Analyzer KTSU0001 enforces required standard packages (SourceLink, Polyfill,
  System.Memory, System.Threading.Tasks.Extensions) and is fed framework facts
  via `CompilerVisibleProperty`.

iOS/Android differ from existing targets in ways that drive this plan:

- **TFMs are platform-specific:** `net10.0-ios`, `net10.0-android`.
- **Workloads required:** `dotnet workload install ios android maui`. These are
  not present on a stock `dotnet` install or the current CI image.
- **Build host constraints:** Android can be built/packaged on Linux, macOS, or
  Windows; iOS device/simulator builds require a **macOS** host with Xcode.
- **Different RIDs:** iOS uses `ios-arm64`, `iossimulator-x64`,
  `iossimulator-arm64`; Android uses `android-arm64`, `android-arm`,
  `android-x64`, `android-x86`. The desktop RID list must not leak in.
- **Different required properties:** `SupportedOSPlatformVersion`,
  `ApplicationId`, `ApplicationDisplayName`, `ApplicationVersion`, signing, etc.

## Goals

- Provide `ktsu.Sdk.iOS` and `ktsu.Sdk.Android` packages following the existing
  sub-SDK pattern.
- A consuming project referencing one of these SDKs builds for the correct
  platform with sensible defaults and minimal boilerplate.
- Auto-detect `{Solution}.iOS` / `{Solution}.Android` projects so metadata,
  namespaces, and packaging conventions apply consistently.
- Keep analyzer enforcement meaningful (not spuriously failing) on mobile TFMs.
- Document usage and the macOS/workload prerequisites.

## Non-Goals

- Shipping a full MAUI cross-platform single-project SDK (one project, many
  platforms). That is a possible future follow-up; this plan delivers separate
  per-platform app SDKs consistent with the current `Sdk.App` model.
- Provisioning Apple signing certificates / Google Play upload pipelines.
- Changing the desktop `Sdk.App` behavior.

## Proposed Changes

### 1. New sub-SDK: `Sdk.iOS/`

Files (mirroring `Sdk.App/`):

- `Sdk.iOS/Sdk.iOS.csproj` — copy of `Sdk.App/Sdk.App.csproj`, renamed.
  Keeps the multi-targeted package build and `PackageType=MSBuildSdk`, and
  imports the shared `Sdk.Common.*` files plus `Sdk.Common.SdkContent.targets`
  and `Sdk.Common.PackageContent.targets`.
- `Sdk.iOS/Sdk.props`:
  ```xml
  <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <TargetFramework>net10.0-ios</TargetFramework>
      <TargetFrameworks></TargetFrameworks>
      <OutputType>Exe</OutputType>
      <SupportedOSPlatformVersion>15.0</SupportedOSPlatformVersion>
      <!-- iOS device + simulator RIDs only; clear inherited desktop RIDs -->
      <RuntimeIdentifiers>ios-arm64;iossimulator-x64;iossimulator-arm64</RuntimeIdentifiers>
    </PropertyGroup>
  </Project>
  ```
- `Sdk.iOS/Sdk.targets` — empty placeholder (matching convention), reserved for
  future iOS-specific build logic (e.g. signing, bundle defaults).

### 2. New sub-SDK: `Sdk.Android/`

- `Sdk.Android/Sdk.Android.csproj` — copy of the App csproj, renamed.
- `Sdk.Android/Sdk.props`:
  ```xml
  <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
      <TargetFramework>net10.0-android</TargetFramework>
      <TargetFrameworks></TargetFrameworks>
      <OutputType>Exe</OutputType>
      <SupportedOSPlatformVersion>21.0</SupportedOSPlatformVersion>
      <!-- Android RIDs are managed by the android workload; do not inherit desktop RIDs -->
      <RuntimeIdentifiers></RuntimeIdentifiers>
    </PropertyGroup>
  </Project>
  ```
- `Sdk.Android/Sdk.targets` — empty placeholder.

> The exact `SupportedOSPlatformVersion` values and whether to default any
> Android `RuntimeIdentifiers` should be confirmed against the workload's
> defaults during implementation; the values above are conservative starting
> points.

### 3. Core project-type detection (`Sdk/Sdk.props`)

Add detection blocks mirroring the existing `AppProject` block (lines ~103-132)
for the new names, plus corresponding `Is*Project` flags:

- `IsIosProject` for `{Solution}.iOS`, `{Solution}iOS`, `{Solution}.Ios`.
- `IsAndroidProject` for `{Solution}.Android`, `{Solution}Android`,
  `{Solution}.Droid`.

Add these to the same evaluation phase as the other detections so namespace,
metadata, and packaging conventions apply. Consider treating them as
"executable, non-packable" like the other app types (verify against the
`IsExecutable` / `IsPackable` derivation later in `Sdk/Sdk.props`).

### 4. Analyzer requirement adjustments (`Sdk.Analyzers`)

KTSU0001 requires SourceLink/Polyfill/System.Memory/System.Threading.Tasks.Extensions.
On `net10.0-ios` / `net10.0-android`:

- Polyfill / System.Memory / System.Threading.Tasks.Extensions are largely
  unnecessary (modern BCL), exactly as they already are relaxed for modern
  `net*` TFMs. Confirm the existing TFM-based relaxation logic also keys off
  the platform TFMs (the `TargetFramework` / `TargetFrameworkIdentifier`
  compiler-visible properties will carry the platform).
- SourceLink should still be encouraged.

Action: review the requirement matrix in `Sdk.Analyzers` and add explicit
handling for `-ios` / `-android` TFMs so app projects are not flagged for
packages that don't make sense on mobile. Add/extend analyzer unit tests for
these TFMs. Update `make-analyzer-releases.ps1`-tracked release notes if
diagnostic behavior changes.

### 5. Polyfill configuration (`Sdk/Sdk.targets`)

The `Poly*` switches (lines ~76-82) are set for non-test projects. They are
harmless but redundant on modern TFMs. No change strictly required; verify they
don't conflict with the mobile workloads. Gate them off for `-ios`/`-android`
if any warning surfaces.

### 6. Solution + packaging wiring

- Add `Sdk.iOS` and `Sdk.Android` projects to `Sdk.sln`.
- They package the same way (`Sdk.Common.SdkContent.targets` puts
  `Sdk.props`/`Sdk.targets` under `\Sdk` in the nupkg). No new packaging logic.
- Confirm `dotnet pack` produces `ktsu.Sdk.iOS` and `ktsu.Sdk.Android` nupkgs.

### 7. CI/CD (`.github/workflows/dotnet-sdk.yml`)

Current workflow builds on a single runner with .NET SDK 10. Mobile builds need
workloads and, for iOS, macOS:

- Install workloads before build: `dotnet workload install android ios maui`
  (or `wasm-tools` as needed). On Linux/Windows runners, `ios` cannot fully
  build — restrict iOS build/pack steps to a macOS job.
- Recommended: add a small build matrix or a dedicated macOS job that builds the
  mobile SDK packages, while the existing Linux job continues to build/test the
  cross-platform SDKs. Since these sub-SDKs are just MSBuild content packages
  (no compiled assembly that requires the device toolchain to *pack*), pack may
  succeed without the full toolchain — **validate this early**; if pack only
  needs the SDK props/targets content, the existing runner may suffice and the
  macOS requirement applies only to *consumers*.
- Ensure release/publish steps include the two new packages.

### 8. Documentation

- `README.md`: add `ktsu.Sdk.iOS` and `ktsu.Sdk.Android` to the SDK component
  list, with usage snippets and an explicit **prerequisites** note (install
  `android`/`ios` workloads; iOS requires macOS + Xcode).
- `CLAUDE.md`: add the two sub-projects under "Project Structure", document the
  new `IsIosProject`/`IsAndroidProject` detection names, and note the mobile TFMs
  and RID handling.

## Affected Files

| File | Change |
| --- | --- |
| `Sdk.iOS/Sdk.iOS.csproj` | new |
| `Sdk.iOS/Sdk.props` | new |
| `Sdk.iOS/Sdk.targets` | new |
| `Sdk.Android/Sdk.Android.csproj` | new |
| `Sdk.Android/Sdk.props` | new |
| `Sdk.Android/Sdk.targets` | new |
| `Sdk/Sdk.props` | add iOS/Android detection + flags |
| `Sdk.Analyzers/*` | relax KTSU0001 for mobile TFMs + tests |
| `Sdk.sln` | add new projects |
| `.github/workflows/dotnet-sdk.yml` | workloads + macOS job for iOS |
| `README.md`, `CLAUDE.md` | docs |

## Implementation Phases

1. **Scaffold sub-SDKs** — create `Sdk.iOS` and `Sdk.Android` dirs/files, add to
   `Sdk.sln`, confirm `dotnet pack` emits both nupkgs.
2. **Detection** — add `IsIosProject` / `IsAndroidProject` to `Sdk/Sdk.props`;
   wire into executable/packable derivation.
3. **Analyzer** — adjust KTSU0001 requirement matrix for `-ios`/`-android`; add
   unit tests.
4. **Local validation** — build a throwaway consuming `Foo.Android` (Linux/macOS)
   and `Foo.iOS` (macOS) project against the locally packed SDKs using
   `RestoreAdditionalProjectSources`; confirm correct TFM, OutputType, RIDs, and
   that the analyzer doesn't false-positive.
5. **CI** — add workload install and, if iOS device builds are needed in CI, a
   macOS job; ensure publish includes the new packages.
6. **Docs** — update `README.md` and `CLAUDE.md`.

## Testing & Validation

- `dotnet pack --configuration Release --output ./staging` produces
  `ktsu.Sdk.iOS.*.nupkg` and `ktsu.Sdk.Android.*.nupkg`.
- Sample `*.Android` project restores and builds on Linux with the `android`
  workload; produces an `.apk`/`.aab` output as expected.
- Sample `*.iOS` project restores and builds on macOS with the `ios` workload.
- Analyzer test suite passes, including new `-ios`/`-android` cases (no spurious
  KTSU0001).
- Existing console/desktop SDK behavior unchanged (regression check on
  `Sdk.App` / `Sdk.ConsoleApp` consumers).

## Risks & Open Questions

- **iOS in CI:** Confirm whether *packing the SDK* needs the iOS toolchain at
  all (likely not — it's MSBuild content). The macOS requirement may apply only
  to consumers, which would keep CI simple. Validate in Phase 1.
- **Workload version drift:** Mobile workloads are versioned alongside the .NET
  SDK; pin/track them to avoid CI breakage.
- **Default versions:** `SupportedOSPlatformVersion` defaults (iOS 15 / Android
  API 21) and Android RID defaults need confirmation against the workload.
- **MAUI vs. bare platform SDKs:** This plan ships bare per-platform app SDKs.
  If a single multi-targeting MAUI SDK is desired instead, that is a larger,
  separate design.
- **Naming:** Confirm preferred project suffixes (`.iOS`/`.Android` vs.
  `.Droid`, casing of `iOS`) before finalizing detection patterns.

## Rollout

Ship behind the normal release process. Because these are new, additive
packages, there is no breaking change to existing consumers. Announce
prerequisites (workloads, macOS-for-iOS) prominently in release notes.
