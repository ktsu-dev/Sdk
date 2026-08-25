# .NET 11 readiness implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Trim the frameworks that are already out of support, and remove every hardcoded framework literal from the test suite, so the .NET 11 flip on 2026-11-10 is a mechanical edit of known literals rather than a search across forty files.

**Architecture:** No new MSBuild properties. The SDK keeps its literals, and the test suite stops keeping its own. One internal constants class in the test infrastructure holds the expected values, a pure rewrite function keeps the example projects in step with them, and a new test pins the core SDK's default framework list so a change to it can never pass unnoticed.

**Tech Stack:** MSBuild props/targets, C# 13 on net10.0, MSTest, `dotnet msbuild -getProperty` for property evaluation.

**Spec:** `docs/plans/dotnet-11-readiness.md`

## Global constraints

- **Indentation in `test/` is four spaces, not tabs.** `test/.editorconfig` line 15 sets `indent_style = space` for `*.cs`, overriding the repository root's `indent_style = tab`. Files elsewhere in the repository use tabs. Match the file you are in.
- **LF line endings, not CRLF.** `.gitattributes:11` pins `* text=auto eol=lf` and both `.editorconfig:14` and `test/.editorconfig` restate `end_of_line = lf`. The user-level `CLAUDE.md` says to use CRLF for Windows projects; this repository deliberately overrides that, and `.gitattributes` normalizes on commit regardless of a machine's `core.autocrlf`. Verify stored bytes with `git cat-file blob`, never `git show`, which applies the working-tree conversion and reports CRLF on a Windows checkout.
- File-scoped namespace first, `using` directives after it, matching `test/Sdk.Examples.Tests/Infrastructure/ExampleWorkspace.cs`.
- `Nullable` enabled, `TreatWarningsAsErrors=true`, `AnalysisLevel=10.0-all`. A warning fails the build.
- MSTest with semantic asserts. `Assert.AreEqual`, `CollectionAssert.Contains`, `StringAssert.Contains`. Never `Assert.IsTrue` on an equality.
- Commit messages carry a version tag: `[patch]` for docs, metadata, and CI, `[minor]` for source changes. **Do not add `Co-Authored-By` lines.**
- Never edit `VERSION.md`, `CHANGELOG.md`, or `LICENSE.md`. They are generated.
- `Sdk.Tasks` stays on `netstandard2.0` with no runtime package dependencies. `Sdk.Analyzers` stays on `netstandard2.0`. Neither is in scope for a framework bump.
- The value of `TargetFrameworks.Latest` stays `net10.0` for this entire plan. Only Task 3 changes an SDK framework value, and it changes the multi-target list only.

## Running the tests

The suite packs the SDK into a local feed on first use, then builds throwaway example workspaces. A warm full run is about 3 minutes 30 seconds, which fits inside the Bash tool's 600s ceiling. Run it in the FOREGROUND with an explicit `timeout: 600000`. Never background a test run: a backgrounded run is torn down when a subagent yields its turn, which cost one full run early in this plan's execution.

```powershell
dotnet test test/Sdk.Examples.Tests --configuration Release -m:1
```

A single class, which is what most steps below need:

```powershell
dotnet test test/Sdk.Examples.Tests --configuration Release -m:1 --filter "FullyQualifiedName~TargetFrameworkPolicyTests"
```

---

### Task 1: Shared framework constants for the test suite

Pure refactor. No behavior changes, and the existing suite is the gate. The constants land first so later tasks have something to reference.

**Files:**
- Create: `test/Sdk.Examples.Tests/Infrastructure/TargetFrameworks.cs`
- Modify: `test/Sdk.Examples.Tests/PlatformSdkResolutionTests.cs:14-33`
- Modify: `test/Sdk.Examples.Tests/ToolSdkTests.cs:33`, `:79`
- Modify: `test/Sdk.Examples.Tests/CliProcessLifetimeTests.cs:62-66`
- Modify: `test/Sdk.Examples.Tests/StyleConfigSyncTests.cs:141-156`

**Interfaces:**
- Consumes: nothing.
- Produces: `internal static class Sdk.Examples.Tests.Infrastructure.TargetFrameworks` with `const string Latest`, `const string Library`, `const string MultiTargetProbe`, and `static string Platform(string suffix)`. Task 2 adds `RewritePins` to the same class. Task 3 asserts against `Library`.

- [ ] **Step 1: Confirm the suite is green before touching anything**

