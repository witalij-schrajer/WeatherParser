using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Tests.Configuration;

namespace WeatherParser.Cli.Tests;

[TestClass]
[TestCategory("T-P3-07")]
public class HttpUserAgentTests
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(45);
    private static readonly IReadOnlyDictionary<string, string?> NoEnvironmentConfig = new Dictionary<string, string?> { ["WEATHERPARSER_CONFIG"] = null };
    private string _workingDirectory = null!;
    private LoopbackHttpServer _server = null!;

    [TestInitialize]
    public void Initialize()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-cli-useragent-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workingDirectory);
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

    // Runs --ParseOstHessen in JSON mode against the loopback page, optionally with Http.UserAgent in the configuration.
    private async Task<CliProcessResult> RunAsync(string? userAgent)
    {
        var configuration = JsonNode.Parse(ProcessFixtures.OsthessenConfiguration(_server.BaseUri, mqtt: false, publicationPermitted: false))!.AsObject();
        if (userAgent != null)
        {
            configuration["Http"] = new JsonObject { ["UserAgent"] = userAgent };
        }

        var configPath = Path.Combine(_workingDirectory, "config.json");
        await File.WriteAllTextAsync(configPath, configuration.ToJsonString());
        return await CliProcess.RunCliAsync(new[] { "--ParseOstHessen", "--StationNamePattern", "*", "--Output", "json", "--Config", configPath }, _workingDirectory, ProcessTimeout, NoEnvironmentConfig);
    }

    [TestMethod]
    [Timeout(60000)]
    public void CreateHttpClient_ConfiguredUserAgent_ReplacesTheDefault()
    {
        using var client = CompositionRoot.CreateHttpClient("ExampleAgent/1.0 (+https://example.invalid/contact)");
        client.DefaultRequestHeaders.NonValidated["User-Agent"].ToString().Should().Be("ExampleAgent/1.0 (+https://example.invalid/contact)");
    }

    [TestMethod]
    [Timeout(60000)]
    public void CreateHttpClient_NoUserAgent_KeepsTheDefault()
    {
        using var client = CompositionRoot.CreateHttpClient(null);
        client.DefaultRequestHeaders.UserAgent.ToString().Should().StartWith("WeatherParser/");
    }

    [TestMethod]
    [Timeout(60000)]
    public void Container_HttpSectionRegistered_ClientUsesItsUserAgent()
    {
        using var provider = CompositionRoot.Build(services =>
        {
            TestConfiguration.Register(services);
            services.AddSingleton(new HttpSection { UserAgent = "ExampleAgent/2.0" });
        });
        provider.GetRequiredService<HttpClient>().DefaultRequestHeaders.NonValidated["User-Agent"].ToString().Should().Be("ExampleAgent/2.0");
    }

    [TestMethod]
    [Timeout(60000)]
    public void ConfigurationRegistrar_RegistersTheHttpSection()
    {
        var configuration = TestConfigurations.Valid();
        configuration.Http = new HttpSection { UserAgent = "ExampleAgent/4.0" };
        var services = new ServiceCollection();
        new ConfigurationRegistrar().Register(services, configuration, TestConfigurations.Request(CliCommand.ParseDwd, OutputFormat.Json));
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<HttpSection>().UserAgent.Should().Be("ExampleAgent/4.0");
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task Process_ConfiguredUserAgent_SentToTheSource()
    {
        var result = await RunAsync("ExampleAgent/3.0 (+https://example.invalid/contact)");
        result.TimedOut.Should().BeFalse();
        result.ExitCode.Should().Be(3);
        _server.UserAgents.Should().Equal("ExampleAgent/3.0 (+https://example.invalid/contact)");
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task Process_NoUserAgent_DefaultSentToTheSource()
    {
        var result = await RunAsync(null);
        result.ExitCode.Should().Be(3);
        _server.UserAgents.Should().ContainSingle().Which.Should().StartWith("WeatherParser/");
    }
}
