namespace Sdk.Examples.Tests.Infrastructure;

using System.Diagnostics;
using System.Text;

/// <summary>The result of running a command-line process.</summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Output">Combined standard output and standard error.</param>
internal readonly record struct CliResult(int ExitCode, string Output)
{
    /// <summary>Whether the process exited successfully.</summary>
    public bool Succeeded => ExitCode == 0;

    /// <summary>Returns the distinct KTSU diagnostic IDs (e.g. KTSU0001) present in the output.</summary>
    public IReadOnlySet<string> KtsuDiagnostics()
    {
        HashSet<string> ids = [];
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(Output, "KTSU[0-9]{4}"))
        {
            ids.Add(m.Value);
        }

        return ids;
    }
}

/// <summary>Runs <c>dotnet</c> commands and captures their output.</summary>
internal static class Cli
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

    /// <summary>Runs <c>dotnet</c> with the given arguments in <paramref name="workingDirectory"/>.</summary>
    public static CliResult Dotnet(string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo psi = new()
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (string arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        // Keep builds deterministic and quiet, and avoid telemetry/first-run noise in output.
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        psi.Environment["DOTNET_NOLOGO"] = "1";
        psi.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

        StringBuilder output = new();
        using Process process = new() { StartInfo = psi };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) { lock (output) { output.AppendLine(e.Data); } } };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) { lock (output) { output.AppendLine(e.Data); } } };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit((int)Timeout.TotalMilliseconds))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            throw new TimeoutException($"dotnet {string.Join(' ', arguments)} timed out after {Timeout}.");
        }

        // Ensure async output handlers have flushed.
        process.WaitForExit();

        return new CliResult(process.ExitCode, output.ToString());
    }
}
