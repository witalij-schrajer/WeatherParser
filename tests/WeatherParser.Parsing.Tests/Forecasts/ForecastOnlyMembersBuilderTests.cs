using System;
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
public sealed class ForecastOnlyMembersBuilderTests
{
    private IForecastRunBuilder _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new ForecastRunBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter());
    }

    private static readonly DateTime IssuedAt = new(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc);
    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);

    private static ForecastRunInput Input(DateTime[] timeline, params ForecastSeries[] series) => new()
    {
        Product = MosmixProduct.MosmixL, MosmixId = "10637",
        Location = new ForecastLocation { Name = "FRANKFURT/M", Latitude = 50.05m, Longitude = 8.6m },
        IssuedAtUtc = IssuedAt, RetrievedAtUtc = Utc(10, 12), Timeline = timeline, Series = series
    };

    private static AveragingPeriod Hour(int endHour) => new(Utc(endHour - 1, 0), Utc(endHour, 0));

    private static ForecastSeries Gust(double?[] values, AveragingPeriod?[]? periods, WindSpeedUnit? unit)
        => new()
        {
            Kind = QuantityKind.WindGust,
            Values = values,
            Periods = periods,
            WindSpeedUnit = unit,
            Product = MosmixProduct.MosmixL,
            MosmixId = "10637",
            IssuedAtUtc = IssuedAt
        };

    private static ForecastSeries Accumulation(double?[] values, AveragingPeriod?[] periods)
        => new()
        {
            Kind = QuantityKind.PrecipitationAccumulation,
            Values = values,
            Periods = periods,
            Product = MosmixProduct.MosmixL,
            MosmixId = "10637",
            IssuedAtUtc = IssuedAt
        };

    private static ForecastSeries Rate(double?[] values, AveragingPeriod?[] periods)
        => new()
        {
            Kind = QuantityKind.PrecipitationRate,
            Values = values,
            Periods = periods,
            Product = MosmixProduct.MosmixL,
            MosmixId = "10637",
            IssuedAtUtc = IssuedAt
        };

    private static ForecastSeries Temperature(params double?[] values)
        => new()
        {
            Kind = QuantityKind.Temperature,
            Values = values,
            Product = MosmixProduct.MosmixL,
            MosmixId = "10637",
            IssuedAtUtc = IssuedAt
        };

    [TestMethod]
    public void Build_GustSeries_ExportsIntervalMaximum()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Gust(new double?[] { 10.0 }, new AveragingPeriod?[] { Hour(12) }, WindSpeedUnit.MetresPerSecond));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(1);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.WindGustKmh.Should().NotBeNull();
        result.Run.Forecasts[0].Values.WindGustKmh!.Value.Should().Be(36m);
        result.Run.Forecasts[0].Values.WindGustKmh!.PeriodStartUtc.Should().Be(Utc(11, 0));
        result.Run.Forecasts[0].Values.WindGustKmh!.PeriodEndUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.WindGustKmh!.Aggregation.Should().Be(IntervalAggregation.Maximum);
        result.Run.Forecasts[0].Values.WindSpeedKmh.Should().BeNull();
        result.OmittedValues.Should().BeEmpty();
    }

    [TestMethod]
    public void Build_GustPeriodsNull_ThrowsArgumentException()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Gust(new double?[] { 10.0 }, null, WindSpeedUnit.MetresPerSecond));

        // Act
        Action act = () => _subject.Build(input);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Build_GustWithoutWindSpeedUnit_ThrowsArgumentException()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Gust(new double?[] { 10.0 }, new AveragingPeriod?[] { Hour(12) }, null));

        // Act
        Action act = () => _subject.Build(input);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Build_GustNullPeriodAtIndex_OmittedMissingPeriod()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0), Utc(13, 0) },
            Temperature(20.0, 21.0),
            Gust(new double?[] { 10.0, 12.0 }, new AveragingPeriod?[] { Hour(12), null }, WindSpeedUnit.MetresPerSecond));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(2);
        result.Run.Forecasts[1].ValidAtUtc.Should().Be(Utc(13, 0));
        result.Run.Forecasts[1].Values.WindGustKmh.Should().BeNull();
        result.OmittedValues.Should().ContainSingle().Which.Should().Be(new OmittedForecastValue(Utc(13, 0), QuantityKind.WindGust, OmissionReason.MissingPeriod));
    }

    [TestMethod]
    public void Build_NegativeGust_OmittedOutOfRange()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Temperature(20.0),
            Gust(new double?[] { -1.0 }, new AveragingPeriod?[] { Hour(12) }, WindSpeedUnit.MetresPerSecond));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(1);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.WindGustKmh.Should().BeNull();
        result.OmittedValues.Should().ContainSingle().Which.Should().Be(new OmittedForecastValue(Utc(12, 0), QuantityKind.WindGust, OmissionReason.OutOfRange));
    }

    [TestMethod]
    public void Build_TwoGustSeries_ThrowsArgumentException()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Gust(new double?[] { 10.0 }, new AveragingPeriod?[] { Hour(12) }, WindSpeedUnit.MetresPerSecond),
            Gust(new double?[] { 12.0 }, new AveragingPeriod?[] { Hour(12) }, WindSpeedUnit.MetresPerSecond));

        // Act
        Action act = () => _subject.Build(input);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Rounding_GustTiesAwayFromZero()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Gust(new double?[] { 12.5 }, new AveragingPeriod?[] { Hour(12) }, WindSpeedUnit.KilometresPerHour));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(1);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.WindGustKmh.Should().NotBeNull();
        result.Run.Forecasts[0].Values.WindGustKmh!.Value.Should().Be(13m);
    }

    [TestMethod]
    public void Build_AccumulationSeries_FillsRateAndAmount()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0), Utc(13, 0) },
            Accumulation(new double?[] { 1.5, 0.0 }, new AveragingPeriod?[] { Hour(12), Hour(13) }));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(2);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.RainRateMmH.Should().NotBeNull();
        result.Run.Forecasts[0].Values.RainRateMmH!.Value.Should().Be(1.5m);
        result.Run.Forecasts[0].Values.RainRateMmH!.Aggregation.Should().Be(IntervalAggregation.Mean);
        result.Run.Forecasts[0].Values.RainRateMmH!.PeriodStartUtc.Should().Be(Utc(11, 0));
        result.Run.Forecasts[0].Values.RainRateMmH!.PeriodEndUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.PrecipitationAmountMm.Should().NotBeNull();
        result.Run.Forecasts[0].Values.PrecipitationAmountMm!.Value.Should().Be(1.5m);
        result.Run.Forecasts[0].Values.PrecipitationAmountMm!.Aggregation.Should().Be(IntervalAggregation.Sum);
        result.Run.Forecasts[0].Values.PrecipitationAmountMm!.PeriodStartUtc.Should().Be(Utc(11, 0));
        result.Run.Forecasts[0].Values.PrecipitationAmountMm!.PeriodEndUtc.Should().Be(Utc(12, 0));

        result.Run.Forecasts[1].ValidAtUtc.Should().Be(Utc(13, 0));
        result.Run.Forecasts[1].Values.RainRateMmH.Should().NotBeNull();
        result.Run.Forecasts[1].Values.RainRateMmH!.Value.Should().Be(0m);
        result.Run.Forecasts[1].Values.RainRateMmH!.Aggregation.Should().Be(IntervalAggregation.Mean);
        result.Run.Forecasts[1].Values.RainRateMmH!.PeriodStartUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[1].Values.RainRateMmH!.PeriodEndUtc.Should().Be(Utc(13, 0));
        result.Run.Forecasts[1].Values.PrecipitationAmountMm.Should().NotBeNull();
        result.Run.Forecasts[1].Values.PrecipitationAmountMm!.Value.Should().Be(0m);
        result.Run.Forecasts[1].Values.PrecipitationAmountMm!.Aggregation.Should().Be(IntervalAggregation.Sum);
        result.Run.Forecasts[1].Values.PrecipitationAmountMm!.PeriodStartUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[1].Values.PrecipitationAmountMm!.PeriodEndUtc.Should().Be(Utc(13, 0));

        result.OmittedValues.Should().BeEmpty();
    }

    [TestMethod]
    public void Build_RateSeries_FillsOnlyRate()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Rate(new double?[] { 2.0 }, new AveragingPeriod?[] { Hour(12) }));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(1);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.RainRateMmH.Should().NotBeNull();
        result.Run.Forecasts[0].Values.RainRateMmH!.Value.Should().Be(2.0m);
        result.Run.Forecasts[0].Values.PrecipitationAmountMm.Should().BeNull();
    }

    [TestMethod]
    public void Build_RateAndAccumulationSeries_ThrowsArgumentException()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Rate(new double?[] { 2.0 }, new AveragingPeriod?[] { Hour(12) }),
            Accumulation(new double?[] { 1.5 }, new AveragingPeriod?[] { Hour(12) }));

        // Act
        Action act = () => _subject.Build(input);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Build_AccumulationMissingValue_OneOmissionBothAbsent()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0), Utc(13, 0) },
            Temperature(20.0, 21.0),
            Accumulation(new double?[] { null, 1.0 }, new AveragingPeriod?[] { Hour(12), Hour(13) }));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(2);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.RainRateMmH.Should().BeNull();
        result.Run.Forecasts[0].Values.PrecipitationAmountMm.Should().BeNull();
        result.OmittedValues.Should().ContainSingle().Which.Should().Be(new OmittedForecastValue(Utc(12, 0), QuantityKind.PrecipitationAccumulation, OmissionReason.Missing));
    }

    [TestMethod]
    public void Build_AccumulationNullPeriod_OneMissingPeriodBothAbsent()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0) },
            Temperature(20.0),
            Accumulation(new double?[] { 1.0 }, new AveragingPeriod?[] { null }));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(1);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.RainRateMmH.Should().BeNull();
        result.Run.Forecasts[0].Values.PrecipitationAmountMm.Should().BeNull();
        result.OmittedValues.Should().ContainSingle().Which.Should().Be(new OmittedForecastValue(Utc(12, 0), QuantityKind.PrecipitationAccumulation, OmissionReason.MissingPeriod));
    }

    [TestMethod]
    public void Build_AccumulationRateOverflow_KeepsAmountOmitsRate()
    {
        // Arrange
        var period = new AveragingPeriod(new DateTime(2026, 7, 15, 11, 59, 59, DateTimeKind.Utc), Utc(12, 0));
        var input = Input(
            new[] { Utc(12, 0) },
            Accumulation(new double?[] { 1e27 }, new AveragingPeriod?[] { period }));

        // Act
        var result = _subject.Build(input);

        // Assert
        result.Run.Forecasts.Should().HaveCount(1);
        result.Run.Forecasts[0].ValidAtUtc.Should().Be(Utc(12, 0));
        result.Run.Forecasts[0].Values.RainRateMmH.Should().BeNull();
        result.Run.Forecasts[0].Values.PrecipitationAmountMm.Should().NotBeNull();
        result.Run.Forecasts[0].Values.PrecipitationAmountMm!.Value.Should().Be(1000000000000000000000000000m);
        result.Run.Forecasts[0].Values.PrecipitationAmountMm!.Aggregation.Should().Be(IntervalAggregation.Sum);
        result.Run.Forecasts[0].Values.PrecipitationAmountMm!.PeriodStartUtc.Should().Be(period.StartUtc);
        result.Run.Forecasts[0].Values.PrecipitationAmountMm!.PeriodEndUtc.Should().Be(Utc(12, 0));
        result.OmittedValues.Should().ContainSingle().Which.Should().Be(new OmittedForecastValue(Utc(12, 0), QuantityKind.PrecipitationAccumulation, OmissionReason.Unrepresentable));
    }
}
