# Host runtime only flag

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give `ktsu.Sdk` an opt-in property that pins every project to the host's runtime identifier, and have `ktsubuild test all` set it so a whole workspace can be tested in one `dotnet test` invocation without copying native assets for platforms nothing will load.

**Architecture:** `ktsu.Sdk`'s base props translates `KtsuHostRuntimeOnly=true` into a per-project `RuntimeIdentifier`. `ktsubuild` passes the flag as a global property, which is legal, where passing `RuntimeIdentifier` itself is not.

**Tech Stack:** MSBuild SDK props, C#, .NET 10.

**Spec:** none. This comes from measurements and experiments recorded below.

**This plan spans two repositories.** Tasks 1 and 2 are in `C:\dev\ktsu-dev\Sdk`. Task 3 is in `C:\dev\ktsu-dev\KtsuBuild`. The ledger lives with this plan, in the Sdk repository.

## Why

ImGuiApp's Windows test job runs 22.5 minutes against 8.0 on Linux for identical work. The difference is file writing, not compilation: `Setup .NET SDK`, which extracts an archive and compiles nothing, is 3.5 to 9.2 times slower on hosted Windows across four measured runs. The build gives it a great deal to write. Measured on ImGuiApp: 13 GB of `bin` against 109 MB of `obj`, because every project's output carries native binaries for all sixteen runtime identifiers the ImGui packages ship, `android-arm64` and `android-x64` among them, at roughly 230 MB per project per configuration.

Pinning the runtime fixes that. The smallest test project drops from 115 MB to 39 MB, with its tests passing either way.

The obstacle was that `dotnet test` refuses a runtime identifier on anything covering more than one project:

```
error NETSDK1134: Building a solution with a specific RuntimeIdentifier is not supported.
If you would like to publish for a single RID, specify the RID at the individual project level instead.
```

`ktsubuild test all` shipped in 2.6.0 working around that with one invocation per project. It pinned successfully and still lost, because fourteen test host startups cost more than the copying saved:

| | Windows | Ubuntu | Test-side billed |
| --- | --- | --- | --- |
| `ktsubuild build`, one invocation, no pin | 22.5 min | 8.0 min | ~53 |
| `test all`, per project, pinned | 21.4 min | 12.7 min | ~56 |

The error is about the runtime identifier arriving as a **global property**. Set inside each project's evaluation it is a per-project property, which is what the message asks for, and the solution check does not fire.

## What was established before writing this

All by execution, so the implementer confirms rather than rediscovers:

- **The flag approach works.** With a props file translating `KtsuHostRuntimeOnly=true` into a `RuntimeIdentifier`, one workspace-wide `dotnet test` across two projects reported `total: 4, succeeded: 4`, no `NETSDK1134`, and output landed at `net10.0/win-x64/A.Tests.dll`.
- **The Sdk is the right home.** `NETCoreSdkRuntimeIdentifier` reads as `win-x64` at the position a second `<Sdk Name>` element's props occupies. It is **empty** in a root `Directory.Build.props`, which imports before Microsoft.NET.Sdk sets it. An earlier attempt at a repository props file failed silently for exactly this reason.
- **The base Sdk reaches everything.** Every ktsu project declares `<Sdk Name="ktsu.Sdk" />`, with variants such as `ktsu.Sdk.App` layered after it.
- **A repository's own props survives.** With a `Directory.Build.props` present setting a marker, both applied: `{"MarkerFromRepoProps": "present", "RuntimeIdentifier": "win-x64"}`. This matters because 5 of the 67 repositories under `ktsu-dev` have one.

## A deliberate consequence

`Sdk.App/Sdk.props` sets `OutputType` to `WinExe` when the host is Windows and the runtime identifier starts with `win`. Because `ktsu.Sdk` is declared before `ktsu.Sdk.App`, a runtime identifier set in the base props is visible to that condition, so **app projects build as `WinExe` rather than `Exe` during a run with this flag set**.

