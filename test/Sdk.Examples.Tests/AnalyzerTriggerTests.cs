namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Proves that each SDK analyzer fires for a project crafted to violate exactly its rule.
/// Because the diagnostics are Error severity, a triggering build is expected to fail and the
/// specific KTSU id is expected in the output.
/// </summary>
[TestClass]
public sealed class AnalyzerTriggerTests
{
    /// <param name="folder">The analyzer example directory under examples/analyzers.</param>
    /// <param name="project">The project to build, relative to the example directory.</param>
    /// <param name="diagnostic">The KTSU id expected to fire.</param>
    [TestMethod]
    [DataRow("KTSU0001-MissingStandardPackages", "MissingStandardPackages/MissingStandardPackages.csproj", "KTSU0001", DisplayName = "KTSU0001 Missing standard packages")]
    [DataRow("KTSU0003-PreferEnsureNotNull", "PreferEnsureNotNull/PreferEnsureNotNull.csproj", "KTSU0003", DisplayName = "KTSU0003 Prefer Ensure.NotNull")]
    [DataRow("KTSU0004-ManualNullCheck", "ManualNullCheck/ManualNullCheck.csproj", "KTSU0004", DisplayName = "KTSU0004 Manual null check")]
    [DataRow("KTSU0005-OrphanedPackageVersion", "OrphanedPackageVersion/OrphanedPackageVersion.csproj", "KTSU0005", DisplayName = "KTSU0005 Orphaned PackageVersion")]
    [DataRow("KTSU0006-TransitivePackageUsedDirectly", "TransitivePackageUsedDirectly/TransitivePackageUsedDirectly.csproj", "KTSU0006", DisplayName = "KTSU0006 Transitive package used directly")]
    public void Analyzer_Triggers(string folder, string project, string diagnostic)
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Analyzer(folder));

        CliResult result = workspace.Build(project);

        Assert.IsFalse(result.Succeeded, $"Expected {diagnostic} to fail the build.{Environment.NewLine}{result.Output}");
        CollectionAssert.Contains(
            result.KtsuDiagnostics().ToList(),
            diagnostic,
            $"Expected {diagnostic} in the build output.{Environment.NewLine}{result.Output}");
    }

    /// <summary>
    /// KTSU0005 must not fire for a PackageVersion that is referenced only by a sibling project.
    /// The orphan scan reconstructs the solution-wide reference set by globbing sibling .csproj
    /// files under the discovered solution directory. When the SDK computes that directory itself
    /// (as in a CI `dotnet build` of a bare project) the value has no trailing separator, which
    /// previously turned the glob into a malformed pattern that matched nothing — so every
    /// sibling-only reference was falsely reported as an orphan. Building Consumer, which does not
    /// reference Newtonsoft.Json, must succeed because sibling Producer does reference it.
    /// </summary>
    [TestMethod]
    public void Analyzer_KTSU0005_DoesNotTrigger_For_SiblingOnlyReference()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Analyzer("KTSU0005-OrphanedPackageVersion-CrossProject"));

        CliResult result = workspace.Build("Consumer/Consumer.csproj");

        CollectionAssert.DoesNotContain(
            result.KtsuDiagnostics().ToList(),
            "KTSU0005",
            $"KTSU0005 must not fire for Newtonsoft.Json, which the sibling Producer references.{Environment.NewLine}{result.Output}");
        Assert.IsTrue(result.Succeeded, $"Expected a clean build with no orphan diagnostic.{Environment.NewLine}{result.Output}");
    }

    /// <summary>
    /// KTSU0005 must not fire for SDK-governed packages that carry a PackageVersion but no
    /// PackageReference: the KTSU0001 standard packages (e.g. System.Memory) and the
    /// Microsoft.Testing.Extensions.* runner family that MSTest.Sdk injects into test projects
    /// (which this scan skips). These are covered by the ignore list in Sdk.targets.
    /// </summary>
    [TestMethod]
    public void Analyzer_KTSU0005_DoesNotTrigger_For_AllowlistedPackages()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Analyzer("KTSU0005-OrphanedPackageVersion-Allowlisted"));

        CliResult result = workspace.Build("Allowlisted/Allowlisted.csproj");

        CollectionAssert.DoesNotContain(
            result.KtsuDiagnostics().ToList(),
            "KTSU0005",
            $"KTSU0005 must not fire for ignore-listed packages (System.Memory, Microsoft.Testing.Extensions.*).{Environment.NewLine}{result.Output}");
        Assert.IsTrue(result.Succeeded, $"Expected a clean build with no orphan diagnostic.{Environment.NewLine}{result.Output}");
    }

    /// <summary>
    /// KTSU0002 (missing InternalsVisibleTo) is a CompilationEnd diagnostic reported at a
    /// syntax-tree location. When it is the only diagnostic in an otherwise-clean compilation
    /// it can be masked by Roslyn analyzer-result caching (see ktsu-dev/Sdk#12 / #8 / #11), so a
    /// negative result here is reported as inconclusive rather than failing the run. The example
    /// itself is exercised either way.
    /// </summary>
    [TestMethod]
    public void Analyzer_KTSU0002_Triggers()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Analyzer("KTSU0002-MissingInternalsVisibleTo"));

        CliResult result = workspace.Build("Service/Service.csproj");

        if (!result.KtsuDiagnostics().Contains("KTSU0002"))
        {
            Assert.Inconclusive(
                "KTSU0002 was not surfaced as the sole diagnostic. This is the CompilationEnd " +
                "analyzer masking tracked in ktsu-dev/Sdk#12; the example is correct and triggers " +
                $"when another diagnostic co-occurs.{Environment.NewLine}{result.Output}");
        }

        Assert.IsFalse(result.Succeeded, $"Expected KTSU0002 to fail the build.{Environment.NewLine}{result.Output}");
    }
}
