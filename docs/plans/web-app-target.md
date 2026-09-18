# Plan: ASP.NET Core Web Applications (`ktsu.Sdk.Web`)

## Summary

Add a `ktsu.Sdk.Web` sub-SDK so an ASP.NET Core web application or HTTP service gets the same
zero-configuration conventions the SDK already provides for libraries, console apps, GUI apps and
tools.

A web project produces a **published directory or container image, never a package**. It targets a
single framework, because ASP.NET Core has no netstandard surface to multi-target against.

## Background / Current State

Nothing in the SDK addresses web projects today, and the core SDK's defaults are actively wrong for
one in two independent ways. Both were found by building a real minimal API against the packed SDK
rather than by reading the props.

### 1. The multi-target default does not compile

`Sdk/Sdk.props` sets:

```xml
<TargetFrameworks>net10.0;net9.0;net8.0;net7.0;net6.0;net5.0;netstandard2.0;netstandard2.1</TargetFrameworks>
```

`Microsoft.NET.Sdk.Web` contributes implicit global usings for `Microsoft.AspNetCore.*` and
`Microsoft.Extensions.*`. Neither resolves on a netstandard target, so the netstandard inner builds
fail with a wall of `CS0234` inside `obj/.../GlobalUsings.g.cs`:

```
error CS0234: The type or namespace name 'AspNetCore' does not exist in the namespace 'Microsoft'
```

The diagnostic names generated code. It names neither the framework that failed nor the property
that introduced it, and a consumer reading it has no path back to the cause.

### 2. The documentation default fails on the first public type

`Sdk/Sdk.props` sets `GenerateDocumentationFile=true` alongside `TreatWarningsAsErrors=true`. `CS1591`
is not in the core suppression list, so a single undocumented public type is a build error:

```
error CS1591: Missing XML comment for publicly visible type or member 'HealthResponse'
```

For a library this is the SDK doing its job: a library's public types are its product, and an
undocumented one is a real defect. A web application's public types are request and response shapes
with no external consumer and no published API surface. The rule is enforcing a contract that does
not exist.

This is easy to miss when evaluating the idea, because the canonical minimal API sample declares no
public types at all. It appears the moment anyone adds a DTO.

## Design

### Consumption

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <Sdk Name="ktsu.Sdk" />
  <Sdk Name="ktsu.Sdk.Web" />
</Project>
```

**This is the only SDK in the family whose outer SDK is not `Microsoft.NET.Sdk`.** An
`<Sdk Name="..." />` element extends the base SDK rather than replacing it, so `ktsu.Sdk.Web` cannot
bring the ASP.NET Core framework reference in on its own. The precedent is `ktsu.Sdk.Godot`, which
composes with `Godot.NET.Sdk` as the outer SDK for the same structural reason.

A service that wants the runtime without the Web SDK's static asset and Razor machinery can instead
keep `Microsoft.NET.Sdk` and add:

```xml
<FrameworkReference Include="Microsoft.AspNetCore.App" />
```

Both forms are verified. The second does **not** bring the Web SDK's implicit global usings, so that
project writes its own `using Microsoft.AspNetCore.Builder;` and friends.

### Properties

| Property | Value | Why |
| --- | --- | --- |
| `TargetFramework` | `net10.0` | ASP.NET Core has no netstandard surface |
| `TargetFrameworks` | *(cleared)* | Same |
| `OutputType` | `Exe` | ASP.NET Core self-hosts |
| `NoWarn` | `+= CS1591` | Payload shapes are not an API surface |
| `EnablePackageValidation` | `false` | Not a package |
| `ApiCompatValidateAssemblies` | `false` | Not a package |
| `IncludeSource` | `false` | Not a package |
| `IsWebProject` | `true` | Project-type flag, matching `IsWindowsProject` and siblings |

`GenerateDocumentationFile` stays **on**. Only the enforcement is dropped. OpenAPI generators
(Swashbuckle, NSwag) read the XML documentation file to enrich generated schemas, which is a real
reason a web project wants one, and turning generation off to silence a warning would take the
capability with it.

`RuntimeIdentifiers` is deliberately **not** narrowed, unlike `ktsu.Sdk.Tool` (which clears it
because `PackAsTool` turns each RID into a separate package) and `ktsu.Sdk.Godot` (which clears it
because the export pipeline derives its platform from a single RID). Publishing does no such fan-out:
the list is a set of permitted values, a service is published with an explicit RID about as often as
without one, and narrowing to Linux would reject a legitimate `dotnet publish -r win-x64` for a
service hosted on Windows.

### Diagnostics

**KTSU1004** errors when `TargetFrameworks` is set, mirroring `KTSU1001` on `ktsu.Sdk.Tool`. A
consumer's own `PropertyGroup` is evaluated after `Sdk.props`, so without the guard the netstandard
inner builds come back and the failure reverts to the CS0234 wall.

**KTSU1005** errors when neither `UsingMicrosoftNETSdkWeb` is true nor a `Microsoft.AspNetCore.App`
`FrameworkReference` is present, which is the shape of "used this SDK, forgot the outer one".

KTSU1005 is hooked `BeforeTargets="CoreCompile"` rather than `BeforeTargets="Build"`. A target's
`DependsOnTargets` are already satisfied by the time its `BeforeTargets` hooks run, so on a
single-framework project a `Build` hook fires *after* the compile it is meant to pre-empt and the
compiler error wins the race. This was observed, not predicted: the first implementation used
`Build` and the guard never appeared. KTSU1001 and KTSU1004 do not have the problem, because the
condition each guards can only be true in an outer build, and an outer build dispatches its inner
builds from the `Build` target itself.

## Validation

The `Web` demo under `examples/demos/Web` is a real ASP.NET Core service and is built by
`DemoBuildTests` on every CI run, like the other buildable demos. It carries a public,
deliberately undocumented `HealthStatus` record, which is what would fail the build if the CS1591
suppression regressed.

Verified by hand against the locally packed SDK, beyond what the demo asserts:

- Builds on the canonical `Microsoft.NET.Sdk.Web` composition.
- Builds on the `FrameworkReference` composition.
- `dotnet publish -r linux-x64 --no-self-contained` produces a framework-dependent output: a Linux
  apphost, no runtime assemblies, and the Web SDK's static asset manifest present.
- The published application starts and serves a request (`200`, expected JSON body).
- KTSU1004 fires on a re-declared `TargetFrameworks`.
- KTSU1005 fires when the ASP.NET Core framework reference is missing.

## Out of Scope

- **Container image generation.** .NET's built-in `PublishContainer` support is a reasonable future
  addition, but it is a distribution decision with registry and tagging implications that belong to
  the consuming repository's pipeline rather than to a compiler-facing SDK.
- **Supplying implicit global usings on the `FrameworkReference` path.** It would make the two
  compositions equivalent, at the cost of this SDK maintaining a parallel copy of a list
  `Microsoft.NET.Sdk.Web` already owns and revises per release.
- **`InvariantGlobalization`.** The core SDK sets it true, which is a deliberate repository-wide
  default and a defensible one for a container service. It is left alone here rather than flipped
  for web projects specifically, because it is a runtime behavior question for the whole family
  rather than a web concern, and changing it silently for one project type would be worse than
  either answer.
