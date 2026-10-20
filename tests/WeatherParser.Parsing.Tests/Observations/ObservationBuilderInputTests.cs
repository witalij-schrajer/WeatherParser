using System;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations;

[TestClass]
public class ObservationBuilderInputTests
{
    private IObservationDatasetBuilder _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new ObservationDatasetBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter());
    }

    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);
    private static NormalizedSourceTime SourceTime(int hour, int minute) => new(Utc(hour, minute), TimestampQuality.Source, null);
    private static ObservationLocation Location() => new() { StationId = "00044", Name = "Großenkneten", Latitude = 52.9336m, Longitude = 8.2370m };

    [TestMethod]
    public void Build_NullReadings_ThrowsArgumentException()
    {
        // Arrange
        var input = new ObservationInput { Provider = "dwd", Location = Location(), RetrievedAtUtc = Utc(12, 5), Readings = null! };

        // Act
        Action act = () => _subject.Build(input);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [DataRow("PressureUnit")]
    [DataRow("PressureReference")]
    [DataRow("WindSpeedUnit")]
    [TestMethod]
    public void Build_UndefinedUnitOnOtherKind_ThrowsArgumentException(string member)
    {
        // Arrange
        var reading = member switch
        {
            "PressureUnit" => new ObservationReading { Kind = QuantityKind.Temperature, Value = 20.3, Time = SourceTime(12, 0), PressureUnit = (PressureUnit)9 },
            "PressureReference" => new ObservationReading { Kind = QuantityKind.Temperature, Value = 20.3, Time = SourceTime(12, 0), PressureReference = (PressureReference)9 },
            "WindSpeedUnit" => new ObservationReading { Kind = QuantityKind.Temperature, Value = 20.3, Time = SourceTime(12, 0), WindSpeedUnit = (WindSpeedUnit)9 },
            _ => throw new ArgumentException($"Unknown member {member}.", nameof(member))
        };

        var input = new ObservationInput
        {
            Provider = "dwd",
            Location = Location(),
            RetrievedAtUtc = Utc(12, 5),
            Readings = new[] { reading }
        };

        // Act
        Action act = () => _subject.Build(input);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Build_Illuminance_IsExportedUnrounded()
    {
        // Arrange
        var reading = new ObservationReading
        {
            Kind = QuantityKind.Temperature,
            Value = 20.3,
            Time = SourceTime(12, 0)
        };

        var illuminanceReading = new ObservationReading
        {
            Kind = QuantityKind.Illuminance,
            Value = 12345.67,
            Time = SourceTime(12, 0)
        };

        var input = new ObservationInput
        {
            Provider = "dwd",
            Location = Location(),
            RetrievedAtUtc = Utc(12, 5),
            Readings = new[] { reading, illuminanceReading }
        };

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Dataset.Measurements.IlluminanceLux!.Value.Should().Be(12345.67m);
    }

    [TestMethod]
    public void Build_DirectIrradianceAndRainRate_AreExportedUnchanged()
    {
        // Arrange
        var input = new ObservationInput
        {
            Provider = "dwd",
            Location = Location(),
            RetrievedAtUtc = Utc(12, 5),
            Readings = new[]
            {
                new ObservationReading
                {
                    Kind = QuantityKind.Irradiance,
                    Value = 612.5,
                    Time = SourceTime(12, 0)
                },
                new ObservationReading
                {
                    Kind = QuantityKind.PrecipitationRate,
                    Value = 1.25,
                    Time = SourceTime(12, 0)
                }
            }
        };

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Dataset.Measurements.SolarIrradianceWm2!.Value.Should().Be(612.5m);
        result.Dataset.Measurements.RainRateMmH!.Value.Should().Be(1.25m);
        result.OmittedReadings.Should().BeEmpty();
    }
}
