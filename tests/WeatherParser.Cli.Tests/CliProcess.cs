using System.Diagnostics;
using System.Text;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// A process-test harness.
/// </summary>
internal sealed class CliProcess
{
    /// <summary>
    /// Runs the CLI with the specified arguments.
    /// </summary>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="timeout">The timeout.</param>
    /// <returns>The result of the process run.</returns>
    public static async Task<CliProcessResult> RunCliAsync(IReadOnlyList<string> arguments, string workingDirectory, TimeSpan timeout)
    {
        var cliArguments = new List<string> { Path.Combine(AppContext.BaseDirectory, "WeatherParser.dll") };
        cliArguments.AddRange(arguments);
        return await RunAsync("dotnet", cliArguments, workingDirectory, timeout);
    }

    /// <summary>
    /// Runs the specified executable with the specified arguments.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="timeout">The timeout.</param>
    /// <returns>The result of the process run.</returns>
    public static async Task<CliProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, string workingDirectory, TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory,
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false)
        };

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)!;
        process.StandardInput.Close();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            return new CliProcessResult(process.ExitCode, string.Empty, string.Empty, TimedOut: true);
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new CliProcessResult(process.ExitCode, stdout, stderr, TimedOut: false);
    }
}

/// <summary>
/// The result of a CLI process run.
/// </summary>
/// <param name="ExitCode">The exit code.</param>
/// <param name="StandardOutput">The standard output.</param>
/// <param name="StandardError">The standard error.</param>
/// <param name="TimedOut">Whether the process timed out.</param>
internal sealed record CliProcessResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut);
