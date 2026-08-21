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
    public static CliResult Dotnet(string workingDirectory, params string[] arguments) =>
        Run("dotnet", workingDirectory, arguments);

    /// <summary>Runs an arbitrary executable in <paramref name="workingDirectory"/>.</summary>
    public static CliResult Run(string fileName, string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo psi = new()
        {
            FileName = fileName,
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

        // MSBuild worker nodes inherit the redirected handles set up below, and with node reuse left
        // on they outlive the build by the reuse idle timeout, keeping those pipes open. The
        // WaitForExit() at the end of this method waits for the streams to reach EOF rather than for
        // the process to exit, so any build that had to spawn fresh nodes stalled here for the full
        // fifteen minutes with nothing running. Node reuse has nothing to offer a harness that
        // builds each project once in a throwaway workspace.
        psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";

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
            throw new TimeoutException($"{fileName} {string.Join(' ', arguments)} timed out after {Timeout}.");
        }

        // Ensure async output handlers have flushed.
        process.WaitForExit();

        return new CliResult(process.ExitCode, output.ToString());
    }
}
