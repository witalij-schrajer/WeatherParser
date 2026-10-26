using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Selection;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class ComposedOsthessenWetterPipelineTests
{
    private MockRepository _mocks = null!;
    private Mock<HttpMessageHandler> _handler = null!;
    private HttpClient _http = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<OsthessenWetterParser> _logger = null!;
    private int _sends;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _handler = _mocks.Create<HttpMessageHandler>();
        _http = new HttpClient(_handler.Object, disposeHandler: false);
        _logger = new();
        _clock = new(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
        _http.Dispose();
    }

    private static OsthessenWetterOptions Options(TimeSpan? deadline = null)
    {
        var options = new OsthessenWetterOptions
        {
            Stations =
            {
                new OsthessenWetterStationMetadata { Name = "Bimbach", Latitude = 50.53m, Longitude = 9.72m, AltitudeM = 312 },
                new OsthessenWetterStationMetadata { Name = "Ro\u00dfbach", Latitude = 50.6m, Longitude = 9.9m },
                new OsthessenWetterStationMetadata { Name = "Langenbieber", Latitude = 50.5m, Longitude = 9.8m }
            }
        };
        if (deadline.HasValue)
            options.PageDeadline = deadline.Value;
        return options;
    }

    private ServiceProvider Provider(OsthessenWetterOptions options)
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.AddSingleton(_http);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton(options);
        services.AddSingleton(new OsthessenWetterEndpoints { PageUri = new Uri("https://example.org/messwerte/?token=q1secret") });
        services.AddSingleton<ILogger<OsthessenWetterParser>>(_logger);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private void RespondPage(string html, TimeSpan? delay = null)
    {
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => _sends++)
            .Returns(async (HttpRequestMessage _, CancellationToken _) =>
            {
                if (delay.HasValue)
                {
                    _clock.Advance(delay.Value);
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(html, Encoding.UTF8, "text/html")
                };
            });
    }

    private static string Overview(params string[] rows) => OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(), rows);

    private static string Wind(params string[] rows) => OsthessenWetterHtml.Table(OsthessenWetterHtml.WindHeader(), rows);

    private static DateTime Utc(int day, int hour, int minute, int second = 0) => new(2026, 1, day, hour, minute, second, DateTimeKind.Utc);

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_MixedStations_AllDatasetsAndFailuresOneRequest()
    {
        var options = Options();
        using var provider = Provider(options);
        var parser = provider.GetRequiredService<IOsthessenWetterParser>();
        var page = OsthessenWetterHtml.Page(
            Overview(
                OsthessenWetterHtml.OverviewRow("Bimbach", time: "09:31 Uhr"),
                OsthessenWetterHtml.OverviewRow("Langenbieber", status: "Station offline", time: "", temperature: "", change: "", humidity: ""),
                OsthessenWetterHtml.OverviewRow("Schlitz"),
                OsthessenWetterHtml.OverviewRow("Ro\u00dfbach")),
            null);
        RespondPage(page);

        var result = await parser.ParseAsync("*", CancellationToken.None);

        result.Datasets.Select(d => d.Location.StationId).Should().Equal(new[] { "bimbach", "rossbach" });
        result.StationFailures.Select(f => (f.StationName, f.ErrorId)).Should().Equal(
            new[]
            {
                ("Langenbieber", OsthessenWetterNoticeIds.StationOffline),
                ("Schlitz", OsthessenWetterNoticeIds.StationMetadataMissing)
            });
        _sends.Should().Be(1);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_OfflineRowWithValidValues_FailureNoDataset()
    {
        var options = Options();
        using var provider = Provider(options);
        var parser = provider.GetRequiredService<IOsthessenWetterParser>();
        var page = OsthessenWetterHtml.Page(
            Overview(
                OsthessenWetterHtml.OverviewRow("Langenbieber", status: "Station offline"),
                OsthessenWetterHtml.OverviewRow("Bimbach")),
            null);
        RespondPage(page);

        var result = await parser.ParseAsync("*", CancellationToken.None);

        result.Datasets.Count.Should().Be(1);
        result.Datasets[0].Location.StationId.Should().Be("bimbach");
        result.StationFailures.Select(f => (f.StationName, f.ErrorId)).Should().ContainSingle()
            .Which.Should().Be(("Langenbieber", OsthessenWetterNoticeIds.StationOffline));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_OldOnlineDisplay_DatasetWithInferredTimes()
    {
        var options = Options();
        using var provider = Provider(options);
        var parser = provider.GetRequiredService<IOsthessenWetterParser>();
        var page = OsthessenWetterHtml.Page(
            Overview(OsthessenWetterHtml.OverviewRow("Bimbach", time: "06:00 Uhr")),
            null);
        RespondPage(page);

        var result = await parser.ParseAsync("Bimbach", CancellationToken.None);

        result.Datasets.Count.Should().Be(1);
        result.Datasets[0].Measurements.TemperatureC!.MeasuredAtUtc.Should().Be(Utc(15, 5, 0));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_Dataset_SourceLocationTimes()
    {
        var options = Options();
        using var provider = Provider(options);
        var parser = provider.GetRequiredService<IOsthessenWetterParser>();
        var page = OsthessenWetterHtml.Page(
            Overview(
                OsthessenWetterHtml.OverviewRow("Bimbach", time: "09:31 Uhr"),
                OsthessenWetterHtml.OverviewRow("Ro\u00dfbach", time: "09:31 Uhr")),
            Wind(
                OsthessenWetterHtml.WindRow("Bimbach", time: "09:40 Uhr")));
        RespondPage(page);

        var result = await parser.ParseAsync("*", CancellationToken.None);

        result.Datasets.Count.Should().Be(2);
        var bimbach = result.Datasets[0];
        bimbach.Source.Should().Be("osthessenwetter:bimbach");
        bimbach.Location.Name.Should().Be("Bimbach");
        bimbach.Location.Latitude.Should().Be(50.53m);
        bimbach.Location.Longitude.Should().Be(9.72m);
        bimbach.Location.AltitudeM.Should().Be(312);
        bimbach.RetrievedAtUtc.Should().Be(Utc(15, 9, 0));
        bimbach.MeasuredAtUtc.Should().Be(Utc(15, 8, 40));
        bimbach.TimestampQuality.Should().Be(TimestampQuality.EstimatedDate);

        var rossbach = result.Datasets[1];
        rossbach.Source.Should().Be("osthessenwetter:rossbach");
        rossbach.Location.Name.Should().Be("Ro\u00dfbach");
        rossbach.Location.AltitudeM.Should().BeNull();
        rossbach.MeasuredAtUtc.Should().Be(Utc(15, 8, 31));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DelayedResponseAcrossMidnight_ReadingGetsResponseDate()
    {
        _clock = new(new DateTimeOffset(2026, 1, 15, 22, 59, 55, TimeSpan.Zero));
        var options = Options();
        using var provider = Provider(options);
        var parser = provider.GetRequiredService<IOsthessenWetterParser>();
        var page = OsthessenWetterHtml.Page(
            Overview(OsthessenWetterHtml.OverviewRow("Bimbach", time: "00:00 Uhr")),
            null);
        RespondPage(page, TimeSpan.FromSeconds(10));

        var result = await parser.ParseAsync("Bimbach", CancellationToken.None);

        result.Datasets[0].Measurements.TemperatureC!.MeasuredAtUtc.Should().Be(Utc(15, 23, 0));
        result.Datasets[0].RetrievedAtUtc.Should().Be(Utc(15, 23, 0, 5));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_CollisionOutsideMatchedSet_ThrowsIdCollision()
    {
        var options = Options();
        using var provider = Provider(options);
        var parser = provider.GetRequiredService<IOsthessenWetterParser>();
        var page = OsthessenWetterHtml.Page(
            Overview(
                OsthessenWetterHtml.OverviewRow("Bimbach"),
                OsthessenWetterHtml.OverviewRow("Ro\u00dfbach"),
                OsthessenWetterHtml.OverviewRow("Rossbach")),
            null);
        RespondPage(page);

        Func<Task> act = () => parser.ParseAsync("Bimbach", CancellationToken.None);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.ErrorId.Should().Be(OsthessenWetterException.StationIdCollisionErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_Composed_BodyStalledAfterHeaders_DeadlineExceeded()
    {
        var options = Options(TimeSpan.FromSeconds(10));
        using var provider = Provider(options);
        var parser = provider.GetRequiredService<IOsthessenWetterParser>();
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage _, CancellationToken _) =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(new OsthessenWetterHtml.PendingStream())
                };
            });

        var task = parser.ParseAsync("*", CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(10));

        (await FluentActions.Awaiting(() => task).Should().ThrowAsync<OsthessenWetterException>()).Which.ErrorId.Should().Be(OsthessenWetterException.DeadlineExceededErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_Logs_NoValuesNoPageTextNoQuery()
    {
        var options = Options();
        using var provider = Provider(options);
        var parser = provider.GetRequiredService<IOsthessenWetterParser>();
        var page = OsthessenWetterHtml.Page(
            Overview(
                OsthessenWetterHtml.OverviewRow("Bimbach", temperature: "abc"),
                OsthessenWetterHtml.OverviewRow("Langenbieber", status: "Station offline"),
                OsthessenWetterHtml.OverviewRow("Schlitz")),
            null);
        RespondPage(page);

        await parser.ParseAsync("*", CancellationToken.None);

        _logger.Entries.Should().Contain(e => e.EventName == OsthessenWetterNoticeIds.ValueUnreadable);
        _logger.Entries.Should().NotContain(e => e.Message.Contains("web000"));
        _logger.Entries.Should().NotContain(e => e.Message.Contains("db.example.invalid"));
        _logger.Entries.Should().NotContain(e => e.Message.Contains("token"));
        _logger.Entries.Should().NotContain(e => e.Message.Contains("q1secret"));
        _logger.Entries.Should().NotContain(e => e.Message.Contains("abc"));
        _logger.Entries.Should().NotContain(e => e.Message.Contains("93"));
    }

    [TestMethod]
    public void Construct_MetadataLatitudeOrLongitudeOutOfRange_ThrowsOptionsInvalid()
    {
        var options = Options();
        options.Stations.Clear();
        options.Stations.Add(new OsthessenWetterStationMetadata { Name = "Test", Latitude = 90.1m, Longitude = 9.7m });
        using var provider = Provider(options);
        FluentActions.Invoking(() => provider.GetRequiredService<IOsthessenWetterParser>()).Should().Throw<OsthessenWetterException>().Which.ErrorId.Should().Be(OsthessenWetterException.OptionsInvalidErrorId);

        options.Stations.Clear();
        options.Stations.Add(new OsthessenWetterStationMetadata { Name = "Test", Latitude = 50.5m, Longitude = -180.1m });
        using var provider2 = Provider(options);
        FluentActions.Invoking(() => provider2.GetRequiredService<IOsthessenWetterParser>()).Should().Throw<OsthessenWetterException>().Which.ErrorId.Should().Be(OsthessenWetterException.OptionsInvalidErrorId);
    }

    [TestMethod]
    public void Construct_MetadataDuplicateName_ThrowsOptionsInvalid()
    {
        var options = Options();
        options.Stations.Clear();
        options.Stations.Add(new OsthessenWetterStationMetadata { Name = "M\u00fcs", Latitude = 50.5m, Longitude = 9.7m });
        options.Stations.Add(new OsthessenWetterStationMetadata { Name = "Mu\u0308s", Latitude = 50.6m, Longitude = 9.8m });
        using var provider = Provider(options);
        FluentActions.Invoking(() => provider.GetRequiredService<IOsthessenWetterParser>()).Should().Throw<OsthessenWetterException>().Which.ErrorId.Should().Be(OsthessenWetterException.OptionsInvalidErrorId);
    }

    [TestMethod]
    public void Construct_MetadataEmptyName_ThrowsOptionsInvalid()
    {
        var options = Options();
        options.Stations.Clear();
        options.Stations.Add(new OsthessenWetterStationMetadata { Name = " ", Latitude = 50.5m, Longitude = 9.7m });
        using var provider = Provider(options);
        FluentActions.Invoking(() => provider.GetRequiredService<IOsthessenWetterParser>()).Should().Throw<OsthessenWetterException>().Which.ErrorId.Should().Be(OsthessenWetterException.OptionsInvalidErrorId);
    }

    [TestMethod]
    public void Construct_MetadataNullEntry_ThrowsOptionsInvalid()
    {
        var options = Options();
        options.Stations.Clear();
        options.Stations.Add(null!);
        using var provider = Provider(options);
        FluentActions.Invoking(() => provider.GetRequiredService<IOsthessenWetterParser>()).Should().Throw<OsthessenWetterException>().Which.ErrorId.Should().Be(OsthessenWetterException.OptionsInvalidErrorId);
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesOsthessenWetterParser()
    {
        var options = Options();
        using var provider = Provider(options);
        provider.GetRequiredService<IOsthessenWetterParser>().Should().BeOfType<OsthessenWetterParser>();
    }
}