```powershell
dotnet test test/Sdk.Examples.Tests --configuration Release -m:1
```

Expected: PASS. Record the test count. If it is already red, stop and report, because this task cannot be verified against a broken baseline.

- [ ] **Step 2: Create the constants class**

Create `test/Sdk.Examples.Tests/Infrastructure/TargetFrameworks.cs`:

```csharp
namespace Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// The frameworks ktsu.Sdk pins, mirrored here as the suite's expected values.
/// </summary>
/// <remarks>
/// These are hand-maintained expectations rather than values read back from the SDK, so a test
/// still fails when the SDK changes underneath it. Update them in the same commit that changes
/// <c>Sdk/Sdk.props</c>. The rule that decides which frameworks belong in <see cref="Library"/>
/// is in <c>docs/plans/dotnet-11-readiness.md</c>: a framework enters the list when it ships and
/// leaves when it goes out of support.
/// </remarks>
internal static class TargetFrameworks
{
    /// <summary>The single framework every extension SDK and every test project pins.</summary>
    public const string Latest = "net10.0";

    /// <summary>The default multi-target list a library inherits from the core SDK.</summary>
    public const string Library = "net10.0;net9.0;net8.0;netstandard2.0;netstandard2.1";

    /// <summary>
    /// A short list used only to force a build to fan out into parallel inner builds. It needs
    /// two or more frameworks the pinned SDK can build, and stays deliberately shorter than
    /// <see cref="Library"/> so the timing-sensitive harness tests do not slow down.
    /// </summary>
    public const string MultiTargetProbe = "net10.0;net9.0;net8.0";

    /// <summary>Composes a platform-specific framework, for example <c>net10.0-ios</c>.</summary>
    /// <param name="suffix">The platform suffix including its leading hyphen, or an empty string.</param>
    /// <returns>The framework the matching platform SDK pins.</returns>
    public static string Platform(string suffix) => Latest + suffix;
}
```

- [ ] **Step 3: Route `PlatformSdkResolutionTests` through the constant**

`DataRow` arguments must be compile-time constants, so the rows pass a platform suffix and the body composes the framework. Replace lines 14 to 33 with:

```csharp
    /// <param name="demo">The demo directory under examples/demos.</param>
    /// <param name="project">The consuming project, relative to the demo directory.</param>
    /// <param name="frameworkSuffix">The platform suffix appended to the pinned framework, or empty.</param>
    /// <param name="outputType">Expected OutputType.</param>
    /// <param name="rids">Expected RuntimeIdentifiers.</param>
    /// <param name="flag">The project-type detection flag expected to be 'true'.</param>
    [TestMethod]
    [DataRow("Windows", "Demo.Windows/Demo.Windows.csproj", "", "WinExe", "win-x64;win-x86;win-arm64", "IsWindowsProject", DisplayName = "ktsu.Sdk.Windows")]
    [DataRow("macOS", "Demo.macOS/Demo.macOS.csproj", "", "Exe", "osx-x64;osx-arm64", "IsMacProject", DisplayName = "ktsu.Sdk.macOS")]
    [DataRow("iOS", "Demo.iOS/Demo.iOS.csproj", "-ios", "Exe", "ios-arm64;iossimulator-x64;iossimulator-arm64", "IsIosProject", DisplayName = "ktsu.Sdk.iOS")]
    [DataRow("Android", "Demo.Android/Demo.Android.csproj", "-android", "Exe", "", "IsAndroidProject", DisplayName = "ktsu.Sdk.Android")]
    public void PlatformSdk_ResolvesExpectedProperties(
        string demo, string project, string frameworkSuffix, string outputType, string rids, string flag)
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo(demo));

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            project, "TargetFramework", "OutputType", "RuntimeIdentifiers", flag);

        Assert.AreEqual(TargetFrameworks.Platform(frameworkSuffix), props["TargetFramework"], "TargetFramework");
```

Leave the three assertions after it untouched.

- [ ] **Step 4: Route `ToolSdkTests` through the constant**

Line 33 becomes:

```csharp
        Assert.AreEqual(TargetFrameworks.Latest, props["TargetFramework"], "TargetFramework");
```

Line 79 becomes an interpolated constant, which is legal because every part is itself `const`:

```csharp
        const string toolsDir = $"tools/{TargetFrameworks.Latest}/any/";
```

- [ ] **Step 5: Route both csproj-rewriting helpers through the constants**

In `CliProcessLifetimeTests.cs`, the body of `MultiTargetDemoProject` becomes:

