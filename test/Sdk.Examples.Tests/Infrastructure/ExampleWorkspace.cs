namespace Sdk.Examples.Tests.Infrastructure;

using System.Text.Json;

/// <summary>
/// An isolated copy of an example project tree, configured to consume the locally packed
/// ktsu.Sdk packages. Copying to a unique temp directory keeps the repository pristine and
/// lets each test build with a clean intermediate output.
/// </summary>
internal sealed class ExampleWorkspace : IDisposable
{
    private readonly string root;

    private ExampleWorkspace(string root) => this.root = root;

    /// <summary>Copies an example directory to a fresh temp workspace wired to the local SDK feed.</summary>
    public static ExampleWorkspace Create(string sourceDir)
    {
        string dest = Path.Combine(Path.GetTempPath(), "ktsu-sdk-example-" + Guid.NewGuid().ToString("N"));
        CopyTree(sourceDir, dest);

        WriteGlobalJson(dest);
        WriteNuGetConfig(dest);
        MaybeWriteCompilerToolset(dest);

        return new ExampleWorkspace(dest);
    }

    /// <summary>Runs <c>dotnet build</c> on a project relative to the workspace root.</summary>
    public CliResult Build(string projectRelativePath, params string[] extraArgs)
    {
        // build-server shutdown + --no-incremental + no shared compilation defeat the
        // Roslyn analyzer-result caching that can otherwise mask CompilationEnd diagnostics.
        Cli.Dotnet(root, "build-server", "shutdown");

        List<string> args =
        [
            "build", projectRelativePath,
            "-c", "Release", "--nologo", "--no-incremental",
            "-p:UseAppHost=false",
            "-p:UseSharedCompilation=false",
        ];
        args.AddRange(extraArgs);
        return Cli.Dotnet(root, [.. args]);
    }

    /// <summary>Evaluates the given MSBuild properties on a project (no build/restore of outputs).</summary>
    public IReadOnlyDictionary<string, string> Evaluate(string projectRelativePath, params string[] properties)
    {
        List<string> args = ["msbuild", projectRelativePath];
        args.AddRange(properties.Select(p => "-getProperty:" + p));

        CliResult result = Cli.Dotnet(root, [.. args]);

        // `dotnet msbuild -getProperty` emits a single value when one property is requested,
        // or a JSON object under "Properties" when several are requested.
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        string output = result.Output.Trim();

        int brace = output.IndexOf('{', StringComparison.Ordinal);
        if (properties.Length == 1 && brace < 0)
        {
            values[properties[0]] = output;
            return values;
        }

        using JsonDocument doc = JsonDocument.Parse(output[brace..]);
        JsonElement props = doc.RootElement.GetProperty("Properties");
        foreach (string property in properties)
        {
            values[property] = props.TryGetProperty(property, out JsonElement v) ? (v.GetString() ?? string.Empty) : string.Empty;
        }

        return values;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
    }

    private static void WriteGlobalJson(string dest)
    {
        string[] sdks =
        [
            "ktsu.Sdk", "ktsu.Sdk.ConsoleApp", "ktsu.Sdk.App",
            "ktsu.Sdk.Windows", "ktsu.Sdk.Linux", "ktsu.Sdk.macOS",
            "ktsu.Sdk.iOS", "ktsu.Sdk.Android",
        ];
        string entries = string.Join("," + Environment.NewLine,
            sdks.Select(s => $"    \"{s}\": \"{SdkFeed.Version}\""));
        File.WriteAllText(Path.Combine(dest, "global.json"),
            $"{{{Environment.NewLine}  \"msbuild-sdks\": {{{Environment.NewLine}{entries}{Environment.NewLine}  }}{Environment.NewLine}}}{Environment.NewLine}");
    }

    private static void WriteNuGetConfig(string dest)
    {
        File.WriteAllText(Path.Combine(dest, "nuget.config"),
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="ktsu-local" value="{SdkFeed.FeedDir}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """);
    }

    /// <summary>
    /// When KTSU_TOOLSET is set, pins a specific Roslyn compiler for the build. This is only
    /// needed when the host .NET SDK ships an older Roslyn than ktsu.Sdk.Analyzers was built
    /// against (CS9057). On a current SDK this is unset and the bundled compiler is used.
    /// </summary>
    private static void MaybeWriteCompilerToolset(string dest)
    {
        string? toolset = Environment.GetEnvironmentVariable("KTSU_TOOLSET");
        if (string.IsNullOrWhiteSpace(toolset))
        {
            return;
        }

        File.WriteAllText(Path.Combine(dest, "Directory.Build.props"),
            $"""
            <Project>
              <ItemGroup>
                <PackageReference Include="Microsoft.Net.Compilers.Toolset" VersionOverride="{toolset}" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
    }

    private static void CopyTree(string source, string dest)
    {
        foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(dir);
            if (name is "bin" or "obj")
            {
                continue;
            }

            Directory.CreateDirectory(dir.Replace(source, dest, StringComparison.Ordinal));
        }

        Directory.CreateDirectory(dest);
        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            string target = file.Replace(source, dest, StringComparison.Ordinal);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}
