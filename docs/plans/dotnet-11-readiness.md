# Plan: .NET 11 readiness

## Summary

Prepare the SDK for .NET 11, which ships on 2026-11-10, and adopt a single rule
for the default framework set: **a framework enters the list when it ships and
leaves when it goes out of support.**

The work splits into three commits.

| Commit | When | Change |
| --- | --- | --- |
| 1 | Now | Trim net5.0, net6.0, and net7.0, which are already unsupported. Remove the framework literals from the test assertions. |
| 2 | 2026-11-10 | Add net11.0 as it ships, drop net8.0 and net9.0 as they retire. Eleven SDK literals, three test constants, one CI variable, and one SDK pin. |
| 3 | After | `AnalysisLevel` to `11.0-all`, on its own so a break is attributable. |

No new MSBuild properties are introduced, and no behavior changes for a consumer
on a currently supported framework.

## Background

### Where the version lives today

`Sdk/Sdk.props:460` holds the library multi-target list:

```xml
<TargetFrameworks>net10.0;net9.0;net8.0;net7.0;net6.0;net5.0;netstandard2.0;netstandard2.1</TargetFrameworks>
```

All eight frameworks in a consuming library's build come from that one line.
Every other framework literal in the repository is either a single-target
pin in an extension SDK, a test assertion, or documentation.

| Location | Count | What |
| --- | --- | --- |
| `Sdk/Sdk.props:458,460,461` | 3 | Library list, test project framework |
| Eight extension SDK `Sdk.props` | 8 | Single-target pins, including `-ios` and `-android` |
| `test/Sdk.Examples.Tests`, 4 files | ~30 | Hardcoded assertions |
| `examples/**`, 25 csproj | 17 + 8 | 17 element pins, 8 comment mentions |
| `.github/workflows/dotnet-sdk.yml:24` | 1 | `DOTNET_VERSION`, feeds SDK install and publish framework |
| `examples/global.json` | 1 | SDK pin, `rollForward: latestFeature` |
| `README.md`, `CLAUDE.md` | ~21 | Documentation |

### Support dates

| Framework | Status on 2026-08-25 |
| --- | --- |
| net5.0 | Out of support since May 2022 |
| net7.0 | Out of support since May 2024 |
| net6.0 | Out of support since November 2024 |
| net8.0 | Supported to 2026-11-10 |
| net9.0 | Supported to November 2026 |
| net10.0 | LTS, supported into November 2028 |
| net11.0 | Ships 2026-11-10, supported to 2028-11-09 |

Three frameworks in the current default list have been unsupported for two to
four years. Two more retire in the same month .NET 11 ships, which is what makes
the November edit a single event rather than a sequence.

.NET Standard 2.0 and 2.1 sit outside the rule. They are API standards rather
than runtimes, they have no support term, and they are the reason trimming a
`netX.0` entry is safe.

## Decisions

**The list follows the support lifecycle.** Not "everything we can compile", and
not "only what we build against".

**Nothing is trimmed before its end-of-support date, and nothing is added before
it ships.** A preview or release candidate is not a supported runtime, so
net11.0 does not enter the default list at RC1 in September. A consumer who
wants to test against RC bits sets `TargetFrameworks` in their csproj, which
`README.md` already documents.

**One version number drives everything, apps and tools included.** The extension
SDKs move to net11.0 with the library list. This costs consumers a runtime
upgrade to run a ktsu tool, and it slightly shortens the supported life of an
app, because .NET 11 ends support within days of .NET 10 rather than after it.
The alternative, pinning apps to the newest LTS and letting only libraries take
net11.0, was considered and rejected as two version numbers to keep straight.

**No new MSBuild properties.** An earlier draft introduced
`KtsuLatestTargetFramework` and `KtsuLibraryTargetFrameworks`. The second was
pure indirection, since `TargetFrameworks` already lives in exactly one place.
The first would have turned an eight-file annual edit into a one-file edit, at
the cost of a ktsu-specific concept to document and a value that only resolves
when the core SDK is imported first. Eight small edits once a year is cheaper
than the concept.

**Nothing gets derived from `TargetFrameworks`.** Deriving an extension SDK's
pin from the first entry of the library list would give a single literal, but
`README.md` tells consumers to override `TargetFrameworks` per project, so an
unrelated consumer edit would silently change which runtime their tool requires.

**Commit 1 is tagged `[minor]`, not `[major]`.** It removes assets from every downstream
published package and, per the Risks section below, requires a one-time
`CompatibilitySuppressions.xml` regeneration in three of five surveyed consumer repositories.
Neither makes it a breaking change in the semantic-versioning sense: no consumer is stranded
(the netstandard2.1 fallback below covers them), and nothing happens automatically. A consumer
only takes the trim by bumping `ktsu.Sdk` in their own `global.json`, which is an opt-in action
on their side.

