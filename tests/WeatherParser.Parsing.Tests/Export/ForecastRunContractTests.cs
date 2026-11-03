using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ForecastRunContractTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    private static ForecastRun BuildRun()
    {
        return new ForecastRun
        {
            Source = "dwd-mosmix-l:10637",
            MosmixId = "10637",
            Location = new ForecastLocation
            {
                Name = "FRANKFURT/M",
                Latitude = 50.05m,
                Longitude = 8.6m,
                AltitudeM = 111m
            },
            Product = MosmixProduct.MosmixL,
            IssuedAtUtc = Utc(9, 0),
            RetrievedAtUtc = Utc(10, 12),
            Forecasts =
            {
                new ForecastStep
                {
                    ValidAtUtc = Utc(12, 0),
                    Values = new ForecastValues
                    {
                        TemperatureC = 21.4m,
                        PressureHpa = 1013m,
                        PressureMmHg = 760m,
                        PressureReference = PressureReference.SeaLevel,
                        RainRateMmH = new IntervalValue
                        {
                            Value = 0.3m,
                            PeriodStartUtc = Utc(11, 0),
                            PeriodEndUtc = Utc(12, 0),
                            Aggregation = IntervalAggregation.Mean
                        }
                    }
                },
                new ForecastStep
                {
                    ValidAtUtc = Utc(14, 0),
                    Values = new ForecastValues
                    {
                        TemperatureC = 23.0m
                    }
                }
            }
        };
    }

    [TestMethod]
    public async Task Serialize_ForecastRun_WritesRequiredMetadataInBothFormats()
    {
        var run = BuildRun();
        using var jsonStream = new MemoryStream();
        await _json.SerializeAsync(run, jsonStream, CancellationToken.None);
        jsonStream.Position = 0;
        using var jsonDoc = JsonDocument.Parse(jsonStream);
        var root = jsonDoc.RootElement;

        root.GetProperty("schema_version").GetString().Should().Be("1");
        root.GetProperty("dataset_type").GetString().Should().Be("forecast");
        root.GetProperty("source").GetString().Should().Be("dwd-mosmix-l:10637");
        root.GetProperty("mosmix_id").GetString().Should().Be("10637");
        root.GetProperty("location").GetProperty("name").GetString().Should().Be("FRANKFURT/M");
        root.GetProperty("product").GetString().Should().Be("MOSMIX_L");
        root.GetProperty("issued_at").GetString().Should().Be("2026-07-15T09:00:00");
        root.GetProperty("retrieved_at").GetString().Should().Be("2026-07-15T10:12:00");

        var forecasts = root.GetProperty("forecasts");
        forecasts.GetArrayLength().Should().Be(2);

        var forecast0 = forecasts[0];
        forecast0.GetProperty("valid_at").GetString().Should().Be("2026-07-15T12:00:00");
        forecast0.GetProperty("values").GetProperty("temperature_c").GetDecimal().Should().Be(21.4m);
        forecast0.GetProperty("values").GetProperty("pressure_reference").GetString().Should().Be("sea_level");

        var rainRate = forecast0.GetProperty("values").GetProperty("rain_rate_mm_h");
        rainRate.GetProperty("period_start").GetString().Should().Be("2026-07-15T11:00:00");
        rainRate.GetProperty("aggregation").GetString().Should().Be("mean");

        var forecast1 = forecasts[1];
        forecast1.GetProperty("values").TryGetProperty("pressure_hpa", out _).Should().BeFalse();
        forecast1.GetProperty("values").TryGetProperty("rain_rate_mm_h", out _).Should().BeFalse();
        using var xmlStream = new MemoryStream();
        await _xml.SerializeAsync(run, xmlStream, CancellationToken.None);
        xmlStream.Position = 0;
        var xmlDoc = XDocument.Load(xmlStream);

        xmlDoc.Root.Should().NotBeNull();
        xmlDoc.Root.Name.LocalName.Should().Be("forecast_run");
        xmlDoc.Root.Name.Namespace.Should().Be(Ns);

        var rootElement = xmlDoc.Root;
        rootElement.Element(Ns + "schema_version")!.Value.Should().Be("1");
        rootElement.Element(Ns + "dataset_type")!.Value.Should().Be("forecast");

        rootElement.Element(Ns + "source")!.Value.Should().Be("dwd-mosmix-l:10637");
        rootElement.Element(Ns + "mosmix_id")!.Value.Should().Be("10637");
        rootElement.Element(Ns + "location")!.Element(Ns + "name")!.Value.Should().Be("FRANKFURT/M");
        rootElement.Element(Ns + "product")!.Value.Should().Be("MOSMIX_L");
        rootElement.Element(Ns + "issued_at")!.Value.Should().Be("2026-07-15T09:00:00");
        rootElement.Element(Ns + "retrieved_at")!.Value.Should().Be("2026-07-15T10:12:00");

        var forecastsElement = rootElement.Element(Ns + "forecasts");
        forecastsElement!.Elements(Ns + "forecast").Should().HaveCount(2);

        var forecast0Xml = forecastsElement.Elements(Ns + "forecast").ElementAt(0);
        forecast0Xml!.Element(Ns + "valid_at")!.Value.Should().Be("2026-07-15T12:00:00");

        var values0Xml = forecast0Xml.Element(Ns + "values");
        values0Xml!.Element(Ns + "temperature_c")!.Value.Should().Be("21.4");
        values0Xml.Element(Ns + "pressure_reference")!.Value.Should().Be("sea_level");

        var rainRateXml = values0Xml.Element(Ns + "rain_rate_mm_h");
        rainRateXml!.Element(Ns + "period_start")!.Value.Should().Be("2026-07-15T11:00:00");
        rainRateXml.Element(Ns + "aggregation")!.Value.Should().Be("mean");

        var forecast1Xml = forecastsElement.Elements(Ns + "forecast").ElementAt(1);
        var values1Xml = forecast1Xml!.Element(Ns + "values");
        values1Xml!.Element(Ns + "pressure_hpa").Should().BeNull();
        values1Xml.Element(Ns + "rain_rate_mm_h").Should().BeNull();
        xmlDoc.Descendants().Attributes(XName.Get("nil", "http://www.w3.org/2001/XMLSchema-instance")).Should().BeEmpty();
    }

    [TestMethod]
    public async Task Serialize_ForecastRun_AsStandaloneJsonRoot_IsOneCompleteDocument()
    {
        var run = BuildRun();

        using var stream = new MemoryStream();
        await _json.SerializeAsync(run, stream, CancellationToken.None);
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var jsonText = await reader.ReadToEndAsync();

        var text = Encoding.UTF8.GetString(stream.ToArray()).Trim();
        text.Should().StartWith("{");
        text.Should().EndWith("}");

        using var doc = JsonDocument.Parse(jsonText);
        doc.RootElement.GetProperty("dataset_type").GetString().Should().Be("forecast");
    }

    [TestMethod]
    public void ForecastTypes_HaveNoMeasuredAtMember()
    {
        var types = new[]
        {
            typeof(ForecastRun),
            typeof(ForecastLocation),
            typeof(ForecastStep),
            typeof(ForecastValues),
            typeof(IntervalValue)
        };

        foreach (var type in types)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            properties.Should().NotContain(p => p.Name.Contains("MeasuredAt"));
        }
    }

    [TestMethod]
    public async Task Serialize_ForecastRun_ContainsNoMeasuredAt()
    {
        var run = BuildRun();
        using var jsonStream = new MemoryStream();
        await _json.SerializeAsync(run, jsonStream, CancellationToken.None);
        jsonStream.Position = 0;
        using var reader = new StreamReader(jsonStream, Encoding.UTF8);
        var jsonText = await reader.ReadToEndAsync();
        jsonText.Should().NotContain("measured_at");
        using var xmlStream = new MemoryStream();
        await _xml.SerializeAsync(run, xmlStream, CancellationToken.None);
        xmlStream.Position = 0;
        var xmlText = Encoding.UTF8.GetString(xmlStream.ToArray());
        xmlText.Should().NotContain("measured_at");
    }
}
