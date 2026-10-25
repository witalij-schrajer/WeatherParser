using System;
using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class OsthessenWetterStationIdentityTests
{
    private OsthessenWetterStationIdentity _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new OsthessenWetterStationIdentity();
    }

    [TestMethod]
    public void ToStationId_Umlauts()
    {
        _subject.ToStationId("M\u00fcs-Hohh\u00fctte").Should().Be("mues-hohhuette");
        _subject.ToStationId("\u00d6lberg \u00c4u").Should().Be("oelberg-aeu");
    }

    [TestMethod]
    public void ToStationId_Eszett()
    {
        _subject.ToStationId("Ro\u00dfbach").Should().Be("rossbach");
    }

    [TestMethod]
    public void ToStationId_SpacesAndPunctuation()
    {
        _subject.ToStationId("Bad Salzschlirf").Should().Be("bad-salzschlirf");
        _subject.ToStationId("  Bad  --  Hersfeld (Nord);  ").Should().Be("bad-hersfeld-nord");
        _subject.ToStationId("Station 7").Should().Be("station-7");
    }

    [TestMethod]
    public void ToStationId_NfdEqualsNfc()
    {
        _subject.ToStationId("Mu\u0308s").Should().Be("mues");
    }

    [TestMethod]
    public void ToStationId_OtherNonAsciiLetter_BecomesSeparator()
    {
        _subject.ToStationId("Caf\u00e9 Nord").Should().Be("caf-nord");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("---;")]
    [DataRow("\u00e9\u00e8")]
    public void ToStationId_NoAsciiLetterOrDigit_ThrowsFormatChanged(string name)
    {
        var ex = FluentActions.Invoking(() => _subject.ToStationId(name))
            .Should().Throw<OsthessenWetterException>().Which;
        ex.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
    }

    [TestMethod]
    public void ToStationId_Null_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => _subject.ToStationId(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void ToStationId_StableAcrossCultures()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            var trCulture = new CultureInfo("tr-TR");
            CultureInfo.CurrentCulture = trCulture;
            CultureInfo.CurrentUICulture = trCulture;

            _subject.ToStationId("ILLER Imbach").Should().Be("iller-imbach");

            var deCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = deCulture;
            CultureInfo.CurrentUICulture = deCulture;

            _subject.ToStationId("ILLER Imbach").Should().Be("iller-imbach");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [TestMethod]
    public void EnsureUnique_TwoNamesOneSlug_ThrowsIdCollision()
    {
        var ex = FluentActions.Invoking(() => _subject.EnsureUnique(new[] { "Bimbach", "Ro\u00dfbach", "Rossbach" }))
            .Should().Throw<OsthessenWetterException>().Which;
        ex.ErrorId.Should().Be(OsthessenWetterException.StationIdCollisionErrorId);
        ex.Message.Should().Contain("rossbach");
    }

    [TestMethod]
    public void EnsureUnique_EqualNames_ThrowsIdCollision()
    {
        var ex = FluentActions.Invoking(() => _subject.EnsureUnique(new[] { "Bimbach", "Bimbach" }))
            .Should().Throw<OsthessenWetterException>().Which;
        ex.ErrorId.Should().Be(OsthessenWetterException.StationIdCollisionErrorId);
    }

    [TestMethod]
    public void EnsureUnique_DistinctSlugs_Passes()
    {
        FluentActions.Invoking(() => _subject.EnsureUnique(new[] { "Bimbach", "Ro\u00dfbach", "M\u00fcs-Hohh\u00fctte", "Bad Salzschlirf" }))
            .Should().NotThrow();
    }

    [TestMethod]
    public void EnsureUnique_Empty_Passes()
    {
        FluentActions.Invoking(() => _subject.EnsureUnique(Array.Empty<string>()))
            .Should().NotThrow();
    }

    [TestMethod]
    public void EnsureUnique_NameWithoutSlug_ThrowsFormatChanged()
    {
        var ex = FluentActions.Invoking(() => _subject.EnsureUnique(new[] { "Bimbach", "---" }))
            .Should().Throw<OsthessenWetterException>().Which;
        ex.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
    }

    [TestMethod]
    public void EnsureUnique_Null_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => _subject.EnsureUnique(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesStationIdentity()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IOsthessenWetterStationIdentity>().Should().BeOfType<OsthessenWetterStationIdentity>();
    }

    [TestMethod]
    public void ToStationId_CharactersAroundTheMappedRanges_BecomeSeparators()
    {
        _subject.ToStationId("a{b~c/d:e@f[g`h\u00b0i\u00e0j\u00f8k\u0142l\u00ffm").Should().Be("a-b-c-d-e-f-g-h-i-j-k-l-m");
    }
}