```csharp
        string multiTargeted = original
            .Replace($"<TargetFramework>{TargetFrameworks.Latest}</TargetFramework>", "<TargetFramework></TargetFramework>", StringComparison.Ordinal)
            .Replace("<TargetFrameworks></TargetFrameworks>", $"<TargetFrameworks>{TargetFrameworks.MultiTargetProbe}</TargetFrameworks>", StringComparison.Ordinal);
```

In `StyleConfigSyncTests.cs`, the body of its `MultiTargetDemoProject` becomes:

```csharp
        string multiTargeted = original.Replace(
            $"<TargetFramework>{TargetFrameworks.Latest}</TargetFramework>",
            "<TargetFramework></TargetFramework>",
            StringComparison.Ordinal)
            .Replace(
                "<TargetFrameworks></TargetFrameworks>",
                $"<TargetFrameworks>{TargetFrameworks.MultiTargetProbe}</TargetFrameworks>",
                StringComparison.Ordinal);
```

Both files already have `using Sdk.Examples.Tests.Infrastructure;`. Verify rather than assume, and add it if missing.

- [ ] **Step 6: Confirm no framework literals remain in the test sources**

```powershell
Select-String -Path test/Sdk.Examples.Tests/*.cs, test/Sdk.Examples.Tests/Infrastructure/*.cs -Pattern 'net\d+\.\d+'
```

Expected: matches only in `TargetFrameworks.cs`. Any other hit is a literal that was missed.

- [ ] **Step 7: Run the full suite**

```powershell
dotnet test test/Sdk.Examples.Tests --configuration Release -m:1
```

Expected: PASS, with the same test count as Step 1. A behavior change here means the refactor is wrong, not that the SDK is.

- [ ] **Step 8: Commit**

```powershell
git add test/Sdk.Examples.Tests
git commit -m "test: route framework assertions through one constant [patch]"
```

---

### Task 2: Keep the example projects in step with the constant

The examples pin `net10.0` in seventeen csproj files. Two tests rewrite that pin by literal string match, so when the SDK's framework moves and the examples do not, those rewrites silently stop matching and the tests fail for a reason that has nothing to do with what they test. The harness repins the copied workspace instead.

**Files:**
- Modify: `test/Sdk.Examples.Tests/Infrastructure/TargetFrameworks.cs`
- Modify: `test/Sdk.Examples.Tests/Infrastructure/ExampleWorkspace.cs:21-31`, and add a private helper
- Create: `test/Sdk.Examples.Tests/TargetFrameworkPolicyTests.cs`

**Interfaces:**
- Consumes: `TargetFrameworks.Latest` from Task 1.
- Produces: `static string TargetFrameworks.RewritePins(string projectXml, string latest)`, returning the rewritten project text. Task 3 adds a second test method to `TargetFrameworkPolicyTests`.

- [ ] **Step 1: Write the failing tests**

Create `test/Sdk.Examples.Tests/TargetFrameworkPolicyTests.cs`:

```csharp
namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Pins the framework policy: the rewrite that keeps the example projects in step with the
/// framework the SDK pins, and the default multi-target list the core SDK gives a library.
/// </summary>
[TestClass]
public sealed class TargetFrameworkPolicyTests
{
    /// <param name="projectXml">The project text to rewrite.</param>
    /// <param name="latest">The framework to repin to.</param>
    /// <param name="expected">The expected result.</param>
    [TestMethod]
    [DataRow(
        "<TargetFramework>net10.0</TargetFramework>", "net11.0",
        "<TargetFramework>net11.0</TargetFramework>",
        DisplayName = "a versioned pin is repinned")]
    [DataRow(
        "<TargetFramework>net10.0-ios</TargetFramework>", "net11.0",
        "<TargetFramework>net11.0-ios</TargetFramework>",
        DisplayName = "a platform pin keeps its platform")]
    [DataRow(
        "<TargetFramework>netstandard2.0</TargetFramework>", "net11.0",
        "<TargetFramework>netstandard2.0</TargetFramework>",
        DisplayName = "netstandard is left alone")]
    [DataRow(
        "<TargetFrameworks>net10.0;net9.0</TargetFrameworks>", "net11.0",
        "<TargetFrameworks>net10.0;net9.0</TargetFrameworks>",
        DisplayName = "the plural element is left alone")]
    [DataRow(
        "<OutputType>Exe</OutputType>", "net11.0",
        "<OutputType>Exe</OutputType>",
        DisplayName = "a project with no pin is unchanged")]
    public void RewritePins_RepinsOnlyVersionedFrameworks(string projectXml, string latest, string expected) =>
        Assert.AreEqual(expected, TargetFrameworks.RewritePins(projectXml, latest));
}
```

