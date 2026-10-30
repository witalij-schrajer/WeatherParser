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

    /// <summary>
    /// Verifies that invalid arguments fail with a usage error before any configuration is read and without creating a file (REQ-010, CL-03).
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    [DataRow("--ParseDWD --Langitude 1", "CLI-ARGUMENT-INVALID")]
    [DataRow("--SelectDWD --Latitude 91 --Longitude 9 --Altitude 1 --MaxDistance 1 --MaxAltDifference 1", "COORDINATE-INVALID")]
    [DataRow("--ParseDWD --Output json --OutputFile out.json --Config missing-config.json --Bogus x", "CLI-ARGUMENT-INVALID")]
    [DataRow("--ParseDWD --ParseOstHessen", "CLI-COMMAND-CONFLICT")]
    public async Task Process_InvalidArguments_NoConfigurationRead_NoFile(string arguments, string errorId)
    {
        var result = await CliProcess.RunCliAsync(arguments.Split(' '), _workingDirectory, _timeout);
        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().NotBe(0);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().StartWith(errorId + ":");
        Directory.EnumerateFileSystemEntries(_workingDirectory).Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that no configuration fails with a missing config error (CL-04).
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-05")]
    public async Task Process_NoConfiguration_ConfigMissing_NoFile()
    {
        var result = await CliProcess.RunCliAsync(new[] { "--ParseDWD", "--Output", "json" }, _workingDirectory, _timeout, new Dictionary<string, string?> { ["WEATHERPARSER_CONFIG"] = null });
        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().NotBe(0);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().StartWith("CLI-CONFIG-MISSING:");
        Directory.EnumerateFileSystemEntries(_workingDirectory).Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a valid configuration is loaded and validated before the command is executed (CL-04, CL-05).
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-05")]
    public async Task Process_ValidConfiguration_ValidatedBeforeCommand()
    {
        var configPath = Path.Combine(_workingDirectory, "config.json");
        await File.WriteAllTextAsync(configPath, MinimalValidConfiguration);
        var result = await CliProcess.RunCliAsync(new[] { "--ParseDWD", "--StationID", "00044", "--Output", "json", "--Config", "config.json" }, _workingDirectory, _timeout, new Dictionary<string, string?> { ["WEATHERPARSER_CONFIG"] = null });
        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(1);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Be("The command is not implemented yet." + Environment.NewLine);
        Directory.EnumerateFileSystemEntries(_workingDirectory).Should().ContainSingle().Which.Should().Be(configPath);
    }

    /// <summary>
    /// Verifies that an invalid configuration fails with a validation error (CL-05).
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-05")]
    public async Task Process_InvalidConfiguration_ConfigInvalid()
    {
        var configPath = Path.Combine(_workingDirectory, "config.json");
        await File.WriteAllTextAsync(configPath, "{\"Dwdd\":{}}");
        var result = await CliProcess.RunCliAsync(new[] { "--ParseDWD", "--Output", "json", "--Config", "config.json" }, _workingDirectory, _timeout, new Dictionary<string, string?> { ["WEATHERPARSER_CONFIG"] = null });
        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().NotBe(0);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().StartWith("CLI-CONFIG-INVALID:");
    }

    private const string MinimalValidConfiguration = """
        {
          "Dwd": {
            "Catalog": {
              "AirTemperatureStationList": "https://example.invalid/cdc/air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt",
              "AirTemperatureNowDirectory": "https://example.invalid/cdc/air_temperature/now/",
              "WindStationList": "https://example.invalid/cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt",
              "WindNowDirectory": "https://example.invalid/cdc/wind/now/",
              "PrecipitationStationList": "https://example.invalid/cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt",
              "PrecipitationNowDirectory": "https://example.invalid/cdc/precipitation/now/",
              "SolarStationList": "https://example.invalid/cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt",
              "SolarNowDirectory": "https://example.invalid/cdc/solar/now/"
            }
          }
        }
        """;
}
