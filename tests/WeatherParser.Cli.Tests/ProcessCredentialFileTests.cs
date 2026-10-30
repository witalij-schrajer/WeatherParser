using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Cli.Tests;

[TestClass]
public class ProcessCredentialFileTests
{
    private string _workingDirectory = null!;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);
    private static readonly IReadOnlyDictionary<string, string?> CleanEnvironment = new Dictionary<string, string?>
    {
        ["WEATHERPARSER_CONFIG"] = null,
        ["WEATHERPARSER_MQTT_USERNAME"] = null,
        ["WEATHERPARSER_MQTT_PASSWORD"] = null,
    };

    private const string Marker = "s3cr3t-Marker-7f2a";

    [DllImport("libc")]
    private static extern uint geteuid();

    [DllImport("libc", SetLastError = true)]
    private static extern int chown(string path, uint owner, uint group);

    [TestInitialize]
    public void Initialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-cli-credentials-" + Guid.NewGuid().ToString("N"));
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
    [TestCategory("T-P3-07")]
    public async Task Process_CredentialFileReadableByGroup_ExitTwo_ZeroRequests_NoBrokerConnection()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("Windows has no Unix owner or permission bits; the credential file is not checked there (CL-06).");
            return;
        }

        var credentialsPath = await WriteCredentialsAsync();
        File.SetUnixFileMode(credentialsPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
        await AssertRejectedAsync(credentialsPath);
    }

    [TestMethod]
    [TestCategory("T-P3-07")]
    public async Task Process_CredentialFileOwnedByAnotherUser_ExitTwo_ZeroRequests_NoBrokerConnection()
    {
        if (OperatingSystem.IsWindows() || geteuid() != 0)
        {
            Assert.Inconclusive("Giving a file to another owner needs root on Linux; the owner comparison is covered with a test double by CredentialProviderTests.");
            return;
        }

        var credentialsPath = await WriteCredentialsAsync();
        File.SetUnixFileMode(credentialsPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        chown(credentialsPath, 65534, 65534).Should().Be(0);
        await AssertRejectedAsync(credentialsPath);
    }

    private async Task<string> WriteCredentialsAsync()
    {
        var path = Path.Combine(_workingDirectory, "broker.credentials.json");
        await File.WriteAllTextAsync(path, "{\"Username\":\"mqtt-user\",\"Password\":\"" + Marker + "\"}");
        return path;
    }

    private async Task AssertRejectedAsync(string credentialsPath)
    {
        await using var server = new LoopbackHttpServer
        {
            Responder = path => path == "/osthessen/" ? (200, ProcessFixtures.Utf8(ProcessFixtures.OsthessenPage())) : (404, Array.Empty<byte>())
        };
        await using var broker = new ClosingTcpListener();
        var config = ProcessFixtures.OsthessenConfiguration(server.BaseUri, mqtt: true, publicationPermitted: true, brokerPort: broker.Port)
            .Replace("\"AllowUnencrypted\": true }", "\"AllowUnencrypted\": true, \"CredentialsFile\": \"" + credentialsPath + "\" }");
        config.Should().Contain("\"CredentialsFile\"");
        var configPath = Path.Combine(_workingDirectory, "config.json");
        await File.WriteAllTextAsync(configPath, config);

        var result = await CliProcess.RunCliAsync(new[] { "--ParseOstHessen", "--StationNamePattern", "*", "--Config", configPath }, _workingDirectory, _timeout, CleanEnvironment);

        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(2);
        result.StandardOutput.Should().BeEmpty();
        result.StandardError.Should().Contain(" ERROR CLI-CREDENTIALS-INVALID command=ParseOstHessen ");
        result.StandardError.Should().Contain("must be owned by the current user and must not be accessible by group or others");
        result.StandardError.Should().NotContain(Marker);
        server.RequestCount.Should().Be(0);
        broker.AcceptedCount.Should().Be(0);
    }
}