The `netstandard` case is not hypothetical. `examples/analyzers/KTSU0001-MissingStandardPackages-WithPolyfill/MissingSystemMemory/MissingSystemMemory.csproj:16` pins `netstandard2.0` on purpose, because that example exists to prove the analyzer's netstandard requirements. Repinning it would delete the thing it tests.

- [ ] **Step 2: Run the tests to verify they fail**

```powershell
dotnet test test/Sdk.Examples.Tests --configuration Release -m:1 --filter "FullyQualifiedName~TargetFrameworkPolicyTests"
```

Expected: FAIL to compile, with `CS0117: 'TargetFrameworks' does not contain a definition for 'RewritePins'`.

- [ ] **Step 3: Implement the rewrite**

Add to `TargetFrameworks.cs`. The class becomes `partial` so `GeneratedRegex` can be used, and the file gains `using System.Text.RegularExpressions;` after the namespace declaration:

```csharp
    /// <summary>
    /// Repins every versioned .NET framework in a project file to <paramref name="latest"/>,
    /// preserving any platform suffix.
    /// </summary>
    /// <param name="projectXml">The project file's contents.</param>
    /// <param name="latest">The framework to pin, for example <c>net11.0</c>.</param>
    /// <returns>The rewritten contents, unchanged when the file pins no versioned framework.</returns>
    /// <remarks>
    /// Only a <c>netX.Y</c> pin is rewritten. A <c>netstandard2.0</c> pin is deliberate in the
    /// KTSU0001 examples and is left alone, which falls out of the pattern requiring a digit
    /// immediately after <c>net</c>. The plural <c>TargetFrameworks</c> element is also left
    /// alone, since the tests that build a multi-target list compose it from
    /// <see cref="MultiTargetProbe"/> rather than from whatever an example happens to declare.
    /// </remarks>
    public static string RewritePins(string projectXml, string latest) =>
        VersionedPin().Replace(projectXml, $"<TargetFramework>{latest}${{platform}}</TargetFramework>");

    [GeneratedRegex(@"<TargetFramework>net\d+\.\d+(?<platform>-[a-z][a-z0-9.]*)?</TargetFramework>")]
    private static partial Regex VersionedPin();
```

The `${{platform}}` in the interpolated string produces the literal `${platform}`, which is the regex substitution for the named group.

- [ ] **Step 4: Run the tests to verify they pass**

```powershell
dotnet test test/Sdk.Examples.Tests --configuration Release -m:1 --filter "FullyQualifiedName~TargetFrameworkPolicyTests"
```

Expected: PASS, 5 cases.

- [ ] **Step 5: Wire the rewrite into the workspace copy**

In `ExampleWorkspace.cs`, add the call to `Create` after `CopyTree`:

```csharp
        string dest = Path.Combine(Path.GetTempPath(), "ktsu-sdk-example-" + Guid.NewGuid().ToString("N"));
        CopyTree(sourceDir, dest);

        RewriteFrameworkPins(dest);
        WriteGlobalJson(dest);
        WriteNuGetConfig(dest);
        MaybeWriteCompilerToolset(dest);
```

And add the helper alongside the other private statics:

```csharp
    /// <summary>
    /// Repins every versioned framework in the copied examples to the framework the SDK pins.
    /// Without this an example's literal and the SDK's value drift apart on a framework bump, and
    /// the tests that rewrite project text by literal match stop matching, failing for a reason
    /// unrelated to what they test.
    /// </summary>
    private static void RewriteFrameworkPins(string dest)
    {
        foreach (string projectFile in Directory.GetFiles(dest, "*.csproj", SearchOption.AllDirectories))
        {
            string original = File.ReadAllText(projectFile);
            string rewritten = TargetFrameworks.RewritePins(original, TargetFrameworks.Latest);
            if (!string.Equals(original, rewritten, StringComparison.Ordinal))
            {
                File.WriteAllText(projectFile, rewritten);
            }
        }
    }
```

- [ ] **Step 6: Run the full suite**

```powershell
dotnet test test/Sdk.Examples.Tests --configuration Release -m:1
```

