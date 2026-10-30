using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Cli.Tests;

[TestClass]
public class ProcessOutputTests
{
    private string _workingDirectory = null!;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);
    private static readonly IReadOnlyDictionary<string, string?> NoEnvironmentConfig = new Dictionary<string, string?> { ["WEATHERPARSER_CONFIG"] = null };

    [TestInitialize]
    public void Initialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-cli-output-" + Guid.NewGuid().ToString("N"));
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

    private async Task<string> WriteConfigAsync(string json)
    {
        var configPath = Path.Combine(_workingDirectory, "config.json");
        await File.WriteAllTextAsync(configPath, json);
        return configPath;
    }

    private Task<CliProcessResult> RunAsync(params string[] arguments) => CliProcess.RunCliAsync(arguments, _workingDirectory, _timeout, NoEnvironmentConfig);

    private static LoopbackHttpServer OsthessenServer() => new()
    {
        Responder = path => path == "/osthessen/" ? (200, ProcessFixtures.Utf8(ProcessFixtures.OsthessenPage())) : (404, Array.Empty<byte>())
    };

    private void AssertNoTemporaryFile() => Directory.EnumerateFiles(_workingDirectory, "*.tmp").Should().BeEmpty();

    [TestMethod]
    [TestCategory("T-P3-12")]
    [TestCategory("T-P3-08")]
    public async Task Process_PartialFailure_StdoutHoldsOnlyTheCompleteEnvelope_Utf8()
    {
        await using var server = OsthessenServer();
        var config = ProcessFixtures.OsthessenConfiguration(server.BaseUri, mqtt: false, publicationPermitted: false);
        var configPath = await WriteConfigAsync(config);
        var result = await RunAsync("--ParseOstHessen", "--StationNamePattern", "*", "--Output", "json", "--Config", configPath);

        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(3);

        result.StandardOutput.Should().NotStartWith("﻿");
        using var doc = JsonDocument.Parse(result.StandardOutput);
        var observations = doc.RootElement.GetProperty("observations");
        observations.GetArrayLength().Should().Be(1);
        var errors = doc.RootElement.GetProperty("errors");
        errors.GetArrayLength().Should().Be(1);
        errors[0].GetProperty("source").GetString().Should().Be("osthessenwetter:langenbieber");

        result.StandardOutput.Should().Contain("Müs-Hohhütte");

        result.StandardOutput.Should().NotContain(ProcessFixtures.Marker);
        result.StandardError.Should().NotContain(ProcessFixtures.Marker);
    }

    [TestMethod]
    [TestCategory("T-P3-12")]
    public async Task Process_OutputFile_Written_Utf8WithoutBom_NoTemporaryFile()
    {
        await using var server = OsthessenServer();
        var config = ProcessFixtures.OsthessenConfiguration(server.BaseUri, mqtt: false, publicationPermitted: false);
        var configPath = await WriteConfigAsync(config);
        var result = await RunAsync("--ParseOstHessen", "--StationNamePattern", "*", "--Output", "json", "--OutputFile", "out.json", "--Config", configPath);

        result.ExitCode.Should().Be(3);
        result.StandardOutput.Should().BeEmpty();

        var bytes = await File.ReadAllBytesAsync(Path.Combine(_workingDirectory, "out.json"));
        bytes[0].Should().Be((byte)'{');
        new UTF8Encoding(false, true).GetString(bytes).Should().Contain("Müs-Hohhütte");

        AssertNoTemporaryFile();
        Directory.EnumerateFiles(_workingDirectory).Should().BeEquivalentTo(new[] { configPath, Path.Combine(_workingDirectory, "out.json") });
    }

    [TestMethod]
    [TestCategory("T-P3-12")]
    public async Task Process_OutputFile_ExistingDestination_RejectedBeforeRetrieval()
    {
        await using var server = OsthessenServer();
        var config = ProcessFixtures.OsthessenConfiguration(server.BaseUri, mqtt: false, publicationPermitted: false);
        var configPath = await WriteConfigAsync(config);
        var outputPath = Path.Combine(_workingDirectory, "out.json");
        await File.WriteAllTextAsync(outputPath, "old");

        var result = await RunAsync("--ParseOstHessen", "--StationNamePattern", "*", "--Output", "json", "--OutputFile", "out.json", "--Config", configPath);

        result.ExitCode.Should().Be(2);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR CLI-OUTPUT-EXISTS command=ParseOstHessen ");

        server.RequestCount.Should().Be(0);
        (await File.ReadAllTextAsync(outputPath)).Should().Be("old");
    }

    [TestMethod]
    [TestCategory("T-P3-12")]
    public async Task Process_OutputFile_MissingDirectory_RejectedBeforeRetrieval()
    {
        await using var server = OsthessenServer();
        var config = ProcessFixtures.OsthessenConfiguration(server.BaseUri, mqtt: false, publicationPermitted: false);
        var configPath = await WriteConfigAsync(config);

        var result = await RunAsync("--ParseOstHessen", "--StationNamePattern", "*", "--Output", "json", "--OutputFile", "missing/out.json", "--Config", configPath);

        result.ExitCode.Should().Be(2);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR CLI-OUTPUT-FAILED ");

        server.RequestCount.Should().Be(0);
        Directory.Exists(Path.Combine(_workingDirectory, "missing")).Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P3-11")]
    [TestCategory("T-P3-12")]
    public async Task Process_CommandLevelFailure_StdoutEmpty_NoFile()
    {
        await using var server = new LoopbackHttpServer(); // Default responder returns 404
        var config = ProcessFixtures.OsthessenConfiguration(server.BaseUri, mqtt: false, publicationPermitted: false);
        var configPath = await WriteConfigAsync(config);

        var result = await RunAsync("--ParseOstHessen", "--StationNamePattern", "*", "--Output", "json", "--OutputFile", "out.json", "--Config", configPath);

        result.ExitCode.Should().Be(1);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR OSTHESSEN-PAGE-UNAVAILABLE command=ParseOstHessen ");

        File.Exists(Path.Combine(_workingDirectory, "out.json")).Should().BeFalse();
        AssertNoTemporaryFile();
    }

    [TestMethod]
    [TestCategory("T-P3-11")]
    [TestCategory("CL-21")]
    public async Task Process_Redirect_IsRetrievalFailure_NotFollowed()
    {
        var server = new LoopbackHttpServer
        {
            Redirect = path => path == "/osthessen/" ? "/elsewhere/" : null
        };
        var config = ProcessFixtures.OsthessenConfiguration(server.BaseUri, mqtt: false, publicationPermitted: false);
        var configPath = await WriteConfigAsync(config);

        var result = await RunAsync("--ParseOstHessen", "--StationNamePattern", "*", "--Output", "json", "--Config", configPath);

        result.ExitCode.Should().Be(1);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR OSTHESSEN-PAGE-UNAVAILABLE ");

        server.RequestedPaths.Should().Equal("/osthessen/");
        AssertNoTemporaryFile();
    }

    [TestMethod]
    [TestCategory("T-P3-03")]
    public async Task Process_ParseDwd_InvalidStationId_ExitTwo_ZeroRequests()
    {
        await using var server = new LoopbackHttpServer();
        var config = ProcessFixtures.DwdConfiguration(server.BaseUri);
        var configPath = await WriteConfigAsync(config);

        var result = await RunAsync("--ParseDWD", "--StationID", "ABC", "--Output", "json", "--Config", configPath);

        result.ExitCode.Should().Be(2);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR STATION-ID-INVALID command=ParseDWD ");

        server.RequestCount.Should().Be(0);
    }

    [TestMethod]
    [TestCategory("T-P3-03")]
    public async Task Process_ParseMosmix_InvalidMosmixId_ExitTwo_ZeroRequests()
    {
        await using var server = new LoopbackHttpServer();
        var config = ProcessFixtures.MosmixConfiguration(server.BaseUri);
        var configPath = await WriteConfigAsync(config);

        var result = await RunAsync("--ParseMOSMIX", "--Product", "MOSMIX_L", "--MOSMIXID", "!!", "--Output", "json", "--Config", configPath);

        result.ExitCode.Should().Be(2);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR MOSMIX-ID-INVALID command=ParseMOSMIX ");

        server.RequestCount.Should().Be(0);
    }

    [TestMethod]
    [TestCategory("T-P3-03")]
    public async Task Process_ParseOstHessen_InvalidPattern_ExitTwo_ZeroRequests()
    {
        await using var server = OsthessenServer();
        var config = ProcessFixtures.OsthessenConfiguration(server.BaseUri, mqtt: false, publicationPermitted: false);
        var configPath = await WriteConfigAsync(config);

        var result = await RunAsync("--ParseOstHessen", "--StationNamePattern", "   ", "--Output", "json", "--Config", configPath);

        result.ExitCode.Should().Be(2);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR OSTHESSEN-PATTERN-INVALID command=ParseOstHessen ");

        server.RequestCount.Should().Be(0);
    }

    [TestMethod]
    [TestCategory("T-P3-16")]
    [TestCategory("OW-14")]
    [TestCategory("CL-16")]
    public async Task Process_ParseOstHessen_Mqtt_WithoutPermission_FailsBeforeRetrieval()
    {
        await using var server = OsthessenServer();
        var config = ProcessFixtures.OsthessenConfiguration(server.BaseUri, mqtt: true, publicationPermitted: false);
        var configPath = await WriteConfigAsync(config);

        var result = await RunAsync("--ParseOstHessen", "--StationNamePattern", "*", "--Config", configPath);

        result.ExitCode.Should().Be(2);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR CLI-OSTHESSEN-PUBLICATION-NOT-PERMITTED command=ParseOstHessen ");

        server.RequestCount.Should().Be(0);
    }

    [TestMethod]
    [TestCategory("T-P3-16")]
    public async Task Process_ParseOstHessen_Mqtt_WithPermission_PassesTheRestriction()
    {
        await using var server = OsthessenServer();
        var config = ProcessFixtures.OsthessenConfiguration(server.BaseUri, mqtt: true, publicationPermitted: true);
        var configPath = await WriteConfigAsync(config);

        var result = await RunAsync("--ParseOstHessen", "--StationNamePattern", "*", "--Config", configPath);

        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().NotContain("CLI-OSTHESSEN-PUBLICATION-NOT-PERMITTED ");

        server.RequestCount.Should().BeGreaterThan(0);
    }
}