The repository owner decided to accept that. It is harmless for a test run, since nothing launches the app projects, but it means a test build and a release build differ in a way that is not obvious from either file. **Say so in a comment where the flag is implemented**, so that a later reader who finds it surprising does not quietly "fix" it.

## Global Constraints

- **The flag must be inert by default.** Every repository builds through this Sdk. A project built without `KtsuHostRuntimeOnly` must produce byte-identical properties to today. This is the single most important property in the plan.
- Never clobber a runtime identifier a project set for itself. Only apply the pin when `RuntimeIdentifier` is empty.
- `SelfContained=false` must accompany the runtime identifier. Setting a runtime identifier alone makes the build self-contained, which copies the whole framework into the output and makes the size problem worse rather than better.
- Match the surrounding file's style. `Sdk.props` uses two-space indentation and the `xmlns="http://schemas.microsoft.com/developer/msbuild/2003"` project element.
- US English. Comments must not use em dashes, en dashes, or semicolons joining clauses, and must not coin hyphenated labels.
- In KtsuBuild: tabs, file-scoped namespaces, usings inside the namespace, braces on all control flow, explicit accessibility, nullable enabled, warnings as errors, IDE0060 is an error. MSTest with `Assert.AreEqual(n, x.Count)`; `Assert.HasCount` and `Assert.IsEmpty` must not appear. Never `--nologo`.
- **Building rewrites `.editorconfig` in KtsuBuild.** Run `git checkout .editorconfig` before committing there, stage files by name, never `git add -A`.
- Sdk branch: `feat/host-runtime-only-flag`. KtsuBuild branch: `feat/use-host-runtime-flag`.
- Commit messages carry a version tag. No Co-Authored-By lines.

---

### Task 1: Teach the Sdk the flag

**Repository:** `C:\dev\ktsu-dev\Sdk`

**Files:**
- Modify: `Sdk/Sdk.props`

**Interfaces:**
- Produces: the `KtsuHostRuntimeOnly` property. When `true` and the project has not set its own `RuntimeIdentifier`, the Sdk sets `RuntimeIdentifier` to `$(NETCoreSdkRuntimeIdentifier)` and `SelfContained` to `false`. Task 3 sets it from `ktsubuild`.

- [ ] **Step 1: Find where to put it**

```bash
cd /c/dev/ktsu-dev/Sdk
head -40 Sdk/Sdk.props
grep -n "PropertyGroup" Sdk/Sdk.props | head -10
```

It must sit where `NETCoreSdkRuntimeIdentifier` is already populated, which the probe confirmed is true anywhere in this file, and before anything that reads `RuntimeIdentifier`. Near the top of the first `PropertyGroup` region is fine. Read enough of the file to place it somewhere a reader would expect to find it.

- [ ] **Step 2: Add the property**

```xml
  <!-- Opt in, off unless a caller asks for it. When set, every project builds for the host's
       runtime only, so its output carries that runtime's native assets instead of one copy per
       runtime its packages ship. For a repository using the ImGui packages that is sixteen
       copies, and the smallest test project measured 115 MB against 39 MB pinned.

       Only applied when the project has not chosen a runtime identifier itself, so a project
       that knows what it targets keeps its own choice.

       Deliberate consequence: ktsu.Sdk.App turns OutputType into WinExe when the runtime
       identifier starts with win, and ktsu.Sdk is imported before it, so app projects build as
       WinExe during a run with this set. That is accepted. It is harmless when the purpose of
       the run is to execute tests, and it is the reason a build with this flag is not
       interchangeable with a release build. -->
  <PropertyGroup Condition="'$(KtsuHostRuntimeOnly)' == 'true' and '$(RuntimeIdentifier)' == ''">
    <RuntimeIdentifier>$(NETCoreSdkRuntimeIdentifier)</RuntimeIdentifier>
    <SelfContained>false</SelfContained>
  </PropertyGroup>
```

- [ ] **Step 3: Prove the default is inert**

This is the check that matters most, because every ktsu repository builds through this Sdk.

