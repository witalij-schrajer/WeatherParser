using System;
using System.Globalization;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class StationNamePatternMatcherTests
{
    private StationNamePatternMatcher _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new StationNamePatternMatcher();
    }

    private void AssertInvalid(string? pattern)
    {
        var ex = FluentActions.Invoking(() => _subject.Validate(pattern)).Should().Throw<OsthessenWetterException>().Which;
        ex.ErrorId.Should().Be(OsthessenWetterException.PatternInvalidErrorId);
    }

    [TestMethod]
    public void Validate_Null_ThrowsPatternInvalid()
    {
        AssertInvalid(null);
    }

    [TestMethod]
    public void Validate_Empty_ThrowsPatternInvalid()
    {
        AssertInvalid("");
    }

    [TestMethod]
    [DataRow(" ")]
    [DataRow("\t")]
    [DataRow("  \n ")]
    public void Validate_WhitespaceOnly_ThrowsPatternInvalid(string pattern)
    {
        AssertInvalid(pattern);
    }

    [TestMethod]
    public void Validate_128Characters_Accepted()
    {
        var pattern = new string('a', 128);
        _subject.Validate(pattern).Value.Should().Be(pattern);
    }

    [TestMethod]
    public void Validate_129Characters_ThrowsPatternInvalid()
    {
        AssertInvalid(new string('a', 129));
    }

    [TestMethod]
    public void Validate_StarOnly_Accepted()
    {
        _subject.Validate("*").Value.Should().Be("*");
    }

    [TestMethod]
    public void Validate_NfdInput_StoredAsNfc()
    {
        // \u0308 is the diaeresis combining character
        _subject.Validate("Mu\u0308s").Value.Should().Be("M\u00fcs");
    }

    [TestMethod]
    public void Validate_NfcInput_Unchanged()
    {
        _subject.Validate("Ro\u00dfbach").Value.Should().Be("Ro\u00dfbach");
    }

    [TestMethod]
    public void Validate_Invalid_MessageDoesNotEchoPattern()
    {
        var pattern = new string('x', 120) + "SECRET-TOKEN";
        var ex = FluentActions.Invoking(() => _subject.Validate(pattern)).Should().Throw<OsthessenWetterException>().Which;
        ex.Message.Should().NotContain("SECRET-TOKEN");
        ex.Message.Should().Contain("128");
    }

    [TestMethod]
    public void IsMatch_NullPattern_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => _subject.IsMatch(null!, "Name"))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void IsMatch_NullName_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => _subject.IsMatch(_subject.Validate("*"), null!))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesStationNamePatternMatcher()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IStationNamePatternMatcher>().Should().BeOfType<StationNamePatternMatcher>();
    }

    private bool Match(string pattern, string name) => _subject.IsMatch(_subject.Validate(pattern), name);

    [TestMethod]
    public void IsMatch_NoWildcard_WholeNameOnly()
    {
        Match("M\u00fcs", "M\u00fcs").Should().BeTrue();
        Match("M\u00fcs", "M\u00fcs-Hohh\u00fctte").Should().BeFalse();
        Match("Hohh\u00fctte", "M\u00fcs-Hohh\u00fctte").Should().BeFalse();
    }

    [TestMethod]
    public void IsMatch_Star_MatchesZeroOneMany()
    {
        Match("M\u00fcs*", "M\u00fcs").Should().BeTrue();
        Match("M\u00fcs*", "M\u00fcs-").Should().BeTrue();
        Match("M\u00fcs*", "M\u00fcs-Hohh\u00fctte").Should().BeTrue();
        Match("M\u00fcs*", "Bimbach").Should().BeFalse();
    }

    [TestMethod]
    public void IsMatch_Question_MatchesExactlyOne()
    {
        Match("M?s", "M\u00fcs").Should().BeTrue();
        Match("M?s", "Ms").Should().BeFalse();
        Match("M?s", "M\u00fc\u00fcs").Should().BeFalse();
    }

    [TestMethod]
    public void IsMatch_Question_SupplementaryScalar()
    {
        var name = "A\U0001D11EB";
        Match("A?B", name).Should().BeTrue();
        Match("A??B", name).Should().BeFalse();
    }

    [TestMethod]
    public void IsMatch_CaseInsensitive_WithUmlauts()
    {
        Match("m\u00fcs", "M\u00fcs").Should().BeTrue();
        Match("M\u00dcS", "M\u00fcs").Should().BeTrue();
        Match("bad SALZSCHLIRF", "Bad Salzschlirf").Should().BeTrue();
    }

    [TestMethod]
    public void IsMatch_NfcAndNfdEqual()
    {
        Match("Mu\u0308s", "M\u00fcs").Should().BeTrue();
        Match("M\u00fcs", "Mu\u0308s").Should().BeTrue();
    }

    [TestMethod]
    [DataRow("a.c", "a.c", true)]
    [DataRow("a.c", "abc", false)]
    [DataRow("[ab]", "[ab]", true)]
    [DataRow("[ab]", "a", false)]
    [DataRow("a+", "a+", true)]
    [DataRow("a+", "aa", false)]
    [DataRow("a\\b", "a\\b", true)]
    [DataRow("^a$", "^a$", true)]
    [DataRow("(a|b)", "(a|b)", true)]
    [DataRow("(a|b)", "a", false)]
    public void IsMatch_RegexMetacharactersLiteral(string pattern, string name, bool expected)
    {
        Match(pattern, name).Should().Be(expected);
    }

    [TestMethod]
    public void IsMatch_EszettLiteral()
    {
        Match("Ro\u00df*", "Rossbach").Should().BeFalse();
        Match("Ro\u00df*", "Ro\u00dfbach").Should().BeTrue();
        Match("RO\u00dfBACH", "Ro\u00dfbach").Should().BeTrue();
    }

    [TestMethod]
    public void IsMatch_LeadingAndTrailingStar()
    {
        Match("*h\u00fctte", "M\u00fcs-Hohh\u00fctte").Should().BeTrue();
        Match("*sal*", "Bad Salzschlirf").Should().BeTrue();
        Match("*", "").Should().BeTrue();
        Match("?", "").Should().BeFalse();
        Match("*x*", "Bad Salzschlirf").Should().BeFalse();
    }

    [TestMethod]
    public void IsMatch_ManyStars_StepCountWithinBound()
    {
        var pattern = _subject.Validate("*a*a*a*a*a*a*a*a*b");
        var name = new string('a', 2000);
        var result = _subject.IsMatch(pattern, name, out var steps);
        result.Should().BeFalse();
        steps.Should().BeLessThanOrEqualTo((long)pattern.Value.Length * (name.Length + 1));
    }

    [TestMethod]
    public void IsMatch_LongLiteralSuffix_StepCountWithinBound()
    {
        var pattern = _subject.Validate("*" + new string('a', 126) + "b");
        var name = new string('a', 2000);
        var result = _subject.IsMatch(pattern, name, out var steps);
        result.Should().BeFalse();
        steps.Should().BeLessThanOrEqualTo((long)pattern.Value.Length * (name.Length + 1));
    }

    [TestMethod]
    public void IsMatch_CultureIndependent()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            foreach (var cultureName in new[] { "tr-TR", "de-DE", "" })
            {
                var culture = string.IsNullOrEmpty(cultureName) ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                Match("ilm*", "ILMENAU").Should().BeTrue();
                Match("I*", "isar").Should().BeTrue();
                Match("M\u00dcS", "m\u00fcs").Should().BeTrue();
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [TestMethod]
    public void Validate_129InputCharactersNormalizingTo128_ThrowsPatternInvalid()
    {
        AssertInvalid(new string('a', 127) + "u\u0308");
    }

    [TestMethod]
    public void Validate_128InputCharactersWithCombiningMark_Accepted()
    {
        var pattern = new string('a', 126) + "u\u0308";
        _subject.Validate(pattern).Value.Should().Be(new string('a', 126) + "\u00fc");
    }

    [TestMethod]
    [DataRow("*a", "*ba", true)]
    [DataRow("*?", "*", true)]
    [DataRow("*", "*", true)]
    [DataRow("**", "*", true)]
    [DataRow("a*", "a*", true)]
    [DataRow("a*b", "a*xb", true)]
    [DataRow("*b", "*", false)]
    [DataRow("?*", "*", true)]
    public void IsMatch_StarBeforeLiteralStarInName(string pattern, string name, bool expected)
    {
        Match(pattern, name).Should().Be(expected);
    }

    [TestMethod]
    [DataRow("a**b", "ab", true)]
    [DataRow("a**b", "axyzb", true)]
    [DataRow("***", "", true)]
    [DataRow("a***", "a", true)]
    [DataRow("**a**", "bab", true)]
    [DataRow("**a**", "bbb", false)]
    public void IsMatch_ConsecutiveStars_SameAsSingleStar(string pattern, string name, bool expected)
    {
        Match(pattern, name).Should().Be(expected);
    }
}
