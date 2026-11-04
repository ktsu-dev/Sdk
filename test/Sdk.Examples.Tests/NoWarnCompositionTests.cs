namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Pins how the core SDK composes <c>NoWarn</c> with a consumer's own suppressions.
/// </summary>
/// <remarks>
/// <c>Directory.Build.props</c> is imported by <c>Microsoft.Common.props</c> through the outer
/// <c>Project Sdk=</c> attribute, so it is evaluated before <c>Sdk/Sdk.props</c>. An assignment
/// there without a <c>$(NoWarn);</c> prefix replaced every repo-wide suppression the consumer had
/// set, and with <c>TreatWarningsAsErrors</c> on the suppressed warning came back as a build
/// error. See ktsu-dev/Sdk#59.
/// </remarks>
[TestClass]
public sealed class NoWarnCompositionTests
{
    [TestMethod]
    public void DirectoryBuildPropsNoWarn_SurvivesTheSdkProps()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Library"));

        workspace.WriteFile("Directory.Build.props",
            """
            <Project>
              <PropertyGroup>
                <NoWarn>$(NoWarn);CS0168</NoWarn>
              </PropertyGroup>
            </Project>
            """);

        IReadOnlyDictionary<string, string> props = workspace.Evaluate("Library/Library.csproj", "NoWarn");
        string[] codes = props["NoWarn"].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        CollectionAssert.Contains(codes, "CS0168", $"The consumer's Directory.Build.props suppression was discarded: NoWarn={props["NoWarn"]}");
        CollectionAssert.Contains(codes, "CA1724", $"The SDK's own suppressions are missing: NoWarn={props["NoWarn"]}");
    }
}
