# ktsu.Sdk.Unity demo

A Unity **managed plug-in** and the Unity project that consumes it.

```
Demo.Unity/            the plug-in: an ordinary netstandard2.1 library built by your .NET SDK
UnityProject/          the consuming Unity project
  Assets/Plugins/      where the built plug-in lands
  Assets/Scripts/      engine-side code that calls into it, compiled by Unity
```

Unity never builds `Demo.Unity.csproj`. It builds nothing of yours that lives outside
`Assets/`, resolves no NuGet packages, and imports whatever assemblies it finds under
`Assets/`. So the workflow is two steps: build the plug-in with the .NET SDK, then get the
output into `Assets/Plugins/`.

## Build and deploy

```bash
dotnet build Demo.Unity/Demo.Unity.csproj
```

The `DeployToUnityProject` target in the project file copies the build output to
`UnityProject/Assets/Plugins/` afterwards. It copies the whole output directory rather than
just the plug-in, because Unity resolves nothing for you — every assembly needed at run time
has to sit next to it.

This demo turns out to need nothing else, which is worth understanding rather than assuming.
`Polyfill` embeds source at build time and has no runtime assembly (which is also why KTSU0007
wants `PrivateAssets="all"` on it). `System.Memory` ships `lib/netstandard2.1/_._`, because
netstandard2.1 already has `Span<T>` — the reference is there to satisfy KTSU0001 and
contributes nothing to the output. Retarget to netstandard2.0 for an older editor and
`System.Memory.dll` does appear, and does have to be deployed.

A real project would more often deploy from a `dotnet publish`, or skip the copy entirely and
consume the package through NuGetForUnity. The target here is the smallest thing that shows
the shape of the step.

## Why netstandard2.1

`ktsu.Sdk.Unity` pins it, and the pin is not a style choice. Unity's scripting runtime is Mono
or IL2CPP, not .NET Core. Both API Compatibility Levels Unity offers — ".NET Standard 2.1"
(the default) and ".NET Framework" — implement netstandard2.1, so it is the one target that
loads in every supported configuration. A `netX.0` assembly does not import at all, and
**KTSU1003** fails the build rather than letting that surface later in the Unity console.

The plug-in is compiled by *your* .NET SDK, so it may use C# language features newer than
Unity's in-editor compiler accepts — that is fine, because Unity only ever sees the IL.
Features that need a newer *runtime* (ref fields, static abstract interface members) are a
different matter and will still fail on Unity's.

## About the Unity project

`UnityProject/` is a skeleton, not a full Unity project: it carries the editor version, a
package manifest and the two `Assets/` folders that matter here. Opening it in Unity
regenerates the rest.

### Version control

A Unity project brings its own `.gitignore`, and this one is worth reading before you copy the
layout. Two rules in the shared ktsu.Sdk `.gitignore` — both generic .NET/Visual Studio rules
that predate any engine support — will quietly eat Unity source if nothing overrides them:

| Rule | Meant for | What it also catches |
| --- | --- | --- |
| `**/[Pp]ackages/*` | a NuGet restore folder | Unity's `Packages/manifest.json` and `packages-lock.json`, which are project source |
| `*.meta` | the Visual Studio C++ build artifact | every Unity `.meta` file |

The `.meta` one is the dangerous one. Unity generates a `.meta` per asset carrying the GUID that
scenes, prefabs and serialized references point at. Leave them out of version control and every
clone regenerates fresh GUIDs, silently breaking those references — including for a plug-in whose
`.dll` is itself a build output, since the `.meta` is what keeps the reference stable across
rebuilds. The packaged `.gitignore` now negates both rules for the asset tree, so a consuming
repository gets this right by default.

`UnityProject/.gitignore` repeats those negations anyway, because this demo cannot rely on them:
`examples/.gitignore` is generated — the SDK's `_KtsuSyncStyleConfigFiles` target rewrites it from
whichever ktsu.Sdk version [`global.json`](../../global.json) pins, today a published one that
predates them — and a rule in that copy beats a *negation* of the same rule at the repository
root. (A brand-new rule with no counterpart there, like Godot's `.godot/`, is unaffected and needs
no such workaround.)

That file also ignores Unity's own generated folders — `Library/`, `Temp/`, `Logs/` and friends.
Those stay out of the shared `.gitignore` on purpose: the names are only caches when they sit
beside `Assets/` and `ProjectSettings/`, and ignoring something as generic as `Library/`
repo-wide would catch unrelated directories — this repository's own `examples/demos/Library`
among them. `Assets/Plugins/.gitignore` keeps the deployed `.dll` and `.pdb` out while leaving
their `.meta` files committable.
