using System;
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Export;
using WeatherParser.Parsing.Tests.Observations.Osthessen;
using WeatherParser.Parsing.Tests.Selection;

namespace WeatherParser.Parsing.Tests.Live;

[TestClass]
public sealed class LiveCheckOsthessenRunnerTests
{
    private string _directory = null!;
    private StaticHttpHandler _handler = null!;
    private ManualTimeProvider _clock = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "wp-live-osthessen-" + Guid.NewGuid());
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

    private LiveCheckSettings Settings() => LiveCheckTestData.Settings(_directory) with { OsthessenPageUri = new Uri(LiveCheckTestData.Base + "messwerte/") };

    private void ServePage(LiveCheckSettings settings, params string[] rows) => _handler.Add(settings.OsthessenPageUri, Encoding.UTF8.GetBytes(OsthessenWetterHtml.Page(OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(), rows), null)));

    private static Func<string, string?> Env(string? enable, string? mode) => name => name == LiveCheckRunner.EnableVariable ? enable : name == LiveCheckRunner.ModeVariable ? mode : null;

    [DataRow("1", null)]
    [DataRow("1", "")]
    [DataRow("1", "observation")]
    [DataRow("1", "OSTHESSEN")]
    [DataRow("0", "osthessen")]
    [DataRow(null, "osthessen")]
    [TestMethod]
    public async Task RunOsthessenIfEnabledAsync_WithoutOsthessenMode_ReturnsNullBeforeAnyRequest(string? enable, string? mode)
    {
        var settings = Settings();
        ServePage(settings, OsthessenWetterHtml.OverviewRow("Bimbach"));

        var result = await LiveCheckRunner.RunOsthessenIfEnabledAsync(
            Env(enable, mode),
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        result.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
    }

    [DataRow("1", "osthessen", true)]
    [DataRow("1", "Osthessen", false)]
    [DataRow("1", null, false)]
    [DataRow("0", "osthessen", false)]
    [TestMethod]
    public void IsOsthessenMode_OnlyForEnabledAndExactMode(string? enable, string? mode, bool expected)
    {
        LiveCheckRunner.IsOsthessenMode(Env(enable, mode)).Should().Be(expected);
    }

    [DataRow("1", null, true)]
    [DataRow("1", "", true)]
    [DataRow("1", "observation", false)]
    [DataRow("1", "osthessen", false)]
    [DataRow("0", null, false)]
    [DataRow(null, null, false)]
    [TestMethod]
    public void IsSelectionMode_OnlyWhenEnabledWithoutObservationOrOsthessenMode(string? enable, string? mode, bool expected)
    {
        LiveCheckRunner.IsSelectionMode(Env(enable, mode)).Should().Be(expected);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunOsthessenIfEnabledAsync_OsthessenMode_OneRequestToThePage()
    {
        var settings = Settings();
        ServePage(settings, OsthessenWetterHtml.OverviewRow("Bimbach"), OsthessenWetterHtml.OverviewRow("Langenbieber", status: "Station offline", time: "", temperature: "", change: "", humidity: ""));

        var result = await LiveCheckRunner.RunOsthessenIfEnabledAsync(
            Env("1", "osthessen"),
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Requests.Select(r => r.Uri).Should().ContainSingle().And.Equal(settings.OsthessenPageUri.AbsoluteUri);
        _handler.Requests.Should().ContainSingle().And.Equal(settings.OsthessenPageUri.AbsoluteUri);
        result.ErrorId.Should().BeNull();
        result.Datasets.Select(d => d.Source).Should().ContainSingle().And.Equal("osthessenwetter:bimbach");
        result.StationFailures.Select(f => (f.StationName, f.ErrorId)).Should().ContainSingle().And.Equal(("Langenbieber", OsthessenWetterNoticeIds.StationOffline));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task OsthessenBudgetOne_RefusesSecondRequest()
    {
        var settings = Settings();
        ServePage(settings, OsthessenWetterHtml.OverviewRow("Bimbach"));

        using var client = new HttpClient(new RequestBudgetHandler(LiveCheckSettings.OsthessenRequestBudget, _handler));
        var response = await client.GetAsync(LiveCheckTestData.Base + "x");
        response.Dispose();

        Func<Task> act = async () => await client.GetAsync(LiveCheckTestData.Base + "x");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(e => e.Message.Contains("budget of 1"));

        _handler.Requests.Should().ContainSingle();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunOsthessenAsync_PageUnavailable_PropagatesWithoutRetry()
    {
        var settings = Settings();
        _handler.AddStatus(settings.OsthessenPageUri, HttpStatusCode.InternalServerError);

        Func<Task> act = async () => await LiveCheckRunner.RunOsthessenAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        await act.Should().ThrowAsync<OsthessenWetterException>()
            .Where(e => e.ErrorId == OsthessenWetterException.PageUnavailableErrorId);
        _handler.Requests.Should().ContainSingle();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunOsthessenAsync_NoUsableStation_ReturnsFailuresWithoutThrowing()
    {
        var settings = Settings();
        ServePage(settings, OsthessenWetterHtml.OverviewRow("Langenbieber", status: "Station offline", time: "", temperature: "", change: "", humidity: ""));

        var result = await LiveCheckRunner.RunOsthessenAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        result.ErrorId.Should().Be(OsthessenWetterException.NoUsableStationErrorId);
        result.Datasets.Should().BeEmpty();
        result.StationFailures.Select(f => (f.StationName, f.ErrorId)).Should().ContainSingle().And.Equal(("Langenbieber", OsthessenWetterNoticeIds.StationOffline));
        result.Json.Should().BeEmpty();
        result.Xml.Should().BeEmpty();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunOsthessenAsync_DatasetsSchemaValidAndEquivalent()
    {
        var settings = Settings();
        ServePage(settings, OsthessenWetterHtml.OverviewRow("Bimbach"));

        var result = await LiveCheckRunner.RunOsthessenAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        result.SchemaEvents.Should().BeEmpty();
        result.Json.Should().ContainSingle();
        result.Xml.Should().ContainSingle();

        using var json = JsonDocument.Parse(result.Json[0]);
        var xml = XDocument.Parse(Encoding.UTF8.GetString(result.Xml[0]));
        ExportEquivalence.AssertEquivalent(json.RootElement, xml.Root!, "observation_dataset");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunOsthessenAsync_PlaceholderMetadataForEveryStation()
    {
        var settings = Settings();
        ServePage(settings, OsthessenWetterHtml.OverviewRow("Bimbach"));

        var result = await LiveCheckRunner.RunOsthessenAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        var dataset = result.Datasets.Single();
        dataset.Location.Latitude.Should().Be(50.5m);
        dataset.Location.Longitude.Should().Be(9.7m);
        dataset.Location.Name.Should().Be("Bimbach");
        dataset.Location.AltitudeM.Should().BeNull();

        var lookup = new PlaceholderMetadataLookup();
        var metadata = lookup.Find("Irgendwo");
        metadata.Should().NotBeNull();
        metadata!.Name.Should().Be("Irgendwo");
        metadata!.Latitude.Should().Be(50.5m);
        metadata!.Longitude.Should().Be(9.7m);
        metadata!.AltitudeM.Should().BeNull();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunOsthessenAsync_LogsCaptured()
    {
        var settings = Settings();
        ServePage(settings, OsthessenWetterHtml.OverviewRow("Bimbach", temperature: "abc"), OsthessenWetterHtml.OverviewRow("Langenbieber", status: "Station offline", time: "", temperature: "", change: "", humidity: ""));

        var result = await LiveCheckRunner.RunOsthessenAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        result.Notices.Should().Contain(n => n.EventName == OsthessenWetterNoticeIds.ValueUnreadable);
        result.Notices.Should().Contain(n => n.EventName == OsthessenWetterNoticeIds.StationOffline);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task WriteAsync_WritesOnlySummaryWithoutValuesOrPage()
    {
        var settings = Settings();
        ServePage(settings, OsthessenWetterHtml.OverviewRow("Bimbach"), OsthessenWetterHtml.OverviewRow("Langenbieber", status: "Station offline", time: "", temperature: "", change: "", humidity: ""));

        var result = await LiveCheckRunner.RunOsthessenAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        var output = Path.Combine(_directory, "out");
        Directory.CreateDirectory(output);

        await LiveOsthessenArtifacts.WriteAsync(result, settings, output, CancellationToken.None);

        Directory.GetFiles(output).Select(Path.GetFileName).Should().ContainSingle().And.Equal("summary.txt");

        var lines = await File.ReadAllLinesAsync(Path.Combine(output, "summary.txt"));
        lines.Should().Contain("budget=1");
        lines.Should().Contain("requests=1");
        lines.Should().Contain("pattern=*");
        lines.Should().Contain("error=none");
        lines.Should().Contain("datasets=1");
        lines.Should().Contain("failures=1");
        lines.Should().Contain("schema_events=0");
        lines.Should().Contain("matched=2");

        lines.Should().Contain("dataset osthessenwetter:bimbach 2026-10-21T07:31:00 temperature_c,humidity_percent Bimbach");
        lines.Should().Contain("failure OSTHESSEN-STATION-OFFLINE Langenbieber");

        lines.Where(l => l.StartsWith("200 ", StringComparison.Ordinal)).Should().ContainSingle().Which.Should().EndWith(settings.OsthessenPageUri.AbsoluteUri);
        lines.Should().NotContain(l => l.Contains("10.3"));
        lines.Should().NotContain(l => l.Contains("93"));
        lines.Should().NotContain(l => l.Contains("57.9"));
        lines.Should().NotContain(l => l.Contains("11.1"));
        lines.Should().NotContain(l => l.Contains("<"));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task WriteAsync_StationNames_EscapedToOneAsciiLine()
    {
        var settings = Settings();
        ServePage(settings, 
            OsthessenWetterHtml.OverviewRow("M&uuml;s-Hohh&uuml;tte"),
            OsthessenWetterHtml.OverviewRow("Bimbach&lt;tag&gt;&#10;datasets=999", status: "Station offline", time: "", temperature: "", change: "", humidity: ""));

        var result = await LiveCheckRunner.RunOsthessenAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        var output = Path.Combine(_directory, "out");
        Directory.CreateDirectory(output);

        await LiveOsthessenArtifacts.WriteAsync(result, settings, output, CancellationToken.None);

        var lines = await File.ReadAllLinesAsync(Path.Combine(output, "summary.txt"));
        lines.Should().Contain("dataset osthessenwetter:mues-hohhuette 2026-10-21T07:31:00 temperature_c,humidity_percent M\\u00FCs-Hohh\\u00FCtte");
        lines.Should().Contain("failure OSTHESSEN-STATION-OFFLINE Bimbach\\u003Ctag\\u003E\\u000Adatasets=999");
        lines.Should().Contain("matched=2");

        lines.Should().NotContain("datasets=999");
        lines.Should().NotContain(l => l.Contains("<"));
        lines.Should().NotContain(l => l.Contains(">"));

        var bytes = await File.ReadAllBytesAsync(Path.Combine(output, "summary.txt"));
        bytes.Should().OnlyContain(b => b < 0x7F);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task EnsureUsable_NoUsableStation_Throws()
    {
        var settings = Settings();
        ServePage(settings, OsthessenWetterHtml.OverviewRow("Langenbieber", status: "Station offline", time: "", temperature: "", change: "", humidity: ""));

        var result = await LiveCheckRunner.RunOsthessenAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        FluentActions.Invoking(() => LiveCheckRunner.EnsureUsable(result)).Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task EnsureUsable_WithDataset_DoesNotThrow()
    {
        var settings = Settings();
        ServePage(settings, 
            OsthessenWetterHtml.OverviewRow("Bimbach"),
            OsthessenWetterHtml.OverviewRow("Langenbieber", status: "Station offline", time: "", temperature: "", change: "", humidity: ""));

        var result = await LiveCheckRunner.RunOsthessenAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        FluentActions.Invoking(() => LiveCheckRunner.EnsureUsable(result)).Should().NotThrow();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task WriteAsync_NoUsableStation_SummaryNamesTheError()
    {
        var settings = Settings();
        ServePage(settings, OsthessenWetterHtml.OverviewRow("Langenbieber", status: "Station offline", time: "", temperature: "", change: "", humidity: ""));

        var result = await LiveCheckRunner.RunOsthessenAsync(
            settings,
            _handler,
            _clock,
            CancellationToken.None);

        var output = Path.Combine(_directory, "out");
        Directory.CreateDirectory(output);

        await LiveOsthessenArtifacts.WriteAsync(result, settings, output, CancellationToken.None);

        var lines = await File.ReadAllLinesAsync(Path.Combine(output, "summary.txt"));
        lines.Should().Contain("error=OSTHESSEN-NO-USABLE-STATION");
        lines.Should().Contain("datasets=0");
    }
}
