using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ObservationDatasetContractTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);

    private static ObservationDataset BuildFullDataset()
    {
        return new ObservationDataset
        {
            Source = "dwd:00044",
            Location = new ObservationLocation
            {
                StationId = "00044",
                Name = "Großenkneten",
                Latitude = 52.9336m,
                Longitude = 8.2370m,
                AltitudeM = 44m
            },
            MeasuredAtUtc = Utc(12, 0),
            RetrievedAtUtc = Utc(12, 5),
            TimestampQuality = TimestampQuality.EstimatedDate,
            Measurements = new ObservationMeasurements
            {
                TemperatureC = new MeasurementValue
                {
                    Value = 20.3m,
                    MeasuredAtUtc = Utc(12, 0),
                    TimestampQuality = TimestampQuality.Source
                },
                PressureHpa = new PressureMeasurementValue
                {
                    Value = 1013m,
                    MeasuredAtUtc = Utc(12, 0),
                    PressureReference = PressureReference.SeaLevel,
                    TimestampQuality = TimestampQuality.Source
                },
                PressureMmHg = new PressureMeasurementValue
                {
                    Value = 760m,
                    MeasuredAtUtc = Utc(12, 0),
                    PressureReference = PressureReference.SeaLevel,
                    TimestampQuality = TimestampQuality.Source
                },
                HumidityPercent = new MeasurementValue
                {
                    Value = 55m,
                    MeasuredAtUtc = Utc(11, 50),
                    Quality = "1",
                    TimestampQuality = TimestampQuality.Source
                },
                WindDirectionDeg = new MeasurementValue
                {
                    Value = 270m,
                    MeasuredAtUtc = Utc(12, 0),
                    TimestampQuality = TimestampQuality.EstimatedDate,
                    SourceTimezone = "Europe/Berlin"
                },
                WindSpeedKmh = new MeasurementValue
                {
                    Value = 0m,
                    MeasuredAtUtc = Utc(12, 0),
                    TimestampQuality = TimestampQuality.Source
                },
                IlluminanceLux = new MeasurementValue
                {
                    Value = 12000m,
                    MeasuredAtUtc = Utc(12, 0),
                    TimestampQuality = TimestampQuality.Source
                },
                SolarIrradianceWm2 = new MeasurementValue
                {
                    Value = 600m,
                    MeasuredAtUtc = Utc(12, 0),
                    Period = new MeasurementPeriod
                    {
                        StartUtc = Utc(11, 50),
                        EndUtc = Utc(12, 0)
                    },
                    TimestampQuality = TimestampQuality.Source
                },
                RainRateMmH = new MeasurementValue
                {
                    Value = 3m,
                    MeasuredAtUtc = Utc(12, 0),
                    Period = new MeasurementPeriod
                    {
                        StartUtc = Utc(11, 50),
                        EndUtc = Utc(12, 0)
                    },
                    TimestampQuality = TimestampQuality.Source
                }
            }
        };
    }

    [TestMethod]
    public async Task SerializeAsync_Json_FullObservation_WritesAllContractFields()
    {
        var dataset = BuildFullDataset();
        using var stream = new MemoryStream();
        await _json.SerializeAsync(dataset, stream, CancellationToken.None);
        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;

        root.GetProperty("schema_version").GetString().Should().Be("1");
        root.GetProperty("source").GetString().Should().Be("dwd:00044");

        var location = root.GetProperty("location");
        location.GetProperty("station_id").GetString().Should().Be("00044");
        location.GetProperty("name").GetString().Should().Be("Großenkneten");
        location.GetProperty("latitude").GetDecimal().Should().Be(52.9336m);
        location.GetProperty("longitude").GetDecimal().Should().Be(8.2370m);
        location.GetProperty("altitude_m").GetDecimal().Should().Be(44m);

        root.GetProperty("measured_at").GetString().Should().Be("2026-07-15T12:00:00");
        root.GetProperty("retrieved_at").GetString().Should().Be("2026-07-15T12:05:00");
        root.GetProperty("timestamp_quality").GetString().Should().Be("estimated_date");

        var names = new[]
        {
            "temperature_c",
            "pressure_hpa",
            "pressure_mmhg",
            "humidity_percent",
            "wind_direction_deg",
            "wind_speed_kmh",
            "illuminance_lux",
            "solar_irradiance_w_m2",
            "rain_rate_mm_h"
        };

        foreach (var name in names)
        {
            var element = root.GetProperty("measurements").GetProperty(name);
            element.TryGetProperty("value", out _).Should().BeTrue(because: name);
            element.TryGetProperty("measured_at", out _).Should().BeTrue(because: name);
            element.TryGetProperty("timestamp_quality", out _).Should().BeTrue(because: name);
        }

        var temp = root.GetProperty("measurements").GetProperty("temperature_c");
        temp.GetProperty("value").GetDecimal().Should().Be(20.3m);
        temp.GetProperty("measured_at").GetString().Should().Be("2026-07-15T12:00:00");
        temp.GetProperty("timestamp_quality").GetString().Should().Be("source");

        var pressureHpa = root.GetProperty("measurements").GetProperty("pressure_hpa");
        pressureHpa.GetProperty("value").GetDecimal().Should().Be(1013m);
        pressureHpa.GetProperty("measured_at").GetString().Should().Be("2026-07-15T12:00:00");
        pressureHpa.GetProperty("pressure_reference").GetString().Should().Be("sea_level");

        var pressureMmHg = root.GetProperty("measurements").GetProperty("pressure_mmhg");
        pressureMmHg.GetProperty("value").GetDecimal().Should().Be(760m);
        pressureMmHg.GetProperty("measured_at").GetString().Should().Be("2026-07-15T12:00:00");
        pressureMmHg.GetProperty("pressure_reference").GetString().Should().Be("sea_level");

        var humidity = root.GetProperty("measurements").GetProperty("humidity_percent");
        humidity.GetProperty("value").GetDecimal().Should().Be(55m);
        humidity.GetProperty("measured_at").GetString().Should().Be("2026-07-15T11:50:00");
        humidity.GetProperty("quality").GetString().Should().Be("1");

        var windDir = root.GetProperty("measurements").GetProperty("wind_direction_deg");
        windDir.GetProperty("value").GetDecimal().Should().Be(270m);
        windDir.GetProperty("measured_at").GetString().Should().Be("2026-07-15T12:00:00");
        windDir.GetProperty("timestamp_quality").GetString().Should().Be("estimated_date");
        windDir.GetProperty("source_timezone").GetString().Should().Be("Europe/Berlin");

        var windSpeed = root.GetProperty("measurements").GetProperty("wind_speed_kmh");
        windSpeed.GetProperty("value").GetDecimal().Should().Be(0m);
        windSpeed.GetProperty("measured_at").GetString().Should().Be("2026-07-15T12:00:00");

        var illuminance = root.GetProperty("measurements").GetProperty("illuminance_lux");
        illuminance.GetProperty("value").GetDecimal().Should().Be(12000m);
        illuminance.GetProperty("measured_at").GetString().Should().Be("2026-07-15T12:00:00");

        var solarIrradiance = root.GetProperty("measurements").GetProperty("solar_irradiance_w_m2");
        solarIrradiance.GetProperty("value").GetDecimal().Should().Be(600m);
        solarIrradiance.GetProperty("measured_at").GetString().Should().Be("2026-07-15T12:00:00");
        solarIrradiance.GetProperty("period").GetProperty("start").GetString().Should().Be("2026-07-15T11:50:00");
        solarIrradiance.GetProperty("period").GetProperty("end").GetString().Should().Be("2026-07-15T12:00:00");

        var rainRate = root.GetProperty("measurements").GetProperty("rain_rate_mm_h");
        rainRate.GetProperty("value").GetDecimal().Should().Be(3m);
        rainRate.GetProperty("measured_at").GetString().Should().Be("2026-07-15T12:00:00");
        rainRate.GetProperty("period").GetProperty("start").GetString().Should().Be("2026-07-15T11:50:00");
        rainRate.GetProperty("period").GetProperty("end").GetString().Should().Be("2026-07-15T12:00:00");
        temp.TryGetProperty("source_timezone", out _).Should().BeFalse();
        temp.TryGetProperty("period", out _).Should().BeFalse();
        temp.TryGetProperty("quality", out _).Should().BeFalse();
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_FullObservation_WritesAllContractElements()
    {
        var dataset = BuildFullDataset();
        using var stream = new MemoryStream();
        await _xml.SerializeAsync(dataset, stream, CancellationToken.None);
        stream.Position = 0;
        var doc = XDocument.Load(stream);

        var root = doc.Root!;
        root.Name.LocalName.Should().Be("observation_dataset");
        root.Name.Namespace.Should().Be(Ns);

        root.Element(Ns + "schema_version")!.Value.Should().Be("1");
        root.Element(Ns + "source")!.Value.Should().Be("dwd:00044");

        var location = root.Element(Ns + "location")!;
        location.Element(Ns + "station_id")!.Value.Should().Be("00044");
        location.Element(Ns + "name")!.Value.Should().Be("Großenkneten");
        location.Element(Ns + "latitude")!.Value.Should().Be("52.9336");
        location.Element(Ns + "longitude")!.Value.Should().Be("8.2370");
        location.Element(Ns + "altitude_m")!.Value.Should().Be("44");

        root.Element(Ns + "measured_at")!.Value.Should().Be("2026-07-15T12:00:00");
        root.Element(Ns + "retrieved_at")!.Value.Should().Be("2026-07-15T12:05:00");
        root.Element(Ns + "timestamp_quality")!.Value.Should().Be("estimated_date");

        var names = new[]
        {
            "temperature_c",
            "pressure_hpa",
            "pressure_mmhg",
            "humidity_percent",
            "wind_direction_deg",
            "wind_speed_kmh",
            "illuminance_lux",
            "solar_irradiance_w_m2",
            "rain_rate_mm_h"
        };

        foreach (var name in names)
        {
            var element = root.Element(Ns + "measurements")!.Element(Ns + name)!;
            element.Element(Ns + "value")!.Should().NotBeNull(because: name);
            element.Element(Ns + "measured_at")!.Should().NotBeNull(because: name);
            element.Element(Ns + "timestamp_quality")!.Should().NotBeNull(because: name);
        }

        var temp = root.Element(Ns + "measurements")!.Element(Ns + "temperature_c")!;
        temp.Element(Ns + "value")!.Value.Should().Be("20.3");
        temp.Element(Ns + "measured_at")!.Value.Should().Be("2026-07-15T12:00:00");
        temp.Element(Ns + "timestamp_quality")!.Value.Should().Be("source");

        var pressureHpa = root.Element(Ns + "measurements")!.Element(Ns + "pressure_hpa")!;
        pressureHpa.Element(Ns + "value")!.Value.Should().Be("1013");
        pressureHpa.Element(Ns + "measured_at")!.Value.Should().Be("2026-07-15T12:00:00");
        pressureHpa.Element(Ns + "pressure_reference")!.Value.Should().Be("sea_level");

        var pressureMmHg = root.Element(Ns + "measurements")!.Element(Ns + "pressure_mmhg")!;
        pressureMmHg.Element(Ns + "value")!.Value.Should().Be("760");
        pressureMmHg.Element(Ns + "measured_at")!.Value.Should().Be("2026-07-15T12:00:00");
        pressureMmHg.Element(Ns + "pressure_reference")!.Value.Should().Be("sea_level");

        var humidity = root.Element(Ns + "measurements")!.Element(Ns + "humidity_percent")!;
        humidity.Element(Ns + "value")!.Value.Should().Be("55");
        humidity.Element(Ns + "measured_at")!.Value.Should().Be("2026-07-15T11:50:00");
        humidity.Element(Ns + "quality")!.Value.Should().Be("1");

        var windDir = root.Element(Ns + "measurements")!.Element(Ns + "wind_direction_deg")!;
        windDir.Element(Ns + "value")!.Value.Should().Be("270");
        windDir.Element(Ns + "measured_at")!.Value.Should().Be("2026-07-15T12:00:00");
        windDir.Element(Ns + "timestamp_quality")!.Value.Should().Be("estimated_date");
        windDir.Element(Ns + "source_timezone")!.Value.Should().Be("Europe/Berlin");

        var windSpeed = root.Element(Ns + "measurements")!.Element(Ns + "wind_speed_kmh")!;
        windSpeed.Element(Ns + "value")!.Value.Should().Be("0");
        windSpeed.Element(Ns + "measured_at")!.Value.Should().Be("2026-07-15T12:00:00");

        var illuminance = root.Element(Ns + "measurements")!.Element(Ns + "illuminance_lux")!;
        illuminance.Element(Ns + "value")!.Value.Should().Be("12000");
        illuminance.Element(Ns + "measured_at")!.Value.Should().Be("2026-07-15T12:00:00");

        var solarIrradiance = root.Element(Ns + "measurements")!.Element(Ns + "solar_irradiance_w_m2")!;
        solarIrradiance.Element(Ns + "value")!.Value.Should().Be("600");
        solarIrradiance.Element(Ns + "measured_at")!.Value.Should().Be("2026-07-15T12:00:00");
        solarIrradiance.Element(Ns + "period")!.Element(Ns + "start")!.Value.Should().Be("2026-07-15T11:50:00");
        solarIrradiance.Element(Ns + "period")!.Element(Ns + "end")!.Value.Should().Be("2026-07-15T12:00:00");

        var rainRate = root.Element(Ns + "measurements")!.Element(Ns + "rain_rate_mm_h")!;
        rainRate.Element(Ns + "value")!.Value.Should().Be("3");
        rainRate.Element(Ns + "measured_at")!.Value.Should().Be("2026-07-15T12:00:00");
        rainRate.Element(Ns + "period")!.Element(Ns + "start")!.Value.Should().Be("2026-07-15T11:50:00");
        rainRate.Element(Ns + "period")!.Element(Ns + "end")!.Value.Should().Be("2026-07-15T12:00:00");
        temp.Elements(Ns + "source_timezone").Should().BeEmpty();
        temp.Elements(Ns + "period").Should().BeEmpty();
        temp.Elements(Ns + "quality").Should().BeEmpty();
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        doc.Descendants().Attributes(xsi + "nil").Should().BeEmpty();
    }
}