Expected: PASS. `TargetFrameworks.Latest` is still `net10.0` and the examples already pin `net10.0`, so the rewrite is a no-op today and nothing may change.

- [ ] **Step 7: Commit**

```powershell
git add test/Sdk.Examples.Tests
git commit -m "test: repin example frameworks from the shared constant [patch]"
```

---

### Task 3: Pin the library framework list, then trim the unsupported frameworks

This is the change the spec is about. The test comes first and fails, because `TargetFrameworks.Library` from Task 1 already names the trimmed list while `Sdk/Sdk.props` still names all eight.

**Files:**
- Modify: `test/Sdk.Examples.Tests/TargetFrameworkPolicyTests.cs`
- Modify: `Sdk/Sdk.props:459-460`

**Interfaces:**
- Consumes: `TargetFrameworks.Library` from Task 1, `ExampleWorkspace.Create` and `EvaluateWith` from the existing infrastructure.
- Produces: nothing later tasks depend on.

- [ ] **Step 1: Write the failing test**

Add to `TargetFrameworkPolicyTests`:

```csharp
    /// <summary>
    /// The default multi-target list a library inherits. Frameworks follow the .NET support
    /// lifecycle: one enters when it ships and leaves when it goes out of support.
    /// netstandard2.0 and 2.1 are API standards rather than runtimes, have no support term, and
    /// stay as the fallback that keeps a consumer on a trimmed framework working.
    /// </summary>
    [TestMethod]
    public void CoreSdk_MultiTargetsTheSupportedFrameworks()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));

        // The demo pins a single framework to keep the smoke tests fast, and the project body is
        // evaluated after the SDK props, so both elements have to go before the SDK's own default
        // is observable.
        string projectPath = Path.Combine(workspace.Root, "Library", "Library.csproj");
        string original = File.ReadAllText(projectPath);
        string unpinned = original
            .Replace($"<TargetFramework>{TargetFrameworks.Latest}</TargetFramework>", string.Empty, StringComparison.Ordinal)
            .Replace("<TargetFrameworks></TargetFrameworks>", string.Empty, StringComparison.Ordinal);

        Assert.AreNotEqual(original, unpinned, "The Library demo no longer has the expected framework properties.");
        File.WriteAllText(projectPath, unpinned);

        IReadOnlyDictionary<string, string> props = workspace.Evaluate(
            "Library/Library.csproj", "TargetFrameworks");

        Assert.AreEqual(TargetFrameworks.Library, props["TargetFrameworks"], "TargetFrameworks");
    }
```

- [ ] **Step 2: Run the test to verify it fails**

```powershell
dotnet test test/Sdk.Examples.Tests --configuration Release -m:1 --filter "FullyQualifiedName~CoreSdk_MultiTargetsTheSupportedFrameworks"
```

Expected: FAIL, with the actual value `net10.0;net9.0;net8.0;net7.0;net6.0;net5.0;netstandard2.0;netstandard2.1` against the expected `net10.0;net9.0;net8.0;netstandard2.0;netstandard2.1`.

If it fails any other way, stop. A failure to strip the pins means the demo project changed shape and the test needs updating before the SDK does.

- [ ] **Step 3: Trim the SDK's default list**

In `Sdk/Sdk.props`, replace line 460 with the comment and the trimmed list. The comment states the rule so the list reads as policy rather than as accumulation:

```xml
    <!-- Frameworks follow the .NET support lifecycle: one enters the list when it ships and
         leaves when it goes out of support. net5.0 (May 2022), net7.0 (May 2024) and net6.0
         (November 2024) are long gone. netstandard2.0/2.1 are API standards, not runtimes, have
         no support term, and stay as the fallback: .NET 5 through 9 all implement netstandard2.1,
         so a consumer on a trimmed framework resolves that asset instead of being stranded, and
         .NET Framework needs 2.0 because it never implemented 2.1.
         Next change: 2026-11-10, when .NET 11 ships and .NET 8 and .NET 9 retire together. -->
    <TargetFrameworks>net10.0;net9.0;net8.0;netstandard2.0;netstandard2.1</TargetFrameworks>
```

Leave lines 456, 458, and 461 exactly as they are. `TargetFramework` for test projects stays `net10.0`.

- [ ] **Step 4: Run the test to verify it passes**

```powershell
dotnet test test/Sdk.Examples.Tests --configuration Release -m:1 --filter "FullyQualifiedName~CoreSdk_MultiTargetsTheSupportedFrameworks"
```

Expected: PASS.

- [ ] **Step 5: Run the full suite**

