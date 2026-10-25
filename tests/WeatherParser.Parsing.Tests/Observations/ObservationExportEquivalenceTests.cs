using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;
using WeatherParser.Parsing.Tests.Export;

namespace WeatherParser.Parsing.Tests.Observations;

[TestClass]
public sealed class ObservationExportEquivalenceTests
{
    private IObservationDatasetBuilder _builder = null!;
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _builder = new ObservationDatasetBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter());
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);
    private static NormalizedSourceTime SourceTime(int hour, int minute) => new(Utc(hour, minute), TimestampQuality.Source, null);

    private ObservationDataset BuildFullDataset()
    {
        var input = new ObservationInput
        {
            Provider = "dwd",
            Location = new ObservationLocation { StationId = "00044", Name = "Großenkneten", Latitude = 52.9336m, Longitude = 8.2370m, AltitudeM = 44m },
            RetrievedAtUtc = Utc(12, 5),
            Readings = new[]
            {
                new ObservationReading { Kind = QuantityKind.Temperature, Value = 20.25, Time = SourceTime(12, 0) },
                new ObservationReading { Kind = QuantityKind.Pressure, Value = 1000.5, Time = SourceTime(12, 0), PressureUnit = PressureUnit.Hectopascal, PressureReference = PressureReference.SeaLevel, Quality = "1" },
                new ObservationReading { Kind = QuantityKind.RelativeHumidity, Value = 55, Time = SourceTime(11, 50) },
                new ObservationReading { Kind = QuantityKind.WindDirection, Value = 270, Time = new NormalizedSourceTime(Utc(12, 0), TimestampQuality.EstimatedDate, "Europe/Berlin") },
                new ObservationReading { Kind = QuantityKind.MeanWindSpeed, Value = 0, Time = SourceTime(12, 0), WindSpeedUnit = WindSpeedUnit.KilometresPerHour },
                new ObservationReading { Kind = QuantityKind.PrecipitationAccumulation, Value = 0.5, Time = SourceTime(12, 0), Period = new AveragingPeriod(Utc(11, 50), Utc(12, 0)) },
                new ObservationReading { Kind = QuantityKind.Illuminance, Value = null, Time = SourceTime(12, 0) }
            }
        };

        return _builder.Build(input).Dataset;
    }

    [TestMethod]
    public async Task Serialize_FullObservation_JsonAndXmlAreSemanticallyEqual()
    {
        var dataset = BuildFullDataset();
        using var jsonStream = new MemoryStream();
        using var xmlStream = new MemoryStream();
        await _json.SerializeAsync(dataset, jsonStream, CancellationToken.None);
        await _xml.SerializeAsync(dataset, xmlStream, CancellationToken.None);

        var jsonMap = new Dictionary<string, string>();
        using (var document = JsonDocument.Parse(jsonStream.ToArray()))
        {
            ExportEquivalence.FlattenJson(document.RootElement, string.Empty, jsonMap);
        }

        var xmlMap = new Dictionary<string, string>();
        foreach (var element in XDocument.Parse(System.Text.Encoding.UTF8.GetString(xmlStream.ToArray())).Root!.Elements())
        {
            ExportEquivalence.FlattenXml(element, string.Empty, xmlMap);
        }

        jsonMap.Should().BeEquivalentTo(xmlMap);
        foreach (var map in new[] { jsonMap, xmlMap })
        {
            map["measurements/temperature_c/value"].Should().Be("20.3");
            map["measurements/pressure_mmhg/value"].Should().Be("750");
            map["measurements/wind_speed_kmh/value"].Should().Be("0");
            map["measurements/rain_rate_mm_h/period/start"].Should().Be("2026-07-15T11:50:00");
            map["measurements/humidity_percent/measured_at"].Should().Be("2026-07-15T11:50:00");
            map["measurements/wind_direction_deg/source_timezone"].Should().Be("Europe/Berlin");
            map["timestamp_quality"].Should().Be("estimated_date");
            map.Keys.Should().NotContain(key => key.StartsWith("measurements/illuminance_lux", StringComparison.Ordinal));
        }
    }
}
