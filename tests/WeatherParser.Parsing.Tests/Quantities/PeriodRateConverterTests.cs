using FluentAssertions;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Quantities;

[TestClass]
public sealed class PeriodRateConverterTests
{
    private IPeriodRateConverter _subject = null!;

    private static DateTime Utc(int hour, int minute, int second) => new DateTime(2026, 7, 15, hour, minute, second, DateTimeKind.Utc);

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new PeriodRateConverter();
    }

    [TestMethod]
    public void ToMeanIrradiance_UsesKnownPeriod()
    {
        var period = new AveragingPeriod(Utc(10, 0, 0), Utc(10, 10, 0));
        var result = _subject.Convert(QuantityKind.RadiationEnergy, QuantityKind.Irradiance, 360000m, period);

        result.Value.Should().Be(600m);
        result.Kind.Should().Be(QuantityKind.Irradiance);
    }

    [TestMethod]
    public void ToMeanIrradiance_OneHour()
    {
        var period = new AveragingPeriod(Utc(10, 0, 0), Utc(11, 0, 0));
        var result = _subject.Convert(QuantityKind.RadiationEnergy, QuantityKind.Irradiance, 1800000m, period);

        result.Value.Should().Be(500m);
    }

    [TestMethod]
    public void ToMeanRainRate_UsesKnownPeriod()
    {
        var period = new AveragingPeriod(Utc(10, 0, 0), Utc(10, 10, 0));
        var result = _subject.Convert(QuantityKind.PrecipitationAccumulation, QuantityKind.PrecipitationRate, 0.5m, period);

        result.Value.Should().Be(3m);
        result.Kind.Should().Be(QuantityKind.PrecipitationRate);
    }

    [TestMethod]
    public void ToMeanRainRate_OneHour()
    {
        var period = new AveragingPeriod(Utc(10, 0, 0), Utc(11, 0, 0));
        var result = _subject.Convert(QuantityKind.PrecipitationAccumulation, QuantityKind.PrecipitationRate, 1.2m, period);

        result.Value.Should().Be(1.2m);
    }

    [TestMethod]
    public void PeriodTransforms_RetainPeriodInResult()
    {
        var period = new AveragingPeriod(Utc(10, 0, 0), Utc(10, 10, 0));

        var result1 = _subject.Convert(QuantityKind.RadiationEnergy, QuantityKind.Irradiance, 360000m, period);
        var result2 = _subject.Convert(QuantityKind.PrecipitationAccumulation, QuantityKind.PrecipitationRate, 0.5m, period);

        result1.Period.Should().BeSameAs(period);
        result2.Period.Should().BeSameAs(period);
    }

    [TestMethod]
    public void PeriodTransforms_RejectMissingPeriod()
    {
        FluentActions.Invoking(() => _subject.Convert(QuantityKind.RadiationEnergy, QuantityKind.Irradiance, 360000m, null))
            .Should().Throw<QuantityConversionException>();
        FluentActions.Invoking(() => _subject.Convert(QuantityKind.PrecipitationAccumulation, QuantityKind.PrecipitationRate, 0.5m, null))
            .Should().Throw<QuantityConversionException>();
    }

    [TestMethod]
    public void PeriodTransforms_ResultIsUnrounded()
    {
        var period = new AveragingPeriod(Utc(10, 0, 0), Utc(10, 7, 0));
        var result = _subject.Convert(QuantityKind.PrecipitationAccumulation, QuantityKind.PrecipitationRate, 0.1m, period);

        result.Value.Should().Be(0.1m * 3600m / 420m);
    }

    [TestMethod]
    public void ToMeanRainRate_LargeAccumulationOverOneHour_IsExact()
    {
        var period = new AveragingPeriod(Utc(10, 0, 0), Utc(11, 0, 0));
        var result = _subject.Convert(QuantityKind.PrecipitationAccumulation, QuantityKind.PrecipitationRate, 100000000000000000000000000m, period);

        result.Value.Should().Be(100000000000000000000000000m);
    }

    [TestMethod]
    public void ToMeanRainRate_IntermediateOverflowWithRepresentableResult_IsComputed()
    {
        var period = new AveragingPeriod(Utc(10, 0, 0), Utc(10, 0, 7));
        var result = _subject.Convert(QuantityKind.PrecipitationAccumulation, QuantityKind.PrecipitationRate, 100000000000000000000000000m, period);

        result.Value.Should().BeApproximately(51428571428571428571428571429m, 1m);
    }

    [TestMethod]
    public void ToMeanRainRate_UnrepresentableResult_RaisesQuantityConversionException()
    {
        var period = new AveragingPeriod(Utc(10, 0, 0), Utc(10, 0, 1));
        FluentActions.Invoking(() => _subject.Convert(QuantityKind.PrecipitationAccumulation, QuantityKind.PrecipitationRate, decimal.MaxValue, period))
            .Should().Throw<QuantityConversionException>();
    }
}
