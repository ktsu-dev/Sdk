# ktsu.Sdk examples

This directory contains runnable example projects for the `ktsu.Sdk` family of MSBuild SDKs.
They serve two purposes:

1. **Demos / smoke tests** (`demos/`) — minimal, copy-pasteable consumers of each SDK that
   prove the SDK works end-to-end.
2. **Analyzer triggers** (`analyzers/`) — one project per analyzer, each crafted to violate
   exactly one rule, proving the diagnostic fires.

All of these are exercised automatically by the MSTest integration project in
[`../test/Sdk.Examples.Tests`](../test/Sdk.Examples.Tests), which packs the SDK to a local
feed and builds each example against it. That project runs in CI via the
`example-integration-tests` job.

## Demos (`demos/`)

| Demo | SDK(s) | What it proves |
| --- | --- | --- |
| `Library` | `ktsu.Sdk` | A packable library builds and satisfies the analyzer requirements. |
| `ConsoleApp` | `ktsu.Sdk` + `ktsu.Sdk.ConsoleApp` | `net10.0` console executable. |
| `App` | `ktsu.Sdk` + `ktsu.Sdk.App` | GUI/ImGui-style app (`WinExe` on Windows, `Exe` elsewhere). |
| `Windows` | `ktsu.Sdk` + `ktsu.Sdk.Windows` | `WinExe`, Windows RIDs. |
| `Linux` | `ktsu.Sdk` + `ktsu.Sdk.Linux` | `Exe`, Linux RIDs. |
| `macOS` | `ktsu.Sdk` + `ktsu.Sdk.macOS` | `Exe`, macOS RIDs. |
| `iOS` | `ktsu.Sdk` + `ktsu.Sdk.iOS` | `net10.0-ios` (requires the `ios` workload + macOS host to build). |
| `Android` | `ktsu.Sdk` + `ktsu.Sdk.Android` | `net10.0-android` (requires the `android` workload to build). |
| `Test` | `ktsu.Sdk` | Library + MSTest project: test-project detection and `InternalsVisibleTo`. |

`Library`, `ConsoleApp`, `App`, `Linux` and `Test` build fully on a Linux runner. The
remaining platform SDKs need another OS or a workload, so CI verifies them by **property
evaluation** (`TargetFramework`, `OutputType`, `RuntimeIdentifiers`, detection flag) instead
of a full build.

## Analyzer triggers (`analyzers/`)

Each folder is an isolated solution that triggers a single diagnostic:

| Example | Diagnostic | How it triggers |
| --- | --- | --- |
| `KTSU0001-MissingStandardPackages` | KTSU0001 | Omits the required SourceLink / Polyfill packages. |
| `KTSU0002-MissingInternalsVisibleTo` | KTSU0002 | A non-test project with a sibling test project but no `InternalsVisibleTo`. |
| `KTSU0003-PreferEnsureNotNull` | KTSU0003 | Calls `ArgumentNullException.ThrowIfNull`. |
| `KTSU0004-ManualNullCheck` | KTSU0004 | Manual `if (x is null) throw new ArgumentNullException(...)`. |
| `KTSU0005-OrphanedPackageVersion` | KTSU0005 | A `PackageVersion` no project references. |
| `KTSU0006-TransitivePackageUsedDirectly` | KTSU0006 | Uses `ILogger` from a transitive package. |

> **Note on KTSU0002:** it is a `CompilationEnd` diagnostic reported at a syntax-tree
> location, and Roslyn can mask it when it is the *only* diagnostic in an otherwise clean
> compilation (see [#12](https://github.com/ktsu-dev/Sdk/issues/12) / #8 / #11). The example
> is correct; the integration test reports an inconclusive result rather than failing if the
> diagnostic is masked.

These projects are **expected to fail to build** — that is the point. They are deliberately
kept out of `Sdk.sln` so a normal `dotnet build` of the repository does not see them.

One folder is the inverse — a regression guard that is **expected to build cleanly**:

| Example | Guards against | Why it must pass |
| --- | --- | --- |
| `KTSU0005-OrphanedPackageVersion-CrossProject` | False KTSU0005 | A `PackageVersion` referenced only by a sibling project must not be reported as an orphan when another project is built on its own. |
| `KTSU0005-OrphanedPackageVersion-Allowlisted` | False KTSU0005 | SDK-governed packages (KTSU0001 standard packages, `Microsoft.Testing.Extensions.*` runner family injected into test projects) must not be flagged even with no direct `PackageReference`. |

## Building an example by hand

The examples consume the SDK via the documented `Microsoft.NET.Sdk` + `<Sdk Name="…" />`
form, with the version pinned in [`global.json`](./global.json). Against the **published**
SDK you can simply build a demo:

```bash
dotnet build demos/ConsoleApp/ConsoleApp/ConsoleApp.csproj
```

To build against a **local** (in-development) SDK, pack it to a feed and point a
`nuget.config` at it — exactly what the integration tests automate. See
[`../test/Sdk.Examples.Tests`](../test/Sdk.Examples.Tests).

If your `dotnet` SDK ships an older Roslyn than `ktsu.Sdk.Analyzers` was built against, the
analyzers will not load (CS9057). Set `KTSU_TOOLSET` to a matching
`Microsoft.Net.Compilers.Toolset` version when running the integration tests to pin a
compatible compiler, e.g. `KTSU_TOOLSET=5.3.0 dotnet test …`.
