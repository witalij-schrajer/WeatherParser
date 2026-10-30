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
        return await RunCliAsync(arguments, workingDirectory, timeout, null);
    }

    /// <summary>
    /// Runs the CLI with the specified arguments.
    /// </summary>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="timeout">The timeout.</param>
    /// <param name="environment">The environment variables.</param>
    /// <returns>The result of the process run.</returns>
    public static async Task<CliProcessResult> RunCliAsync(IReadOnlyList<string> arguments, string workingDirectory, TimeSpan timeout, IReadOnlyDictionary<string, string?>? environment)
    {
        var cliArguments = new List<string> { Path.Combine(AppContext.BaseDirectory, "WeatherParser.dll") };
        cliArguments.AddRange(arguments);
        return await RunAsync("dotnet", cliArguments, workingDirectory, timeout, environment);
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
        return await RunAsync(fileName, arguments, workingDirectory, timeout, null);
    }

    /// <summary>
    /// Runs the specified executable with the specified arguments.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <param name="timeout">The timeout.</param>
    /// <param name="environment">The environment variables.</param>
    /// <returns>The result of the process run.</returns>
    public static async Task<CliProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, string workingDirectory, TimeSpan timeout, IReadOnlyDictionary<string, string?>? environment)
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

        if (environment != null)
        {
            foreach (var entry in environment)
            {
                if (entry.Value == null)
                {
                    startInfo.Environment.Remove(entry.Key);
                }
                else
                {
                    startInfo.Environment[entry.Key] = entry.Value;
                }
            }
        }

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
            await Task.WhenAll(stdoutTask, stderrTask).WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            return new CliProcessResult(process.ExitCode, string.Empty, string.Empty, TimedOut: true);
        }

        return new CliProcessResult(process.ExitCode, await stdoutTask, await stderrTask, TimedOut: false);
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
