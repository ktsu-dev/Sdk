# ktsu.Sdk.Godot demo

A Godot 4 game assembly — an openable Godot project plus the C# it loads.

```
AUTHORS.md             "ktsu" - the authors namespace the core SDK builds names from
Demo.Godot/
  project.godot        the Godot project, including dotnet/project/assembly_name
  Main.tscn            a scene with Main.cs attached
  Main.cs              the node script Godot instantiates
  Greeter.cs           engine-agnostic logic with no GodotSharp types
  Demo.Godot.csproj    Godot.NET.Sdk + ktsu.Sdk + ktsu.Sdk.Godot
```

The `AUTHORS.md` is load-bearing for the demo, not decoration. It is what makes the core SDK
derive `ktsu.Demo.Godot` — without an authors namespace the derived name and the project name
would be the same string, and the assembly-name contract described below would be invisible.

## Build

```bash
dotnet build Demo.Godot/Demo.Godot.csproj
```

No engine installation is needed: `Godot.NET.Sdk` and the GodotSharp bindings come from NuGet,
so the assembly compiles anywhere. Running the game does of course need the Godot editor —
open `Demo.Godot/` in Godot 4.7 or newer and press play.

## What ktsu.Sdk.Godot is doing here

`Godot.NET.Sdk` is the **outer** SDK, and it has to be: it supplies the GodotSharp bindings,
the source generators, the `Debug;ExportDebug;ExportRelease` configurations and the engine's
output layout. `ktsu.Sdk` and `ktsu.Sdk.Godot` are layered on top, in that order, so the
extension gets the last word over the core defaults.

Two of those defaults would otherwise break the engine's own conventions, and the SDK puts
both back — but only when `Godot.NET.Sdk` is the outer SDK, so a plain library that merely
references GodotSharp is left alone:

| Property | Why |
| --- | --- |
| `AppendTargetFrameworkToOutputPath=false` | Godot loads the assembly from `.godot/mono/temp/bin/<Configuration>/` with no framework folder. The core SDK sets this back to `true`, which moves the output one directory deeper and leaves the editor reporting a missing assembly. |
| `AssemblyName=$(MSBuildProjectName)` | `project.godot` resolves the assembly by the name in `dotnet/project/assembly_name`, which Godot writes as the project name. The core SDK sets `AssemblyName` to the fully-qualified namespace (`ktsu.Demo.Godot`), which is no longer the file Godot looks for. |

`RootNamespace`, `PackageId` and `Title` keep the ktsu-namespaced value — only the assembly
file name is pinned — and a project whose `project.godot` says something else can still set
`AssemblyName` itself.

The target framework is pinned to the same `net10.0` as the rest of this SDK family.
`Godot.NET.Sdk` deliberately sets none of its own: GodotSharp declares the *minimum* (net8.0
as of Godot 4.7) and the project picks anything at or above it.

## One thing to know about the name

The project is named `{Solution}.Godot` so the core SDK detects the project type, which makes
the `Demo.Godot` namespace shadow the engine's own `Godot` namespace. `using global::Godot;`
is the fix, and `Main.cs` shows it.
