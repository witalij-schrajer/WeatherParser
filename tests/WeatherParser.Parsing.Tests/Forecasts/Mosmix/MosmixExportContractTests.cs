using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Export;
using WeatherParser.Parsing.Tests.Schemas;
using WeatherParser.Parsing.Tests.Selection;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public sealed class MosmixExportContractTests
{
    private MockRepository _mocks = null!;
    private Mock<HttpMessageHandler> _handler = null!;
    private HttpClient _http = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<MosmixLParser> _lLogger = null!;
    private CapturingLogger<MosmixSParser> _sLogger = null!;
    private readonly List<Uri> _lRequests = new();
    private readonly List<Uri> _sRequests = new();
    private Func<HttpResponseMessage> _lResponse = null!;
    private Func<HttpResponseMessage> _sResponse = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _handler = _mocks.Create<HttpMessageHandler>();
        _http = new HttpClient(_handler.Object, disposeHandler: false);
        _clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 26, 16, 16, 30, TimeSpan.Zero));
        _lLogger = new();
        _sLogger = new();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _http.Dispose();
        _mocks.VerifyAll();
    }

    private ServiceProvider Provider(TimeSpan? lMaxRunAge = null, TimeSpan? sMaxRunAge = null)
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.AddSingleton(_http);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://l.example.org/mosmix/single_stations/") });
        services.AddSingleton(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = lMaxRunAge ?? TimeSpan.FromHours(8) });
        services.AddSingleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://s.example.org/mosmix/MOSMIX_S_LATEST_240.kmz") });
        services.AddSingleton(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = sMaxRunAge ?? TimeSpan.FromHours(2) });
        services.AddSingleton<ILogger<MosmixLParser>>(_lLogger);
        services.AddSingleton<ILogger<MosmixSParser>>(_sLogger);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private void SetupResponses()
    {
        _handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback((HttpRequestMessage request, CancellationToken _) =>
            {
                if (request.RequestUri!.Host == "l.example.org")
                {
                    _lRequests.Add(request.RequestUri);
                }
                else if (request.RequestUri!.Host == "s.example.org")
                {
                    _sRequests.Add(request.RequestUri);
                }
            })
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                if (request.RequestUri!.Host == "l.example.org")
                {
                    return _lResponse();
                }
                else if (request.RequestUri!.Host == "s.example.org")
                {
                    return _sResponse();
                }

                throw new InvalidOperationException("Unexpected host");
            });
    }

    private static string Ttt => MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15");

    private static string Forecast(string name, string values) => MosmixForecastKmzBuilder.Forecast(name, values);

    private static readonly string[] Full = new[]
    {
        Ttt,
        Forecast("PPPP", "100050 100100 100150"),
        Forecast("DD", "0 180 360"),
        Forecast("FF", "0.00 5.00 10.00"),
        Forecast("FX1", "0.00 12.50 25.00"),
        Forecast("Rad1h", "0.00 1800.00 3600.00"),
        Forecast("RR1c", "0.00 0.50 1.25")
    };

    private static HttpResponseMessage L(string id, string description, params string[] forecasts)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single(id, description, "9.6,50.5,250.0", forecasts)))
        };
    }

    private static HttpResponseMessage S(string id, string description, params string[] forecasts)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(MosmixForecastKmzBuilder.Kmz(
                MosmixForecastKmzBuilder.Document(
                    MosmixForecastKmzBuilder.ProductDefinition(),
                    MosmixForecastKmzBuilder.Placemark("P0001", "OTHER", "8.0,51.0,100.0", Ttt),
                    MosmixForecastKmzBuilder.Placemark(id, description, "9.6,50.5,250.0", forecasts)),
                memberName: MosmixForecastKmzBuilder.AllStationsMemberName))
        };
    }

    private static async Task<(byte[] Json, byte[] Xml)> Serialize(ServiceProvider provider, ForecastRun run)
    {
        var json = provider.GetRequiredService<IJsonExportSerializer>();
        var xml = provider.GetRequiredService<IXmlExportSerializer>();
        using var jsonStream = new MemoryStream();
        using var xmlStream = new MemoryStream();
        await json.SerializeAsync(run, jsonStream, CancellationToken.None);
        await xml.SerializeAsync(run, xmlStream, CancellationToken.None);
        var jsonBytes = jsonStream.ToArray();
        var xmlBytes = xmlStream.ToArray();
        ExportSchemaValidation.Validate(xmlBytes).Should().BeEmpty();
        return (jsonBytes, xmlBytes);
    }

    private static void AssertEquivalent(byte[] json, byte[] xml)
    {
        using var document = JsonDocument.Parse(json);
        ExportEquivalence.AssertEquivalent(document.RootElement, XDocument.Parse(Encoding.UTF8.GetString(xml)).Root!, "forecast_run");
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

    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    private static JsonElement JsonRoot(byte[] json) => JsonDocument.Parse(json).RootElement.Clone();

    private static JsonElement JsonStepValues(byte[] json, int index) => JsonRoot(json).GetProperty("forecasts")[index].GetProperty("values");

    private static XElement XmlRoot(byte[] xml) => XDocument.Parse(Encoding.UTF8.GetString(xml)).Root!;

    private static XElement XmlStepValues(byte[] xml, int index) => XmlRoot(xml).Element(Ns + "forecasts")!.Elements().ElementAt(index).Element(Ns + "values")!;

    private static string JsonText(JsonElement e, string name) => e.GetProperty(name).ValueKind == JsonValueKind.String ? e.GetProperty(name).GetString()! : e.GetProperty(name).GetRawText();

    private static string XmlText(XElement e, string name) => e.Element(Ns + name)!.Value;

    private static async Task<ForecastRun> ParseL(ServiceProvider provider, string id)
    {
        var l = provider.GetRequiredService<IMosmixLParser>();
        return (await l.ParseAsync(id, CancellationToken.None)).Run;
    }

    private static async Task<ForecastRun> ParseS(ServiceProvider provider, string id)
    {
        var s = provider.GetRequiredService<IMosmixSParser>();
        return (await s.ParseAsync(id, CancellationToken.None)).Run;
    }

    private async Task<ForecastRun> Parse(ServiceProvider provider, MosmixProduct product, string id, string description, params string[] forecasts)
    {
        if (product == MosmixProduct.MosmixS) { _sResponse = () => S(id, description, forecasts); } else { _lResponse = () => L(id, description, forecasts); }
        SetupResponses();
        return product == MosmixProduct.MosmixS ? await ParseS(provider, id) : await ParseL(provider, id);
    }

    private static string SourceOf(MosmixProduct product, string id) => (product == MosmixProduct.MosmixS ? "dwd-mosmix-s:" : "dwd-mosmix-l:") + id;

    [TestMethod]
    [Timeout(20000)]
    public async Task Export_MosmixLRun_JsonXmlEquivalent()
    {
        using var provider = Provider();
        _lResponse = () => L("10637", "BEISPIELSTADT", Full);
        SetupResponses();

        var run = await ParseL(provider, "10637");
        var (json, xml) = await Serialize(provider, run);
        AssertEquivalent(json, xml);

        JsonMap(json)["product"].Should().Be("MOSMIX_L");
        JsonMap(json)["source"].Should().Be(SourceOf(MosmixProduct.MosmixL, "10637"));
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task Export_MosmixSRun_JsonXmlEquivalent()
    {
        using var provider = Provider();
        _sResponse = () => S("10637", "BEISPIELSTADT", Full);
        SetupResponses();

        var run = await ParseS(provider, "10637");
        var (json, xml) = await Serialize(provider, run);
        AssertEquivalent(json, xml);

        JsonMap(json)["product"].Should().Be("MOSMIX_S");
        JsonMap(json)["source"].Should().Be(SourceOf(MosmixProduct.MosmixS, "10637"));
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task Export_Runs_SchemaValid()
    {
        using var provider = Provider();
        _lResponse = () => L("10637", "BEISPIELSTADT", Full);
        _sResponse = () => S("10637", "BEISPIELSTADT", Full);
        SetupResponses();

        var lRun = await ParseL(provider, "10637");
        var sRun = await ParseS(provider, "10637");

        var (lJson, lXml) = await Serialize(provider, lRun);
        var (sJson, sXml) = await Serialize(provider, sRun);

        ExportSchemaValidation.Validate(lXml).Should().BeEmpty();
        ExportSchemaValidation.Validate(sXml).Should().BeEmpty();
    }

    [DataRow(MosmixProduct.MosmixL)]
    [DataRow(MosmixProduct.MosmixS)]
    [TestMethod]
    [Timeout(20000)]
    public async Task Export_Times_UtcWithoutSuffix(MosmixProduct product)
    {
        using var provider = Provider();
        var run = await Parse(provider, product, "10637", "BEISPIELSTADT", Full);
        var (json, xml) = await Serialize(provider, run);

        var jsonRoot = JsonRoot(json);
        var xmlRoot = XmlRoot(xml);

        JsonMap(json)["issued_at"].Should().Be("2026-10-26T15:00:00");
        XmlMap(xml)["issued_at"].Should().Be("2026-10-26T15:00:00");

        JsonMap(json)["retrieved_at"].Should().Be("2026-10-26T16:16:30");
        XmlMap(xml)["retrieved_at"].Should().Be("2026-10-26T16:16:30");

        jsonRoot.GetProperty("forecasts")[0].GetProperty("valid_at").GetString().Should().Be("2026-10-26T16:00:00");
        xmlRoot.Element(Ns + "forecasts")!.Elements().First().Element(Ns + "valid_at")!.Value.Should().Be("2026-10-26T16:00:00");

        foreach (var map in new[] { JsonMap(json), XmlMap(xml) })
        {
            foreach (var key in map.Keys)
            {
                if (key.EndsWith("measured_at") || key.EndsWith("retrieved_at") || key.EndsWith("/start") || key.EndsWith("/end"))
                {
                    map[key].Should().NotEndWith("Z");
                    map[key].Should().NotContain("+00:00");
                }
            }
        }
    }

    [DataRow(MosmixProduct.MosmixL)]
    [DataRow(MosmixProduct.MosmixS)]
    [TestMethod]
    [Timeout(20000)]
    public async Task Export_NoMeasuredAtAnywhere(MosmixProduct product)
    {
        using var provider = Provider();
        var run = await Parse(provider, product, "10637", "BEISPIELSTADT", Full);
        var (json, xml) = await Serialize(provider, run);

        var jsonText = Encoding.UTF8.GetString(json);
        var xmlText = Encoding.UTF8.GetString(xml);

        jsonText.Should().NotContain("measured_at");
        xmlText.Should().NotContain("measured_at");
    }

    [DataRow(MosmixProduct.MosmixL)]
    [DataRow(MosmixProduct.MosmixS)]
    [TestMethod]
    [Timeout(20000)]
    public async Task Export_OmittedValues_AbsentBothFormats(MosmixProduct product)
    {
        using var provider = Provider();
        var run = await Parse(provider, product, "10637", "BEISPIELSTADT", Ttt, Forecast("FF", "- 5.00 -"));
        var (json, xml) = await Serialize(provider, run);
        
        // Steps 0 and 2 have no wind_speed_kmh
        foreach (var i in new[] { 0, 2 })
        {
            JsonStepValues(json, i).TryGetProperty("wind_speed_kmh", out _).Should().BeFalse();
            XmlStepValues(xml, i).Element(Ns + "wind_speed_kmh").Should().BeNull();
        }

        // Step 1 should have wind_speed_kmh
        JsonText(JsonStepValues(json, 1), "wind_speed_kmh").Should().Be("18");
        XmlText(XmlStepValues(xml, 1), "wind_speed_kmh").Should().Be("18");

        AssertEquivalent(json, xml);
    }

    [DataRow(MosmixProduct.MosmixL)]
    [DataRow(MosmixProduct.MosmixS)]
    [TestMethod]
    [Timeout(20000)]
    public async Task Export_Zeros_Kept(MosmixProduct product)
    {
        using var provider = Provider();
        var run = await Parse(provider, product, "10637", "BEISPIELSTADT", Full);
        var (json, xml) = await Serialize(provider, run);
        AssertEquivalent(json, xml);

        var jsonRoot = JsonRoot(json);
        var xmlRoot = XmlRoot(xml);

        // Check step 0 values
        var j = JsonStepValues(json, 0);
        var x = XmlStepValues(xml, 0);

        JsonText(j, "wind_speed_kmh").Should().Be("0");
        JsonText(j, "wind_direction_deg").Should().Be("0");
        XmlText(x, "wind_speed_kmh").Should().Be("0");
        XmlText(x, "wind_direction_deg").Should().Be("0");

        JsonText(j.GetProperty("rain_rate_mm_h"), "value").Should().Be("0");
        JsonText(j.GetProperty("precipitation_amount_mm"), "value").Should().Be("0");
        JsonText(j.GetProperty("solar_irradiance_w_m2"), "value").Should().Be("0");

        XmlText(x.Element(Ns + "rain_rate_mm_h")!, "value").Should().Be("0");
        XmlText(x.Element(Ns + "precipitation_amount_mm")!, "value").Should().Be("0");
        XmlText(x.Element(Ns + "solar_irradiance_w_m2")!, "value").Should().Be("0");
    }

    [DataRow(MosmixProduct.MosmixL, "01001")]
    [DataRow(MosmixProduct.MosmixL, "X123A")]
    [DataRow(MosmixProduct.MosmixS, "01001")]
    [DataRow(MosmixProduct.MosmixS, "X123A")]
    [TestMethod]
    [Timeout(20000)]
    public async Task Export_IdsLeadingZeroAndAlphanumeric_Kept(MosmixProduct product, string id)
    {
        using var provider = Provider();
        var run = await Parse(provider, product, id, "BEISPIELSTADT", Full);
        var (json, xml) = await Serialize(provider, run);
        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);

        jsonMap["mosmix_id"].Should().Be(id);
        xmlMap["mosmix_id"].Should().Be(id);
        jsonMap["source"].Should().Be(SourceOf(product, id));
        xmlMap["source"].Should().Be(SourceOf(product, id));
    }

    [DataRow(MosmixProduct.MosmixL)]
    [DataRow(MosmixProduct.MosmixS)]
    [TestMethod]
    [Timeout(20000)]
    public async Task Export_NameWithUmlautsAndXmlSpecialCharacters_EscapedAndEqual(MosmixProduct product)
    {
        using var provider = Provider();
        var run = await Parse(provider, product, "10637", "Gr\u00f6\u00dfenwahn &amp; &lt;Co&gt; \"Test\"", Full);
        var (json, xml) = await Serialize(provider, run);
        AssertEquivalent(json, xml);

        var jsonRoot = JsonRoot(json);
        var xmlRoot = XmlRoot(xml);

        jsonRoot.GetProperty("location").GetProperty("name").GetString().Should().Be("Gr\u00f6\u00dfenwahn & <Co> \"Test\"");
        xmlRoot.Element(Ns + "location")!.Element(Ns + "name")!.Value.Should().Be("Gr\u00f6\u00dfenwahn & <Co> \"Test\"");

        var xmlText = Encoding.UTF8.GetString(xml);
        xmlText.Should().Contain("&amp;");
        xmlText.Should().Contain("&lt;Co&gt;");

        ExportSchemaValidation.Validate(xml).Should().BeEmpty();
    }

    [DataRow(MosmixProduct.MosmixL)]
    [DataRow(MosmixProduct.MosmixS)]
    [TestMethod]
    [Timeout(20000)]
    public async Task Export_IntervalValues_PeriodAndAggregation(MosmixProduct product)
    {
        using var provider = Provider();
        var run = await Parse(provider, product, "10637", "BEISPIELSTADT", Full);
        var (json, xml) = await Serialize(provider, run);
        AssertEquivalent(json, xml);

        // Step 1 values
        var j = JsonStepValues(json, 1);
        var x = XmlStepValues(xml, 1);

        // wind_gust_kmh
        JsonText(j.GetProperty("wind_gust_kmh"), "value").Should().Be("45");
        XmlText(x.Element(Ns + "wind_gust_kmh")!, "value").Should().Be("45");
        JsonText(j.GetProperty("wind_gust_kmh"), "aggregation").Should().Be("maximum");
        XmlText(x.Element(Ns + "wind_gust_kmh")!, "aggregation").Should().Be("maximum");
        j.GetProperty("wind_gust_kmh").GetProperty("period_start").GetString().Should().Be("2026-10-26T16:00:00");
        x.Element(Ns + "wind_gust_kmh")!.Element(Ns + "period_start")!.Value.Should().Be("2026-10-26T16:00:00");
        j.GetProperty("wind_gust_kmh").GetProperty("period_end").GetString().Should().Be("2026-10-26T17:00:00");
        x.Element(Ns + "wind_gust_kmh")!.Element(Ns + "period_end")!.Value.Should().Be("2026-10-26T17:00:00");

        // precipitation_amount_mm
        JsonText(j.GetProperty("precipitation_amount_mm"), "value").Should().Be("0.5");
        XmlText(x.Element(Ns + "precipitation_amount_mm")!, "value").Should().Be("0.5");
        JsonText(j.GetProperty("precipitation_amount_mm"), "aggregation").Should().Be("sum");
        XmlText(x.Element(Ns + "precipitation_amount_mm")!, "aggregation").Should().Be("sum");
        j.GetProperty("precipitation_amount_mm").GetProperty("period_start").GetString().Should().Be("2026-10-26T16:00:00");
        x.Element(Ns + "precipitation_amount_mm")!.Element(Ns + "period_start")!.Value.Should().Be("2026-10-26T16:00:00");
        j.GetProperty("precipitation_amount_mm").GetProperty("period_end").GetString().Should().Be("2026-10-26T17:00:00");
        x.Element(Ns + "precipitation_amount_mm")!.Element(Ns + "period_end")!.Value.Should().Be("2026-10-26T17:00:00");

        // rain_rate_mm_h
        JsonText(j.GetProperty("rain_rate_mm_h"), "value").Should().Be("0.5");
        XmlText(x.Element(Ns + "rain_rate_mm_h")!, "value").Should().Be("0.5");
        JsonText(j.GetProperty("rain_rate_mm_h"), "aggregation").Should().Be("mean");
        XmlText(x.Element(Ns + "rain_rate_mm_h")!, "aggregation").Should().Be("mean");
        j.GetProperty("rain_rate_mm_h").GetProperty("period_start").GetString().Should().Be("2026-10-26T16:00:00");
        x.Element(Ns + "rain_rate_mm_h")!.Element(Ns + "period_start")!.Value.Should().Be("2026-10-26T16:00:00");
        j.GetProperty("rain_rate_mm_h").GetProperty("period_end").GetString().Should().Be("2026-10-26T17:00:00");
        x.Element(Ns + "rain_rate_mm_h")!.Element(Ns + "period_end")!.Value.Should().Be("2026-10-26T17:00:00");

        // solar_irradiance_w_m2
        JsonText(j.GetProperty("solar_irradiance_w_m2"), "value").Should().Be("500");
        XmlText(x.Element(Ns + "solar_irradiance_w_m2")!, "value").Should().Be("500");
        JsonText(j.GetProperty("solar_irradiance_w_m2"), "aggregation").Should().Be("mean");
        XmlText(x.Element(Ns + "solar_irradiance_w_m2")!, "aggregation").Should().Be("mean");
        j.GetProperty("solar_irradiance_w_m2").GetProperty("period_start").GetString().Should().Be("2026-10-26T16:00:00");
        x.Element(Ns + "solar_irradiance_w_m2")!.Element(Ns + "period_start")!.Value.Should().Be("2026-10-26T16:00:00");
        j.GetProperty("solar_irradiance_w_m2").GetProperty("period_end").GetString().Should().Be("2026-10-26T17:00:00");
        x.Element(Ns + "solar_irradiance_w_m2")!.Element(Ns + "period_end")!.Value.Should().Be("2026-10-26T17:00:00");
    }

    [DataRow(MosmixProduct.MosmixL, false)]
    [DataRow(MosmixProduct.MosmixL, true)]
    [DataRow(MosmixProduct.MosmixS, false)]
    [DataRow(MosmixProduct.MosmixS, true)]
    [TestMethod]
    [Timeout(20000)]
    public async Task Export_StaleMarker_AppendedAndEqual(MosmixProduct product, bool stale)
    {
        using var provider = Provider(stale ? TimeSpan.FromHours(1) : null, stale && product == MosmixProduct.MosmixS ? TimeSpan.FromHours(1) : null);
        var run = await Parse(provider, product, "10637", "BEISPIELSTADT", Full);
        var (json, xml) = await Serialize(provider, run);
        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);

        jsonMap["run_age_exceeded"].Should().Be(stale ? "true" : "false");
        xmlMap["run_age_exceeded"].Should().Be(stale ? "true" : "false");

        var names = JsonRoot(json).EnumerateObject().Select(p => p.Name).ToList();
        names[^2].Should().Be("forecasts");
        names[^1].Should().Be("run_age_exceeded");
        var elements = XmlRoot(xml).Elements().Select(e => e.Name.LocalName).ToList();
        elements[^2].Should().Be("forecasts");
        elements[^1].Should().Be("run_age_exceeded");
        AssertEquivalent(json, xml);
    }

    [DataRow(MosmixProduct.MosmixL)]
    [DataRow(MosmixProduct.MosmixS)]
    [TestMethod]
    [Timeout(20000)]
    public async Task Export_PressureReferenceUnknown_BothFormats(MosmixProduct product)
    {
        using var provider = Provider();
        var run = await Parse(provider, product, "10637", "BEISPIELSTADT", Ttt, Forecast("PPPP", "100050 100100 100150"));
        var (json, xml) = await Serialize(provider, run);
        AssertEquivalent(json, xml);

        // Step 1 values
        JsonText(JsonStepValues(json, 1), "pressure_hpa").Should().Be("1001");
        XmlText(XmlStepValues(xml, 1), "pressure_hpa").Should().Be("1001");

        JsonText(JsonStepValues(json, 1), "pressure_mmhg").Should().Be("751");
        XmlText(XmlStepValues(xml, 1), "pressure_mmhg").Should().Be("751");

        JsonText(JsonStepValues(json, 1), "pressure_reference").Should().Be("unknown");
        XmlText(XmlStepValues(xml, 1), "pressure_reference").Should().Be("unknown");
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task Export_SourcesDistinct_SAndL()
    {
        using var provider = Provider();
        _lResponse = () => L("10637", "BEISPIELSTADT", Full);
        _sResponse = () => S("10637", "BEISPIELSTADT", Full);
        SetupResponses();

        var lRun = await ParseL(provider, "10637");
        var sRun = await ParseS(provider, "10637");

        var (lJson, lXml) = await Serialize(provider, lRun);
        var (sJson, sXml) = await Serialize(provider, sRun);

        var lJsonMap = JsonMap(lJson);
        var sJsonMap = JsonMap(sJson);

        lJsonMap["source"].Should().Be("dwd-mosmix-l:10637");
        sJsonMap["source"].Should().Be("dwd-mosmix-s:10637");

        lJsonMap["product"].Should().Be("MOSMIX_L");
        sJsonMap["product"].Should().Be("MOSMIX_S");
    }

    [DataRow(MosmixProduct.MosmixL)]
    [DataRow(MosmixProduct.MosmixS)]
    [TestMethod]
    [Timeout(20000)]
    public async Task Export_RoundingOnWire_TiesAwayFromZero(MosmixProduct product)
    {
        using var provider = Provider();
        var run = await Parse(provider, product, "10637", "BEISPIELSTADT", Forecast("TTT", "293.40 293.45 293.35"));
        var (json, xml) = await Serialize(provider, run);
        AssertEquivalent(json, xml);

        JsonText(JsonStepValues(json, 0), "temperature_c").Should().Be("20.3");
        JsonText(JsonStepValues(json, 1), "temperature_c").Should().Be("20.3");
        JsonText(JsonStepValues(json, 2), "temperature_c").Should().Be("20.2");

        XmlText(XmlStepValues(xml, 0), "temperature_c").Should().Be("20.3");
        XmlText(XmlStepValues(xml, 1), "temperature_c").Should().Be("20.3");
        XmlText(XmlStepValues(xml, 2), "temperature_c").Should().Be("20.2");
    }

    [DataRow(MosmixProduct.MosmixL)]
    [DataRow(MosmixProduct.MosmixS)]
    [TestMethod]
    [Timeout(20000)]
    public async Task Export_SameRunTwice_ByteIdentical(MosmixProduct product)
    {
        using var provider = Provider();
        var run = await Parse(provider, product, "10637", "BEISPIELSTADT", Full);
        var (json1, xml1) = await Serialize(provider, run);
        var (json2, xml2) = await Serialize(provider, run);

        json1.Should().Equal(json2);
        xml1.Should().Equal(xml2);
    }

    [DataRow(MosmixProduct.MosmixL)]
    [DataRow(MosmixProduct.MosmixS)]
    [TestMethod]
    [Timeout(20000)]
    public async Task Export_NoSecondRetrieval(MosmixProduct product)
    {
        using var provider = Provider();
        var run = await Parse(provider, product, "10637", "BEISPIELSTADT", Full);
        var (json, xml) = await Serialize(provider, run);
        var (json2, xml2) = await Serialize(provider, run);

        if (product == MosmixProduct.MosmixL)
        {
            _lRequests.Count.Should().Be(1);
            _sRequests.Count.Should().Be(0);
        }
        else
        {
            _lRequests.Count.Should().Be(0);
            _sRequests.Count.Should().Be(1);
        }
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task Export_EnvelopeWithBothProducts_EquivalentAndValid()
    {
        using var provider = Provider();
        _lResponse = () => L("10637", "BEISPIELSTADT", Full);
        _sResponse = () => S("10637", "BEISPIELSTADT", Full);
        SetupResponses();

        var lRun = await ParseL(provider, "10637");
        var sRun = await ParseS(provider, "10637");

        var envelope = new ExportDocument { ForecastRuns = new List<ForecastRun> { sRun, lRun } };

        var json = provider.GetRequiredService<IJsonExportSerializer>();
        var xml = provider.GetRequiredService<IXmlExportSerializer>();
        using var jsonStream = new MemoryStream();
        using var xmlStream = new MemoryStream();
        await json.SerializeAsync(envelope, jsonStream, CancellationToken.None);
        await xml.SerializeAsync(envelope, xmlStream, CancellationToken.None);

        var jsonBytes = jsonStream.ToArray();
        var xmlBytes = xmlStream.ToArray();

        var document = JsonDocument.Parse(jsonBytes);
        ExportEquivalence.AssertEquivalent(document.RootElement, XDocument.Parse(Encoding.UTF8.GetString(xmlBytes)).Root!, "export_document");

        ExportSchemaValidation.Validate(xmlBytes).Should().BeEmpty();

        var jsonMap = JsonMap(jsonBytes);

        // Check that the forecast_runs are separate
        jsonMap.Keys.Should().Contain("forecast_runs");

        // Check that the sources are in order
        JsonRoot(jsonBytes).GetProperty("forecast_runs").GetArrayLength().Should().Be(2);
        JsonRoot(jsonBytes).GetProperty("forecast_runs")[0].GetProperty("source").GetString().Should().Be("dwd-mosmix-s:10637");
        JsonRoot(jsonBytes).GetProperty("forecast_runs")[1].GetProperty("source").GetString().Should().Be("dwd-mosmix-l:10637");

        var xmlRoot = XmlRoot(xmlBytes);
        xmlRoot.Element(Ns + "forecast_runs")!.Elements().Count().Should().Be(2);
        xmlRoot.Element(Ns + "forecast_runs")!.Elements().ElementAt(0).Element(Ns + "source")!.Value.Should().Be("dwd-mosmix-s:10637");
        xmlRoot.Element(Ns + "forecast_runs")!.Elements().ElementAt(1).Element(Ns + "source")!.Value.Should().Be("dwd-mosmix-l:10637");
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task Export_EnvelopeWithObservationsAndForecasts_KindsSeparate()
    {
        using var provider = Provider();
        _lResponse = () => L("10637", "BEISPIELSTADT", Full);
        SetupResponses();

        var lRun = await ParseL(provider, "10637");

        var observation = AllRootsTestData.RichDataset();

        var envelope = new ExportDocument
        {
            Observations = new List<ObservationDataset> { observation },
            ForecastRuns = new List<ForecastRun> { lRun }
        };

        var json = provider.GetRequiredService<IJsonExportSerializer>();
        var xml = provider.GetRequiredService<IXmlExportSerializer>();
        using var jsonStream = new MemoryStream();
        using var xmlStream = new MemoryStream();
        await json.SerializeAsync(envelope, jsonStream, CancellationToken.None);
        await xml.SerializeAsync(envelope, xmlStream, CancellationToken.None);

        var jsonBytes = jsonStream.ToArray();
        var xmlBytes = xmlStream.ToArray();

        var document = JsonDocument.Parse(jsonBytes);
        ExportEquivalence.AssertEquivalent(document.RootElement, XDocument.Parse(Encoding.UTF8.GetString(xmlBytes)).Root!, "export_document");

        ExportSchemaValidation.Validate(xmlBytes).Should().BeEmpty();

        var root = JsonRoot(jsonBytes);
        root.GetProperty("observations").GetArrayLength().Should().Be(1);
        root.GetProperty("forecast_runs").GetArrayLength().Should().Be(1);
        var forecast = root.GetProperty("forecast_runs")[0];
        forecast.TryGetProperty("issued_at", out _).Should().BeTrue();
        forecast.TryGetProperty("measured_at", out _).Should().BeFalse();
        var observationElement = root.GetProperty("observations")[0];
        observationElement.TryGetProperty("issued_at", out _).Should().BeFalse();
        
        XmlRoot(xmlBytes).Element(Ns + "observations")!.Elements().Should().HaveCount(1);
        XmlRoot(xmlBytes).Element(Ns + "forecast_runs")!.Elements().Should().HaveCount(1);
    }
}
