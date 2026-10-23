using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Geography;

[TestClass]
public sealed class CoordinateParserTests
{
    private ICoordinateParser _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new CoordinateParser();
    }

    [TestMethod]
    public void ParseLatitude_DecimalDegrees_ReturnsExactValue()
    {
        var input = "50.5549";
        var actual = _subject.ParseLatitude(input);
        actual.Should().Be(50.5549m, because: input);
    }

    [TestMethod]
    public void ParseLongitude_DecimalDegrees_ReturnsExactValue()
    {
        var input = "9.6738";
        var actual = _subject.ParseLongitude(input);
        actual.Should().Be(9.6738m, because: input);
    }

    [TestMethod]
    public void ParseLatitude_AtInclusiveBounds_Accepts()
    {
        var testCases = new[]
        {
            ("-90", -90m),
            ("90", 90m),
            ("+90", 90m)
        };

        foreach (var (input, expected) in testCases)
        {
            var actual = _subject.ParseLatitude(input);
            actual.Should().Be(expected, because: input);
        }
    }

    [TestMethod]
    public void ParseLongitude_AtInclusiveBounds_Accepts()
    {
        var testCases = new[]
        {
            ("-180", -180m),
            ("180", 180m)
        };

        foreach (var (input, expected) in testCases)
        {
            var actual = _subject.ParseLongitude(input);
            actual.Should().Be(expected, because: input);
        }
    }

    [TestMethod]
    public void ParseLatitude_OutOfRange_Throws()
    {
        var inputs = new[] { "90.0001", "-90.0001" };
        foreach (var input in inputs)
        {
            Action act = () => _subject.ParseLatitude(input);
            act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }
    }

    [TestMethod]
    public void ParseLongitude_OutOfRange_Throws()
    {
        var inputs = new[] { "180.0001", "-180.0001" };
        foreach (var input in inputs)
        {
            Action act = () => _subject.ParseLongitude(input);
            act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }
    }

    [TestMethod]
    public void Parse_NonFiniteOrEmpty_Throws()
    {
        var inputs = new[] { "NaN", "Infinity", "-Infinity", "", "   " };
        foreach (var input in inputs)
        {
            Action actLat = () => _subject.ParseLatitude(input);
            actLat.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);

            Action actLon = () => _subject.ParseLongitude(input);
            actLon.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }
    }

    [TestMethod]
    public void Parse_Null_ThrowsArgumentNullException()
    {
        Action actLat = () => _subject.ParseLatitude(null!);
        actLat.Should().Throw<ArgumentNullException>();

        Action actLon = () => _subject.ParseLongitude(null!);
        actLon.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Parse_DecimalWith20FractionalDigits_Accepted()
    {
        var input = "50.55490000000000000001";
        var actual = _subject.ParseLatitude(input);
        actual.Should().Be(50.55490000000000000001m, because: input);
    }

    [TestMethod]
    public void Parse_DecimalWith21FractionalDigits_Throws()
    {
        var input = "50.554900000000000000001";
        Action act = () => _subject.ParseLatitude(input);
        act.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
    }

    [TestMethod]
    public void Parse_DecimalWithMoreThan3IntegerDigits_Throws()
    {
        Action act = () => _subject.ParseLatitude("0050.5549");
        act.Should().Throw<CoordinateFormatException>(because: "0050.5549").Which.ErrorId.Should().Be("COORDINATE-INVALID", because: "0050.5549");

        var actual = _subject.ParseLatitude("050.5549");
        actual.Should().Be(50.5549m, because: "050.5549");
    }

    [TestMethod]
    public void Parse_ExponentGroupingOrComma_Throws()
    {
        var inputs = new[] { "1e1", "1,000", "50,5549", "50. 5549", "5 0.5549" };
        foreach (var input in inputs)
        {
            Action actLat = () => _subject.ParseLatitude(input);
            actLat.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);

            Action actLon = () => _subject.ParseLongitude(input);
            actLon.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }
    }

    [TestMethod]
    public void Parse_SurroundingWhitespace_IsTrimmed()
    {
        var input = "  50.5549 ";
        var actual = _subject.ParseLatitude(input);
        actual.Should().Be(50.5549m, because: input);
    }

    [TestMethod]
    public void Parse_UnderGermanCulture_UsesInvariantFormat()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var deCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = deCulture;
            CultureInfo.CurrentUICulture = deCulture;

            var input = "50.5549";
            var actual = _subject.ParseLatitude(input);
            actual.Should().Be(50.5549m, because: input);

            input = "50,5549";
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
    public void AddWeatherParsing_ResolvesCoordinateParser()
    {
        using var provider = new ServiceCollection()
            .AddWeatherParsing().AddTestConsumerServices()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var actual = provider.GetRequiredService<ICoordinateParser>();
        actual.Should().BeOfType<CoordinateParser>();
    }

    [TestMethod]
    public void Parse_NonAsciiDigits_Throws()
    {
        var inputs = new[] { "\u0665\u0660.5", "\uFF15\uFF10.5", "50.\u0665" };
        foreach (var input in inputs)
        {
            Action actLat = () => _subject.ParseLatitude(input);
            actLat.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);

            Action actLon = () => _subject.ParseLongitude(input);
            actLon.Should().Throw<CoordinateFormatException>(because: input).Which.ErrorId.Should().Be("COORDINATE-INVALID", because: input);
        }
    }
}
