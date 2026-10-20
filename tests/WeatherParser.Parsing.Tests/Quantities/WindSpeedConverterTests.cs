using System.Globalization;
using FluentAssertions;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Quantities;

[TestClass]
public sealed class WindSpeedConverterTests
{
    private IWindSpeedConverter _subject = null!;
    private IOutputRounding _rounding = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new WindSpeedConverter();
        _rounding = new OutputRounding();
    }

    [TestMethod]
    [DataRow("10", "36.0")]
    [DataRow("3.47", "12.492")]
    [DataRow("0", "0.0")]
    [DataRow("0.5", "1.80")]
    public void ToKilometresPerHour_UsesExactFactor(string input, string expected)
    {
        _subject.ToKilometresPerHour(decimal.Parse(input, CultureInfo.InvariantCulture))
            .ToString(CultureInfo.InvariantCulture)
            .Should().Be(expected);
    }

    [TestMethod]
    public void ToKilometresPerHour_ThenOutputRounding_RoundsOnlyAfterConversion()
    {
        var mean = _rounding.Round(QuantityKind.MeanWindSpeed, _subject.ToKilometresPerHour(3.47m));
        mean.Should().Be(12m);
        mean.ToString(CultureInfo.InvariantCulture).Should().Be("12");

        _rounding.Round(QuantityKind.WindGust, _subject.ToKilometresPerHour(3.75m))
            .Should().Be(14m);

        // Converting first: 3.4 m/s = 12.24 km/h -> 12. Rounding first would give 3 m/s = 10.8 km/h -> 11.
        _rounding.Round(QuantityKind.MeanWindSpeed, _subject.ToKilometresPerHour(3.4m))
            .Should().Be(12m);
    }

    [TestMethod]
    public void ToKilometresPerHour_ResultOutsideDecimalRange_RaisesQuantityConversionException()
    {
        FluentActions.Invoking(() => _subject.ToKilometresPerHour(3e28m))
            .Should().Throw<QuantityConversionException>()
            .WithInnerException<OverflowException>();
    }
}
