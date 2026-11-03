using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Forecasts;

[TestClass]
public sealed class ForecastRunBuilderTests
{
    private IForecastRunBuilder _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new ForecastRunBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter());
    }

    private static readonly DateTime IssuedAt = new(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc);
    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);

    private static ForecastSeries Series(QuantityKind kind, params double?[] values) => new()
    {
        Kind = kind, Values = values, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt
    };

    private static ForecastSeries IntervalSeries(QuantityKind kind, double?[] values, AveragingPeriod?[] periods) => new()
    {
        Kind = kind, Values = values, Periods = periods, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt
    };

    private static ForecastRunInput WithProduct(MosmixProduct product, ForecastRunInput input) => new()
    {
        Product = product, MosmixId = input.MosmixId, Location = input.Location, IssuedAtUtc = input.IssuedAtUtc,
        RetrievedAtUtc = input.RetrievedAtUtc, Timeline = input.Timeline, Series = input.Series
    };

    private static ForecastRunInput Input(DateTime[] timeline, params ForecastSeries[] series) => new()
    {
        Product = MosmixProduct.MosmixL, MosmixId = "10637",
        Location = new ForecastLocation { Name = "FRANKFURT/M", Latitude = 50.05m, Longitude = 8.6m },
        IssuedAtUtc = IssuedAt, RetrievedAtUtc = Utc(10, 12), Timeline = timeline, Series = series
    };

    [TestMethod]
    public void Build_UnsortedTimeline_ProducesChronologicalSteps()
    {
        // Arrange
        var input = Input(
            new[] { Utc(14, 0), Utc(12, 0), Utc(13, 0) },
            Series(QuantityKind.Temperature, 15.0, 13.0, 14.0));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(3);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.TemperatureC.Should().Be(13.0m);
        result.Run.Forecasts[1].ValidAtUtc.Should().Be(Utc(13, 0));
        result.Run.Forecasts[1].Values.TemperatureC.Should().Be(14.0m);
        result.Run.Forecasts[2].ValidAtUtc.Should().Be(Utc(14, 0));
        result.Run.Forecasts[2].Values.TemperatureC.Should().Be(15.0m);
    }

    [TestMethod]
    public void Build_MissingValue_DoesNotShiftLaterValues()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0), Utc(13, 0), Utc(14, 0) },
            Series(QuantityKind.Temperature, 14.0, null, 15.0));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(2);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.TemperatureC.Should().Be(14.0m);
        result.Run.Forecasts[1].ValidAtUtc.Should().Be(Utc(14, 0));
        result.Run.Forecasts[1].Values.TemperatureC.Should().Be(15.0m);
        result.OmittedValues.Should().ContainSingle();
        result.OmittedValues[0].Should().Be(new OmittedForecastValue(Utc(13, 0), QuantityKind.Temperature, OmissionReason.Missing));
    }

    [TestMethod]
    public void Build_IntervalQuantity_KeepsPeriodStartAndEnd()
    {
        // Arrange
        var period = new AveragingPeriod(Utc(11, 50), Utc(12, 0));
        var input = Input(
            new[] { Utc(12, 0) },
            IntervalSeries(QuantityKind.PrecipitationAccumulation, new double?[] { 0.5 }, new AveragingPeriod?[] { period }));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(1);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.RainRateMmH.Should().NotBeNull();
        result.Run.Forecasts[0].Values.RainRateMmH!.Value.Should().Be(3m);
        result.Run.Forecasts[0].Values.RainRateMmH!.PeriodStartUtc.Should().Be(Utc(11, 50));
        result.Run.Forecasts[0].Values.RainRateMmH!.PeriodEndUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.RainRateMmH!.Aggregation.Should().Be(IntervalAggregation.Mean);
        result.Run.Forecasts[0].Values.PrecipitationAmountMm.Should().NotBeNull();
        result.Run.Forecasts[0].Values.PrecipitationAmountMm!.Value.Should().Be(0.5m);
        result.Run.Forecasts[0].Values.PrecipitationAmountMm!.PeriodStartUtc.Should().Be(Utc(11, 50));
        result.Run.Forecasts[0].Values.PrecipitationAmountMm!.PeriodEndUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.PrecipitationAmountMm!.Aggregation.Should().Be(IntervalAggregation.Sum);
    }

    [TestMethod]
    public void Build_RadiationEnergy_IsWrittenAsIrradianceInterval()
    {
        // Arrange
        var period = new AveragingPeriod(Utc(11, 50), Utc(12, 0));
        var input = Input(
            new[] { Utc(12, 0) },
            IntervalSeries(QuantityKind.RadiationEnergy, new double?[] { 360000 }, new AveragingPeriod?[] { period }));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(1);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.SolarIrradianceWm2.Should().NotBeNull();
        result.Run.Forecasts[0].Values.SolarIrradianceWm2!.Value.Should().Be(600m);
        result.Run.Forecasts[0].Values.SolarIrradianceWm2!.PeriodStartUtc.Should().Be(Utc(11, 50));
        result.Run.Forecasts[0].Values.SolarIrradianceWm2!.PeriodEndUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.SolarIrradianceWm2!.Aggregation.Should().Be(IntervalAggregation.Mean);
    }

    [TestMethod]
    public void Build_IntervalWithoutPeriodAtIndex_IsOmittedWithMissingPeriod()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0), Utc(13, 0) },
            Series(QuantityKind.Temperature, 20.0, 21.0),
            IntervalSeries(QuantityKind.PrecipitationRate, new double?[] { 1.5, 2.5 }, new AveragingPeriod?[] { new AveragingPeriod(Utc(11, 0), Utc(12, 0)), null }));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(2);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.RainRateMmH.Should().NotBeNull();
        result.Run.Forecasts[0].Values.RainRateMmH!.Value.Should().Be(1.5m);
        result.Run.Forecasts[1].ValidAtUtc.Should().Be(Utc(13, 0));
        result.Run.Forecasts[1].Values.RainRateMmH.Should().BeNull();
        result.OmittedValues.Should().ContainSingle();
        result.OmittedValues[0].Should().Be(new OmittedForecastValue(Utc(13, 0), QuantityKind.PrecipitationRate, OmissionReason.MissingPeriod));
    }

    [TestMethod]
    public void Build_WithRealServices_RoundsOnlyAtOutput()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Series(QuantityKind.Temperature, 20.25),
            new ForecastSeries
            {
                Kind = QuantityKind.Pressure,
                Values = new double?[] { 1000.5 },
                PressureUnit = PressureUnit.Hectopascal,
                PressureReference = PressureReference.SeaLevel,
                Product = MosmixProduct.MosmixL,
                MosmixId = "10637",
                IssuedAtUtc = IssuedAt
            },
            new ForecastSeries
            {
                Kind = QuantityKind.MeanWindSpeed,
                Values = new double?[] { 3.47 },
                WindSpeedUnit = WindSpeedUnit.MetresPerSecond,
                Product = MosmixProduct.MosmixL,
                MosmixId = "10637",
                IssuedAtUtc = IssuedAt
            });

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(1);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.TemperatureC!.Value.ToString(CultureInfo.InvariantCulture).Should().Be("20.3");
        result.Run.Forecasts[0].Values.PressureHpa.Should().Be(1001m);
        result.Run.Forecasts[0].Values.PressureMmHg.Should().Be(750m);
        result.Run.Forecasts[0].Values.PressureReference.Should().Be(PressureReference.SeaLevel);
        result.Run.Forecasts[0].Values.WindSpeedKmh.Should().Be(12m);
    }

    [TestMethod]
    public void Build_GustSeries_IsNotWrittenAsWindSpeed()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Series(QuantityKind.Temperature, 20.0),
            new ForecastSeries { Kind = QuantityKind.WindGust, Values = new double?[] { 40.0 }, Periods = new AveragingPeriod?[] { new AveragingPeriod(Utc(11, 0), Utc(12, 0)) }, WindSpeedUnit = WindSpeedUnit.KilometresPerHour, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt });

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(1);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.WindSpeedKmh.Should().BeNull();
        result.Run.Forecasts[0].Values.WindGustKmh!.Value.Should().Be(40m);
        result.Run.Forecasts[0].Values.WindGustKmh!.Aggregation.Should().Be(IntervalAggregation.Maximum);
        result.OmittedValues.Should().BeEmpty();
    }

    [TestMethod]
    public void Build_ProbabilitySeries_IsNotWrittenAsRainRate()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Series(QuantityKind.Temperature, 20.0),
            Series(QuantityKind.Probability, 40.0));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(1);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.RainRateMmH.Should().BeNull();
        result.OmittedValues.Should().ContainSingle();
        result.OmittedValues[0].Should().Be(new OmittedForecastValue(Utc(12, 0), QuantityKind.Probability, OmissionReason.NotExportable));
    }

    [TestMethod]
    public void Build_MissingHumidity_IsOmittedNotZero()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Series(QuantityKind.Temperature, 20.0),
            Series(QuantityKind.RelativeHumidity, (double?)null));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(1);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.HumidityPercent.Should().BeNull();
        result.OmittedValues.Should().ContainSingle();
        result.OmittedValues[0].Should().Be(new OmittedForecastValue(Utc(12, 0), QuantityKind.RelativeHumidity, OmissionReason.Missing));
    }

    [TestMethod]
    public void Build_Source_IsProductSpecific()
    {
        // Arrange
        var input = WithProduct(MosmixProduct.MosmixS, Input(
            new[] { Utc(12, 0) },
            new ForecastSeries
            {
                Kind = QuantityKind.Temperature,
                Values = new double?[] { 20.0 },
                Product = MosmixProduct.MosmixS,
                MosmixId = "10637",
                IssuedAtUtc = IssuedAt
            }));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Source.Should().Be("dwd-mosmix-s:10637");
        result.Run.Product.Should().Be(MosmixProduct.MosmixS);
    }

    [TestMethod]
    public async Task Build_Result_SerializesInBothFormats()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Series(QuantityKind.Temperature, 20.0),
            Series(QuantityKind.RelativeHumidity, 55.0));

        var result = _subject.Build(input);

        // Act
        var jsonSerializer = new JsonExportSerializer();
        var xmlSerializer = new XmlExportSerializer();

        using var jsonStream = new MemoryStream();
        using var xmlStream = new MemoryStream();

        await jsonSerializer.SerializeAsync(result.Run, jsonStream, default);
        await xmlSerializer.SerializeAsync(result.Run, xmlStream, default);

        // Assert
        jsonStream.Length.Should().BeGreaterThan(0);
        xmlStream.Length.Should().BeGreaterThan(0);
    }
}
