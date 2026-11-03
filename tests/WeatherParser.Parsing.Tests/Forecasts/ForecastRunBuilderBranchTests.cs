using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Forecasts;

[TestClass]
public sealed class ForecastRunBuilderBranchTests
{
    private IForecastRunBuilder _subject = null!;

    private static readonly DateTime IssuedAt = new(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc);
    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);
    private static DateTime[] Noon() => new[] { Utc(12, 0) };
    private static AveragingPeriod Hour() => new(Utc(11, 0), Utc(12, 0));

    private static ForecastSeries Series(QuantityKind kind, params double?[] values) => new()
    {
        Kind = kind, Values = values, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt
    };

    private static ForecastSeries IntervalSeries(QuantityKind kind, double?[] values, AveragingPeriod?[] periods) => new()
    {
        Kind = kind, Values = values, Periods = periods, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt
    };

    private static ForecastLocation Location() => new() { Name = "FRANKFURT/M", Latitude = 50.05m, Longitude = 8.6m };

    private static ForecastRunInput Input(DateTime[] timeline, params ForecastSeries?[] series) => new()
    {
        Product = MosmixProduct.MosmixL, MosmixId = "10637", Location = Location(),
        IssuedAtUtc = IssuedAt, RetrievedAtUtc = Utc(10, 12), Timeline = timeline, Series = series
    };

    private void ShouldBeCallerError(ForecastRunInput input) =>
        FluentActions.Invoking(() => _subject.Build(input)).Should().Throw<ArgumentException>();

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new ForecastRunBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter());
    }

    [TestMethod]
    public void Build_UndefinedKind_IsRejected()
    {
        var input = Input(Noon(), Series((QuantityKind)99, 1.0));

        ShouldBeCallerError(input);
    }

    [TestMethod]
    public void Build_SeriesIssuedAtNotUtc_IsRejected()
    {
        var issued = DateTime.SpecifyKind(IssuedAt, DateTimeKind.Local);
        var series = new ForecastSeries { Kind = QuantityKind.Temperature, Values = new double?[] { 20.0 }, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = issued };

        ShouldBeCallerError(Input(Noon(), series));
    }

    [TestMethod]
    [DataRow("PressureUnit")]
    [DataRow("PressureReference")]
    [DataRow("WindSpeedUnit")]
    public void Build_UndefinedUnitOnAnySeries_IsRejected(string member)
    {
        var undefined = member switch
        {
            "PressureUnit" => new ForecastSeries { Kind = QuantityKind.Temperature, Values = new double?[] { 20.0 }, PressureUnit = (PressureUnit)9, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt },
            "PressureReference" => new ForecastSeries { Kind = QuantityKind.Temperature, Values = new double?[] { 20.0 }, PressureReference = (PressureReference)9, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt },
            "WindSpeedUnit" => new ForecastSeries { Kind = QuantityKind.Temperature, Values = new double?[] { 20.0 }, WindSpeedUnit = (WindSpeedUnit)9, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt },
            _ => throw new ArgumentException($"Unknown member {member}.", nameof(member))
        };

        ShouldBeCallerError(Input(Noon(), undefined));
    }

    [TestMethod]
    public void Build_PressureInMmHg_UsesReverseConversion()
    {
        var result = _subject.Build(Input(Noon(), new ForecastSeries { Kind = QuantityKind.Pressure, Values = new double?[] { 760.0 }, PressureUnit = PressureUnit.MillimetreOfMercury, PressureReference = PressureReference.Unknown, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt }));

        result.Run.Forecasts[0].Values.PressureHpa.Should().Be(1013.0m);
        result.Run.Forecasts[0].Values.PressureMmHg.Should().Be(760.0m);
        result.Run.Forecasts[0].Values.PressureReference.Should().Be(PressureReference.Unknown);
    }

    [TestMethod]
    public void Build_WindDirection_IsWrittenUnchanged()
    {
        var result = _subject.Build(Input(Noon(), Series(QuantityKind.WindDirection, 270.5)));

        result.Run.Forecasts[0].Values.WindDirectionDeg.Should().Be(270.5m);
    }

    [TestMethod]
    public void Build_WindSpeedInKmh_IsOnlyRounded()
    {
        var result = _subject.Build(Input(Noon(), new ForecastSeries { Kind = QuantityKind.MeanWindSpeed, Values = new double?[] { 12.4 }, WindSpeedUnit = WindSpeedUnit.KilometresPerHour, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt }));

        result.Run.Forecasts[0].Values.WindSpeedKmh.Should().Be(12m);
    }

    [TestMethod]
    public void Build_Illuminance_IsWrittenUnchanged()
    {
        var result = _subject.Build(Input(Noon(), Series(QuantityKind.Illuminance, 12345.6)));

        result.Run.Forecasts[0].Values.IlluminanceLux.Should().Be(12345.6m);
    }

    [TestMethod]
    public void Build_DirectIrradiance_IsWrittenAsInterval()
    {
        var result = _subject.Build(Input(Noon(), IntervalSeries(QuantityKind.Irradiance, new double?[] { 612.5 }, new AveragingPeriod?[] { Hour() })));

        result.Run.Forecasts[0].Values.SolarIrradianceWm2!.Value.Should().Be(612.5m);
        result.Run.Forecasts[0].Values.SolarIrradianceWm2!.PeriodStartUtc.Should().Be(Utc(11, 0));
    }

    [TestMethod]
    public void Run_EmptySource_IsRejected()
    {
        FluentActions.Invoking(() => new ForecastRun().Source = "").Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Build_NoUsableValuesForProductS_IsRejected()
    {
        var input = new ForecastRunInput
        {
            Product = MosmixProduct.MosmixS,
            MosmixId = "10637",
            Location = Location(),
            IssuedAtUtc = IssuedAt,
            RetrievedAtUtc = Utc(10, 12),
            Timeline = Noon(),
            Series = new ForecastSeries?[] { new() { Kind = QuantityKind.Temperature, Values = new double?[] { null }, Product = MosmixProduct.MosmixS, MosmixId = "10637", IssuedAtUtc = IssuedAt } }
        };

        FluentActions.Invoking(() => _subject.Build(input)).Should().Throw<ForecastRunRejectedException>().WithMessage("*dwd-mosmix-s:10637*");
    }
}
