#!/usr/bin/env bash
# Validates the platform-specific app SDKs by packing them locally and asserting
# that a consuming project resolves the expected TargetFramework, OutputType,
# RuntimeIdentifiers, and project-type detection flag for each platform.
#
# Runs on Linux/macOS and on Windows runners via `shell: bash`. Requires only the
# base .NET SDK -- no mobile workloads, because property evaluation does not build
# the consumer.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORK="$(mktemp -d)"
FEED="$WORK/feed"
mkdir -p "$FEED"
trap 'rm -rf "$WORK"' EXIT

echo "==> Packing SDK packages to local feed: $FEED"
for proj in Sdk Sdk.Analyzers Sdk.Windows Sdk.Linux Sdk.macOS Sdk.iOS Sdk.Android; do
  dotnet pack "$REPO_ROOT/$proj/$proj.csproj" -c Release -o "$FEED" --nologo -v quiet
done

# Discover the packed version from the core package filename.
core_pkg="$(ls "$FEED"/ktsu.Sdk.*.nupkg | grep -v '\.Analyzers\.' | grep -vE '\.(Windows|Linux|macOS|iOS|Android)\.' | head -1)"
VERSION="$(basename "$core_pkg" | sed -E 's/^ktsu\.Sdk\.(.*)\.nupkg$/\1/')"
echo "==> Discovered SDK version: $VERSION"

FAILURES=0

# assert <label> <actual> <expected>
assert() {
  local label="$1" actual="$2" expected="$3"
  if [[ "$actual" == "$expected" ]]; then
    echo "    PASS  $label = $actual"
  else
    echo "    FAIL  $label = '$actual' (expected '$expected')"
    FAILURES=$((FAILURES + 1))
  fi
}

# validate_platform <SdkName> <ProjectSuffix> <TFM> <OutputType> <RIDs> <DetectFlag>
validate_platform() {
  local sdk="$1" suffix="$2" tfm="$3" outtype="$4" rids="$5" flag="$6"
  local sln="TestApp" proj="TestApp.$suffix"
  local dir="$WORK/$suffix"
  echo "==> Validating $sdk (consumer: $proj)"
  mkdir -p "$dir/$proj"

  cat > "$dir/global.json" <<EOF
{
  "msbuild-sdks": {
    "ktsu.Sdk": "$VERSION",
    "$sdk": "$VERSION"
  }
}
EOF

  cat > "$dir/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$FEED" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

  cat > "$dir/Directory.Packages.props" <<EOF
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
EOF

  printf 'Microsoft Visual Studio Solution File, Format Version 12.00\n' > "$dir/$sln.sln"

  cat > "$dir/$proj/$proj.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="ktsu.Sdk" />
  <Sdk Name="$sdk" />
</Project>
EOF

  echo 'System.Console.WriteLine("hi");' > "$dir/$proj/Program.cs"

  local json
  json="$(dotnet msbuild "$dir/$proj/$proj.csproj" \
    -getProperty:TargetFramework \
    -getProperty:OutputType \
    -getProperty:RuntimeIdentifiers \
    -getProperty:"$flag" 2>/dev/null)"

  local a_tfm a_out a_rids a_flag
  a_tfm="$(echo "$json" | sed -n 's/.*"TargetFramework": "\([^"]*\)".*/\1/p')"
  a_out="$(echo "$json" | sed -n 's/.*"OutputType": "\([^"]*\)".*/\1/p')"
  a_rids="$(echo "$json" | sed -n 's/.*"RuntimeIdentifiers": "\([^"]*\)".*/\1/p')"
  a_flag="$(echo "$json" | sed -n "s/.*\"$flag\": \"\([^\"]*\)\".*/\1/p")"

  assert "$suffix.TargetFramework"   "$a_tfm"  "$tfm"
  assert "$suffix.OutputType"        "$a_out"  "$outtype"
  assert "$suffix.RuntimeIdentifiers" "$a_rids" "$rids"
  assert "$suffix.$flag"             "$a_flag" "true"
}

validate_platform "ktsu.Sdk.Windows" "Windows" "net10.0"         "WinExe" "win-x64;win-x86;win-arm64"                              "IsWindowsProject"
validate_platform "ktsu.Sdk.Linux"   "Linux"   "net10.0"         "Exe"    "linux-x64;linux-arm64;linux-musl-x64;linux-musl-arm64"  "IsLinuxProject"
validate_platform "ktsu.Sdk.macOS"   "macOS"   "net10.0"         "Exe"    "osx-x64;osx-arm64"                                      "IsMacProject"
validate_platform "ktsu.Sdk.iOS"     "iOS"     "net10.0-ios"     "Exe"    "ios-arm64;iossimulator-x64;iossimulator-arm64"          "IsIosProject"
validate_platform "ktsu.Sdk.Android" "Android" "net10.0-android" "Exe"    ""                                                       "IsAndroidProject"

echo ""
if [[ "$FAILURES" -eq 0 ]]; then
  echo "==> All platform SDK validations passed."
else
  echo "==> $FAILURES assertion(s) failed."
  exit 1
fi
