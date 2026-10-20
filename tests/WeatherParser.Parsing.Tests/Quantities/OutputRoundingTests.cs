using WeatherParser.Parsing.Quantities;
using FluentAssertions;
using System.Globalization;

namespace WeatherParser.Parsing.Tests.Quantities;

[TestClass]
public sealed class OutputRoundingTests
{
    private IOutputRounding _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new OutputRounding();
    }

    private static decimal D(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);

    [TestMethod]
    [DataRow(QuantityKind.Temperature, "20.25", "20.3")]
    [DataRow(QuantityKind.Temperature, "-20.25", "-20.3")]
    [DataRow(QuantityKind.Temperature, "20.24", "20.2")]
    [DataRow(QuantityKind.Temperature, "-20.35", "-20.4")]
    [DataRow(QuantityKind.Pressure, "1000.5", "1001")]
    [DataRow(QuantityKind.Pressure, "1013.49", "1013")]
    [DataRow(QuantityKind.RelativeHumidity, "54.5", "55")]
    [DataRow(QuantityKind.RelativeHumidity, "0.4", "0")]
    [DataRow(QuantityKind.MeanWindSpeed, "12.5", "13")]
    [DataRow(QuantityKind.WindGust, "12.492", "12")]
    [DataRow(QuantityKind.WindGust, "36.5", "37")]
    public void Round_UsesIncrementAndAwayFromZero(QuantityKind kind, string input, string expected)
    {
        var result = _subject.Round(kind, D(input));
        result.ToString(CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [TestMethod]
    [DataRow(QuantityKind.Temperature, "20", "20.0")]
    [DataRow(QuantityKind.Temperature, "20.30", "20.3")]
    [DataRow(QuantityKind.Pressure, "1013.0", "1013")]
    [DataRow(QuantityKind.RelativeHumidity, "100.00", "100")]
    public void Round_ResultScaleEqualsIncrementDigits(QuantityKind kind, string input, string expected)
    {
        var result = _subject.Round(kind, D(input));
        result.ToString(CultureInfo.InvariantCulture).Should().Be(expected);
    }

    [TestMethod]
    public void Round_NegativeZeroResult_IsReturnedAsZero()
    {
        var result = _subject.Round(QuantityKind.Temperature, D("-0.04"));
        result.ToString(CultureInfo.InvariantCulture).Should().Be("0.0");
        decimal.IsNegative(result).Should().BeFalse();
    }

    [TestMethod]
    [DataRow(QuantityKind.Irradiance, "600.123")]
    [DataRow(QuantityKind.Illuminance, "12345.67")]
    [DataRow(QuantityKind.PrecipitationRate, "3.25")]
    [DataRow(QuantityKind.Probability, "12.5")]
    [DataRow(QuantityKind.WindDirection, "359.9")]
    [DataRow(QuantityKind.RadiationEnergy, "360000.5")]
    [DataRow(QuantityKind.PrecipitationAccumulation, "0.05")]
    public void Round_KindWithoutIncrement_ReturnsValueUnchanged(QuantityKind kind, string input)
    {
        var result = _subject.Round(kind, D(input));
        result.ToString(CultureInfo.InvariantCulture).Should().Be(input);
    }

    [TestMethod]
    public void Round_UndefinedKind_ThrowsArgumentOutOfRangeException()
    {
        FluentActions.Invoking(() => _subject.Round((QuantityKind)999, 0m)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void Round_TemperatureThatCannotCarryOneDecimal_RaisesQuantityConversionException()
    {
        FluentActions.Invoking(() => _subject.Round(QuantityKind.Temperature, 70000000000000000000000000000m))
            .Should().Throw<QuantityConversionException>();
    }

    [TestMethod]
    public void Round_TemperatureThatCanCarryOneDecimal_KeepsScale()
    {
        var result = _subject.Round(QuantityKind.Temperature, 7000000000000000000000000000m);
        result.ToString(CultureInfo.InvariantCulture).Should().Be("7000000000000000000000000000.0");
    }
}