### Dropping a framework does not strand a consumer

.NET 5, 6, and 7 all implement .NET Standard 2.1. A consumer on net6.0
referencing a ktsu library with no net6.0 asset resolves the netstandard2.1
asset, with no warning and no restore failure. What they lose is any in-box API
that postdates .NET Standard 2.1, which comes from Polyfill's embedded shims
instead. The SDK mandates Polyfill everywhere for exactly this reason.

This is why `netstandard2.0` must stay. .NET Framework never implemented 2.1, so
dropping 2.0 would strand those consumers with no fallback behind it.

What keeping dead frameworks costs: one compile per framework on every library
build in every ktsu repository, with `AnalysisLevel=*-all` and
`TreatWarningsAsErrors`, plus more cross-framework validation noise of the kind
that already forced `EnableStrictModeForCompatibleFrameworksInPackage=false`.

## Design

### Commit 1, now

`Sdk/Sdk.props:460` becomes, with the rule stated above it so the list reads as
policy rather than accumulation:

```xml
<!-- Frameworks follow the .NET support lifecycle: one enters the list when it
     ships and leaves when it goes out of support. netstandard2.0/2.1 are API
     standards, not runtimes, and stay as the fallback for consumers on older
     targets. Next change: 2026-11-10, when .NET 11 ships and .NET 8 and .NET 9
     retire together. -->
<TargetFrameworks>net10.0;net9.0;net8.0;netstandard2.0;netstandard2.1</TargetFrameworks>
```

No other value changes. The eight extension SDKs keep their `net10.0` literals.

`Sdk.Tasks` stays on `netstandard2.0`, and gets a comment saying so, because it
is loaded by whichever MSBuild is running and `netstandard2.0` is the only TFM
both .NET Framework MSBuild in Visual Studio and .NET MSBuild will load.
`Sdk.Analyzers` stays on `netstandard2.0` with Roslyn 5.9.0. Neither is a
consumer-facing framework choice.

### Commit 2, 2026-11-10

Eleven framework literals in the SDK, three test constants, one CI variable, one SDK
pin, and the documentation, all mechanical. `Sdk/Sdk.props` line numbers below drift with
unrelated edits (Task 3 already shifted them once), so the implementation plan locates each
by element and condition, not by number alone:

0. `test/Sdk.Examples.Tests/Infrastructure/TargetFrameworks.cs`, first, so the suite goes
   red before the SDK moves and proves no hidden literal survived
1. `Sdk/Sdk.props`, the library `<TargetFrameworks>` list, to `net11.0;net10.0;netstandard2.0;netstandard2.1`
2. `Sdk/Sdk.props`, the test-project `<TargetFramework>` and `<TargetFrameworks>` conditions,
   to `net11.0`
3. `Sdk.App`, `Sdk.ConsoleApp`, `Sdk.Tool`, `Sdk.Windows`, `Sdk.Linux`,
   `Sdk.macOS` to `net11.0`
4. `Sdk.iOS` to `net11.0-ios`, `Sdk.Android` to `net11.0-android`
5. `.github/workflows/dotnet-sdk.yml:24`, `DOTNET_VERSION` to `11.0`
6. `examples/global.json`, SDK pin to `11.0.100`
7. `README.md` and `CLAUDE.md`, every mention of the current default framework list or the
   pinned .NET SDK version. Neither was in this list originally, and both would otherwise
   still read net10.0-based after the flip. The implementation plan's Task 6 gives the exact
   sites and a search to catch what a fixed list misses.

`DOTNET_VERSION` already feeds both the SDK install and the publish step's
framework argument at line 172, so it is one edit. A .NET 11 SDK builds net10.0
targets, so no second SDK install is needed.

`rollForward: latestFeature` in `examples/global.json` does not cross a major
version, so that pin is not optional.

### Tests

This is the work that makes commit 2 mechanical instead of risky. Four files
hardcode `net10.0` in roughly thirty places:

- `PlatformSdkResolutionTests.cs:21-24` asserts it in four `DataRow` attributes,
  including the `-ios` and `-android` forms
- `ToolSdkTests.cs:33` asserts the property, `:79` asserts the
  `tools/net10.0/any/` package path
- `CliProcessLifetimeTests.cs:64-65` and `StyleConfigSyncTests.cs:147,152`
  rewrite example csproj text by literal string replacement, so they break if an
  example's pin moves without them

