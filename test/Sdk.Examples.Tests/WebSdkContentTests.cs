namespace Sdk.Examples.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// KTSU1005 blesses two ways to reach ASP.NET Core: Microsoft.NET.Sdk.Web as the outer SDK, or
/// plain Microsoft.NET.Sdk plus an explicit FrameworkReference. Only the first brings the Web
/// SDK's appsettings content glob, so on the second the files have to come from ktsu.Sdk.Web
/// itself - otherwise the build succeeds with no warning and the service starts with none of its
/// configuration, which is the failure KTSU1005's own error message steers people toward.
/// </summary>
[TestClass]
public sealed class WebSdkContentTests
{
    /// <summary>The second path KTSU1005 blesses: the base SDK plus an explicit framework reference.</summary>
    private const string PlainSdkProject = """
        <Project Sdk="Microsoft.NET.Sdk">
          <Sdk Name="ktsu.Sdk" />
          <Sdk Name="ktsu.Sdk.Web" />
          <ItemGroup>
            <FrameworkReference Include="Microsoft.AspNetCore.App" />
            <PackageReference Include="Polyfill" PrivateAssets="all" />
          </ItemGroup>
        </Project>
        """;

    /// <summary>
    /// The demo's entry point with the ASP.NET Core usings spelled out. Those arrive as implicit
    /// global usings from Microsoft.NET.Sdk.Web, so a project on the plain-SDK path writes them
    /// itself - unlike the appsettings glob, that is visible at compile time rather than silent.
    /// </summary>
    private const string PlainSdkProgram = """
        using Microsoft.AspNetCore.Builder;
        using Microsoft.AspNetCore.Http;
        using Web;

        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();
        app.MapGet("/healthz", () => Results.Ok(new HealthStatus("ok")));
        app.Run();
        """;

    private const string AppSettings = """{"Logging":{"LogLevel":{"Default":"Information"}}}""";
    private const string AppSettingsDevelopment = """{"Logging":{"LogLevel":{"Default":"Debug"}}}""";

    /// <summary>A service built without the Web SDK still gets its appsettings files in the output.</summary>
    [TestMethod]
    public void PlainSdkService_CopiesAppSettingsToOutput()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Web"));
        workspace.WriteFile("Web/Web.csproj", PlainSdkProject);
        workspace.WriteFile("Web/Program.cs", PlainSdkProgram);
        workspace.WriteFile("Web/appsettings.json", AppSettings);
        workspace.WriteFile("Web/appsettings.Development.json", AppSettingsDevelopment);

        CliResult result = workspace.Build("Web/Web.csproj");

        Assert.IsTrue(result.Succeeded, $"Expected the plain-SDK web project to build.{Environment.NewLine}{result.Output}");

        string outputDir = Path.Join(workspace.Root, "Web", "bin", "Release", TargetFrameworks.Latest);
        AssertCopied(outputDir, "appsettings.json", result);
        AssertCopied(outputDir, "appsettings.Development.json", result);
    }

    /// <summary>
    /// The same files still arrive exactly once when Microsoft.NET.Sdk.Web is the outer SDK. The
    /// Web SDK contributes its own <c>**\*.json</c> content glob, so a second unconditional glob
    /// here would be a duplicate item rather than a fix.
    /// </summary>
    [TestMethod]
    public void WebSdkService_StillCopiesAppSettingsExactlyOnce()
    {
        using ExampleWorkspace workspace = ExampleWorkspace.Create(RepoLayout.Demo("Web"));
        workspace.WriteFile("Web/appsettings.json", AppSettings);

        IReadOnlyList<IReadOnlyDictionary<string, string>> content =
            workspace.EvaluateItems("Web/Web.csproj", "Content");

        int appSettings = content.Count(item =>
            item.TryGetValue("Identity", out string? identity) &&
            identity.EndsWith("appsettings.json", StringComparison.Ordinal));

        Assert.AreEqual(1, appSettings, "appsettings.json should be a Content item exactly once under Microsoft.NET.Sdk.Web.");

        CliResult result = workspace.Build("Web/Web.csproj");

        Assert.IsTrue(result.Succeeded, $"Expected the Web SDK demo to build.{Environment.NewLine}{result.Output}");
        AssertCopied(Path.Join(workspace.Root, "Web", "bin", "Release", TargetFrameworks.Latest), "appsettings.json", result);
    }

    private static void AssertCopied(string outputDir, string fileName, CliResult result) =>
        Assert.IsTrue(
            File.Exists(Path.Join(outputDir, fileName)),
            $"Expected '{fileName}' in '{outputDir}'.{Environment.NewLine}{result.Output}");
}
