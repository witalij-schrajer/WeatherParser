using FluentAssertions;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Quantities;

[TestClass]
public sealed class QuantityKindSeparationTests
{
    private IPeriodRateConverter _subject = null!;
    private static readonly AveragingPeriod Period = new(new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 15, 10, 10, 0, DateTimeKind.Utc));

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new PeriodRateConverter();
    }

    [TestMethod]
    public void Convert_GustToMeanWind_IsRejected()
    {
        FluentActions.Invoking(() => _subject.Convert(QuantityKind.WindGust, QuantityKind.MeanWindSpeed, 10m, Period))
            .Should().Throw<QuantityConversionException>();
    }

    [TestMethod]
    public void Convert_AccumulationToRateWithoutPeriod_IsRejected()
    {
        FluentActions.Invoking(() => _subject.Convert(QuantityKind.PrecipitationAccumulation, QuantityKind.PrecipitationRate, 0.5m, null))
            .Should().Throw<QuantityConversionException>();
    }

    [TestMethod]
    public void Convert_IlluminanceToIrradiance_IsRejected()
    {
        FluentActions.Invoking(() => _subject.Convert(QuantityKind.Illuminance, QuantityKind.Irradiance, 50000m, Period))
            .Should().Throw<QuantityConversionException>();

        FluentActions.Invoking(() => _subject.Convert(QuantityKind.Irradiance, QuantityKind.Illuminance, 400m, Period))
            .Should().Throw<QuantityConversionException>();
    }

    [TestMethod]
    public void Convert_ProbabilityToAmount_IsRejected()
    {
        FluentActions.Invoking(() => _subject.Convert(QuantityKind.Probability, QuantityKind.PrecipitationAccumulation, 40m, Period))
            .Should().Throw<QuantityConversionException>();
    }

    [TestMethod]
    public void Convert_AllowedSourceWithWrongTarget_IsRejected()
    {
        FluentActions.Invoking(() => _subject.Convert(QuantityKind.RadiationEnergy, QuantityKind.PrecipitationRate, 360000m, Period))
            .Should().Throw<QuantityConversionException>();

        FluentActions.Invoking(() => _subject.Convert(QuantityKind.PrecipitationAccumulation, QuantityKind.Irradiance, 0.5m, Period))
            .Should().Throw<QuantityConversionException>();
    }

    [TestMethod]
    public void Convert_EveryPairExceptTheTwoDefinedOnes_IsRejected()
    {
        foreach (var source in Enum.GetValues<QuantityKind>())
        {
            foreach (var target in Enum.GetValues<QuantityKind>())
            {
                if ((source == QuantityKind.RadiationEnergy && target == QuantityKind.Irradiance) ||
                    (source == QuantityKind.PrecipitationAccumulation && target == QuantityKind.PrecipitationRate))
                {
                    continue;
                }

                FluentActions.Invoking(() => _subject.Convert(source, target, 1m, Period))
                    .Should().Throw<QuantityConversionException>()
                    .WithMessage($"*{source} -> {target}*");
            }
        }
    }

    [TestMethod]
    public void Convert_DefinedPairs_StillSucceed()
    {
        var result1 = _subject.Convert(QuantityKind.RadiationEnergy, QuantityKind.Irradiance, 360000m, Period);
        result1.Value.Should().Be(600m);

        var result2 = _subject.Convert(QuantityKind.PrecipitationAccumulation, QuantityKind.PrecipitationRate, 0.5m, Period);
        result2.Value.Should().Be(3m);
    }
}
