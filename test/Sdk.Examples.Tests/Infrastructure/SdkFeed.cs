namespace Sdk.Examples.Tests.Infrastructure;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Packs the ktsu.Sdk MSBuild SDK packages to a temporary local feed once for the whole test
/// run, replicating the <c>{version}</c> token substitution that <c>make-analyzer-releases.ps1</c>
/// performs at release time (without which the analyzer PackageReference cannot restore).
/// </summary>
[TestClass]
public static class SdkFeed
{
    private static string? feedDir;

    /// <summary>The directory containing the freshly packed *.nupkg files.</summary>
    public static string FeedDir => feedDir
        ?? throw new InvalidOperationException("The SDK feed has not been initialized.");

    /// <summary>The packed SDK version (from VERSION.md).</summary>
    public static string Version => RepoLayout.Version;

    /// <summary>Packs every SDK package to the temporary feed before any test runs.</summary>
    [AssemblyInitialize]
    public static void Pack(TestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string feed = Path.Combine(Path.GetTempPath(), "ktsu-sdk-examples-feed-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(feed);
        feedDir = feed;

        // Substitute the {version} placeholder in the Sdk.targets files just for the duration of
        // packing, then restore the working tree so the test run leaves no diff behind.
        Dictionary<string, string> originals = SubstituteVersionTokens(RepoLayout.Version);
        try
        {
            foreach (string project in RepoLayout.SdkProjects)
            {
                string csproj = Path.Combine(RepoLayout.Root, project, project + ".csproj");

                List<string> args =
                [
                    "pack", csproj, "-c", "Release", "-o", feed, "--nologo", "-v", "quiet",
                    "-p:EnablePackageValidation=false",
                ];

                // An MSBuild SDK package's payload is its Sdk.props/Sdk.targets content, which is
                // target-framework agnostic; consumers never reference the compiled lib. Packing a
                // single TFM produces a valid SDK package far faster than building all eight target
                // frameworks. Sdk.Analyzers is excluded: it is a netstandard2.0 Roslyn component, so
                // forcing net10.0 would mismatch its restore.
                if (!string.Equals(project, "Sdk.Analyzers", StringComparison.Ordinal))
                {
                    args.Add("-p:TargetFrameworks=net10.0");
                }

                CliResult result = Cli.Dotnet(RepoLayout.Root, [.. args]);

                Assert.IsTrue(
                    result.Succeeded,
                    $"Failed to pack {project}:{Environment.NewLine}{result.Output}");
            }
        }
        finally
        {
            RestoreFiles(originals);
        }

        string[] packages = Directory.GetFiles(feed, "*.nupkg");
        Assert.IsTrue(
            packages.Length >= RepoLayout.SdkProjects.Count,
            $"Expected at least {RepoLayout.SdkProjects.Count} packages in the feed but found {packages.Length}.");

        context.WriteLine($"Packed ktsu.Sdk {RepoLayout.Version} ({packages.Length} packages) to {feed}");
    }

    /// <summary>Removes the temporary feed when the run is finished.</summary>
    [AssemblyCleanup]
    public static void Cleanup()
    {
        if (feedDir is not null && Directory.Exists(feedDir))
        {
            try { Directory.Delete(feedDir, recursive: true); } catch { /* best effort */ }
        }
    }

    private static Dictionary<string, string> SubstituteVersionTokens(string version)
    {
        Dictionary<string, string> originals = [];
        foreach (string targets in Directory.GetFiles(RepoLayout.Root, "Sdk.targets", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(targets);
            if (content.Contains("{version}", StringComparison.Ordinal))
            {
                originals[targets] = content;
                File.WriteAllText(targets, content.Replace("{version}", version, StringComparison.Ordinal));
            }
        }

        return originals;
    }

    private static void RestoreFiles(Dictionary<string, string> originals)
    {
        foreach ((string path, string content) in originals)
        {
            File.WriteAllText(path, content);
        }
    }
}
