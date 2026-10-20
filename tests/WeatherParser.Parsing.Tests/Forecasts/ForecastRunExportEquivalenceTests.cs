using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Forecasts;

[TestClass]
public sealed class ForecastRunExportEquivalenceTests
{
    private IForecastRunBuilder _builder = null!;
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _builder = new ForecastRunBuilder(
            new QuantityValidator(),
            new OutputRounding(),
            new PressureConverter(new OutputRounding()),
            new PeriodRateConverter(),
            new WindSpeedConverter());
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);
    private static AveragingPeriod Period(int startHour, int startMinute, int endHour, int endMinute) => new(Utc(startHour, startMinute), Utc(endHour, endMinute));

    private ForecastRun BuildFullForecastRun()
    {
        var input = new ForecastRunInput
        {
            Product = MosmixProduct.MosmixL,
            MosmixId = "10637",
            Location = new ForecastLocation { Name = "FRANKFURT/M", Latitude = 50.05m, Longitude = 8.6m, AltitudeM = 111 },
            IssuedAtUtc = Utc(9, 0),
            RetrievedAtUtc = Utc(10, 12),
            Timeline = new[] { Utc(13, 0), Utc(12, 0) },
            Series = new[]
            {
                new ForecastSeries
                {
                    Kind = QuantityKind.Temperature,
                    Values = new double?[] { 20.25, 21.0 },
                    Product = MosmixProduct.MosmixL,
                    MosmixId = "10637",
                    IssuedAtUtc = Utc(9, 0)
                },
                new ForecastSeries
                {
                    Kind = QuantityKind.Pressure,
                    Values = new double?[] { 1000.5, null },
                    PressureUnit = PressureUnit.Hectopascal,
                    PressureReference = PressureReference.SeaLevel,
                    Product = MosmixProduct.MosmixL,
                    MosmixId = "10637",
                    IssuedAtUtc = Utc(9, 0)
                },
                new ForecastSeries
                {
                    Kind = QuantityKind.PrecipitationAccumulation,
                    Values = new double?[] { 0.5, null },
                    Periods = new AveragingPeriod?[] { Period(12, 50, 13, 0), null },
                    Product = MosmixProduct.MosmixL,
                    MosmixId = "10637",
                    IssuedAtUtc = Utc(9, 0)
                },
                new ForecastSeries
                {
                    Kind = QuantityKind.RelativeHumidity,
                    Values = new double?[] { null, 55 },
                    Product = MosmixProduct.MosmixL,
                    MosmixId = "10637",
                    IssuedAtUtc = Utc(9, 0)
                }
            }
        };

        return _builder.Build(input).Run;
    }

    [TestMethod]
    public async Task Serialize_FullForecastRun_JsonAndXmlAreSemanticallyEqual()
    {
        var run = BuildFullForecastRun();
        using var jsonStream = new MemoryStream();
        using var xmlStream = new MemoryStream();
        await _json.SerializeAsync(run, jsonStream, CancellationToken.None);
        await _xml.SerializeAsync(run, xmlStream, CancellationToken.None);

        var jsonMap = new Dictionary<string, string>();
        using (var document = JsonDocument.Parse(jsonStream.ToArray()))
        {
            FlattenJson(document.RootElement, string.Empty, jsonMap);
        }

        var xmlMap = new Dictionary<string, string>();
        foreach (var element in XDocument.Parse(System.Text.Encoding.UTF8.GetString(xmlStream.ToArray())).Root!.Elements())
        {
            FlattenXml(element, string.Empty, xmlMap);
        }

        jsonMap.Should().BeEquivalentTo(xmlMap);
        foreach (var map in new[] { jsonMap, xmlMap })
        {
            map["dataset_type"].Should().Be("forecast");
            map["forecasts/0/valid_at"].Should().Be("2026-07-15T12:00:00");
            map["forecasts/0/values/temperature_c"].Should().Be("21.0");
            map["forecasts/0/values/humidity_percent"].Should().Be("55");
            map["forecasts/1/values/temperature_c"].Should().Be("20.3");
            map["forecasts/1/values/pressure_mmhg"].Should().Be("750");
            map["forecasts/1/values/pressure_reference"].Should().Be("sea_level");
            map["forecasts/1/values/rain_rate_mm_h/aggregation"].Should().Be("mean");
            map.Keys.Should().NotContain(key => key.Contains("measured_at", StringComparison.Ordinal));
        }
    }

    private static void FlattenJson(JsonElement element, string path, Dictionary<string, string> map)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                for (var i = 0; i < element.GetArrayLength(); i++)
                {
                    var childPath = path.Length == 0 ? $"{i}" : $"{path}/{i}";
                    FlattenJson(element[i], childPath, map);
                }
                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var childPath = path.Length == 0 ? property.Name : $"{path}/{property.Name}";
                    FlattenJson(property.Value, childPath, map);
                }
                break;
            case JsonValueKind.String:
                map[path] = element.GetString()!;
                break;
            default:
                map[path] = element.GetRawText();
                break;
        }
    }

    private static void FlattenXml(XElement element, string path, Dictionary<string, string> map)
    {
        var childPath = path.Length == 0 ? element.Name.LocalName : $"{path}/{element.Name.LocalName}";
        if (element.Name.LocalName == "forecasts")
        {
            var index = 0;
            foreach (var step in element.Elements())
            {
                foreach (var child in step.Elements())
                {
                    FlattenXml(child, $"{childPath}/{index}", map);
                }

                index++;
            }
        }
        else if (element.HasElements)
        {
            foreach (var child in element.Elements())
            {
                FlattenXml(child, childPath, map);
            }
        }
        else
        {
            map[childPath] = element.Value;
        }
    }
}
