using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Export;
using WeatherParser.Parsing.Tests.Selection;

namespace WeatherParser.Parsing.Tests.Live;

[TestClass]
public sealed class LiveCheckObservationRunnerTests
{
    private string _directory = null!;
    private StaticHttpHandler _handler = null!;
    private ManualTimeProvider _clock = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "wp-live-obs-" + Guid.NewGuid());
        Directory.CreateDirectory(_directory);
        _clock = new ManualTimeProvider(LiveCheckTestData.Now);
        _handler = new StaticHttpHandler();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private LiveCheckSettings Settings() => LiveCheckTestData.Settings(_directory) with { ObservationStationId = "00044" };

    [DataRow("1", null)]
    [DataRow("1", "")]
    [DataRow("1", "OBSERVATION")]
    [DataRow("1", "selection")]
    [DataRow("0", "observation")]
    [DataRow(null, "observation")]
    [TestMethod]
    public async Task RunObservationIfEnabledAsync_WithoutObservationMode_ReturnsNullBeforeAnyRequest(string? enable, string? mode)
    {
        var settings = Settings();
        LiveCheckTestData.Arrange(_handler, settings);

        var result = await LiveCheckRunner.RunObservationIfEnabledAsync(
            Env(enable, mode),
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        result.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
    }

    [DataRow("1", "observation", true)]
    [DataRow("1", "Observation", false)]
    [DataRow("1", null, false)]
    [DataRow("0", "observation", false)]
    [TestMethod]
    public void IsObservationMode_OnlyForEnabledAndExactMode(string? enable, string? mode, bool expected)
    {
        LiveCheckRunner.IsObservationMode(Env(enable, mode)).Should().Be(expected);
    }

    [TestMethod]
    public async Task RunObservationIfEnabledAsync_ObservationSequence_RequestsListsThenArchives()
    {
        var settings = Settings();
        LiveCheckTestData.Arrange(_handler, settings);

        var result = await LiveCheckRunner.RunObservationIfEnabledAsync(
            Env("1", "observation"),
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Requests.Should().HaveCount(8);

        var firstFour = result.Requests.Take(4).Select(r => r.Uri).ToList();
        firstFour.Should().BeEquivalentTo(
            settings.Cdc.AirTemperatureStationList!.AbsoluteUri,
            settings.Cdc.WindStationList!.AbsoluteUri,
            settings.Cdc.PrecipitationStationList!.AbsoluteUri,
            settings.Cdc.SolarStationList!.AbsoluteUri);

        var lastFour = result.Requests.Skip(4).Select(r => r.Uri).ToList();
        lastFour.Should().BeEquivalentTo(
            new Uri(settings.Cdc.AirTemperatureNowDirectory!, "10minutenwerte_TU_00044_now.zip").AbsoluteUri,
            new Uri(settings.Cdc.WindNowDirectory!, "10minutenwerte_wind_00044_now.zip").AbsoluteUri,
            new Uri(settings.Cdc.PrecipitationNowDirectory!, "10minutenwerte_nieder_00044_now.zip").AbsoluteUri,
            new Uri(settings.Cdc.SolarNowDirectory!, "10minutenwerte_SOLAR_00044_now.zip").AbsoluteUri);

        _handler.Requests.Should().HaveCount(8);
    }

    [TestMethod]
    public async Task RunObservationAsync_MakesNoMosmixOrSelectionRequest()
    {
        var settings = Settings();
        LiveCheckTestData.Arrange(_handler, settings);

        await LiveCheckRunner.RunObservationAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        _handler.Requests.Should().NotContain(settings.Mosmix.CatalogUri!.AbsoluteUri);
        _handler.Requests.Should().NotContain(settings.Mosmix.MosmixSAllStationsUri!.AbsoluteUri);
        _handler.Requests.Should().NotContain(r => r.StartsWith(settings.Mosmix.MosmixLSingleStationsDirectory!.AbsoluteUri));
        Directory.EnumerateFileSystemEntries(_directory).Should().BeEmpty();
    }

    [TestMethod]
    public async Task ObservationBudgetEight_RefusesNinthRequest()
    {
        var settings = Settings();
        LiveCheckTestData.Arrange(_handler, settings);

        using var client = new HttpClient(new RequestBudgetHandler(LiveCheckSettings.ObservationRequestBudget, _handler));
        for (int i = 0; i < LiveCheckSettings.ObservationRequestBudget; i++)
        {
            var response = await client.GetAsync(LiveCheckTestData.Base + "x");
            response.Dispose();
        }

        Func<Task> act = async () => await client.GetAsync(LiveCheckTestData.Base + "x");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(e => e.Message.Contains("budget of 8"));

        _handler.Requests.Should().HaveCount(8);
    }

    [TestMethod]
    public async Task RunObservationAsync_MissingProducts_RecordedInRequestsAndNotices()
    {
        var settings = Settings();
        LiveCheckTestData.Arrange(_handler, settings);

        var result = await LiveCheckRunner.RunObservationAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        result.Requests.Count(r => r.StatusCode == 404).Should().Be(3);
        result.Notices.Count(n => n.EventName == DwdObservationNoticeIds.ProductMissing).Should().Be(3);
        result.Dataset.Measurements.WindSpeedKmh.Should().BeNull();
    }

    [TestMethod]
    public async Task RunObservationAsync_ArchiveFailure_PropagatesWithoutRetry()
    {
        var settings = Settings();
        LiveCheckTestData.Arrange(_handler, settings);

        _handler.AddStatus(new Uri(settings.Cdc.AirTemperatureNowDirectory!, "10minutenwerte_TU_00044_now.zip"), HttpStatusCode.InternalServerError);

        Func<Task> act = async () => await LiveCheckRunner.RunObservationAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        await act.Should().ThrowAsync<CatalogRetrievalException>();
        _handler.Requests.Count(r => r.Contains("10minutenwerte_TU_00044_now.zip")).Should().Be(1);
    }

    [TestMethod]
    public async Task RunObservationAsync_ReturnsSchemaValidEquivalentJsonAndXml()
    {
        var settings = Settings();
        LiveCheckTestData.Arrange(_handler, settings);

        var result = await LiveCheckRunner.RunObservationAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        result.SchemaEvents.Should().BeEmpty();
        result.Dataset.Source.Should().Be("dwd:00044");
        result.Dataset.Measurements.TemperatureC!.Value.Should().Be(12.3m);

        using var json = JsonDocument.Parse(result.Json);
        json.RootElement.GetProperty("retrieved_at").GetString().Should().Be("2026-10-21T12:00:00");

        var xml = XDocument.Parse(Encoding.UTF8.GetString(result.Xml));
        ExportEquivalence.AssertEquivalent(json.RootElement, xml.Root!, "observation_dataset");
    }

    [TestMethod]
    public async Task RunObservationAsync_WritesObservationJsonXmlAndSummary()
    {
        var settings = Settings();
        LiveCheckTestData.Arrange(_handler, settings);
        var result = await LiveCheckRunner.RunObservationAsync(settings, _handler, _clock, CancellationToken.None);
        var output = Path.Combine(_directory, "out");
        Directory.CreateDirectory(output);

        await LiveObservationArtifacts.WriteAsync(result, settings, output, CancellationToken.None);

        (await File.ReadAllBytesAsync(Path.Combine(output, "observation.json"))).Should().Equal(result.Json);
        (await File.ReadAllBytesAsync(Path.Combine(output, "observation.xml"))).Should().Equal(result.Xml);
        var lines = await File.ReadAllLinesAsync(Path.Combine(output, "summary.txt"));
        lines.Should().Contain("budget=8");
        lines.Should().Contain("requests=8");
        lines.Count(l => l.StartsWith("200 ", StringComparison.Ordinal)).Should().Be(5);
        lines.Count(l => l.StartsWith("404 ", StringComparison.Ordinal)).Should().Be(3);
        lines.Should().Contain("station=00044");
        lines.Should().Contain("source=dwd:00044");
        lines.Should().Contain("retrieved_at=2026-10-21T12:00:00");
        lines.Should().Contain("measured_at=2026-10-21T11:50:00");
        lines.Should().Contain("quantity temperature_c 12.3 2026-10-21T11:50:00");
        lines.Should().Contain("quantity humidity_percent 85 2026-10-21T11:50:00");
        lines.Should().Contain("quantity pressure_hpa 1001 2026-10-21T11:50:00");
        lines.Should().Contain("quantity pressure_mmhg 751 2026-10-21T11:50:00");
        lines.Count(l => l.StartsWith("quantity ", StringComparison.Ordinal)).Should().Be(4);
        lines.Should().Contain("schema_events=0");
        lines.Count(l => l.StartsWith("notice Warning " + DwdObservationNoticeIds.ProductMissing + " ", StringComparison.Ordinal)).Should().Be(3);
    }

    private static Func<string, string?> Env(string? enable, string? mode) => name => name == LiveCheckRunner.EnableVariable ? enable : name == LiveCheckRunner.ModeVariable ? mode : null;
}