Build a project against the **local** Sdk both before and after your change, with the flag unset, and compare the evaluated properties. The Sdk's own integration tests under `test/Sdk.Examples.Tests` pack the Sdk to a temporary feed and build examples against it, which is the mechanism to use. Read that project first to learn how it resolves the local Sdk.

At minimum, capture `RuntimeIdentifier`, `SelfContained`, and `OutputType` for one library project and one app project, before and after, with the flag unset. They must be identical. Report both sets.

- [ ] **Step 4: Prove the flag works**

With the flag set, on the same projects:

- `RuntimeIdentifier` equals the host identifier, not empty.
- `SelfContained` is `false`.
- A project that sets its own `RuntimeIdentifier` keeps it. Construct that case.

Report the values.

- [ ] **Step 5: Run the Sdk's own tests**

```bash
cd /c/dev/ktsu-dev/Sdk
dotnet test
```

Report the total. If the suite is long, say how long it took, and do not skip it: this Sdk is what sixty repositories build with.

- [ ] **Step 6: Commit**

```bash
cd /c/dev/ktsu-dev/Sdk
git add Sdk/Sdk.props
git commit -m "feat: add KtsuHostRuntimeOnly to build for the host runtime only [minor]"
```

---

### Task 2: Document the flag

**Repository:** `C:\dev\ktsu-dev\Sdk`

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Document it where the Sdk's other properties are documented**

Read `README.md` and find where properties are described. Add `KtsuHostRuntimeOnly` in that style: what it does, that it is off by default, that it only applies when the project has not set its own runtime identifier, and that it exists so a whole workspace can be tested in one invocation without copying native assets for every runtime.

State the deliberate `WinExe` consequence plainly. A reader who meets it in the wild should find it documented rather than think it a bug.

- [ ] **Step 2: Commit**

```bash
cd /c/dev/ktsu-dev/Sdk
git add README.md
git commit -m "docs: document KtsuHostRuntimeOnly [patch]"
```

- [ ] **Step 3: Open the pull request and stop**

```bash
cd /c/dev/ktsu-dev/Sdk
git push -u origin feat/host-runtime-only-flag
gh pr create --base main --head feat/host-runtime-only-flag \
  --title "Add KtsuHostRuntimeOnly" \
  --body "Adds an opt-in property that builds every project for the host runtime only, so output carries that runtime's native assets rather than one copy per runtime its packages ship. Off by default and inert unless set. Only applies when a project has not chosen a runtime identifier itself. Exists so a whole workspace can be tested in one dotnet test invocation, which cannot take a RuntimeIdentifier global property (NETSDK1134). Deliberate consequence documented: app projects build as WinExe during a run with this set, because ktsu.Sdk.App keys OutputType off the runtime identifier."
```

**Merging and releasing are the repository owner's calls.** Report the URL and stop. Task 3 does not depend on this being released, because an unknown property is simply ignored by an older Sdk.

---

### Task 3: Have test all set the flag

**Repository:** `C:\dev\ktsu-dev\KtsuBuild`

**Files:**
- Modify: `KtsuBuild/Abstractions/IDotNetService.cs`, `KtsuBuild/DotNet/DotNetService.cs`, `KtsuBuild.Tests/DotNet/DotNetServiceTests.cs`, `KtsuBuild.Tool/Commands/TestCommand.cs`

**Interfaces:**
- Consumes: the `KtsuHostRuntimeOnly` property from Task 1.

- [ ] **Step 1: Add the option to the whole-workspace run**

`TestAsync` is the workspace-wide run. Give it `bool hostRuntimeOnly = false` before the `CancellationToken`, and when set append `-p:KtsuHostRuntimeOnly=true` to the `dotnet test` arguments.

**Do not** append `-p:RuntimeIdentifier`. That is what `NETSDK1134` rejects, and it is the mistake this whole design exists to avoid. Say so in a comment.

Leave `TestProjectAsync` exactly as it is. It scopes to one project, where a runtime identifier global property is legal, and `test run` depends on that.

