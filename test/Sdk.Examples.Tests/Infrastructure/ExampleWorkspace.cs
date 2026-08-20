namespace Sdk.Examples.Tests.Infrastructure;

using System.IO.Compression;
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

    /// <summary>The absolute path to the workspace root (the copied example's solution directory).</summary>
    public string Root => root;

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

    /// <summary>Runs <c>dotnet pack</c> on a project and returns the result plus the output directory.</summary>
    public (CliResult Result, string OutputDir) Pack(string projectRelativePath, params string[] extraArgs)
    {
        string outputDir = Path.Combine(root, "pack-output");

        // UseAppHost mirrors Build(): packing a tool runs a publish, and with --no-build that
        // publish reuses the earlier build's intermediate output. If the two disagree the publish
        // fails looking for an apphost the build was told not to produce. A tool package never
        // contains an apphost - the shim is generated at install time.
        List<string> args =
        [
            "pack", projectRelativePath, "-c", "Release", "--nologo", "-o", outputDir,
            "-p:UseAppHost=false",
        ];
        args.AddRange(extraArgs);

        return (Cli.Dotnet(root, [.. args]), outputDir);
    }

    /// <summary>
    /// Installs a packed tool from <paramref name="nupkgDir"/> into an isolated tool path and runs
    /// its command. This is the only check that exercises what a user actually does, and the only
    /// one that fails when a tool package is well-formed but has no runnable payload.
    /// </summary>
    /// <remarks>
    /// The install uses its own nuget.config with <c>&lt;clear /&gt;</c> rather than
    /// <c>--add-source</c>, which the CLI rejects outright when the ambient NuGet configuration
    /// happens to use package source mapping.
    /// </remarks>
    public CliResult InstallAndRunTool(string nupkgDir, string packageId, string version, string command)
    {
        string installRoot = Path.Combine(root, "tool-install");
        string toolPath = Path.Combine(installRoot, "bin");
        Directory.CreateDirectory(installRoot);

        File.WriteAllText(Path.Combine(installRoot, "nuget.config"),
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="packed-tool" value="{nupkgDir}" />
              </packageSources>
            </configuration>
            """);

        CliResult install = Cli.Dotnet(installRoot, "tool", "install", packageId, "--version", version, "--tool-path", toolPath);
        if (!install.Succeeded)
        {
            return install;
        }

        string executable = Path.Combine(toolPath, OperatingSystem.IsWindows() ? command + ".exe" : command);
        return !File.Exists(executable)
            ? new CliResult(1, $"The installed tool path '{executable}' does not exist.{Environment.NewLine}{install.Output}")
            : Cli.Run(executable, installRoot);
    }

    /// <summary>The relative paths of every entry in a .nupkg, using forward slashes.</summary>
    public static IReadOnlyList<string> NupkgEntries(string nupkgPath)
    {
        using ZipArchive archive = ZipFile.OpenRead(nupkgPath);
        return [.. archive.Entries.Select(e => e.FullName)];
    }

    /// <summary>Reads a single entry out of a .nupkg as text.</summary>
    public static string ReadNupkgEntry(string nupkgPath, string entryName)
    {
        using ZipArchive archive = ZipFile.OpenRead(nupkgPath);
        ZipArchiveEntry entry = archive.GetEntry(entryName)
            ?? throw new InvalidOperationException($"'{entryName}' is not present in '{nupkgPath}'.");

        using StreamReader reader = new(entry.Open());
        return reader.ReadToEnd();
    }

    /// <summary>Writes a file into the workspace, creating any missing directories.</summary>
    public void WriteFile(string relativePath, string content)
    {
        string path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    /// <summary>Evaluates the given MSBuild properties on a project (no build/restore of outputs).</summary>
    public IReadOnlyDictionary<string, string> Evaluate(string projectRelativePath, params string[] properties) =>
        EvaluateWith(projectRelativePath, [], properties);

    /// <summary>
    /// Evaluates the given MSBuild properties on a project, passing <paramref name="extraArgs"/>
    /// (for example <c>-p:Foo=bar</c>) to the evaluation.
    /// </summary>
    public IReadOnlyDictionary<string, string> EvaluateWith(string projectRelativePath, string[] extraArgs, params string[] properties)
    {
        List<string> args = ["msbuild", projectRelativePath];
        args.AddRange(properties.Select(p => "-getProperty:" + p));
        args.AddRange(extraArgs);

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

    /// <summary>
    /// Evaluates an item type on a project and returns each item's metadata. No build or restore,
    /// so this is cheap enough to assert on item shape directly.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> EvaluateItems(string projectRelativePath, string itemType)
    {
        CliResult result = Cli.Dotnet(root, "msbuild", projectRelativePath, "-getItem:" + itemType);

        string output = result.Output.Trim();
        int brace = output.IndexOf('{', StringComparison.Ordinal);
        if (brace < 0)
        {
            return [];
        }

        using JsonDocument doc = JsonDocument.Parse(output[brace..]);
        if (!doc.RootElement.TryGetProperty("Items", out JsonElement items) ||
            !items.TryGetProperty(itemType, out JsonElement entries))
        {
            return [];
        }

        List<IReadOnlyDictionary<string, string>> results = [];
        foreach (JsonElement entry in entries.EnumerateArray())
        {
            Dictionary<string, string> metadata = new(StringComparer.Ordinal);
            foreach (JsonProperty property in entry.EnumerateObject())
            {
                metadata[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
            }

            results.Add(metadata);
        }

        return results;
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
            "ktsu.Sdk", "ktsu.Sdk.ConsoleApp", "ktsu.Sdk.App", "ktsu.Sdk.Tool",
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
