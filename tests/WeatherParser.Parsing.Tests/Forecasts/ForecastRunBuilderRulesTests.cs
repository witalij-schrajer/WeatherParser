using FluentAssertions;
using Moq;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Forecasts;

[TestClass]
public sealed class ForecastRunBuilderRulesTests
{
    private MockRepository _repository = null!;
    private IForecastRunBuilder _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _repository = new MockRepository(MockBehavior.Strict);
        _subject = new ForecastRunBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _repository.VerifyAll();
    }

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

    private static ForecastSeries SeriesFor(QuantityKind kind) =>
        kind is QuantityKind.Irradiance or QuantityKind.RadiationEnergy or QuantityKind.PrecipitationRate or QuantityKind.PrecipitationAccumulation
            ? IntervalSeries(kind, new double?[] { 1.0 }, new AveragingPeriod?[] { Hour() })
            : Series(kind, 1.0);

    private static ForecastLocation Location() => new() { Name = "FRANKFURT/M", Latitude = 50.05m, Longitude = 8.6m };

    private static ForecastRunInput Input(DateTime[] timeline, params ForecastSeries?[] series) => new()
    {
        Product = MosmixProduct.MosmixL, MosmixId = "10637", Location = Location(),
        IssuedAtUtc = IssuedAt, RetrievedAtUtc = Utc(10, 12), Timeline = timeline, Series = series
    };

    private void ShouldBeRejected(ForecastRunInput input) =>
        FluentActions.Invoking(() => _subject.Build(input)).Should().Throw<ForecastRunRejectedException>();

    private void ShouldBeCallerError(ForecastRunInput input) =>
        FluentActions.Invoking(() => _subject.Build(input)).Should().Throw<ArgumentException>();

    [TestMethod]
    public void Build_MissingIssuedAt_IsRejected()
    {
        var input = new ForecastRunInput
        {
            Product = MosmixProduct.MosmixL, MosmixId = "10637", Location = Location(), IssuedAtUtc = null,
            RetrievedAtUtc = Utc(10, 12), Timeline = Noon(), Series = new[] { Series(QuantityKind.Temperature, 20.0) }
        };

        ShouldBeRejected(input);
    }

    [TestMethod]
    public void Build_DuplicateValidAt_IsRejected()
    {
        ShouldBeRejected(Input(new[] { Utc(12, 0), Utc(12, 0) }, Series(QuantityKind.Temperature, 20.0, 21.0)));
    }

    [TestMethod]
    public void Build_SeriesLengthDiffersFromTimeline_IsRejected()
    {
        ShouldBeRejected(Input(new[] { Utc(12, 0), Utc(13, 0) }, Series(QuantityKind.Temperature, 20.0)));
    }

    [TestMethod]
    public void Build_PeriodsLengthDiffersFromTimeline_IsRejected()
    {
        ShouldBeRejected(Input(
            new[] { Utc(12, 0), Utc(13, 0) },
            IntervalSeries(QuantityKind.PrecipitationRate, new double?[] { 1.0, 2.0 }, new AveragingPeriod?[] { Hour() })));
    }

    [TestMethod]
    [DataRow("Product")]
    [DataRow("MosmixId")]
    [DataRow("IssuedAt")]
    public void Build_SeriesFromDifferentRun_IsRejected(string member)
    {
        var foreign = member switch
        {
            "Product" => new ForecastSeries { Kind = QuantityKind.Temperature, Values = new double?[] { 20.0 }, Product = MosmixProduct.MosmixS, MosmixId = "10637", IssuedAtUtc = IssuedAt },
            "MosmixId" => new ForecastSeries { Kind = QuantityKind.Temperature, Values = new double?[] { 20.0 }, Product = MosmixProduct.MosmixL, MosmixId = "10638", IssuedAtUtc = IssuedAt },
            "IssuedAt" => new ForecastSeries { Kind = QuantityKind.Temperature, Values = new double?[] { 20.0 }, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = Utc(10, 0) },
            _ => throw new ArgumentException($"Unknown member {member}.", nameof(member))
        };

        ShouldBeRejected(Input(Noon(), foreign, Series(QuantityKind.RelativeHumidity, 55.0)));
    }

    [TestMethod]
    public void Build_NoUsableValues_IsRejected()
    {
        ShouldBeRejected(Input(Noon(), Series(QuantityKind.Temperature, (double?)null), Series(QuantityKind.RelativeHumidity, double.NaN)));
    }

    [TestMethod]
    public void Build_EmptyTimeline_IsRejected()
    {
        ShouldBeRejected(Input(Array.Empty<DateTime>(), Series(QuantityKind.Temperature)));
    }

    [TestMethod]
    public void Build_MissingOrUndefinedProduct_IsRejected()
    {
        var series = new[] { Series(QuantityKind.Temperature, 20.0) };
        var missing = new ForecastRunInput { Product = null, MosmixId = "10637", Location = Location(), IssuedAtUtc = IssuedAt, RetrievedAtUtc = Utc(10, 12), Timeline = Noon(), Series = series };
        var undefined = new ForecastRunInput { Product = (MosmixProduct)9, MosmixId = "10637", Location = Location(), IssuedAtUtc = IssuedAt, RetrievedAtUtc = Utc(10, 12), Timeline = Noon(), Series = series };

        ShouldBeCallerError(missing);
        ShouldBeCallerError(undefined);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("106 37")]
    [DataRow("dwd:10637")]
    public void Build_InvalidMosmixId_IsRejected(string mosmixId)
    {
        var input = new ForecastRunInput
        {
            Product = MosmixProduct.MosmixL, MosmixId = mosmixId, Location = Location(), IssuedAtUtc = IssuedAt, RetrievedAtUtc = Utc(10, 12),
            Timeline = Noon(), Series = new[] { new ForecastSeries { Kind = QuantityKind.Temperature, Values = new double?[] { 20.0 }, Product = MosmixProduct.MosmixL, MosmixId = mosmixId, IssuedAtUtc = IssuedAt } }
        };

        ShouldBeCallerError(input);
    }

    [TestMethod]
    public void Build_MissingLocation_IsRejected()
    {
        var input = new ForecastRunInput
        {
            Product = MosmixProduct.MosmixL, MosmixId = "10637", Location = null, IssuedAtUtc = IssuedAt, RetrievedAtUtc = Utc(10, 12),
            Timeline = Noon(), Series = new[] { Series(QuantityKind.Temperature, 20.0) }
        };

        ShouldBeCallerError(input);
    }

    [TestMethod]
    public void Build_PressureWithoutUnitOrReference_IsRejected()
    {
        var withoutUnit = new ForecastSeries { Kind = QuantityKind.Pressure, Values = new double?[] { 1013.0 }, PressureReference = PressureReference.SeaLevel, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt };
        var withoutReference = new ForecastSeries { Kind = QuantityKind.Pressure, Values = new double?[] { 1013.0 }, PressureUnit = PressureUnit.Hectopascal, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt };

        ShouldBeCallerError(Input(Noon(), withoutUnit));
        ShouldBeCallerError(Input(Noon(), withoutReference));
    }

    [TestMethod]
    public void Build_MeanWindWithoutUnit_IsRejected()
    {
        ShouldBeCallerError(Input(Noon(), Series(QuantityKind.Temperature, 20.0), Series(QuantityKind.MeanWindSpeed, 3.0)));
    }

    [TestMethod]
    [DataRow(QuantityKind.Irradiance, QuantityKind.RadiationEnergy)]
    [DataRow(QuantityKind.PrecipitationRate, QuantityKind.PrecipitationAccumulation)]
    [DataRow(QuantityKind.Temperature, QuantityKind.Temperature)]
    public void Build_DuplicateTargetField_IsRejected(QuantityKind first, QuantityKind second)
    {
        ShouldBeCallerError(Input(Noon(), SeriesFor(first), SeriesFor(second)));
    }

    [TestMethod]
    public void Build_NonUtcOrSubSecondTimeline_IsRejected()
    {
        ShouldBeCallerError(Input(new[] { new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Local) }, Series(QuantityKind.Temperature, 20.0)));
        ShouldBeCallerError(Input(new[] { Utc(12, 0).AddMilliseconds(1) }, Series(QuantityKind.Temperature, 20.0)));
    }

    [TestMethod]
    public void Build_NonUtcIssuedAt_IsRejected()
    {
        var issued = DateTime.SpecifyKind(IssuedAt, DateTimeKind.Unspecified);
        var input = new ForecastRunInput
        {
            Product = MosmixProduct.MosmixL, MosmixId = "10637", Location = Location(), IssuedAtUtc = issued, RetrievedAtUtc = Utc(10, 12), Timeline = Noon(),
            Series = new[] { new ForecastSeries { Kind = QuantityKind.Temperature, Values = new double?[] { 20.0 }, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = issued } }
        };

        ShouldBeCallerError(input);
    }

    [TestMethod]
    public void Build_SeriesWithoutProvenance_IsRejected()
    {
        var series = new ForecastSeries { Kind = QuantityKind.Temperature, Values = new double?[] { 20.0 }, Product = MosmixProduct.MosmixL, IssuedAtUtc = IssuedAt };

        ShouldBeCallerError(Input(Noon(), series));
    }

    [TestMethod]
    public void Build_NullCollections_AreRejected()
    {
        var series = new[] { Series(QuantityKind.Temperature, 20.0) };
        ShouldBeCallerError(new ForecastRunInput { Product = MosmixProduct.MosmixL, MosmixId = "10637", Location = Location(), IssuedAtUtc = IssuedAt, RetrievedAtUtc = Utc(10, 12), Timeline = null, Series = series });
        ShouldBeCallerError(new ForecastRunInput { Product = MosmixProduct.MosmixL, MosmixId = "10637", Location = Location(), IssuedAtUtc = IssuedAt, RetrievedAtUtc = Utc(10, 12), Timeline = Noon(), Series = null });
        ShouldBeCallerError(Input(Noon(), (ForecastSeries?)null));
        ShouldBeCallerError(Input(Noon(), new ForecastSeries { Kind = QuantityKind.Temperature, Values = null, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt }));
    }

    [TestMethod]
    public void Build_IntervalKindWithoutPeriods_IsRejected()
    {
        ShouldBeCallerError(Input(Noon(), Series(QuantityKind.Temperature, 20.0), Series(QuantityKind.PrecipitationRate, 1.0)));
    }

    [TestMethod]
    public void Build_PeriodsOnPointKind_IsRejected()
    {
        ShouldBeCallerError(Input(Noon(), IntervalSeries(QuantityKind.Temperature, new double?[] { 20.0 }, new AveragingPeriod?[] { Hour() })));
    }

    [TestMethod]
    [DataRow("HumidityNaN", OmissionReason.NonFinite)]
    [DataRow("HumidityAboveRange", OmissionReason.OutOfRange)]
    [DataRow("GustNaN", OmissionReason.NotExportable)]
    [DataRow("UnrepresentableWind", OmissionReason.Unrepresentable)]
    public void Build_OmissionReason_FollowsPrecedence(string scenario, OmissionReason expected)
    {
        var series = scenario switch
        {
            "HumidityNaN" => Series(QuantityKind.RelativeHumidity, double.NaN),
            "HumidityAboveRange" => Series(QuantityKind.RelativeHumidity, 101.0),
            "GustNaN" => Series(QuantityKind.WindGust, double.NaN),
            "UnrepresentableWind" => new ForecastSeries { Kind = QuantityKind.MeanWindSpeed, Values = new double?[] { 3e28 }, WindSpeedUnit = WindSpeedUnit.MetresPerSecond, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt },
            _ => throw new ArgumentException($"Unknown scenario {scenario}.", nameof(scenario))
        };

        var result = _subject.Build(Input(Noon(), Series(QuantityKind.Temperature, 20.0), series));

        result.OmittedValues.Should().ContainSingle().Which.Reason.Should().Be(expected);
    }

    [TestMethod]
    public void Build_DuplicateSeries_IsRejectedBeforeAnyServiceCall()
    {
        var validator = _repository.Create<IQuantityValidator>();
        var rounding = _repository.Create<IOutputRounding>();
        var pressure = _repository.Create<IPressureConverter>();
        var period = _repository.Create<IPeriodRateConverter>();
        var wind = _repository.Create<IWindSpeedConverter>();
        var subject = new ForecastRunBuilder(validator.Object, rounding.Object, pressure.Object, period.Object, wind.Object);

        FluentActions.Invoking(() => subject.Build(Input(Noon(), Series(QuantityKind.Temperature, 20.0), Series(QuantityKind.Temperature, 21.0))))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void Constructor_NullDependency_ThrowsArgumentNullException(int nullIndex)
    {
        var rounding = new OutputRounding();

        FluentActions.Invoking(() => new ForecastRunBuilder(
                nullIndex == 0 ? null! : new QuantityValidator(),
                nullIndex == 1 ? null! : rounding,
                nullIndex == 2 ? null! : new PressureConverter(rounding),
                nullIndex == 3 ? null! : new PeriodRateConverter(),
                nullIndex == 4 ? null! : new WindSpeedConverter()))
            .Should().Throw<ArgumentNullException>();
    }
}
