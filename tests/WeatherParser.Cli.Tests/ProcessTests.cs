using System.Diagnostics;
using FluentAssertions;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Tests the CLI process harness.
/// </summary>
[TestClass]
public sealed class ProcessTests
{
    private string _workingDirectory = null!;
    private TimeSpan _timeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Initializes the test fixtures.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-cli-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workingDirectory);
    }

    /// <summary>
    /// Cleans up the test fixtures.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that help prints usage to stdout and exit code zero.
    /// </summary>
    [TestMethod]
    public async Task Process_Help_PrintsUsageToStdout_ExitCodeZero()
    {
        var result = await CliProcess.RunCliAsync(new[] { "--Help" }, _workingDirectory, _timeout);
        result.ExitCode.Should().Be(0);
        result.TimedOut.Should().BeFalse();
        result.StandardOutput.Should().Contain("Usage: WeatherParser --<command> [options]");
    }

    /// <summary>
    /// Verifies that help writes nothing to stderr.
    /// </summary>
    [TestMethod]
    public async Task Process_Help_WritesNothingToStderr()
    {
        var result = await CliProcess.RunCliAsync(new[] { "--Help" }, _workingDirectory, _timeout);
        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(0);
        result.StandardError.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that no arguments prints usage to stderr and non-zero exit code.
    /// </summary>
    [TestMethod]
    public async Task Process_NoArguments_PrintsUsageToStderr_NonZero()
    {
        var result = await CliProcess.RunCliAsync(Array.Empty<string>(), _workingDirectory, _timeout);
        result.ExitCode.Should().NotBe(0);
        result.StandardError.Should().Contain("Usage: WeatherParser --<command> [options]");
        result.StandardOutput.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that the process exits without waiting.
    /// </summary>
    [TestMethod]
    public async Task Process_ExitsWithoutWaiting()
    {
        var result = await CliProcess.RunCliAsync(new[] { "--ParseDWD" }, _workingDirectory, _timeout);
        result.TimedOut.Should().BeFalse();
        Directory.EnumerateFileSystemEntries(_workingDirectory).Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that the harness kills process on timeout.
    /// </summary>
    [TestMethod]
    public async Task Harness_KillsProcessOnTimeout()
    {
        var sw = Stopwatch.StartNew();
        var result = await CliProcess.RunAsync("pwsh", new[] { "-NoProfile", "-Command", "Start-Sleep -Seconds 30" }, _workingDirectory, TimeSpan.FromSeconds(2));
        sw.Stop();
        result.TimedOut.Should().BeTrue();
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(20));
    }

    /// <summary>
    /// Verifies that the harness timeout also bounds reading the output when a descendant process keeps the output pipe open after the started process has exited.
    /// </summary>
    [TestMethod]
    public async Task Harness_TimeoutBoundsOutputReading_WhenDescendantKeepsPipeOpen()
    {
        var sw = Stopwatch.StartNew();
        var result = await CliProcess.RunAsync(
            "pwsh",
            new[] { "-NoProfile", "-Command", "Start-Process -FilePath pwsh -ArgumentList '-NoProfile','-Command','Start-Sleep -Seconds 12' -NoNewWindow; exit 0" },
            _workingDirectory,
            TimeSpan.FromSeconds(5));
        sw.Stop();

        result.TimedOut.Should().BeTrue();
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(25));
    }
}