```powershell
dotnet test test/Sdk.Examples.Tests --configuration Release -m:1
```

Expected: PASS. Watch `CliProcessLifetimeTests` and `StyleConfigSyncTests` in particular: both build `MultiTargetProbe`, which is `net10.0;net9.0;net8.0` and stays inside the trimmed list, so both should be unaffected.

- [ ] **Step 6: Commit**

```powershell
git add Sdk/Sdk.props test/Sdk.Examples.Tests
git commit -m "feat: trim out-of-support frameworks from the default target list [minor]"
```

---

### Task 4: Verify the package validation risk before this ships

The spec names one risk worth proving rather than assuming. Consuming repositories pack with `EnablePackageValidation=true` and baseline validation against their last published package. Removing net5.0, net6.0, and net7.0 removes frameworks the published baseline contains. If the validator does not accept the netstandard2.1 fallback, every consuming repository's next pack fails, and it fails in their repository rather than this one.

This task produces a finding and, if needed, a documented fallback. It changes no SDK behavior.

**Files:**
- Modify: `README.md` (only if the check fails)

**Interfaces:**
- Consumes: the trimmed SDK from Task 3.
- Produces: a recorded finding for Task 5's documentation, and confirmation that a real consumer packs against the trimmed SDK.

- [ ] **Step 1: Determine whether baseline validation is active at all**

Before packing anything, establish whether the risk can even occur. Baseline validation runs only when a baseline package is named. `EnableStrictModeForBaselineValidation` governs how strict that comparison is; on its own it enables nothing.

```powershell
Select-String -Path Sdk/Sdk.props, Sdk.Common.*.props -Pattern 'PackageValidationBaseline'
Select-String -Path C:/dev/ktsu-dev/Containers/*.props, C:/dev/ktsu-dev/Containers/**/*.csproj -Pattern 'PackageValidationBaseline'
```

Expected: no `PackageValidationBaselineVersion`, `PackageValidationBaselineName`, or `PackageValidationBaselinePath` anywhere. If that holds, baseline validation never runs, and removing a framework cannot break it. Record the finding either way. Steps 2 to 5 then serve as the empirical confirmation, and as an end-to-end check that a real consumer still packs against the trimmed SDK.

- [ ] **Step 2: Pack the SDK to a temp feed at a unique local version**

Both details here are load-bearing, and `test/Sdk.Examples.Tests/Infrastructure/SdkFeed.cs:19-31` documents why. Packing at the bare `VERSION.md` value collides with the already-published package of that version, and NuGet resolves `msbuild-sdks` from the global packages folder before any configured source, so the consumer would silently build against the **published** SDK and the whole exercise would prove nothing. Separately, `Sdk/Sdk.targets:242` carries a literal `{version}` placeholder that `make-analyzer-releases.ps1` substitutes at release time; without substituting it, the `ktsu.Sdk.Analyzers` reference cannot restore.

Mirror what `SdkFeed.Pack` does:

1. Pick a version of the form `<VERSION.md>-local<8 hex chars>`, for example `2.27.4-localab12cd34`.
2. In every `Sdk.targets` under the repository that contains `{version}`, replace it with that version. **Record the original contents.**
3. Pack each of the ten SDK projects to a temp feed directory:
   ```powershell
   dotnet pack <Project>/<Project>.csproj -c Release -o <feed> --nologo -p:EnablePackageValidation=false -p:Version=<ver> -p:PackageVersion=<ver>
   ```
4. **Restore the original `Sdk.targets` contents**, so the working tree is left with no diff. Verify with `git status --short` before moving on.

- [ ] **Step 3: Clone a clean consumer to a temp directory**

Do not modify any repository under `C:/dev/ktsu-dev` in place. Clone one to a temp directory and work there, so the user's checkouts are untouched whatever happens.

`Containers`, `DeepClone`, `CaseConverter` and `Invoker` were all clean and on `main` at the time of writing, and all pin `ktsu.Sdk` 2.27.4. Pick one, confirm it is still clean, and clone it:

```powershell
git -C C:/dev/ktsu-dev/Containers status --porcelain   # must be empty
git clone C:/dev/ktsu-dev/Containers $env:TEMP/pkgval-check
```

- [ ] **Step 4: Point the clone at the local feed and pack**

In the clone only, set every `msbuild-sdks` entry in `global.json` to the local version from Step 2, and write a `nuget.config` listing the temp feed ahead of nuget.org. Then:

