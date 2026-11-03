using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Export;
using WeatherParser.Parsing.Tests.Selection;
using WeatherParser.Parsing.Tests.Schemas;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class OsthessenWetterExportContractTests
{
    private MockRepository _mocks = null!;
    private Mock<HttpMessageHandler> _handler = null!;
    private HttpClient _http = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<OsthessenWetterParser> _logger = null!;
    private int _sends;
    private readonly List<ServiceProvider> _providers = new();

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
        foreach (var provider in _providers)
        {
            provider.Dispose();
        }
        _mocks.VerifyAll();
        _http.Dispose();
    }

    private static OsthessenWetterOptions Options()
    {
        var options = new OsthessenWetterOptions
        {
            Stations =
            {
                new OsthessenWetterStationMetadata { Name = "Bimbach", Latitude = 50.53m, Longitude = 9.72m, AltitudeM = 312 },
                new OsthessenWetterStationMetadata { Name = "Ro\u00dfbach", Latitude = 50.6m, Longitude = 9.9m },
                new OsthessenWetterStationMetadata { Name = "M\u00fcs-Hohh\u00fctte", Latitude = 50.45m, Longitude = 9.85m, AltitudeM = 450 },
                new OsthessenWetterStationMetadata { Name = "Schlitz", Latitude = 50.67m, Longitude = 9.56m },
                new OsthessenWetterStationMetadata { Name = "Burghaun", Latitude = 50.7m, Longitude = 9.73m }
            }
        };
        return options;
    }

    private ServiceProvider Provider(OsthessenWetterOptions options)
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.AddSingleton(_http);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton(options);
        services.AddSingleton(new OsthessenWetterEndpoints { PageUri = new Uri("https://example.org/messwerte/?token=q1secret") });
        services.AddSingleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://mosmix.example/single_stations/") });
        services.AddSingleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://mosmix.example/MOSMIX_S_LATEST_240.kmz") });
        services.AddSingleton(new MosmixLForecastOptions { MaxRunAge = TimeSpan.FromHours(8) });
        services.AddSingleton(new MosmixSForecastOptions { MaxRunAge = TimeSpan.FromHours(2) });
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

    private static async Task<(byte[], byte[])> Serialize(ServiceProvider provider, ObservationDataset dataset)
    {
        var json = provider.GetRequiredService<IJsonExportSerializer>();
        var xml = provider.GetRequiredService<IXmlExportSerializer>();
        using var jsonStream = new MemoryStream();
        using var xmlStream = new MemoryStream();
        await json.SerializeAsync(dataset, jsonStream, CancellationToken.None);
        await xml.SerializeAsync(dataset, xmlStream, CancellationToken.None);
        return (jsonStream.ToArray(), xmlStream.ToArray());
    }

    private static Dictionary<string, string> JsonMap(byte[] json)
    {
        var map = new Dictionary<string, string>();
        using var document = JsonDocument.Parse(json);
        ExportEquivalence.FlattenJson(document.RootElement, string.Empty, map);
        return map;
    }

    private static Dictionary<string, string> XmlMap(byte[] xml)
    {
        var map = new Dictionary<string, string>();
        foreach (var element in XDocument.Parse(Encoding.UTF8.GetString(xml)).Root!.Elements())
        {
            ExportEquivalence.FlattenXml(element, string.Empty, map);
        }
        return map;
    }

    private static void AssertEquivalent(byte[] json, byte[] xml)
    {
        using var document = JsonDocument.Parse(json);
        ExportEquivalence.AssertEquivalent(document.RootElement, XDocument.Parse(Encoding.UTF8.GetString(xml)).Root!, "observation_dataset");
    }

    private async Task<(ServiceProvider Provider, OsthessenWetterParseResult Result)> Parse(string overview, string? wind, string pattern = "*")
    {
        var options = Options();
        var provider = Provider(options);
        _providers.Add(provider);
        var parser = provider.GetRequiredService<IOsthessenWetterParser>();
        var page = OsthessenWetterHtml.Page(overview, wind);
        RespondPage(page);

        var result = await parser.ParseAsync(pattern, CancellationToken.None);
        return (provider, result);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Export_Dataset_JsonXmlEquivalent()
    {
        var (provider, result) = await Parse(
            Overview(OsthessenWetterHtml.OverviewRow("Bimbach", time: "09:31 Uhr", temperature: "20.25", humidity: "93")),
            Wind(OsthessenWetterHtml.WindRow("Bimbach", time: "09:40 Uhr", mean10: "12.5")));

        var dataset = result.Datasets[0];
        var (json, xml) = await Serialize(provider, dataset);
        AssertEquivalent(json, xml);
        JsonMap(json).Should().BeEquivalentTo(XmlMap(xml));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Export_Xml_ValidAgainstV1Schema()
    {
        var (provider, result) = await Parse(
            Overview(OsthessenWetterHtml.OverviewRow("Bimbach", time: "09:31 Uhr", temperature: "20.25", humidity: "93")),
            Wind(OsthessenWetterHtml.WindRow("Bimbach", time: "09:40 Uhr", mean10: "12.5")));

        var dataset = result.Datasets[0];
        var (_, xml) = await Serialize(provider, dataset);
        ExportSchemaValidation.Validate(xml).Should().BeEmpty();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Export_Markers_EstimatedDateOnReadingsAndDataset_SourceTimezoneOnReadingsOnly()
    {
        var (provider, result) = await Parse(
            Overview(OsthessenWetterHtml.OverviewRow("Bimbach", time: "09:31 Uhr", temperature: "20.25", humidity: "93")),
            Wind(OsthessenWetterHtml.WindRow("Bimbach", time: "09:40 Uhr", mean10: "12.5")));

        var dataset = result.Datasets[0];
        var (json, xml) = await Serialize(provider, dataset);
        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);

        foreach (var map in new[] { jsonMap, xmlMap })
        {
            map["timestamp_quality"].Should().Be("estimated_date");
            map["measurements/temperature_c/timestamp_quality"].Should().Be("estimated_date");
            map["measurements/humidity_percent/timestamp_quality"].Should().Be("estimated_date");
            map["measurements/wind_speed_kmh/timestamp_quality"].Should().Be("estimated_date");
            map["measurements/temperature_c/source_timezone"].Should().Be("Europe/Berlin");
            map["measurements/humidity_percent/source_timezone"].Should().Be("Europe/Berlin");
            map["measurements/wind_speed_kmh/source_timezone"].Should().Be("Europe/Berlin");
            map.Keys.Should().NotContain("source_timezone");
        }
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Export_WindPeriod_StartAndEndUtc()
    {
        var (provider, result) = await Parse(
            Overview(OsthessenWetterHtml.OverviewRow("Bimbach", time: "09:31 Uhr", temperature: "20.25", humidity: "93")),
            Wind(OsthessenWetterHtml.WindRow("Bimbach", time: "09:40 Uhr", mean10: "12.5")));

        var dataset = result.Datasets[0];
        var (json, xml) = await Serialize(provider, dataset);
        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);

        foreach (var map in new[] { jsonMap, xmlMap })
        {
            map["measurements/wind_speed_kmh/measured_at"].Should().Be("2026-01-15T08:40:00");
            map["measurements/wind_speed_kmh/period/start"].Should().Be("2026-01-15T08:30:00");
            map["measurements/wind_speed_kmh/period/end"].Should().Be("2026-01-15T08:40:00");
        }

        jsonMap.Keys.Should().NotContain(key => key.StartsWith("measurements/temperature_c/period", StringComparison.Ordinal));
        jsonMap.Keys.Should().NotContain(key => key.StartsWith("measurements/humidity_percent/period", StringComparison.Ordinal));
        xmlMap.Keys.Should().NotContain(key => key.StartsWith("measurements/temperature_c/period", StringComparison.Ordinal));
        xmlMap.Keys.Should().NotContain(key => key.StartsWith("measurements/humidity_percent/period", StringComparison.Ordinal));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Export_UtcForm_NoOffsetNoZ()
    {
        var (provider, result) = await Parse(
            Overview(OsthessenWetterHtml.OverviewRow("Bimbach", time: "09:31 Uhr", temperature: "20.25", humidity: "93")),
            Wind(OsthessenWetterHtml.WindRow("Bimbach", time: "09:40 Uhr", mean10: "12.5")));

        var dataset = result.Datasets[0];
        var (json, xml) = await Serialize(provider, dataset);
        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);

        var utcRegex = new Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$");
        foreach (var map in new[] { jsonMap, xmlMap })
        {
            foreach (var key in map.Keys)
            {
                if (key.EndsWith("measured_at") || key.EndsWith("retrieved_at") || key.EndsWith("/start") || key.EndsWith("/end"))
                {
                    utcRegex.IsMatch(map[key]).Should().BeTrue();
                }
            }

            map["retrieved_at"].Should().Be("2026-01-15T09:00:00");
            map["measured_at"].Should().Be("2026-01-15T08:40:00");
            map["measurements/temperature_c/measured_at"].Should().Be("2026-01-15T08:31:00");
            map["measurements/humidity_percent/measured_at"].Should().Be("2026-01-15T08:31:00");
        }
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Export_OmittedMembers_Absent()
    {
        var (provider, result) = await Parse(
            Overview(OsthessenWetterHtml.OverviewRow("Bimbach", time: "09:31 Uhr", temperature: "20.25", humidity: "93")),
            Wind(OsthessenWetterHtml.WindRow("Bimbach", time: "09:40 Uhr", mean10: "12.5")));

        var dataset = result.Datasets[0];
        var (json, xml) = await Serialize(provider, dataset);
        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);

        foreach (var map in new[] { jsonMap, xmlMap })
        {
            map.Keys.Should().NotContain(key => key.StartsWith("measurements/pressure_hpa/", StringComparison.Ordinal));
            map.Keys.Should().NotContain(key => key.StartsWith("measurements/pressure_mmhg/", StringComparison.Ordinal));
            map.Keys.Should().NotContain(key => key.StartsWith("measurements/wind_direction_deg/", StringComparison.Ordinal));
            map.Keys.Should().NotContain(key => key.StartsWith("measurements/solar_irradiance_w_m2/", StringComparison.Ordinal));
            map.Keys.Should().NotContain(key => key.StartsWith("measurements/rain_rate_mm_h/", StringComparison.Ordinal));
            map.Keys.Should().NotContain(key => key.StartsWith("measurements/illuminance_lux/", StringComparison.Ordinal));
            map.Keys.Should().NotContain(key => key.EndsWith("/quality", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Export_Rounding_TiesAwayFromZero()
    {
        var (provider, result) = await Parse(
            Overview(
                OsthessenWetterHtml.OverviewRow("Bimbach", time: "09:31 Uhr", temperature: "20.25"),
                OsthessenWetterHtml.OverviewRow("Schlitz", time: "09:31 Uhr", temperature: "-20.25")),
            Wind(OsthessenWetterHtml.WindRow("Bimbach", time: "09:40 Uhr", mean10: "12.5")));

        var dataset = result.Datasets[0];
        var (json, xml) = await Serialize(provider, dataset);
        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);

        foreach (var map in new[] { jsonMap, xmlMap })
        {
            map["measurements/temperature_c/value"].Should().Be("20.3");
            map["measurements/wind_speed_kmh/value"].Should().Be("13");
        }

        var schlitz = result.Datasets[1];
        var (json2, xml2) = await Serialize(provider, schlitz);
        var jsonMap2 = JsonMap(json2);
        var xmlMap2 = XmlMap(xml2);

        foreach (var map in new[] { jsonMap2, xmlMap2 })
        {
            map["measurements/temperature_c/value"].Should().Be("-20.3");
        }
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Export_Humidity_BoundariesKept()
    {
        var (provider, result) = await Parse(
            Overview(
                OsthessenWetterHtml.OverviewRow("Bimbach", time: "09:31 Uhr", humidity: "0"),
                OsthessenWetterHtml.OverviewRow("Schlitz", time: "09:31 Uhr", humidity: "100")),
            Wind(OsthessenWetterHtml.WindRow("Bimbach", time: "09:40 Uhr", mean10: "12.5")));

        var dataset = result.Datasets[0];
        var (json, xml) = await Serialize(provider, dataset);
        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);

        foreach (var map in new[] { jsonMap, xmlMap })
        {
            map["measurements/humidity_percent/value"].Should().Be("0");
        }

        var schlitz = result.Datasets[1];
        var (json2, xml2) = await Serialize(provider, schlitz);
        var jsonMap2 = JsonMap(json2);
        var xmlMap2 = XmlMap(xml2);

        foreach (var map in new[] { jsonMap2, xmlMap2 })
        {
            map["measurements/humidity_percent/value"].Should().Be("100");
        }
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Export_Umlauts_JsonAndXmlEscapedCorrectly()
    {
        var (provider, result) = await Parse(
            Overview(
                OsthessenWetterHtml.OverviewRow("M\u00fcs-Hohh\u00fctte", time: "09:31 Uhr", temperature: "20.25", humidity: "93"),
                OsthessenWetterHtml.OverviewRow("Ro\u00dfbach", time: "09:31 Uhr", temperature: "20.25", humidity: "93")),
            Wind(OsthessenWetterHtml.WindRow("M\u00fcs-Hohh\u00fctte", time: "09:40 Uhr", mean10: "12.5")));

        var dataset = result.Datasets[0];
        var (json, xml) = await Serialize(provider, dataset);
        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);

        foreach (var map in new[] { jsonMap, xmlMap })
        {
            map["location/name"].Should().Be("M\u00fcs-Hohh\u00fctte");
            map["source"].Should().Be("osthessenwetter:mues-hohhuette");
        }

        var dataset2 = result.Datasets[1];
        var (json2, xml2) = await Serialize(provider, dataset2);
        var jsonMap2 = JsonMap(json2);
        var xmlMap2 = XmlMap(xml2);

        foreach (var map in new[] { jsonMap2, xmlMap2 })
        {
            map["location/name"].Should().Be("Ro\u00dfbach");
            map["source"].Should().Be("osthessenwetter:rossbach");
        }

        ExportSchemaValidation.Validate(xml).Should().BeEmpty();
        ExportSchemaValidation.Validate(xml2).Should().BeEmpty();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Export_SeveralDatasets_EachSerializedSeparately()
    {
        var (provider, result) = await Parse(
            Overview(
                OsthessenWetterHtml.OverviewRow("Bimbach", time: "09:31 Uhr", temperature: "20.25", humidity: "93"),
                OsthessenWetterHtml.OverviewRow("Schlitz", time: "09:31 Uhr", temperature: "20.25", humidity: "93"),
                OsthessenWetterHtml.OverviewRow("Burghaun", time: "09:31 Uhr", temperature: "20.25", humidity: "93")),
            Wind(OsthessenWetterHtml.WindRow("Bimbach", time: "09:40 Uhr", mean10: "12.5")));

        result.Datasets.Count.Should().Be(3);

        var jsonSources = new List<string>();
        var xmlSources = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var (json, xml) = await Serialize(provider, result.Datasets[i]);
            using (var document = JsonDocument.Parse(json))
            {
                document.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
            }

            XDocument.Parse(Encoding.UTF8.GetString(xml)).Root!.Name.LocalName.Should().Be("observation_dataset");
            jsonSources.Add(JsonMap(json)["source"]);
            xmlSources.Add(XmlMap(xml)["source"]);
        }

        var expected = new[] { "osthessenwetter:bimbach", "osthessenwetter:schlitz", "osthessenwetter:burghaun" };
        jsonSources.Should().Equal(expected);
        xmlSources.Should().Equal(expected);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Export_SameGraph_NoSecondRetrieval()
    {
        var (provider, result) = await Parse(
            Overview(OsthessenWetterHtml.OverviewRow("Bimbach", time: "09:31 Uhr", temperature: "20.25", humidity: "93")),
            Wind(OsthessenWetterHtml.WindRow("Bimbach", time: "09:40 Uhr", mean10: "12.5")));

        var dataset = result.Datasets[0];
        _sends.Should().Be(1);

        await Serialize(provider, dataset);
        _sends.Should().Be(1);

        await Serialize(provider, dataset);
        _sends.Should().Be(1);
    }
}
