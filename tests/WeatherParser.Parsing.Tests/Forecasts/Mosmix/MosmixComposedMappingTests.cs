using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public class MosmixComposedMappingTests
{
    private MosmixElementMapper _mapper = null!;
    private ForecastRunBuilder _builder = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mapper = new MosmixElementMapper(new MosmixUnitScaler());
        _builder = new ForecastRunBuilder(
            new QuantityValidator(),
            new OutputRounding(),
            new PressureConverter(new OutputRounding()),
            new PeriodRateConverter(),
            new WindSpeedConverter());
    }

    private static DateTime Utc(int hour) => new(2026, 10, 26, hour, 0, 0, DateTimeKind.Utc);

    private static MosmixForecastSeries Series(string name, params string?[] values) =>
        new() { ElementName = name, Values = values.ToList() };

    private static MosmixForecastDocument Document(DateTime[] timeline, params MosmixForecastSeries[] series) =>
        new()
        {
            MosmixId = "10637",
            IssuedAtUtc = Utc(15),
            Timeline = timeline.ToList(),
            LocationName = "BEISPIELSTADT",
            Latitude = 50.5m,
            Longitude = 9.6m,
            AltitudeM = 250.0m,
            Series = series.ToList()
        };

    private ForecastRunBuildResult Build(MosmixForecastDocument d) => _builder.Build(_mapper.Map(d, MosmixProduct.MosmixL, Utc(20), false).Input);

    [TestMethod]
    public void Composed_ValidDocument_RunWithConvertedValues()
    {
        // Arrange
        var document = Document(
            new[] { Utc(16) },
            Series("TTT", "293.55"),
            Series("PPPP", "101325"),
            Series("DD", "180"),
            Series("FF", "5"),
            Series("FX1", "10"),
            Series("Rad1h", "1800"),
            Series("RR1c", "1.2"));

        // Act
        var result = Build(document);
        var run = result.Run;

        // Assert
        run.Forecasts.Should().HaveCount(1);
        var values = run.Forecasts[0].Values;
        values.TemperatureC.Should().Be(20.4m);
        values.PressureHpa.Should().Be(1013m);
        values.PressureMmHg.Should().Be(760m);
        values.PressureReference.Should().Be(PressureReference.Unknown);
        values.WindDirectionDeg.Should().Be(180m);
        values.WindSpeedKmh.Should().Be(18m);
        values.WindGustKmh!.Value.Should().Be(36m);
        values.WindGustKmh!.Aggregation.Should().Be(IntervalAggregation.Maximum);
        values.WindGustKmh!.PeriodStartUtc.Should().Be(Utc(15));
        values.WindGustKmh!.PeriodEndUtc.Should().Be(Utc(16));
        values.SolarIrradianceWm2!.Value.Should().Be(500m);
        values.SolarIrradianceWm2!.Aggregation.Should().Be(IntervalAggregation.Mean);
        values.RainRateMmH!.Value.Should().Be(1.2m);
        values.RainRateMmH!.Aggregation.Should().Be(IntervalAggregation.Mean);
        values.PrecipitationAmountMm!.Value.Should().Be(1.2m);
        values.PrecipitationAmountMm!.Aggregation.Should().Be(IntervalAggregation.Sum);
        values.HumidityPercent.Should().BeNull();
        values.IlluminanceLux.Should().BeNull();

        result.OmittedValues.Should().BeEmpty();
        run.Product.Should().Be(MosmixProduct.MosmixL);
        run.Source.Should().Be("dwd-mosmix-l:10637");
        run.IssuedAtUtc.Should().Be(Utc(15));
        run.RetrievedAtUtc.Should().Be(Utc(20));
        run.RunAgeExceeded.Should().BeFalse();
    }

    [TestMethod]
    public void Composed_ZeroValues_Kept()
    {
        // Arrange
        var document = Document(
            new[] { Utc(16) },
            Series("TTT", "273.15"),
            Series("FF", "0"),
            Series("RR1c", "0"));

        // Act
        var result = Build(document);
        var run = result.Run;

        // Assert
        run.Forecasts.Should().HaveCount(1);
        var values = run.Forecasts[0].Values;
        values.TemperatureC.Should().Be(0.0m);
        values.WindSpeedKmh.Should().Be(0m);
        values.RainRateMmH!.Value.Should().Be(0m);
        values.PrecipitationAmountMm!.Value.Should().Be(0m);
    }

    [TestMethod]
    public void Composed_OutOfRange_OmittedNotClamped()
    {
        // Arrange
        var document = Document(
            new[] { Utc(16) },
            Series("TTT", "283.15"),
            Series("FF", "-1"),
            Series("PPPP", "0"));

        // Act
        var result = Build(document);
        var run = result.Run;

        // Assert
        run.Forecasts.Should().HaveCount(1);
        var values = run.Forecasts[0].Values;
        values.TemperatureC.Should().Be(10.0m);
        values.WindSpeedKmh.Should().BeNull();
        values.PressureHpa.Should().BeNull();

        result.OmittedValues.Should().Equal(
            new OmittedForecastValue(Utc(16), QuantityKind.Pressure, OmissionReason.OutOfRange),
            new OmittedForecastValue(Utc(16), QuantityKind.MeanWindSpeed, OmissionReason.OutOfRange));
    }

    [DataRow("0", true, 0.0)]
    [DataRow("360", true, 0.0)]
    [DataRow("-0.1", false, 0.0)]
    [DataRow("360.1", false, 0.0)]
    [DataRow("-0.000001", false, 0.0)]
    [DataRow("360.000001", false, 0.0)]
    [TestMethod]
    public void Composed_WindDirectionLimits(string token, bool kept, double expected)
    {
        // Arrange
        var document = Document(
            new[] { Utc(16) },
            Series("TTT", "283.15"),
            Series("DD", token));

        // Act
        var result = Build(document);
        var run = result.Run;

        // Assert
        run.Forecasts.Should().HaveCount(1);
        var values = run.Forecasts[0].Values;
        values.TemperatureC.Should().Be(10.0m);

        if (kept)
        {
            values.WindDirectionDeg.Should().Be((decimal)expected);
            result.OmittedValues.Should().BeEmpty();
        }
        else
        {
            values.WindDirectionDeg.Should().BeNull();
            result.OmittedValues.Should().Equal(
                new OmittedForecastValue(Utc(16), QuantityKind.WindDirection, OmissionReason.OutOfRange));
        }
    }

    [TestMethod]
    public void Composed_OnlyMissingAndOutOfRange_RejectedWithOmissions()
    {
        // Arrange
        var document = Document(
            new[] { Utc(16), Utc(17) },
            Series("TTT", null, null),
            Series("FF", "-1", null));

        // Act & Assert
        Action act = () => Build(document);
        act.Should().Throw<ForecastRunRejectedException>()
            .WithMessage("*")
            .Which.OmittedValues.Should().HaveCount(4)
            .And.ContainInOrder(
                new OmittedForecastValue(Utc(16), QuantityKind.Temperature, OmissionReason.Missing),
                new OmittedForecastValue(Utc(17), QuantityKind.Temperature, OmissionReason.Missing),
                new OmittedForecastValue(Utc(16), QuantityKind.MeanWindSpeed, OmissionReason.OutOfRange),
                new OmittedForecastValue(Utc(17), QuantityKind.MeanWindSpeed, OmissionReason.Missing));
    }

    [DataRow(1)]
    [DataRow(247)]
    [TestMethod]
    public void Composed_HorizonEqualsTimeline(int steps)
    {
        // Arrange
        var timeline = Enumerable.Range(0, steps).Select(i => Utc(16).AddHours(i)).ToArray();
        var document = Document(
            timeline,
            Series("TTT", Enumerable.Repeat("283.15", steps).ToArray()));

        // Act
        var result = Build(document);
        var run = result.Run;

        // Assert
        run.Forecasts.Should().HaveCount(steps);
        run.Forecasts[steps - 1].ValidAtUtc.Should().Be(timeline[steps - 1]);
    }

    [TestMethod]
    public void Composed_PrecisionKeptUntilOutputRounding()
    {
        // Arrange
        var document = Document(
            new[] { Utc(16) },
            Series("TTT", "293.399"),
            Series("PPPP", "100050"));

        // Act
        var result = Build(document);
        var run = result.Run;

        // Assert
        run.Forecasts.Should().HaveCount(1);
        var values = run.Forecasts[0].Values;
        values.TemperatureC.Should().Be(20.2m); // 20.249 °C rounded once on output; an earlier rounding to 20.25 would give 20.3
        values.PressureHpa.Should().Be(1001m); // from the unrounded 1000.5 hPa
        values.PressureMmHg.Should().Be(750m); // from the unrounded 1000.5 hPa; rounding the hPa first would give 751 mmHg

        result.OmittedValues.Should().BeEmpty();
    }

    [TestMethod]
    public void Composed_LeadingMissingValue_LaterValuesKeepTheirTimes()
    {
        // Arrange
        var document = Document(
            new[] { Utc(16), Utc(17), Utc(18) },
            Series("TTT", null, "283.15", "284.15"),
            Series("FX1", null, "10", "12"));

        // Act
        var result = Build(document);
        var run = result.Run;

        // Assert
        run.Forecasts.Should().HaveCount(2);
        run.Forecasts[0].ValidAtUtc.Should().Be(Utc(17));
        run.Forecasts[1].ValidAtUtc.Should().Be(Utc(18));

        run.Forecasts[0].Values.TemperatureC.Should().Be(10.0m);
        run.Forecasts[1].Values.TemperatureC.Should().Be(11.0m);

        run.Forecasts[0].Values.WindGustKmh!.Value.Should().Be(36m); // 10 m/s = 36 km/h
        run.Forecasts[1].Values.WindGustKmh!.Value.Should().Be(43m); // 12 m/s = 43.2 km/h rounded to 43

        run.Forecasts[0].Values.WindGustKmh!.PeriodStartUtc.Should().Be(Utc(16));
        run.Forecasts[0].Values.WindGustKmh!.PeriodEndUtc.Should().Be(Utc(17));
        run.Forecasts[1].Values.WindGustKmh!.PeriodStartUtc.Should().Be(Utc(17));
        run.Forecasts[1].Values.WindGustKmh!.PeriodEndUtc.Should().Be(Utc(18));

        result.OmittedValues.Should().Equal(
            new OmittedForecastValue(Utc(16), QuantityKind.Temperature, OmissionReason.Missing),
            new OmittedForecastValue(Utc(16), QuantityKind.WindGust, OmissionReason.Missing));
    }
}
