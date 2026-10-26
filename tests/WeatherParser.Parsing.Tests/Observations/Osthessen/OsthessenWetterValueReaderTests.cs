using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class OsthessenWetterValueReaderTests
{
    private OsthessenWetterValueReader _subject = null!;

    private const OsthessenWetterValueColumn T = OsthessenWetterValueColumn.Temperature;
    private const OsthessenWetterValueColumn H = OsthessenWetterValueColumn.RelativeHumidity;
    private const OsthessenWetterValueColumn W = OsthessenWetterValueColumn.MeanWindSpeed10Minutes;

    private void Present(string text, OsthessenWetterValueColumn column, double expected) =>
        _subject.Read(text, column).Should().Be(new OsthessenWetterCellValue(OsthessenWetterCellValueStatus.Present, expected));

    private void Unreadable(string? text, OsthessenWetterValueColumn column) =>
        _subject.Read(text, column).Should().Be(new OsthessenWetterCellValue(OsthessenWetterCellValueStatus.Unreadable, null));

    private void Missing(string? text, OsthessenWetterValueColumn column) =>
        _subject.Read(text, column).Should().Be(new OsthessenWetterCellValue(OsthessenWetterCellValueStatus.Missing, null));

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new OsthessenWetterValueReader();
    }

    [TestMethod]
    public void Read_Decimal_ReturnsValue()
    {
        Present("10.3\u00b0C", T, 10.3);
        Present("93%", H, 93);
        Present("23.1km/h", W, 23.1);
    }

    [TestMethod]
    public void Read_Negative_ReturnsValue()
    {
        Present("-4.2\u00b0C", T, -4.2);
        Present("-0.5km/h", W, -0.5);
    }

    [TestMethod]
    public void Read_Zero_ReturnsZero()
    {
        Present("0\u00b0C", T, 0);
        Present("0.0km/h", W, 0);
        Present("0%", H, 0);
        _subject.Read("-0.0\u00b0C", T).Value.Should().Be(0);
    }

    [TestMethod]
    public void Read_Empty_ReturnsMissing()
    {
        Missing("", T);
        Missing(null, W);
        Missing("   ", H);
    }

    [TestMethod]
    public void Read_CommaDecimal_Unreadable()
    {
        Unreadable("10,3\u00b0C", T);
        Unreadable("1,000km/h", W);
    }

    [TestMethod]
    public void Read_NaNInfinityExponentThousands_Unreadable()
    {
        var testCases = new[]
        {
            "NaN\u00b0C",
            "Infinity\u00b0C",
            "-Infinity\u00b0C",
            "1e3\u00b0C",
            "1E3\u00b0C",
            "1 000\u00b0C",
            "+10.3\u00b0C",
            ".5\u00b0C",
            "5.\u00b0C",
            "--5\u00b0C",
            "0x1A\u00b0C",
            "\u00b0C",
            "10.3.1\u00b0C"
        };

        foreach (var text in testCases)
        {
            Unreadable(text, T);
        }
    }

    [TestMethod]
    public void Read_NonAsciiDigits_Unreadable()
    {
        Unreadable("\u0661\u0662\u00b0C", T); // Arabic-Indic digits
        Unreadable("\uff11\uff12%", H); // Fullwidth digits
    }

    [TestMethod]
    public void Read_OverflowDigits_ReturnsNonFinite()
    {
        var text = new string('9', 400) + "km/h";
        var result = _subject.Read(text, W);
        result.Status.Should().Be(OsthessenWetterCellValueStatus.Present);
        result.Value.Should().NotBeNull();
        double.IsPositiveInfinity(result.Value!.Value).Should().BeTrue();

        text = "-" + text;
        result = _subject.Read(text, W);
        result.Status.Should().Be(OsthessenWetterCellValueStatus.Present);
        result.Value.Should().NotBeNull();
        double.IsNegativeInfinity(result.Value!.Value).Should().BeTrue();
    }

    [TestMethod]
    public void Read_WrongUnit_Unreadable()
    {
        Unreadable("10.3km/h", T);
        Unreadable("10.3 \u00b0C", T); // space before the unit
        Unreadable("10.3\u00b0C/h", T); // the change unit
        Unreadable("93\u00b0C", H);
        Unreadable("23.1m/s", W);
        Unreadable("23.1", W); // no unit
        Unreadable("23.1KM/H", W); // case differs
    }

    [TestMethod]
    public void Read_TextAroundValue_Unreadable()
    {
        Unreadable("ca. 10.3\u00b0C", T);
        Unreadable("10.3\u00b0C+1.1\u00b0C/h", T); // combined compound cell text
    }

    [TestMethod]
    public void Read_CultureIndependent()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            var deCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = deCulture;
            Present("10.3\u00b0C", T, 10.3);
            Unreadable("10,3\u00b0C", T);

            var trCulture = new CultureInfo("tr-TR");
            CultureInfo.CurrentCulture = trCulture;
            Present("10.3\u00b0C", T, 10.3);
            Unreadable("10,3\u00b0C", T);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [TestMethod]
    public void Read_UndefinedColumn_ThrowsArgumentOutOfRange()
    {
        FluentActions.Invoking(() => _subject.Read("1%", (OsthessenWetterValueColumn)99))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesValueReader()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IOsthessenWetterValueReader>().Should().BeOfType<OsthessenWetterValueReader>();
    }

    [TestMethod] public void Read_SignWithoutDigits_Unreadable()
    {
        Unreadable("-km/h", W);
        Unreadable("-%", H);
        Unreadable("-.5km/h", W);
    }
}
