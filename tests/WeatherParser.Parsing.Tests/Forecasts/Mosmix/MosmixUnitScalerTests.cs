using System;
using System.Globalization;
using FluentAssertions;
using WeatherParser.Parsing.Forecasts.Mosmix;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public class MosmixUnitScalerTests
{
    private IMosmixUnitScaler _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new MosmixUnitScaler();
    }

    [TestMethod]
    [DataRow(273.15, 0.0)]
    [DataRow(253.15, -20.0)]
    [DataRow(0.0, -273.15)]
    [DataRow(300.25, 27.1)]
    [DataRow(293.399, 20.249)]
    public void KelvinToCelsius_ConvertsExactly(double kelvin, double expected)
    {
        // Arrange / Act / Assert
        _subject.KelvinToCelsius(kelvin).Should().Be(expected);
    }

    [TestMethod]
    [DataRow(101325.0, 1013.25)]
    [DataRow(0.0, 0.0)]
    [DataRow(100050.0, 1000.5)]
    [DataRow(100049.123456, 1000.49123456)]
    public void PascalToHectopascal_ConvertsExactly(double pascal, double expected)
    {
        // Arrange / Act / Assert
        _subject.PascalToHectopascal(pascal).Should().Be(expected);
    }

    [TestMethod]
    [DataRow(1.5, 1500.0)]
    [DataRow(0.0, 0.0)]
    public void KilojoulesToJoules_ConvertsExactly(double kilojoules, double expected)
    {
        // Arrange / Act / Assert
        _subject.KilojoulesToJoules(kilojoules).Should().Be(expected);
    }

    [TestMethod]
    [DataRow(0.0, 0.0)]
    [DataRow(2.35, 2.35)]
    public void KilogramsPerSquareMetreToMillimetres_Identity(double kilogramsPerSquareMetre, double expected)
    {
        // Arrange / Act / Assert
        _subject.KilogramsPerSquareMetreToMillimetres(kilogramsPerSquareMetre).Should().Be(expected);
    }

    [TestMethod]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(double.NegativeInfinity)]
    public void Scale_NonFinite_Throws(double value)
    {
        // Arrange / Act / Assert
        _subject.Invoking(s => s.KelvinToCelsius(value)).Should().Throw<ArgumentOutOfRangeException>();
        _subject.Invoking(s => s.PascalToHectopascal(value)).Should().Throw<ArgumentOutOfRangeException>();
        _subject.Invoking(s => s.KilojoulesToJoules(value)).Should().Throw<ArgumentOutOfRangeException>();
        _subject.Invoking(s => s.KilogramsPerSquareMetreToMillimetres(value)).Should().Throw<ArgumentOutOfRangeException>();
    }
}
