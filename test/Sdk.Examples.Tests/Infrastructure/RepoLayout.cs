namespace Sdk.Examples.Tests.Infrastructure;

/// <summary>
/// Locates the repository root and the well-known paths inside it, independent of where
/// the test assembly happens to run from.
/// </summary>
internal static class RepoLayout
{
    private static readonly Lazy<string> RootLazy = new(FindRoot);

    /// <summary>The absolute path to the repository root.</summary>
    public static string Root => RootLazy.Value;

    /// <summary>The SDK version declared in VERSION.md.</summary>
    public static string Version { get; } = File.ReadAllText(Path.Combine(RootLazy.Value, "VERSION.md")).Trim();

    /// <summary>Absolute path to the <c>examples</c> directory.</summary>
    public static string ExamplesDir => Path.Combine(Root, "examples");

    /// <summary>Absolute path to a demo example directory.</summary>
    public static string Demo(string name) => Path.Combine(ExamplesDir, "demos", name);

    /// <summary>Absolute path to an analyzer example directory.</summary>
    public static string Analyzer(string name) => Path.Combine(ExamplesDir, "analyzers", name);

    /// <summary>The MSBuild SDK projects that must be packed for the examples to consume.</summary>
    public static IReadOnlyList<string> SdkProjects { get; } =
    [
        "Sdk",
        "Sdk.Analyzers",
        "Sdk.ConsoleApp",
        "Sdk.App",
        "Sdk.Windows",
        "Sdk.Linux",
        "Sdk.macOS",
        "Sdk.iOS",
        "Sdk.Android",
    ];

    private static string FindRoot()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "VERSION.md")) && File.Exists(Path.Combine(dir, "Sdk.sln")))
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir);
        }

        throw new InvalidOperationException(
            "Could not locate the repository root (a directory containing VERSION.md and Sdk.sln) " +
            $"starting from '{AppContext.BaseDirectory}'.");
    }
}
