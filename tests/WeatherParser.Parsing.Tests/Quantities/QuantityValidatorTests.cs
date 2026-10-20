using System.Globalization;
using FluentAssertions;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Quantities;

[TestClass]
public sealed class QuantityValidatorTests
{
    private IQuantityValidator _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new QuantityValidator();
    }

    [TestMethod]
    [DataRow(QuantityKind.Temperature, double.NaN)]
    [DataRow(QuantityKind.Temperature, double.PositiveInfinity)]
    [DataRow(QuantityKind.Pressure, double.NegativeInfinity)]
    [DataRow(QuantityKind.Illuminance, double.NaN)]
    public void Validate_NonFiniteValues_AreRejected(QuantityKind kind, double value)
    {
        var result = _subject.Validate(kind, value);

        result.IsValid.Should().BeFalse();
        result.Value.Should().BeNull();
        result.RejectionReason.Should().Be(QuantityRejectionReason.NonFinite);
    }

    [TestMethod]
    [DataRow(QuantityKind.Temperature)]
    [DataRow(QuantityKind.Pressure)]
    [DataRow(QuantityKind.RelativeHumidity)]
    [DataRow(QuantityKind.WindDirection)]
    [DataRow(QuantityKind.MeanWindSpeed)]
    [DataRow(QuantityKind.WindGust)]
    [DataRow(QuantityKind.Illuminance)]
    [DataRow(QuantityKind.Irradiance)]
    [DataRow(QuantityKind.RadiationEnergy)]
    [DataRow(QuantityKind.PrecipitationRate)]
    [DataRow(QuantityKind.PrecipitationAccumulation)]
    [DataRow(QuantityKind.Probability)]
    public void Validate_MissingValue_IsRejectedAndNeverDefaulted(QuantityKind kind)
    {
        var result = _subject.Validate(kind, null);

        result.Value.Should().BeNull();
        result.RejectionReason.Should().Be(QuantityRejectionReason.Missing);
    }

    [TestMethod]
    public void Validate_MissingWindDirection_IsNeverDefaultedToNorth()
    {
        var result = _subject.Validate(QuantityKind.WindDirection, null);

        result.IsValid.Should().BeFalse();
        result.Value.Should().BeNull();
        result.RejectionReason.Should().Be(QuantityRejectionReason.Missing);
    }

    [TestMethod]
    [DataRow(-0.1)]
    [DataRow(100.1)]
    public void Validate_HumidityOutsideRange_IsRejected(double value)
    {
        var result = _subject.Validate(QuantityKind.RelativeHumidity, value);

        result.IsValid.Should().BeFalse();
        result.Value.Should().BeNull();
        result.RejectionReason.Should().Be(QuantityRejectionReason.OutOfRange);
    }

    [TestMethod]
    [DataRow(0d, "0")]
    [DataRow(100d, "100")]
    public void Validate_HumidityBoundaries_AreAccepted(double value, string expected)
    {
        var result = _subject.Validate(QuantityKind.RelativeHumidity, value);

        result.IsValid.Should().BeTrue();
        result.Value.Should().Be(decimal.Parse(expected, CultureInfo.InvariantCulture));
        result.RejectionReason.Should().BeNull();
    }

    [TestMethod]
    [DataRow(0d, "0")]
    [DataRow(-12.5d, "-12.5")]
    [DataRow(-0.1d, "-0.1")]
    public void Validate_ZeroAndNegativeTemperature_AreKept(double value, string expected)
    {
        var result = _subject.Validate(QuantityKind.Temperature, value);

        result.IsValid.Should().BeTrue();
        result.Value.Should().Be(decimal.Parse(expected, CultureInfo.InvariantCulture));
        result.RejectionReason.Should().BeNull();
    }

    [TestMethod]
    [DataRow(0d, "0")]
    [DataRow(90d, "90")]
    [DataRow(359.9d, "359.9")]
    [DataRow(360d, "0")]
    public void Validate_WindDirection_AcceptsZeroToThreeSixtyAndNormalizesThreeSixty(double value, string expected)
    {
        var result = _subject.Validate(QuantityKind.WindDirection, value);

        result.IsValid.Should().BeTrue();
        result.Value.Should().Be(decimal.Parse(expected, CultureInfo.InvariantCulture));
        result.RejectionReason.Should().BeNull();
    }

    [TestMethod]
    [DataRow(-1d)]
    [DataRow(-0.1d)]
    [DataRow(360.1d)]
    public void Validate_WindDirectionOutsideRange_IsRejected(double value)
    {
        var result = _subject.Validate(QuantityKind.WindDirection, value);

        result.IsValid.Should().BeFalse();
        result.Value.Should().BeNull();
        result.RejectionReason.Should().Be(QuantityRejectionReason.OutOfRange);
    }

    [TestMethod]
    [DataRow(QuantityKind.Pressure, 0d)]
    [DataRow(QuantityKind.Pressure, -1d)]
    [DataRow(QuantityKind.MeanWindSpeed, -0.1d)]
    [DataRow(QuantityKind.WindGust, -0.1d)]
    [DataRow(QuantityKind.Illuminance, -1d)]
    [DataRow(QuantityKind.Irradiance, -0.1d)]
    [DataRow(QuantityKind.RadiationEnergy, -1d)]
    [DataRow(QuantityKind.PrecipitationRate, -0.1d)]
    [DataRow(QuantityKind.PrecipitationAccumulation, -0.1d)]
    [DataRow(QuantityKind.Probability, -0.1d)]
    [DataRow(QuantityKind.Probability, 100.1d)]
    public void Validate_KindRanges_RejectValuesOutsideRange(QuantityKind kind, double value)
    {
        var result = _subject.Validate(kind, value);

        result.IsValid.Should().BeFalse();
        result.Value.Should().BeNull();
        result.RejectionReason.Should().Be(QuantityRejectionReason.OutOfRange);
    }

    [TestMethod]
    [DataRow(QuantityKind.Pressure, 0.1d, "0.1")]
    [DataRow(QuantityKind.Pressure, 1013.2d, "1013.2")]
    [DataRow(QuantityKind.MeanWindSpeed, 0d, "0")]
    [DataRow(QuantityKind.WindGust, 0d, "0")]
    [DataRow(QuantityKind.Illuminance, 0d, "0")]
    [DataRow(QuantityKind.Irradiance, 0d, "0")]
    [DataRow(QuantityKind.RadiationEnergy, 0d, "0")]
    [DataRow(QuantityKind.PrecipitationRate, 0d, "0")]
    [DataRow(QuantityKind.PrecipitationAccumulation, 0d, "0")]
    [DataRow(QuantityKind.Probability, 0d, "0")]
    [DataRow(QuantityKind.Probability, 100d, "100")]
    public void Validate_KindRanges_AcceptBoundaryValues(QuantityKind kind, double value, string expected)
    {
        var result = _subject.Validate(kind, value);

        result.IsValid.Should().BeTrue();
        result.Value.Should().Be(decimal.Parse(expected, CultureInfo.InvariantCulture));
        result.RejectionReason.Should().BeNull();
    }

    [TestMethod]
    [DataRow(QuantityKind.Temperature, 1e30)]
    [DataRow(QuantityKind.Illuminance, 1e300)]
    [DataRow(QuantityKind.Temperature, 7.922816251426434E+28)]
    [DataRow(QuantityKind.Temperature, -7.922816251426434E+28)]
    public void Validate_FiniteValueOutsideDecimalRange_IsOutOfRange(QuantityKind kind, double value)
    {
        var result = _subject.Validate(kind, value);

        result.IsValid.Should().BeFalse();
        result.Value.Should().BeNull();
        result.RejectionReason.Should().Be(QuantityRejectionReason.OutOfRange);
    }

    [TestMethod]
    public void Validate_UndefinedKind_ThrowsArgumentOutOfRangeException()
    {
        FluentActions.Invoking(() => _subject.Validate((QuantityKind)999, 1d)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    [DataRow(QuantityKind.RelativeHumidity, 100.00000000000001)]
    [DataRow(QuantityKind.WindDirection, 360.00000000000006)]
    [DataRow(QuantityKind.Illuminance, -1e-30)]
    [DataRow(QuantityKind.Probability, -1e-30)]
    [DataRow(QuantityKind.Pressure, 1e-30)]
    public void Validate_OutOfRangeBeforeDecimalConversion_IsRejected(QuantityKind kind, double value)
    {
        var result = _subject.Validate(kind, value);

        result.IsValid.Should().BeFalse();
        result.Value.Should().BeNull();
        result.RejectionReason.Should().Be(QuantityRejectionReason.OutOfRange);
    }
}