```powershell
dotnet pack -c Release -p:EnablePackageValidation=true
```

- [ ] **Step 5: Record the result**

Expected, given Step 1: PASS. No baseline is configured, so baseline validation never runs, and the pack additionally confirms a real consumer builds and packs against the trimmed SDK.

Expected if the risk turns out to be real after all: a `CP` or `PKV` diagnostic naming a target framework present in the baseline and missing from the new package.

Either way, confirm the pack produced a `.nupkg` and record which frameworks its `lib/` contains — that is the direct evidence of what a consumer now ships, and it should show five, not eight. Write the exact outcome down, including any diagnostic code, because Task 5 documents it.

Delete the temp clone and the temp feed when finished.

- [ ] **Step 6: If it failed, document the consumer-side fallback**

Only if Step 5 failed. Add to `README.md`, in the troubleshooting section that already covers overriding `TargetFrameworks` around line 574. The heading and body below go in verbatim, with the version in the example replaced by the first version the repository publishes after taking the trimmed SDK:

````markdown
### Package validation fails after a framework is dropped

When the SDK drops a framework that your last published package contains, baseline
validation reports it as a break. Those frameworks were dropped because they went out
of support, and consumers on them resolve the `netstandard2.1` asset instead, so this
is expected rather than a regression. Move the baseline forward once, to the first
version you publish after taking the trimmed SDK:

```xml
<PropertyGroup>
  <PackageValidationBaselineVersion>2.27.0</PackageValidationBaselineVersion>
</PropertyGroup>
```
````

- [ ] **Step 7: Clean up and commit**

Commit only if Step 6 produced a change. Nothing else in this task leaves anything to revert, because all of its work happened in temp directories:

```powershell
git add README.md
git commit -m "docs: document the package validation baseline fallback [patch]"
```

---

### Task 5: Update the documentation

**Files:**
- Modify: `README.md:125`, `:162`, `:171`, `:179`, `:199`, `:201-203`, `:208-210`
- Modify: `CLAUDE.md`, the `Multi-Targeting` section and the per-SDK bullets
- Modify: `Sdk.Tasks/Sdk.Tasks.csproj:16` area, comment only

**Interfaces:**
- Consumes: the trimmed list from Task 3, the finding from Task 4.
- Produces: nothing.

- [ ] **Step 1: Update the README framework list and add the rule**

`README.md:125` currently reads:

```markdown
- **Multi-Target Support**: .NET 10.0, 9.0, 8.0, 7.0, 6.0, 5.0, .NET Standard 2.0/2.1 (default: net10.0)
```

Replace with:

```markdown
- **Multi-Target Support**: .NET 10.0, 9.0, 8.0, .NET Standard 2.0/2.1 (default: net10.0).
  Frameworks follow the .NET support lifecycle: one enters the list when it ships and leaves
  when it goes out of support. .NET Standard 2.0/2.1 stay as the fallback, so a consumer on an
  older framework resolves the netstandard2.1 asset rather than being stranded.
```

Leave `:162`, `:171`, `:179`, `:199`, and `:201` through `:210` alone. They describe the extension SDKs, which still pin `net10.0` after this plan.

- [ ] **Step 2: Update CLAUDE.md**

In the `Multi-Targeting` section, replace the default list with `net10.0;net9.0;net8.0;netstandard2.0;netstandard2.1` and add the same one-line rule plus the next change date, 2026-11-10.

- [ ] **Step 3: Add the comment guarding Sdk.Tasks**

`Sdk.Tasks/Sdk.Tasks.csproj` already explains why `netstandard2.0` is mandatory. Extend that comment so a framework sweep does not treat it as an oversight:

```xml
       This project is deliberately excluded from the repository's framework policy: it is a
       host-loaded MSBuild task assembly, not a consumer-facing target, and it does not move
       when the default framework list moves.
```

- [ ] **Step 4: Verify no stale framework references remain**

```powershell
Select-String -Path README.md, CLAUDE.md -Pattern 'net[567]\.0'
```

Expected: no matches.

- [ ] **Step 5: Commit**

```powershell
git add README.md CLAUDE.md Sdk.Tasks/Sdk.Tasks.csproj
git commit -m "docs: record the support-lifecycle framework rule [patch]"
```

---

### Task 6: The flip, deferred to 2026-11-10

**Do not execute this task before .NET 11 ships on 2026-11-10.** It is recorded here so the November work is a checklist rather than a rediscovery. Everything before this point is executable now.

