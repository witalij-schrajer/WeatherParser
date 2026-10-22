using System.Globalization;
using FluentAssertions;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Geography;

namespace WeatherParser.Parsing.Tests.Geography;

[TestClass]
public sealed class CoordinateParserDmsTests
{
    private ICoordinateParser _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new CoordinateParser();
    }

    [TestMethod]
    public void ParseLatitude_Dms_North_ReturnsDecimal()
    {
        var input = "50\u00B0 33\u2032 18\u2033 N";
        var actual = _subject.ParseLatitude(input);
        actual.Should().Be(50.555m, because: input);
    }

    [TestMethod]
    public void ParseLongitude_Dms_East_ReturnsDecimal()
    {
        var input = "9\u00B0 40\u2032 26\u2033 E";
        var actual = _subject.ParseLongitude(input);
        actual.Should().BeApproximately(9.673888888888888888888888889m, 0.0000000000000000000001m, because: input);
    }

    [TestMethod]
    public void Parse_Dms_SouthAndWest_AreNegative()
    {
        var input = "50\u00B0 33\u2032 18\u2033 S";
        var actual = _subject.ParseLatitude(input);
        actual.Should().Be(-50.555m, because: input);

        input = "9\u00B0 40\u2032 26\u2033 W";
        var actualLon = _subject.ParseLongitude(input);
        actualLon.Should().BeApproximately(-9.673888888888888888888888889m, 0.0000000000000000000001m, because: input);
    }

    [TestMethod]
    public void Parse_Dms_AsciiApostropheAndQuote_Accepted()
    {
        var input = "50\u00B0 33' 18\" N";
        var actual = _subject.ParseLatitude(input);
        actual.Should().Be(50.555m, because: input);
    }

    [TestMethod]
    public void Parse_Dms_SecondsOptional_Accepted()
    {
        var input = "50\u00B0 33\u2032 N";
        var actual = _subject.ParseLatitude(input);
        actual.Should().Be(50.55m, because: input);
    }

    [TestMethod]
    public void Parse_Dms_LowercaseHemisphere_Accepted()
    {
        var input = "50\u00B0 33\u2032 18\u2033 n";
        var actual = _subject.ParseLatitude(input);
        actual.Should().Be(50.555m, because: input);

        input = "9\u00B0 40\u2032 26\u2033 w";
        var actualLon = _subject.ParseLongitude(input);
        actualLon.Should().BeApproximately(-9.673888888888888888888888889m, 0.0000000000000000000001m, because: input);
    }

    [TestMethod]
    public void Parse_Dms_WithoutSpaces_Accepted()
    {
        var input = "50\u00B033\u203218\u2033N";
        var actual = _subject.ParseLatitude(input);
        actual.Should().Be(50.555m, because: input);
    }

    [TestMethod]
    public void Parse_Dms_WithoutHemisphere_Throws()
    {
        var input = "50\u00B0 33\u2032 18\u2033";
        Action act = () => _subject.ParseLatitude(input);
        act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
    }

    [TestMethod]
    public void Parse_Dms_AnySignWithHemisphere_Throws()
    {
        var inputs = new[] { "+50\u00B0 33\u2032 18\u2033 N", "-50\u00B0 33\u2032 18\u2033 S", "-50\u00B0 33\u2032 18\u2033 N" };
        foreach (var input in inputs)
        {
            Action act = () => _subject.ParseLatitude(input);
            act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }
    }

    [TestMethod]
    public void Parse_Dms_MasculineOrdinalInsteadOfDegree_Throws()
    {
        var input = "50\u00BA 33\u2032 18\u2033 N";
        Action act = () => _subject.ParseLatitude(input);
        act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
    }

    [TestMethod]
    public void Parse_Dms_MinutesOrSecondsAtSixty_Throws()
    {
        var inputs = new[] { "50\u00B0 60\u2032 0\u2033 N", "50\u00B0 33\u2032 60\u2033 N" };
        foreach (var input in inputs)
        {
            Action act = () => _subject.ParseLatitude(input);
            act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }
    }

    [TestMethod]
    public void Parse_Dms_SecondsFiftyNine_Accepted()
    {
        var input = "8\u00B0 15\u2032 59\u2033 E";
        var actual = _subject.ParseLongitude(input);
        actual.Should().BeApproximately(8.266388888888888888888888889m, 0.0000000000000000000001m, because: input);
    }

    [TestMethod]
    public void Parse_Dms_WrongHemisphereForAxis_Throws()
    {
        var inputs = new[] { "50\u00B0 33\u2032 18\u2033 E", "50\u00B0 33\u2032 18\u2033 W" };
        foreach (var input in inputs)
        {
            Action act = () => _subject.ParseLatitude(input);
            act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }

        inputs = new[] { "9\u00B0 40\u2032 26\u2033 N", "9\u00B0 40\u2032 26\u2033 S" };
        foreach (var input in inputs)
        {
            Action act = () => _subject.ParseLongitude(input);
            act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }
    }

    [TestMethod]
    public void Parse_Dms_ResultOutOfRange_Throws()
    {
        var inputs = new[] { "90\u00B0 0\u2032 1\u2033 N" };
        foreach (var input in inputs)
        {
            Action act = () => _subject.ParseLatitude(input);
            act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }

        inputs = new[] { "180\u00B0 0\u2032 1\u2033 E" };
        foreach (var input in inputs)
        {
            Action act = () => _subject.ParseLongitude(input);
            act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }
    }

    [TestMethod]
    public void Parse_Dms_Malformed_Throws()
    {
        var inputs = new[] { "50 33\u2032 18\u2033 N", "50\u00B0 33\u2032 18\u2033 N x", "50\u00B0 18\u2033 N", "50\u00B0 33\u2032 18\u2033 NN" };
        foreach (var input in inputs)
        {
            Action act = () => _subject.ParseLatitude(input);
            act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }
    }

    [TestMethod]
    public void Parse_Dms_NegativeMinutesOrSeconds_Throws()
    {
        var inputs = new[] { "50\u00B0 -1\u2032 0\u2033 N", "50\u00B0 1\u2032 -1\u2033 N" };
        foreach (var input in inputs)
        {
            Action act = () => _subject.ParseLatitude(input);
            act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }
    }

    [TestMethod]
    public void Parse_Dms_ZeroMinutesAndSeconds_Accepted()
    {
        var input = "50\u00B0 0\u2032 0\u2033 N";
        var actual = _subject.ParseLatitude(input);
        actual.Should().Be(50m, because: input);
    }

    [TestMethod]
    public void Parse_Dms_AtPolesAndAntimeridian_Accepted()
    {
        var inputs = new[]
        {
            ("90\u00B0 0\u2032 0\u2033 N", 90m),
            ("90\u00B0 0\u2032 0\u2033 S", -90m)
        };

        foreach (var (input, expected) in inputs)
        {
            var actual = _subject.ParseLatitude(input);
            actual.Should().Be(expected, because: input);
        }

        inputs = new[]
        {
            ("180\u00B0 0\u2032 0\u2033 E", 180m),
            ("180\u00B0 0\u2032 0\u2033 W", -180m)
        };

        foreach (var (input, expected) in inputs)
        {
            var actual = _subject.ParseLongitude(input);
            actual.Should().Be(expected, because: input);
        }
    }

    [TestMethod]
    public void Parse_Dms_UnderGermanCulture_UsesInvariantFormat()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var deCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = deCulture;
            CultureInfo.CurrentUICulture = deCulture;

            var input = "50\u00B0 33\u2032 18.5\u2033 N";
            var actual = _subject.ParseLatitude(input);
            actual.Should().BeApproximately(50.55513888888888888888888889m, 0.0000000000000000000001m, because: input);

            input = "50\u00B0 33\u2032 18,5\u2033 N";
            Action act = () => _subject.ParseLatitude(input);
            act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [TestMethod]
    public void Parse_Dms_TooManyDigitsInDegrees_Throws()
    {
        var input = "0050\u00B0 33\u2032 18\u2033 N";
        Action act = () => _subject.ParseLatitude(input);
        act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
    }

    [TestMethod]
    public void Parse_Dms_TooManyDigitsInMinutes_Throws()
    {
        var input = "50\u00B0 033\u2032 18\u2033 N";
        Action act = () => _subject.ParseLatitude(input);
        act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
    }

    [TestMethod]
    public void Parse_Dms_TooManyDigitsInSeconds_Throws()
    {
        var input = "50\u00B0 33\u2032 018\u2033 N";
        Action act = () => _subject.ParseLatitude(input);
        act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
    }

    [TestMethod]
    public void Parse_Dms_FractionalSeconds_Valid15Digits_Accepted()
    {
        var input = "50\u00B0 33\u2032 18.123456789012345\u2033 N";
        var actual = _subject.ParseLatitude(input);
        actual.Should().BeApproximately(50.55503429355250342916666667m, 0.0000000000000000000001m, because: input);
    }

    [TestMethod]
    public void Parse_Dms_FractionalSeconds_Invalid16Digits_Throws()
    {
        var input = "50\u00B0 33\u2032 18.1234567890123456\u2033 N";
        Action act = () => _subject.ParseLatitude(input);
        act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
    }

    [TestMethod]
    public void Parse_Dms_MinutesFiftyNine_Accepted()
    {
        var input = "50\u00B0 59\u2032 N";
        var actual = _subject.ParseLatitude(input);
        actual.Should().BeApproximately(50.98333333333333333333333333m, 0.0000000000000000000001m, because: input);

        input = "50\u00B0 59\u2032 59\u2033 N";
        actual = _subject.ParseLatitude(input);
        actual.Should().BeApproximately(50.99972222222222222222222222m, 0.0000000000000000000001m, because: input);
    }

    [TestMethod]
    public void Parse_Dms_SecondsJustBelowSixty_Accepted()
    {
        var input = "50\u00B0 33\u2032 59.999999999999999\u2033 N";
        var actual = _subject.ParseLatitude(input);
        actual.Should().BeApproximately(50.56666666666666666638888889m, 0.0000000000000000000001m, because: input);
    }

    [TestMethod]
    public void Parse_Dms_NonAsciiDigits_Throws()
    {
        var inputs = new[] { "\u0665\u0660\u00B0 33\u2032 18\u2033 N", "50\u00B0 \u0663\u0663\u2032 18\u2033 N", "50\u00B0 33\u2032 \u0661\u0668\u2033 N" };
        foreach (var input in inputs)
        {
            Action act = () => _subject.ParseLatitude(input);
            act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }
    }
}
