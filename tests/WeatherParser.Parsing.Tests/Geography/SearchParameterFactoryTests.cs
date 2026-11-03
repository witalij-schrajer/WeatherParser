using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Geography;

[TestClass]
public sealed class SearchParameterFactoryTests
{
    private ISearchParameterFactory _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new SearchParameterFactory();
    }

    [TestMethod]
    public void Create_ValidInput_ReturnsSearchParametersWithUnchangedValues()
    {
        var actual = _subject.Create(50.5549m, 9.6738m, 312.5m, 25m, 150m);
        actual.Latitude.Should().Be(50.5549m);
        actual.Longitude.Should().Be(9.6738m);
        actual.AltitudeM.Should().Be(312.5m);
        actual.MaxDistanceKm.Should().Be(25m);
        actual.MaxAltitudeDifferenceM.Should().Be(150m);
    }

    [TestMethod]
    public void Create_NegativeReferenceAltitude_Accepted()
    {
        var actual = _subject.Create(50m, 9m, -12.5m, 10m, 100m);
        actual.AltitudeM.Should().Be(-12.5m);
    }

    [TestMethod]
    public void Create_AllZeroValues_ReturnsAllFieldsReadable()
    {
        var actual = _subject.Create(0m, 0m, 0m, 0m, 0m);
        actual.Latitude.Should().Be(0m);
        actual.Longitude.Should().Be(0m);
        actual.AltitudeM.Should().Be(0m);
        actual.MaxDistanceKm.Should().Be(0m);
        actual.MaxAltitudeDifferenceM.Should().Be(0m);
    }

    [TestMethod]
    public void Create_NegativeMaxDistance_Throws()
    {
        Action act = () => _subject.Create(50m, 9m, 300m, -0.001m, 100m);
        act.Should().Throw<SearchParameterException>(because: "negative max distance").Which.ErrorId.Should().Be("SEARCH-PARAMETER-INVALID", because: "negative max distance");
    }

    [TestMethod]
    public void Create_NegativeMaxAltitudeDifference_Throws()
    {
        Action act = () => _subject.Create(50m, 9m, 300m, 10m, -0.001m);
        act.Should().Throw<SearchParameterException>(because: "negative max altitude difference").Which.ErrorId.Should().Be("SEARCH-PARAMETER-INVALID", because: "negative max altitude difference");
    }

    [TestMethod]
    public void Create_ReferenceAltitudeAtRepresentableBounds_Accepted()
    {
        var actual1 = _subject.Create(50m, 9m, decimal.MaxValue - 500m, 10m, 100m);
        actual1.AltitudeM.Should().Be(decimal.MaxValue - 500m);

        var actual2 = _subject.Create(50m, 9m, decimal.MinValue + 9000m, 10m, 100m);
        actual2.AltitudeM.Should().Be(decimal.MinValue + 9000m);
    }

    [TestMethod]
    public void Create_ReferenceAltitudeBeyondRepresentableBounds_Throws()
    {
        Action act1 = () => _subject.Create(50m, 9m, decimal.MaxValue - 499m, 10m, 100m);
        act1.Should().Throw<SearchParameterException>(because: "reference altitude beyond representable bounds (too large)").Which.ErrorId.Should().Be("SEARCH-PARAMETER-INVALID", because: "reference altitude beyond representable bounds (too large)");

        Action act2 = () => _subject.Create(50m, 9m, decimal.MinValue + 8999m, 10m, 100m);
        act2.Should().Throw<SearchParameterException>(because: "reference altitude beyond representable bounds (too small)").Which.ErrorId.Should().Be("SEARCH-PARAMETER-INVALID", because: "reference altitude beyond representable bounds (too small)");
    }

    [TestMethod]
    public void Create_LargeLimits_Accepted()
    {
        var actual1 = _subject.Create(50m, 9m, 300m, 1000000m, 1000000m);
        actual1.MaxDistanceKm.Should().Be(1000000m);
        actual1.MaxAltitudeDifferenceM.Should().Be(1000000m);

        var actual2 = _subject.Create(50m, 9m, 300m, decimal.MaxValue, decimal.MaxValue);
        actual2.MaxDistanceKm.Should().Be(decimal.MaxValue);
        actual2.MaxAltitudeDifferenceM.Should().Be(decimal.MaxValue);
    }

    [TestMethod]
    public void Create_LatitudeOrLongitudeOutOfRange_Throws()
    {
        Action act1 = () => _subject.Create(-90.0001m, 9m, 300m, 10m, 100m);
        act1.Should().Throw<SearchParameterException>(because: "latitude out of range (too small)").Which.ErrorId.Should().Be("SEARCH-PARAMETER-INVALID", because: "latitude out of range (too small)");

        Action act2 = () => _subject.Create(90.0001m, 9m, 300m, 10m, 100m);
        act2.Should().Throw<SearchParameterException>(because: "latitude out of range (too large)").Which.ErrorId.Should().Be("SEARCH-PARAMETER-INVALID", because: "latitude out of range (too large)");

        Action act3 = () => _subject.Create(50m, -180.0001m, 300m, 10m, 100m);
        act3.Should().Throw<SearchParameterException>(because: "longitude out of range (too small)").Which.ErrorId.Should().Be("SEARCH-PARAMETER-INVALID", because: "longitude out of range (too small)");

        Action act4 = () => _subject.Create(50m, 180.0001m, 300m, 10m, 100m);
        act4.Should().Throw<SearchParameterException>(because: "longitude out of range (too large)").Which.ErrorId.Should().Be("SEARCH-PARAMETER-INVALID", because: "longitude out of range (too large)");
    }

    [TestMethod]
    public void Create_CoordinatesAtInclusiveBounds_Accepted()
    {
        var actual1 = _subject.Create(-90m, -180m, 0m, 0m, 0m);
        actual1.Latitude.Should().Be(-90m);
        actual1.Longitude.Should().Be(-180m);

        var actual2 = _subject.Create(90m, 180m, 0m, 0m, 0m);
        actual2.Latitude.Should().Be(90m);
        actual2.Longitude.Should().Be(180m);
    }

    [TestMethod]
    public void Create_ReturnsNewInstancePerCall()
    {
        var instance1 = _subject.Create(50m, 9m, 300m, 10m, 100m);
        var instance2 = _subject.Create(50m, 9m, 300m, 10m, 100m);
        instance1.Should().NotBeSameAs(instance2);
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesSearchParameterFactory()
    {
        using var provider = new ServiceCollection()
            .AddWeatherParsing().AddTestConsumerServices()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var actual = provider.GetRequiredService<ISearchParameterFactory>();
        actual.Should().BeOfType<SearchParameterFactory>();
    }
}