**Files:**
- Modify: `Sdk/Sdk.props:458`, `:460`, `:461`
- Modify: `Sdk.App/Sdk.props:5`, `Sdk.ConsoleApp/Sdk.props:4`, `Sdk.Tool/Sdk.props:6`, `Sdk.Windows/Sdk.props:7`, `Sdk.Linux/Sdk.props:6`, `Sdk.macOS/Sdk.props:8`
- Modify: `Sdk.iOS/Sdk.props:7`, `Sdk.Android/Sdk.props:7`
- Modify: `test/Sdk.Examples.Tests/Infrastructure/TargetFrameworks.cs`
- Modify: `.github/workflows/dotnet-sdk.yml:24`
- Modify: `examples/global.json`

- [ ] **Step 1: Update the test constants first, so the suite goes red before the SDK moves**

In `TargetFrameworks.cs`:

```csharp
    public const string Latest = "net11.0";
    public const string Library = "net11.0;net10.0;netstandard2.0;netstandard2.1";
    public const string MultiTargetProbe = "net11.0;net10.0;netstandard2.1";
```

`MultiTargetProbe` needs two or more frameworks that are still in the default list, and net9.0 and net8.0 no longer are.

- [ ] **Step 2: Run the suite to verify it fails**

```powershell
dotnet test test/Sdk.Examples.Tests --configuration Release -m:1
```

Expected: FAIL in at least `TargetFrameworkPolicyTests`, `PlatformSdkResolutionTests`, and `ToolSdkTests`, each reporting `net10.0` where `net11.0` is expected. Other failures are possible and fine at this point, because the harness now repins the copied examples to `net11.0` while the extension SDKs still say `net10.0`. Those three are the ones that must fail: they are the proof that Tasks 1 through 3 removed every hidden literal. A framework assertion that passes here was never actually checking the framework.

- [ ] **Step 3: Move the eleven SDK literals**

| File and line | New value |
| --- | --- |
| `Sdk/Sdk.props:458` | `net11.0` |
| `Sdk/Sdk.props:460` | `net11.0;net10.0;netstandard2.0;netstandard2.1` |
| `Sdk/Sdk.props:461` | `net11.0` |
| `Sdk.App/Sdk.props:5` | `net11.0` |
| `Sdk.ConsoleApp/Sdk.props:4` | `net11.0` |
| `Sdk.Tool/Sdk.props:6` | `net11.0` |
| `Sdk.Windows/Sdk.props:7` | `net11.0` |
| `Sdk.Linux/Sdk.props:6` | `net11.0` |
| `Sdk.macOS/Sdk.props:8` | `net11.0` |
| `Sdk.iOS/Sdk.props:7` | `net11.0-ios` |
| `Sdk.Android/Sdk.props:7` | `net11.0-android` |

Update the comment added in Task 3 to say .NET 8 and .NET 9 retired on 2026-11-10, and give the next change date as November 2027, when .NET 12 ships.

- [ ] **Step 4: Move CI and the examples pin**

`.github/workflows/dotnet-sdk.yml:24` becomes `DOTNET_VERSION: '11.0'`. It already feeds both the SDK install and the publish step's `--framework net${{ env.DOTNET_VERSION }}` at line 172, so this is the only edit needed there.

`examples/global.json` `"version"` becomes `"11.0.100"`. `rollForward: latestFeature` does not cross a major version, so this pin is not optional.

- [ ] **Step 5: Run the full suite**

```powershell
dotnet test test/Sdk.Examples.Tests --configuration Release -m:1
```

Expected: PASS. Requires the .NET 11 SDK installed on the machine.

- [ ] **Step 6: Commit**

```powershell
git add Sdk Sdk.App Sdk.ConsoleApp Sdk.Tool Sdk.Windows Sdk.Linux Sdk.macOS Sdk.iOS Sdk.Android test examples .github
git commit -m "feat: target .NET 11 and drop .NET 8 and .NET 9 [minor]"
```

- [ ] **Step 7: Bump AnalysisLevel separately**

Only after Step 6 is merged and green. `Sdk/Sdk.props:576` becomes `<AnalysisLevel>11.0-all</AnalysisLevel>`. With `TreatWarningsAsErrors`, every rule the new analysis level adds becomes an error in every consuming repository at once, which is why this is a separate commit: when something breaks, it is obvious which change did it.

```powershell
git commit -am "feat: raise AnalysisLevel to 11.0-all [minor]"
```