All four take the value from one constant in
`test/Sdk.Examples.Tests/Infrastructure`. `DataRow` requires compile-time
constants, so the platform rows pass a suffix (empty, `-ios`, `-android`) and
compose the expected framework in the test body.

### Examples

The 17 csproj files with an element pin keep pinning, since the pin exists to
keep the smoke tests fast, but the harness rewrites it from the same constant so
an example and its test cannot drift apart. The 8 demo apps name the framework
only in comments, so those are text edits.

### Documentation

`README.md` has framework literals in nine places, `CLAUDE.md` in about a dozen.
Both take the current values and a one-line statement of the lifecycle rule, so
the next reader knows why the list looks the way it does.

## Out of scope

**The `Directory.Build.props` override.** `Sdk/Sdk.props`'s unconditional
`<TargetFramework></TargetFramework>` clear (currently line 458) sets `TargetFramework` to an
empty value.

`Directory.Build.props` is imported by `Microsoft.Common.props` as part of the
`Project Sdk="Microsoft.NET.Sdk"` attribute, and the `Sdk` element props are
injected after that. So a consumer setting `TargetFramework` in
`Directory.Build.props` is silently overwritten, and only a setting in the csproj
body survives. Adding a condition on an empty `TargetFramework` to an extension
SDK does not fix this, because the core has already cleared the value by then.
It reads like a guarantee and is a no-op.

Fixing it properly means stopping the core from clearing, which is a behavior
change: any repository with `TargetFramework` in a `Directory.Build.props` today
gets multi-targeting because of that wipe and would start single-targeting
instead. That deserves its own change, not a ride along with a version bump.

**`AnalysisLevel`.** Pinned at `10.0-all` in `Sdk/Sdk.props`'s code analysis properties block
(currently line 585). With
`TreatWarningsAsErrors`, every rule a new analysis level adds breaks every
consuming repository at once, which is why it is pinned rather than `latest-all`
in the first place. It moves to `11.0-all` in its own commit after the flip, so
that when something breaks it is obvious which change did it.

## Risks

**Package validation in consuming repositories. Verified: real, but not for the
reason first assumed.** Checked empirically by packing a real consumer against a
locally packed trimmed SDK. The answer came back in two parts.

*Baseline validation is not the mechanism, and never was.* `Sdk/Sdk.props` (currently line 511)
sets `EnableStrictModeForBaselineValidation=true`, but that property only governs
how strict a baseline comparison is. It enables nothing on its own.
`PackageValidationBaselineVersion`, `PackageValidationBaselineName` and
`PackageValidationBaselinePath` are set nowhere, in this SDK or in any surveyed
consumer, so baseline validation never runs and a removed framework cannot break
it. `PackageValidationBaselineVersion` is therefore *not* the consumer-side
fallback, and recommending it would send people to a fix that does nothing.

*The real mechanism is a stale `CompatibilitySuppressions.xml`.* A consumer that
has one carries entries recording comparisons involving net5.0, net6.0 and
net7.0. When those frameworks stop producing an assembly, the entries can never
match a live comparison, and the default
`ApiCompatPermitUnnecessarySuppressions=false` turns an unmatched suppression
into an error rather than discarding it. The pack fails with CP0001, CP0002,
CP0008, CP0014, CP0015 and CP0016 all naming frameworks the package no longer
contains, which is why the wrong fix is the intuitive guess.

*Scope and remedy.* Three of five surveyed consumers carry the file
(`Containers`, `DeepClone`, `Invoker`; `CaseConverter` and `Extensions` do not),
so most repositories taking the trimmed SDK need a one-time regeneration:

```powershell
dotnet pack -p:ApiCompatGenerateSuppressionFile=true
```

That is already this repository's documented convention for the same file, so
the migration step is not a new idea, only a newly required one. `README.md`
carries a troubleshooting entry that rules out the wrong fix before giving this
one.

This repository's own SDK packages are unaffected either way, because
`Sdk.Common.MSBuildSdkPackage.props` sets `EnablePackageValidation=false`.

**Consumers genuinely on net5.0 to net7.0.** They set `TargetFrameworks` in
their csproj, which `README.md` documents at line 574. No consumer is stranded,
per the netstandard2.1 fallback above.

**Consumers pinning an older `ktsu.Sdk`.** Nothing changes for them until they
bump the version in `global.json`.

## Verification

- Full test suite green after the test constant refactor, with the constant
  still reading `net10.0`, proving the refactor changed no behavior
- Pack a real consumer against a locally packed trimmed SDK (done: the local SDK
  must carry a unique `-local` version, or NuGet resolves the published one from the
  global packages folder and the check silently proves nothing)
- After commit 2, a demo app from `examples/demos` builds, packs, and runs
