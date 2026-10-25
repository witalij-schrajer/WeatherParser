using System.Globalization;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations;

[TestClass]
public sealed class ObservationDatasetBuilderTests
{
    private IObservationDatasetBuilder _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new ObservationDatasetBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter());
    }

    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);
    private static NormalizedSourceTime SourceTime(int hour, int minute) => new(Utc(hour, minute), TimestampQuality.Source, null);
    private static NormalizedSourceTime EstimatedTime(int hour, int minute) => new(Utc(hour, minute), TimestampQuality.EstimatedDate, "Europe/Berlin");
    private static ObservationLocation Location() => new() { StationId = "00044", Name = "Großenkneten", Latitude = 52.9336m, Longitude = 8.2370m };
    private static ObservationInput Input(params ObservationReading[] readings) => new() { Provider = "dwd", Location = Location(), RetrievedAtUtc = Utc(12, 5), Readings = readings };
    private static ObservationReading Reading(QuantityKind kind, double? value, NormalizedSourceTime? time) => new() { Kind = kind, Value = value, Time = time };

    [TestMethod]
    public void Build_DifferingReadingTimes_ArePreservedPerQuantity()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0)),
            Reading(QuantityKind.RelativeHumidity, 55, SourceTime(11, 50))
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.TemperatureC!.MeasuredAtUtc.Should().Be(Utc(12, 0));
        result.Dataset.Measurements.HumidityPercent!.MeasuredAtUtc.Should().Be(Utc(11, 50));
    }

    [TestMethod]
    public void Build_DatasetMeasuredAt_IsLatestReadingTime()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0)),
            Reading(QuantityKind.RelativeHumidity, 55, SourceTime(11, 50))
        );

        var result = _subject.Build(input);

        result.Dataset.MeasuredAtUtc.Should().Be(Utc(12, 0));
        result.Dataset.RetrievedAtUtc.Should().Be(Utc(12, 5));
        result.Dataset.Source.Should().Be("dwd:00044");
    }

    [TestMethod]
    public void Build_MissingHumidity_IsOmittedAndReported()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0)),
            Reading(QuantityKind.RelativeHumidity, null, SourceTime(12, 0))
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.HumidityPercent.Should().BeNull();
        result.OmittedReadings.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OmittedReading(QuantityKind.RelativeHumidity, OmissionReason.Missing));
    }

    [TestMethod]
    public void Build_ZeroWindSpeed_IsKept()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0)),
            new ObservationReading
            {
                Kind = QuantityKind.MeanWindSpeed,
                Value = 0,
                Time = SourceTime(12, 0),
                WindSpeedUnit = WindSpeedUnit.KilometresPerHour
            }
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.WindSpeedKmh!.Value.Should().Be(0m);
    }

    [TestMethod]
    public void Build_NonFiniteReading_IsOmittedAndReported()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0)),
            Reading(QuantityKind.RelativeHumidity, double.NaN, SourceTime(12, 0))
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.HumidityPercent.Should().BeNull();
        result.OmittedReadings.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OmittedReading(QuantityKind.RelativeHumidity, OmissionReason.NonFinite));
    }

    [TestMethod]
    public void Build_NegativeTemperature_IsKept()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, -12.5, SourceTime(12, 0))
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.TemperatureC!.Value.Should().Be(-12.5m);
    }

    [TestMethod]
    public void Build_MissingTime_IsOmittedAndNeverUsesRetrievalTime()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0)),
            Reading(QuantityKind.RelativeHumidity, 55, null)
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.HumidityPercent.Should().BeNull();
        result.OmittedReadings.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OmittedReading(QuantityKind.RelativeHumidity, OmissionReason.MissingTime));
    }

    [TestMethod]
    public void Build_AllReadingsUnusable_ThrowsObservationDatasetRejectedException()
    {
        var input = Input(
            Reading(QuantityKind.RelativeHumidity, null, SourceTime(12, 0)),
            Reading(QuantityKind.Temperature, double.NaN, SourceTime(12, 0))
        );

        FluentActions.Invoking(() => _subject.Build(input)).Should().Throw<ObservationDatasetRejectedException>().WithMessage("*dwd:00044*");
    }

    [TestMethod]
    public void Build_AllReadingsUnusable_ExceptionCarriesOmittedReadings()
    {
        var input = Input(
            Reading(QuantityKind.RelativeHumidity, null, SourceTime(12, 0)),
            Reading(QuantityKind.Temperature, double.NaN, SourceTime(12, 0))
        );

        var ex = FluentActions.Invoking(() => _subject.Build(input)).Should().Throw<ObservationDatasetRejectedException>().Which;

        ex.OmittedReadings.Should().BeEquivalentTo(new[] { new OmittedReading(QuantityKind.RelativeHumidity, OmissionReason.Missing), new OmittedReading(QuantityKind.Temperature, OmissionReason.NonFinite) });
    }

    [TestMethod]
    public void ObservationDatasetRejectedException_MessageConstructor_HasNoOmittedReadings()
    {
        var ex = new ObservationDatasetRejectedException("Message.");

        ex.OmittedReadings.Should().BeEmpty();
        ex.Message.Should().Be("Message.");
    }

    [TestMethod]
    public void ObservationDatasetRejectedException_NullOmittedReadings_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new ObservationDatasetRejectedException("Message.", null!)).Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Build_WithRealServices_RoundsOnlyAtOutput()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.25, SourceTime(12, 0)),
            new ObservationReading
            {
                Kind = QuantityKind.Pressure,
                Value = 1000.5,
                Time = SourceTime(12, 0),
                PressureUnit = PressureUnit.Hectopascal,
                PressureReference = PressureReference.SeaLevel
            },
            new ObservationReading
            {
                Kind = QuantityKind.MeanWindSpeed,
                Value = 3.47,
                Time = SourceTime(12, 0),
                WindSpeedUnit = WindSpeedUnit.MetresPerSecond
            }
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.TemperatureC!.Value.ToString(CultureInfo.InvariantCulture).Should().Be("20.3");
        result.Dataset.Measurements.PressureHpa!.Value.Should().Be(1001m);
        result.Dataset.Measurements.PressureMmHg!.Value.Should().Be(750m);
        result.Dataset.Measurements.PressureHpa!.PressureReference.Should().Be(PressureReference.SeaLevel);
        result.Dataset.Measurements.PressureMmHg!.PressureReference.Should().Be(PressureReference.SeaLevel);
        result.Dataset.Measurements.WindSpeedKmh!.Value.Should().Be(12m);
    }

    [TestMethod]
    public void Build_PressureFromMmHg_UsesReverseConversion()
    {
        var input = Input(
            new ObservationReading
            {
                Kind = QuantityKind.Pressure,
                Value = 760,
                Time = SourceTime(12, 0),
                PressureUnit = PressureUnit.MillimetreOfMercury,
                PressureReference = PressureReference.Unknown
            },
            Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0))
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.PressureHpa!.Value.Should().Be(1013m);
        result.Dataset.Measurements.PressureMmHg!.Value.Should().Be(760m);
        result.Dataset.Measurements.PressureHpa!.PressureReference.Should().Be(PressureReference.Unknown);
        result.Dataset.Measurements.PressureMmHg!.PressureReference.Should().Be(PressureReference.Unknown);
    }

    [TestMethod]
    public void Build_EstimatedReading_CarriesEstimatedDateAndBerlinTimezone()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, EstimatedTime(12, 0))
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.TemperatureC!.TimestampQuality.Should().Be(TimestampQuality.EstimatedDate);
        result.Dataset.Measurements.TemperatureC!.SourceTimezone.Should().Be("Europe/Berlin");
    }

    [TestMethod]
    public void Build_DatasetQuality_IsEstimatedWhenAnyReadingIsEstimated()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, EstimatedTime(12, 0)),
            Reading(QuantityKind.RelativeHumidity, 55, SourceTime(12, 0))
        );

        var result = _subject.Build(input);

        result.Dataset.TimestampQuality.Should().Be(TimestampQuality.EstimatedDate);
    }

    [TestMethod]
    public void Build_DatasetQuality_IsSourceWhenNoReadingIsEstimated()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0)),
            Reading(QuantityKind.RelativeHumidity, 55, SourceTime(12, 0))
        );

        var result = _subject.Build(input);

        result.Dataset.TimestampQuality.Should().Be(TimestampQuality.Source);
    }

    [TestMethod]
    public void Build_GustInput_IsNotWrittenAsWindSpeed()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0)),
            new ObservationReading
            {
                Kind = QuantityKind.WindGust,
                Value = 40,
                Time = SourceTime(12, 0)
            }
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.WindSpeedKmh.Should().BeNull();
        result.OmittedReadings.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OmittedReading(QuantityKind.WindGust, OmissionReason.NotExportable));
    }

    [TestMethod]
    public void Build_ProbabilityInput_IsNotExportable()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0)),
            new ObservationReading
            {
                Kind = QuantityKind.Probability,
                Value = 40,
                Time = SourceTime(12, 0)
            }
        );

        var result = _subject.Build(input);

        result.OmittedReadings.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OmittedReading(QuantityKind.Probability, OmissionReason.NotExportable));
    }

    [TestMethod]
    public void Build_RainAccumulationWithoutPeriod_IsNotWrittenAsRate()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0)),
            new ObservationReading
            {
                Kind = QuantityKind.PrecipitationAccumulation,
                Value = 0.5,
                Time = SourceTime(12, 0)
            }
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.RainRateMmH.Should().BeNull();
        result.OmittedReadings.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OmittedReading(QuantityKind.PrecipitationAccumulation, OmissionReason.MissingPeriod));
    }

    [TestMethod]
    public void Build_RainAccumulationWithPeriod_IsWrittenAsRateWithPeriod()
    {
        var period = new AveragingPeriod(Utc(11, 50), Utc(12, 0));
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0)),
            new ObservationReading
            {
                Kind = QuantityKind.PrecipitationAccumulation,
                Value = 0.5,
                Time = SourceTime(12, 0),
                Period = period
            }
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.RainRateMmH!.Value.Should().Be(3m);
        result.Dataset.Measurements.RainRateMmH.Period!.StartUtc.Should().Be(Utc(11, 50));
        result.Dataset.Measurements.RainRateMmH.Period.EndUtc.Should().Be(Utc(12, 0));
    }

    [TestMethod]
    public void Build_RadiationEnergyWithPeriod_IsWrittenAsIrradiance()
    {
        var period = new AveragingPeriod(Utc(11, 50), Utc(12, 0));
        var input = Input(
            new ObservationReading
            {
                Kind = QuantityKind.RadiationEnergy,
                Value = 360000,
                Time = SourceTime(12, 0),
                Period = period
            }
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.SolarIrradianceWm2!.Value.Should().Be(600m);
    }

    [TestMethod]
    public void Build_SourceQualityCode_IsKeptUnchanged()
    {
        var input = Input(
            new ObservationReading
            {
                Kind = QuantityKind.Temperature,
                Value = 20.3,
                Time = SourceTime(12, 0),
                Quality = "3"
            }
        );

        var result = _subject.Build(input);

        result.Dataset.Measurements.TemperatureC!.Quality.Should().Be("3");
    }

    [TestMethod]
    public async Task Build_Result_SerializesInBothFormats()
    {
        var input = Input(
            Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0)),
            new ObservationReading
            {
                Kind = QuantityKind.Pressure,
                Value = 1000,
                Time = SourceTime(12, 0),
                PressureUnit = PressureUnit.Hectopascal,
                PressureReference = PressureReference.SeaLevel
            },
            Reading(QuantityKind.RelativeHumidity, 55, SourceTime(11, 50)),
            new ObservationReading
            {
                Kind = QuantityKind.WindDirection,
                Value = 270,
                Time = EstimatedTime(12, 0)
            }
        );

        var result = _subject.Build(input);
        using var stream = new MemoryStream();

        await new JsonExportSerializer().SerializeAsync(result.Dataset, stream, CancellationToken.None);
        stream.Position.Should().BeGreaterThan(0);

        stream.Position = 0;
        await new XmlExportSerializer().SerializeAsync(result.Dataset, stream, CancellationToken.None);
        stream.Position.Should().BeGreaterThan(0);
    }
}
