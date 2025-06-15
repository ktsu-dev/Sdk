# ktsu.Sdk.Test

This SDK provides standardized test configuration and tools for ktsu projects.

## Features

- Standard test configuration for MSTest projects
- Integrated code coverage collection with dotnet-coverage
- Automatic handling of .runsettings files
- Scripts for running tests with coverage

## Usage

Simply reference this SDK in your test project's `.csproj` file:

```xml
<Project Sdk="ktsu.Sdk.Test">
  <ItemGroup>
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <!-- Other test dependencies -->
  </ItemGroup>
  
  <!-- Project references -->
</Project>
```

## Coverage Collection

The SDK includes scripts for running tests with coverage in the `tools` directory:

```bash
# PowerShell
.\tools\run-tests-with-coverage.ps1

# Batch file (Windows)
.\tools\run-tests-with-coverage.cmd
```

These scripts will:
1. Build your test project
2. Run tests with coverage collection using dotnet-coverage
3. Generate a Cobertura coverage report in the `coverage` directory

### Script Parameters

The scripts accept the following parameters:

- `Configuration` - Build configuration (default: Release)
- `CoverageOutputPath` - Output path for coverage reports (default: coverage)
- `CoverageFormat` - Coverage format (default: cobertura)
- `TestProject` - Test project to run (default: BlastMerge.Test)

Example with custom parameters:
```bash
.\tools\run-tests-with-coverage.ps1 -Configuration Debug -CoverageOutputPath "my-coverage" -TestProject "MyProject.Tests"
```

## Settings

A `.runsettings` file in your solution directory will be automatically detected and used for test runs. Make sure it contains appropriate coverage collection settings.

Example `.runsettings` file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <MaxCpuCount>0</MaxCpuCount>
    <ResultsDirectory>.\TestResults</ResultsDirectory>
    <TestSessionTimeout>100000</TestSessionTimeout>
    <TreatNoTestsAsError>true</TreatNoTestsAsError>
    <Coverage>
      <Output>coverage\coverage.cobertura.xml</Output>
    </Coverage>
  </RunConfiguration>

  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage" uri="datacollector://Microsoft/CodeCoverage/2.0">
        <Configuration>
          <Format>cobertura,opencover</Format>
          <ExcludeByAttribute>
            Obsolete,
            GeneratedCodeAttribute,
            CompilerGeneratedAttribute,
            ExcludeFromCodeCoverageAttribute
          </ExcludeByAttribute>
          <Include>
            [*]*,[ktsu.*]*
          </Include>
          <Exclude>
            [*]*Tests*,[*]*Test*,[*]*.Test*,[xunit.*]*,[Moq*]*,[System.*]*,[Microsoft.*]*
          </Exclude>
          <SingleHit>false</SingleHit>
          <IncludeTestAssembly>false</IncludeTestAssembly>
          <SkipAutoProps>true</SkipAutoProps>
          <ExcludeAssembliesWithoutSources>MissingAll</ExcludeAssembliesWithoutSources>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

## Required Tools

The scripts automatically install the required tools when running tests with coverage:

- dotnet-coverage - For collecting code coverage data 
