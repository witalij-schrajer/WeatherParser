using System.Text.Json;
using System.Xml.Serialization;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ForecastReadBackTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    private static ForecastRun FullRun()
    {
        var run = ForecastTestData.ValidRun();
        run.Forecasts[1].Values.HumidityPercent = 55m;
        run.Forecasts[1].Values.WindDirectionDeg = 270m;
        run.Forecasts[1].Values.WindSpeedKmh = 12m;
        run.Forecasts[1].Values.IlluminanceLux = 12000m;
        run.Forecasts[1].Values.SolarIrradianceWm2 = ForecastTestData.Interval(612.5m, ForecastTestData.Utc(12, 0), ForecastTestData.Utc(13, 0));
        return run;
    }

    private static void AssertRestored(ForecastRun copy)
    {
        copy.SchemaVersion.Should().Be("1");
        copy.IssuedAtUtc.Should().Be(ForecastTestData.IssuedAtUtc);
        copy.RetrievedAtUtc.Should().Be(ForecastTestData.RetrievedAtUtc);
        copy.Location.AltitudeM.Should().Be(111m);

        copy.Forecasts[0].ValidAtUtc.Should().Be(ForecastTestData.Utc(12, 0));
        copy.Forecasts[0].Values.TemperatureC.Should().Be(21.4m);
        copy.Forecasts[0].Values.PressureHpa.Should().Be(1013m);
        copy.Forecasts[0].Values.PressureMmHg.Should().Be(760m);
        copy.Forecasts[0].Values.PressureReference.Should().Be(PressureReference.SeaLevel);
        copy.Forecasts[0].Values.RainRateMmH!.PeriodStartUtc.Should().Be(ForecastTestData.Utc(11, 0));
        copy.Forecasts[0].Values.RainRateMmH!.PeriodEndUtc.Should().Be(ForecastTestData.Utc(12, 0));

        copy.Forecasts[1].Values.HumidityPercent.Should().Be(55m);
        copy.Forecasts[1].Values.WindDirectionDeg.Should().Be(270m);
        copy.Forecasts[1].Values.WindSpeedKmh.Should().Be(12m);
        copy.Forecasts[1].Values.IlluminanceLux.Should().Be(12000m);
        copy.Forecasts[1].Values.SolarIrradianceWm2!.Value.Should().Be(612.5m);
    }

    [TestMethod]
    public async Task Json_ReadBack_RestoresTheFullRun()
    {
        // Arrange
        var run = FullRun();
        using var stream = new MemoryStream();
        await _json.SerializeAsync(run, stream, CancellationToken.None);
        stream.Position = 0;

        // Act
        var copy = JsonSerializer.Deserialize<ForecastRun>(stream.ToArray())!;

        // Assert
        AssertRestored(copy);
    }

    [TestMethod]
    public async Task Xml_ReadBack_RestoresTheFullRun()
    {
        // Arrange
        var run = FullRun();
        using var stream = new MemoryStream();
        await _xml.SerializeAsync(run, stream, CancellationToken.None);
        stream.Position = 0;

        // Act
        var copy = (ForecastRun)new XmlSerializer(typeof(ForecastRun)).Deserialize(new MemoryStream(stream.ToArray()))!;

        // Assert
        AssertRestored(copy);
    }

    [TestMethod]
    public void SchemaVersion_OtherThanOne_IsRejected()
    {
        FluentActions.Invoking(() => new ForecastRun().SchemaVersion = "2").Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void SpecifiedSetFalse_ClearsTheValue()
    {
        // Arrange
        var values = new ForecastValues
        {
            TemperatureC = 1.0m,
            PressureHpa = 1m,
            PressureMmHg = 1m,
            PressureReference = PressureReference.Unknown,
            HumidityPercent = 1m,
            WindDirectionDeg = 1m,
            WindSpeedKmh = 1m,
            IlluminanceLux = 1m
        };

        // Act & Assert
        values.TemperatureCSpecified = false;
        values.TemperatureC.Should().BeNull();

        values.PressureHpaSpecified = false;
        values.PressureHpa.Should().BeNull();

        values.PressureMmHgSpecified = false;
        values.PressureMmHg.Should().BeNull();

        values.PressureReferenceSpecified = false;
        values.PressureReference.Should().BeNull();

        values.HumidityPercentSpecified = false;
        values.HumidityPercent.Should().BeNull();

        values.WindDirectionDegSpecified = false;
        values.WindDirectionDeg.Should().BeNull();

        values.WindSpeedKmhSpecified = false;
        values.WindSpeedKmh.Should().BeNull();

        values.IlluminanceLuxSpecified = false;
        values.IlluminanceLux.Should().BeNull();

        // Act & Assert
        var location = new ForecastLocation { AltitudeM = 44m };
        location.AltitudeMSpecified = false;
        location.AltitudeM.Should().BeNull();
    }

    [TestMethod]
    public void SpecifiedSetTrue_KeepsTheValue()
    {
        // Arrange
        var values = new ForecastValues
        {
            TemperatureC = 1.0m,
            PressureHpa = 1m,
            PressureMmHg = 1m,
            PressureReference = PressureReference.Unknown,
            HumidityPercent = 1m,
            WindDirectionDeg = 1m,
            WindSpeedKmh = 1m,
            IlluminanceLux = 1m
        };

        // Act & Assert
        values.TemperatureCSpecified = true;
        values.TemperatureC.Should().Be(1.0m);

        values.PressureHpaSpecified = true;
        values.PressureHpa.Should().Be(1m);

        values.PressureMmHgSpecified = true;
        values.PressureMmHg.Should().Be(1m);

        values.PressureReferenceSpecified = true;
        values.PressureReference.Should().Be(PressureReference.Unknown);

        values.HumidityPercentSpecified = true;
        values.HumidityPercent.Should().Be(1m);

        values.WindDirectionDegSpecified = true;
        values.WindDirectionDeg.Should().Be(1m);

        values.WindSpeedKmhSpecified = true;
        values.WindSpeedKmh.Should().Be(1m);

        values.IlluminanceLuxSpecified = true;
        values.IlluminanceLux.Should().Be(1m);

        // Act & Assert
        var location = new ForecastLocation { AltitudeM = 44m };
        location.AltitudeMSpecified = true;
        location.AltitudeM.Should().Be(44m);
    }
}
