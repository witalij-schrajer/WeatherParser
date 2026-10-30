using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using System.Text.Json;

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
        result.ExitCode.Should().Be(2);
        result.StandardError.Should().Contain(" ERROR CLI-COMMAND-MISSING command=- ");
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
        // The killed process may hold its current directory for a moment after the kill; the shared temporary directory is never deleted, so cleanup cannot fail on Windows.
        var sw = Stopwatch.StartNew();
        var result = await CliProcess.RunAsync("pwsh", new[] { "-NoProfile", "-Command", "Start-Sleep -Seconds 30" }, Path.GetTempPath(), TimeSpan.FromSeconds(2));
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
        // The orphaned descendant keeps its working directory open after the test; the shared temporary directory is never deleted, so cleanup cannot fail on Windows.
        var sw = Stopwatch.StartNew();
        var result = await CliProcess.RunAsync(
            "pwsh",
            new[] { "-NoProfile", "-Command", "Start-Process -FilePath pwsh -ArgumentList '-NoProfile','-Command','Start-Sleep -Seconds 12' -NoNewWindow; exit 0" },
            Path.GetTempPath(),
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
        result.ExitCode.Should().Be(2);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain($" ERROR {errorId} command=");
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
        result.ExitCode.Should().Be(2);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR CLI-CONFIG-MISSING command=ParseDWD ");
        Directory.EnumerateFileSystemEntries(_workingDirectory).Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a valid configuration runs the command against the configured endpoints: an unavailable source gives an export document with one item failure and the complete-failure exit code (CL-09, CL-11, CL-20).
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-05")]
    [TestCategory("T-P3-11")]
    public async Task Process_ValidConfiguration_RunsCommand_SourceUnavailable_ItemFailure()
    {
        await using var server = new LoopbackHttpServer();
        var configPath = Path.Combine(_workingDirectory, "config.json");
        await File.WriteAllTextAsync(configPath, DwdConfiguration(server.BaseUri));
        var result = await CliProcess.RunCliAsync(new[] { "--ParseDWD", "--StationID", "00044", "--Output", "json", "--Config", "config.json" }, _workingDirectory, _timeout, new Dictionary<string, string?> { ["WEATHERPARSER_CONFIG"] = null });
        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(1);
        server.RequestCount.Should().BeGreaterThan(0);
        using var document = JsonDocument.Parse(result.StandardOutput);
        var errors = document.RootElement.GetProperty("errors");
        errors.GetArrayLength().Should().Be(1);
        errors[0].GetProperty("source").GetString().Should().Be("dwd:00044");
        document.RootElement.GetProperty("observations").GetArrayLength().Should().Be(0);
        result.StandardError.Should().Contain("source=dwd:00044");
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
        result.ExitCode.Should().Be(2);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR CLI-CONFIG-INVALID command=ParseDWD ");
    }

    /// <summary>
    /// Verifies that parsing DWD without a station ID exits before retrieval (CL-06).
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-06")]
    public async Task Process_ParseDwdWithoutStation_ExitsBeforeRetrieval()
    {
        await using var server = new LoopbackHttpServer();
        var configPath = Path.Combine(_workingDirectory, "config.json");
        var configJson = $$"""
            {
              "Dwd": {
                "Catalog": {
                  "AirTemperatureStationList": "{{server.BaseUri}}cdc/air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt",
                  "AirTemperatureNowDirectory": "{{server.BaseUri}}cdc/air_temperature/now/",
                  "WindStationList": "{{server.BaseUri}}cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt",
                  "WindNowDirectory": "{{server.BaseUri}}cdc/wind/now/",
                  "PrecipitationStationList": "{{server.BaseUri}}cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt",
                  "PrecipitationNowDirectory": "{{server.BaseUri}}cdc/precipitation/now/",
                  "SolarStationList": "{{server.BaseUri}}cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt",
                  "SolarNowDirectory": "{{server.BaseUri}}cdc/solar/now/"
                }
              }
            }
            """;
        await File.WriteAllTextAsync(configPath, configJson);

        var result = await CliProcess.RunCliAsync(new[] { "--ParseDWD", "--Output", "json", "--Config", "config.json" }, _workingDirectory, _timeout, new Dictionary<string, string?> { ["WEATHERPARSER_CONFIG"] = null });
        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(2);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR CLI-DWD-STATION-MISSING command=ParseDWD ");
        server.RequestCount.Should().Be(0);
        Directory.EnumerateFileSystemEntries(_workingDirectory).Should().ContainSingle().Which.Should().Be(configPath);
    }

    /// <summary>
    /// Verifies that credential values are never printed to stdout or stderr (CL-07).
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-07")]
    public async Task Process_CredentialValues_NeverPrinted()
    {
        var configPath = Path.Combine(_workingDirectory, "config.json");
        var mqttConfigJson = """
            {
              "Broker": {
                "Host": "broker.example.invalid"
              },
              "Topics": {
                "Observation": {
                  "TopicTemplate": "t/{source}"
                }
              }
            }
            """;
        var configJson = $$"""
            {
              "Dwd": {
                "ReferenceStationId": "00044",
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
              },
              "Mqtt": {{mqttConfigJson}}
            }
            """;
        await File.WriteAllTextAsync(configPath, configJson);

        var result = await CliProcess.RunCliAsync(new[] { "--ParseDWD", "--StationID", "00044", "--Config", "config.json" }, _workingDirectory, _timeout,
            new Dictionary<string, string?>
            {
                ["WEATHERPARSER_CONFIG"] = null,
                ["WEATHERPARSER_MQTT_USERNAME"] = null,
                ["WEATHERPARSER_MQTT_PASSWORD"] = "s3cr3t-Marker-7f2a"
            });
        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(2);
        result.StandardError.Should().Contain(" ERROR CLI-CREDENTIALS-INVALID command=ParseDWD ");
        result.StandardOutput.Should().NotContain("s3cr3t-Marker-7f2a");
        result.StandardError.Should().NotContain("s3cr3t-Marker-7f2a");
    }

    /// <summary>
    /// Verifies that every line on stderr of failing invocations is a log line with UTC time, severity, ID and command, and that stdout stays empty (CL-08, REQ-072, REQ-073).
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-08")]
    public async Task Process_UsageAndConfigurationFailures_EveryStderrLineIsALogLine_StdoutEmpty()
    {
        // Test 1: Invalid argument
        var result1 = await CliProcess.RunCliAsync(new[] { "--ParseDWD", "--Langitude", "1" }, _workingDirectory, _timeout);
        result1.ExitCode.Should().Be(2);
        result1.StandardOutput.Should().BeEmpty();
        result1.StandardError.Should().NotBeEmpty();
        foreach (var line in result1.StandardError.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            line.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2} UTC [A-Z]+ \S+ command=\S+ ");
        }

        // Test 2: Missing config
        var result2 = await CliProcess.RunCliAsync(new[] { "--ParseDWD", "--Output", "json" }, _workingDirectory, _timeout, new Dictionary<string, string?> { ["WEATHERPARSER_CONFIG"] = null });
        result2.ExitCode.Should().Be(2);
        result2.StandardOutput.Should().BeEmpty();
        result2.StandardError.Should().NotBeEmpty();
        foreach (var line in result2.StandardError.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            line.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2} UTC [A-Z]+ \S+ command=\S+ ");
        }

        // Test 3: No arguments
        var result3 = await CliProcess.RunCliAsync(Array.Empty<string>(), _workingDirectory, _timeout);
        result3.ExitCode.Should().Be(2);
        result3.StandardOutput.Should().BeEmpty();
        result3.StandardError.Should().NotBeEmpty();
        foreach (var line in result3.StandardError.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            line.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2} UTC [A-Z]+ \S+ command=\S+ ");
        }
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

    private static string DwdConfiguration(Uri baseUri) => $$"""
        {
          "Dwd": {
            "Catalog": {
              "AirTemperatureStationList": "{{baseUri}}cdc/air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt",
              "AirTemperatureNowDirectory": "{{baseUri}}cdc/air_temperature/now/",
              "WindStationList": "{{baseUri}}cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt",
              "WindNowDirectory": "{{baseUri}}cdc/wind/now/",
              "PrecipitationStationList": "{{baseUri}}cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt",
              "PrecipitationNowDirectory": "{{baseUri}}cdc/precipitation/now/",
              "SolarStationList": "{{baseUri}}cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt",
              "SolarNowDirectory": "{{baseUri}}cdc/solar/now/"
            }
          }
        }
        """;
}
