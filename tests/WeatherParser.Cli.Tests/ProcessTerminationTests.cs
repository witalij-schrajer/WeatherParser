using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Cli.Tests;

[TestClass]
[TestCategory("T-P3-18")]
public sealed class ProcessTerminationTests
{
    /// <summary>
    /// Verifies the real forced termination of CL-18 with the signal test host, whose work ignores cancellation (Linux, where signals can be sent to a child process).
    /// </summary>
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(60);
    private string _workingDirectory = null!;
    private string _readyFile = null!;

    [TestInitialize]
    public void Initialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-cli-termination-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workingDirectory);
        _readyFile = Path.Combine(_workingDirectory, "ready");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, recursive: true);
        }
    }

    private static void RequireLinux()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Inconclusive("POSIX signals are sent to a child process on Linux only; the forced termination is covered by unit tests over the terminator seam elsewhere.");
        }
    }

    // Waits until the host has registered its signals and written the ready file.
    private async Task WaitUntilReadyAsync()
    {
        var watch = Stopwatch.StartNew();
        while (!File.Exists(_readyFile))
        {
            if (watch.Elapsed > ProcessTimeout)
            {
                throw new TimeoutException("The signal test host did not become ready.");
            }

            await Task.Delay(50);
        }
    }

    private Task<CliProcessResult> RunHostAsync(int gracePeriodSeconds, Func<int, Task> whileRunning) =>
        CliProcess.RunAsync(
            "dotnet",
            new[] { Path.Combine(AppContext.BaseDirectory, "WeatherParser.Cli.SignalHost.dll"), _readyFile, gracePeriodSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            _workingDirectory,
            ProcessTimeout,
            null,
            whileRunning);

    [TestMethod]
    [Timeout(90000)]
    public async Task Process_SecondSignal_TerminatesImmediately()
    {
        RequireLinux();
        var sinceSecondSignal = new Stopwatch();
        var result = await RunHostAsync(30, async processId =>
        {
            await WaitUntilReadyAsync();
            PosixSignalSender.Send(processId, PosixSignalSender.Sigint);
            // A different second signal, because two pending signals of the same kind can merge into one.
            await Task.Delay(500);
            sinceSecondSignal.Start();
            PosixSignalSender.Send(processId, PosixSignalSender.Sigterm);
        });
        sinceSecondSignal.Stop();

        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(4);
        sinceSecondSignal.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10), "the grace period of 30 s must not be awaited after a second signal");
    }

    [TestMethod]
    [Timeout(90000)]
    public async Task Process_GracePeriodExpired_Terminates()
    {
        RequireLinux();
        var sinceSignal = new Stopwatch();
        var result = await RunHostAsync(2, async processId =>
        {
            await WaitUntilReadyAsync();
            sinceSignal.Start();
            PosixSignalSender.Send(processId, PosixSignalSender.Sigint);
        });
        sinceSignal.Stop();

        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(4);
        sinceSignal.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(1.5), "the work ignores the token, so only the grace period ends the process");
        sinceSignal.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(20));
    }
}
