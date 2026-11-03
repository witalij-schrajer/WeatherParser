using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Cli.Tests.Lifetime;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Verifies signal handling, single execution and the output channels of the CLI process (CL-12, CL-18, REQ-006, REQ-007).
/// </summary>
[TestClass]
[TestCategory("T-P3-18")]
public sealed class ProcessSignalTests
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(45);
    private static readonly IReadOnlyDictionary<string, string?> NoEnvironmentConfig = new Dictionary<string, string?> { ["WEATHERPARSER_CONFIG"] = null };
    private string _workingDirectory = null!;
    private LoopbackHttpServer _server = null!;
    private TaskCompletionSource _requestArrived = null!;

    [TestInitialize]
    public void Initialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-cli-signal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workingDirectory);
        _requestArrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _server = new LoopbackHttpServer
        {
            Responder = path => path == "/osthessen/" ? (200, ProcessFixtures.Utf8(ProcessFixtures.OsthessenPage())) : (404, Array.Empty<byte>()),
        };
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _server.DisposeAsync();
        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, recursive: true);
        }
    }

    // The fake server holds the page back until the server is disposed, so the command is still retrieving when the test signals.
    private void HoldResponses() => _server.BeforeResponse = async (_, stopping) =>
    {
        _requestArrived.TrySetResult();
        await Task.Delay(Timeout.Infinite, stopping);
    };

    private async Task<string> WriteConfigAsync()
    {
        var configPath = Path.Combine(_workingDirectory, "config.json");
        await File.WriteAllTextAsync(configPath, ProcessFixtures.OsthessenConfiguration(_server.BaseUri, mqtt: false, publicationPermitted: false));
        return configPath;
    }

    private static void AssertEveryLineIsALogLine(string standardError)
    {
        foreach (var line in standardError.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            line.TrimEnd('\r').Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2} UTC [A-Z]+ \S+ command=\S+ ");
        }
    }

    // A real signal to the child process during retrieval: exit 4, no document, no file (CL-12, CL-18).
    private async Task AssertSignalCancelsAsync(int signal)
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Inconclusive("POSIX signals are sent to a child process on Linux only; Windows is covered by InProcess_SignalDuringSlowRetrieval_Exit4_NoOutputFile.");
        }

        HoldResponses();
        var configPath = await WriteConfigAsync();
        var result = await CliProcess.RunCliAsync(
            new[] { "--ParseOstHessen", "--StationNamePattern", "*", "--Output", "json", "--OutputFile", "out.json", "--Config", configPath },
            _workingDirectory,
            ProcessTimeout,
            NoEnvironmentConfig,
            async processId =>
            {
                await _requestArrived.Task;
                PosixSignalSender.Send(processId, signal);
            });

        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(4);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR CLI-CANCELLED command=ParseOstHessen ");
        AssertEveryLineIsALogLine(result.StandardError);
        Directory.EnumerateFiles(_workingDirectory).Should().BeEquivalentTo(new[] { configPath });
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task Process_Sigint_DuringSlowRetrieval_Exit4_NoTemporaryFile()
    {
        await AssertSignalCancelsAsync(PosixSignalSender.Sigint);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task Process_Sigterm_DuringSlowRetrieval_Exit4()
    {
        await AssertSignalCancelsAsync(PosixSignalSender.Sigterm);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task Process_OneCommandThenExit_NoLingeringProcess()
    {
        var configPath = await WriteConfigAsync();
        var processId = 0;
        var result = await CliProcess.RunCliAsync(
            new[] { "--ParseOstHessen", "--StationNamePattern", "*", "--Output", "json", "--Config", configPath },
            _workingDirectory,
            ProcessTimeout,
            NoEnvironmentConfig,
            id =>
            {
                processId = id;
                return Task.CompletedTask;
            });

        // Both output pipes reached their end, so no process the CLI started still holds them (REQ-006).
        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(3);
        result.StandardOutput.Should().StartWith("{");
        AssertEveryLineIsALogLine(result.StandardError);
        _server.RequestCount.Should().Be(1);
        ((Action)(() => Process.GetProcessById(processId))).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task InProcess_SignalDuringSlowRetrieval_Exit4_NoOutputFile()
    {
        HoldResponses();
        var configPath = await WriteConfigAsync();
        var outputFile = Path.Combine(_workingDirectory, "out.json");
        using var signal = new TestShutdownSignal();
        using var standardOutput = new MemoryStream();
        using var standardError = new MemoryStream();
        var run = Task.Run(() => Program.Main(new[] { "--ParseOstHessen", "--StationNamePattern", "*", "--Output", "json", "--OutputFile", outputFile, "--Config", configPath }, standardOutput, standardError, signal));

        await _requestArrived.Task.WaitAsync(ProcessTimeout);
        signal.Raise();
        var exitCode = await run.WaitAsync(ProcessTimeout);

        exitCode.Should().Be(4);
        standardOutput.Length.Should().Be(0);
        var log = Encoding.UTF8.GetString(standardError.ToArray());
        log.Should().Contain(" ERROR CLI-CANCELLED command=ParseOstHessen ");
        AssertEveryLineIsALogLine(log);
        Directory.EnumerateFiles(_workingDirectory).Should().BeEquivalentTo(new[] { configPath });
        signal.GracePeriod.Should().Be(TimeSpan.FromSeconds(5));
    }
}
