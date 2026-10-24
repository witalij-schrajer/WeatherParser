using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Evidence.MosmixL;
using WeatherParser.Parsing.Tests.Evidence.MosmixS;
using WeatherParser.Parsing.Tests.Parsing;
using WeatherParser.Parsing.Tests.Selection;

namespace WeatherParser.Parsing.Tests.Live;

[TestClass]
public sealed class LiveCheckRunnerTests
{
    private string _directory = null!;
    private StaticHttpHandler _handler = null!;
    private ManualTimeProvider _clock = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "wp-live-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _handler = new StaticHttpHandler();
        _clock = new ManualTimeProvider(LiveCheckTestData.Now);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("0")]
    [DataRow("true")]
    [DataRow(" 1")]
    public async Task RunIfEnabled_WithoutExactOptIn_ReturnsNullBeforeAnyRequest(string? value)
    {
        var settings = LiveCheckTestData.Settings(_directory);
        LiveCheckResult? result = await LiveCheckRunner.RunIfEnabledAsync(_ => value, settings, _handler, _clock, CancellationToken.None);
        result.Should().BeNull();
        _handler.Requests.Should().BeEmpty();
    }

    [TestMethod]
    [DataRow("1", true)]
    [DataRow("0", false)]
    [DataRow(null, false)]
    public void IsEnabled_OnlyForExactlyOne(string? value, bool expected)
    {
        var result = LiveCheckRunner.IsEnabled(name => name == LiveCheckRunner.EnableVariable ? value : null);
        result.Should().Be(expected);
    }

    [TestMethod]
    public async Task RunIfEnabled_WithOptIn_RunsRefreshAndBothSelections()
    {
        var settings = LiveCheckTestData.Settings(_directory);
        LiveCheckTestData.Arrange(_handler, settings);

        var result = await LiveCheckRunner.RunIfEnabledAsync(_ => "1", settings, _handler, _clock, CancellationToken.None);
        result.Should().NotBeNull();

        result.Index.Should().NotBeNull();
        result.Index!.DownloadedAtUtc.Should().Be(new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc));
        result.Index.IssuedAtUtc.Should().Be(new DateTime(2026, 10, 23, 3, 0, 0, DateTimeKind.Utc));

        result.MosmixList.Locations.Should().HaveCount(2);
        result.MosmixList.Locations[0].MosmixId.Should().Be("X123A");
        result.MosmixList.Locations[1].MosmixId.Should().Be("01234");

        var x123A = result.MosmixList.Locations[0];
        var mosmixL = x123A.Products.Single(p => p.Product == MosmixProduct.MosmixL);
        mosmixL.Availability.Should().Be(AvailabilityState.Available);
        mosmixL.AvailableForecastElements.Should().Equal("FF", "TTT");

        var _01234 = result.MosmixList.Locations[1];
        var mosmixL2 = _01234.Products.Single(p => p.Product == MosmixProduct.MosmixL);
        mosmixL2.Availability.Should().Be(AvailabilityState.Unavailable);
        mosmixL2.EvidenceRetrievedAtUtc.Should().BeNull();

        result.CdcList.Stations.Should().HaveCount(1);
        result.CdcList.Stations[0].StationId.Should().Be("00044");
        result.CdcList.Stations[0].AvailableMeasurements.Should().Equal("humidity_percent", "pressure_hpa", "pressure_mmhg", "temperature_c");

        result.MosmixSchemaEvents.Should().BeEmpty();
        result.CdcSchemaEvents.Should().BeEmpty();

        result.MosmixJson.Should().NotBeEmpty();
        result.MosmixXml.Should().NotBeEmpty();
        result.CdcJson.Should().NotBeEmpty();
        result.CdcXml.Should().NotBeEmpty();

        result.Requests.Should().HaveCount(12);
        result.Requests[0].Uri.Should().Contain(settings.Mosmix.MosmixSAllStationsUri!.AbsoluteUri);
        result.Requests.Count(r => r.StatusCode == 404).Should().Be(4);
        foreach (var req in result.Requests)
        {
            req.Uri.Should().StartWith(LiveCheckTestData.Base);
        }

        result.Requests.Select(r => r.Uri).Should().Contain(new Uri(settings.Cdc.WindNowDirectory!, "10minutenwerte_wind_00044_now.zip").AbsoluteUri);
        result.Requests.Select(r => r.Uri).Should().Contain(new Uri(settings.Cdc.PrecipitationNowDirectory!, "10minutenwerte_nieder_00044_now.zip").AbsoluteUri);
        result.Requests.Select(r => r.Uri).Should().Contain(new Uri(settings.Cdc.SolarNowDirectory!, "10minutenwerte_SOLAR_00044_now.zip").AbsoluteUri);

        var tuRequest = result.Requests.Single(r => r.Uri.EndsWith("10minutenwerte_TU_00044_now.zip"));
        tuRequest.BytesRead.Should().Be(tuRequest.ContentLength!.Value);

        result.Notices.Should().NotContain(n => n.EventName == "SELECTION-EMPTY");
    }

    [TestMethod]
    public async Task RunAsync_RequestCountStaysWithinTheComputedBudget()
    {
        var settings = LiveCheckTestData.Settings(_directory);
        settings.RequestBudget.Should().Be(56); // 1 + 1 + 10 + 4 + (4 * 10)

        LiveCheckTestData.Arrange(_handler, settings);

        var result = await LiveCheckRunner.RunAsync(settings, _handler, _clock, CancellationToken.None);
        result.Requests.Count.Should().BeLessThanOrEqualTo(settings.RequestBudget);
    }

    [TestMethod]
    public async Task RequestBudgetHandler_RefusesTheRequestBeyondTheBudget()
    {
        _handler.Add(new Uri(LiveCheckTestData.Base + "x"), new byte[] { 1 });
        var budget = new RequestBudgetHandler(2, _handler);
        using var client = new HttpClient(budget);

        await client.GetAsync(new Uri(LiveCheckTestData.Base + "x"));
        await client.GetAsync(new Uri(LiveCheckTestData.Base + "x"));

        Func<Task> act = async () => await client.GetAsync(new Uri(LiveCheckTestData.Base + "x"));
        await act.Should().ThrowAsync<InvalidOperationException>()
            .Where(e => e.Message.Contains("budget"));

        _handler.Requests.Should().HaveCount(2);
        budget.Requests.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task RequestBudgetHandler_ResponseWithoutContentLength_CountsBytesRead()
    {
        var body = HttpTestContent.Bytes(300);
        var budget = new RequestBudgetHandler(1, new FixedResponseHandler(body));
        using var client = new HttpClient(budget);

        var bytes = await client.GetByteArrayAsync(new Uri(LiveCheckTestData.Base + "chunked"));

        bytes.Length.Should().Be(300);
        budget.Requests.Should().ContainSingle();
        budget.Requests[0].ContentLength.Should().BeNull();
        budget.Requests[0].BytesRead.Should().Be(300);
    }

    private sealed class FixedResponseHandler(byte[] body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(HttpTestContent.Ok(HttpTestContent.Unknown(body)));
        }
    }

    [TestMethod]
    public void CreateTransport_DisablesAutomaticRedirects()
    {
        using var transport = LiveCheckRunner.CreateTransport();
        transport.AllowAutoRedirect.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunAsync_WithoutRefreshAndEmptyCache_FailsWithEvidenceMissingBeforeAnyRequest()
    {
        var settings = LiveCheckTestData.Settings(_directory, refreshFirst: false);
        LiveCheckTestData.Arrange(_handler, settings);

        Func<Task> act = async () => await LiveCheckRunner.RunAsync(settings, _handler, _clock, CancellationToken.None);
        await act.Should().ThrowAsync<EvidenceCacheException>()
            .Where(e => e.ErrorId == EvidenceCacheException.EvidenceMissingErrorId);
        _handler.Requests.Should().BeEmpty();
    }

    [TestMethod]
    public async Task RunAsync_OversizedIndex_FailsWithEvidenceMissing()
    {
        var settings = LiveCheckTestData.Settings(_directory, refreshFirst: false);
        LiveCheckTestData.Arrange(_handler, settings);

        var indexPath = LiveCheckTestData.IndexPath(settings);
        File.WriteAllText(indexPath, new string(' ', 8 * 1024 * 1024 + 1));

        Func<Task> act = async () => await LiveCheckRunner.RunAsync(settings, _handler, _clock, CancellationToken.None);
        await act.Should().ThrowAsync<EvidenceCacheException>()
            .Where(e => e.ErrorId == EvidenceCacheException.EvidenceMissingErrorId);
        _handler.Requests.Should().BeEmpty();
    }

    [TestMethod]
    [DataRow("{ not json")]
    [DataRow("{\"format_version\":2}")]
    public async Task RunAsync_InvalidIndex_FailsWithEvidenceMissing(string content)
    {
        var settings = LiveCheckTestData.Settings(_directory, refreshFirst: false);
        LiveCheckTestData.Arrange(_handler, settings);

        var indexPath = LiveCheckTestData.IndexPath(settings);
        File.WriteAllText(indexPath, content);

        Func<Task> act = async () => await LiveCheckRunner.RunAsync(settings, _handler, _clock, CancellationToken.None);
        await act.Should().ThrowAsync<EvidenceCacheException>()
            .Where(e => e.ErrorId == EvidenceCacheException.EvidenceMissingErrorId);
        _handler.Requests.Should().BeEmpty();
    }

    [TestMethod]
    public async Task RunAsync_StaleIndex_UsedWithOneNoticeAndPreservedTimestamps()
    {
        var settings = LiveCheckTestData.Settings(_directory, refreshFirst: false);
        LiveCheckTestData.Arrange(_handler, settings);

        await LiveCheckTestData.WriteIndexAsync(settings, new DateTime(2026, 10, 21, 5, 0, 0, DateTimeKind.Utc));

        var result = await LiveCheckRunner.RunAsync(settings, _handler, _clock, CancellationToken.None);
        result.Notices.Should().ContainSingle(n => n.EventName == "MOSMIX-S-EVIDENCE-STALE" && n.Level == LogLevel.Warning);

        var x123A = result.MosmixList.Locations[0];
        var mosmixS = x123A.Products.Single(p => p.Product == MosmixProduct.MosmixS);
        mosmixS.EvidenceRetrievedAtUtc.Should().Be(new DateTime(2026, 10, 21, 5, 0, 0, DateTimeKind.Utc));

        result.Requests.Should().NotContain(r => r.Uri.Contains(settings.Mosmix.MosmixSAllStationsUri!.AbsoluteUri));
    }
}
