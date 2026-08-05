# Demo.Tool

A minimal `ktsu.Sdk.Tool` example. The project packs as a .NET tool and installs as `demo`.

The solution carries the metadata files a real ktsu solution has (`LICENSE.md`, `README.md`,
`icon.png`, `VERSION.md`) because `dotnet pack` fails without the files the SDK
declares as package metadata. The other demos only build, so they don't need them.