- [ ] **Step 2: Test it, both directions**

Add tests asserting the argument is present when the option is set and absent by default, following the existing capture helpers. Also assert `-p:RuntimeIdentifier` never appears on the workspace run, because that is the failure mode.

**Verify by reversal.** Break each and confirm the matching test fails, then restore. Report what you broke and which assertions fired.

- [ ] **Step 3: Point `test all` at a single invocation**

`test all` currently loops, calling `TestProjectAsync` once per project. Replace that with one `TestAsync` call passing `hostRuntimeOnly: true`.

**Keep the host filter and its reporting.** The command must still say how many projects it will run and name every one it is skipping, with the reason, before the run starts. A project silently dropped from a test run looks exactly like success. `TestAsync` filters internally, but the reporting is the command's job, so keep using `GetTestProjects` for it.

The per-project failure accumulation goes away with the loop, because a single invocation reports every project's results itself. Note that in a comment so it does not look accidental.

- [ ] **Step 4: Prove it on a real multi-project workspace**

Build a two-project fixture with a `global.json` selecting the Microsoft.Testing.Platform runner and projects on `MSTest.Sdk/4.3.3`. Then confirm:

1. `test all` produces **one** invocation reporting the combined total, not one run per project.
2. `-p:KtsuHostRuntimeOnly=true` is on the emitted command.
3. Nothing was written into the fixture.

Note the fixture will not have `ktsu.Sdk`, so the flag will be inert there. That is expected and still proves the invocation shape and the argument. Say so plainly rather than claiming the pin was verified.

- [ ] **Step 5: Confirm `ci` did not move**

Run the differential harness at
`C:\Users\matth\AppData\Local\Temp\claude\C--dev-ktsu-dev-ImageGui\33d4d4e9-691c-4940-af2b-b84aba29be91\scratchpad\diffharness`,
17 cases, expect 0 divergences.

**Put your positive control in the harness's own `NewPipeline.cs`, not in shared code.** A control that mutates shared code applies to both sides of the comparison and cancels out, always returning zero, which is indistinguishable from a clean result. That mistake has already been made and caught on this work. Clear `bin` and `obj` before re-running after a revert, because `dotnet run` can serve a stale incremental build.

- [ ] **Step 6: Update the README and commit**

`README.md` is LF in KtsuBuild's working tree. The `test all` section already exists and describes the per-project shape from 2.6.0. Rewrite it for one invocation across the workspace, with the runtime pinned by the Sdk when the repository uses a version that understands the flag, and inert otherwise.

```bash
cd /c/dev/ktsu-dev/KtsuBuild
git checkout .editorconfig
git add KtsuBuild/Abstractions/IDotNetService.cs KtsuBuild/DotNet/DotNetService.cs KtsuBuild.Tests/DotNet/DotNetServiceTests.cs KtsuBuild.Tool/Commands/TestCommand.cs README.md
git commit -m "fix: run test all in one invocation and ask the Sdk for the host runtime [minor]"
```

- [ ] **Step 7: Open the pull request and stop**

```bash
cd /c/dev/ktsu-dev/KtsuBuild
git push -u origin feat/use-host-runtime-flag
gh pr create --base main --head feat/use-host-runtime-flag \
  --title "Run test all in one invocation, asking the Sdk for the host runtime" \
  --body "test all now runs a single dotnet test across the workspace and sets KtsuHostRuntimeOnly, which ktsu.Sdk translates into a per-project runtime identifier. The per-project shape shipped in 2.6.0 paid fourteen test host startups to get the pin and came out slower than an unpinned single invocation. Passing RuntimeIdentifier directly is not possible on a workspace-wide run, which is what NETSDK1134 rejects. Repositories on an Sdk that does not know the flag ignore it, so this is safe to release before the Sdk change lands."
```

Report both URLs. State that the effect is only visible once a repository builds with both the new Sdk and the new tool, and that the measurement to watch is ImGuiApp's Windows job, last seen at 21.4 minutes against 22.5 for an unpinned single invocation.
