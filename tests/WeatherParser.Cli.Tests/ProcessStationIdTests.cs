using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Cli.Tests;

[TestClass]
public class ProcessStationIdTests
{
    private string _workingDirectory = null!;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);
    private static readonly IReadOnlyDictionary<string, string?> NoEnvironmentConfig = new Dictionary<string, string?> { ["WEATHERPARSER_CONFIG"] = null };

    [TestInitialize]
    public void Initialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-cli-station-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workingDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, true);
        }
    }

    [TestMethod]
    [TestCategory("T-P3-03")]
    [TestCategory("T-P3-06")]
    public async Task Process_ParseDwd_EmptyStationId_WithConfiguredReference_ExitTwo_ZeroRequests()
    {
        await using var server = new LoopbackHttpServer();
        var config = ProcessFixtures.DwdConfiguration(server.BaseUri).Replace("\"Dwd\": {", "\"Dwd\": { \"ReferenceStationId\": \"00044\",");
        config.Should().Contain("\"ReferenceStationId\": \"00044\"");
        var configPath = Path.Combine(_workingDirectory, "config.json");
        await File.WriteAllTextAsync(configPath, config);

        var result = await CliProcess.RunCliAsync(new[] { "--ParseDWD", "--StationID", "", "--Output", "json", "--Config", configPath }, _workingDirectory, _timeout, NoEnvironmentConfig);

        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(2);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR STATION-ID-INVALID command=ParseDWD ");
        // The library's own grammar check rejected the empty ID (DwdStationResolver, contract OC-01).
        result.StandardError.Should().Contain("The station ID must be exactly five ASCII digits");
        server.RequestCount.Should().Be(0);
    }
}
