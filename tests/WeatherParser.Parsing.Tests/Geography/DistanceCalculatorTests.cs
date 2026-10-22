using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Geography;

[TestClass]
public sealed class DistanceCalculatorTests
{
    private IDistanceCalculator _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new DistanceCalculator();
    }

    [TestMethod]
    public void CalculateKm_ReferencePairs_WithinOneMillimetre()
    {
        var testCases = new[]
        {
            (50.5549m, 9.6738m, 52.52m, 13.405m, 338.776389246m, "German pair"),
            (50m, 179.9m, 50m, -179.9m, 14.339146451m, "Antimeridian pair"),
            (50.5549m, 9.6738m, -33.8688m, 151.2093m, 16396.963638898m, "Long pair")
        };

        foreach (var (lat1, lon1, lat2, lon2, reference, name) in testCases)
        {
            var actual = _subject.CalculateKm(lat1, lon1, lat2, lon2);
            actual.Should().BeApproximately(reference, 0.000001m, because: name);
        }
    }

    [TestMethod]
    public void CalculateKm_PoleToPole_MatchesReference()
    {
        var actual = _subject.CalculateKm(90m, 0m, -90m, 0m);
        actual.Should().BeApproximately(20003.931458625m, 0.000001m, because: "Pole pair");
    }

    [TestMethod]
    public void CalculateKm_NearAntipodal_ReturnsSphericalFallbackWithinSixTenthsPercent()
    {
        var actual = _subject.CalculateKm(0m, 0m, 0.5m, 179.7m);
        actual.Should().BeApproximately(19944.127420750m, 19944.127420750m * 0.006m, because: "Near-antipodal pair");
        actual.Should().Be(_subject.SphericalKm(0m, 0m, 0.5m, 179.7m), because: "Fallback equals spherical result");
    }

    [TestMethod]
    public void SphericalKm_ReferencePairs_WithinSixTenthsPercent()
    {
        var testCases = new[]
        {
            (50.5549m, 9.6738m, 52.52m, 13.405m, 338.776389246m, "German pair"),
            (50m, 179.9m, 50m, -179.9m, 14.339146451m, "Antimeridian pair"),
            (90m, 0m, -90m, 0m, 20003.931458625m, "Pole pair"),
            (50.5549m, 9.6738m, -33.8688m, 151.2093m, 16396.963638898m, "Long pair"),
            (0m, 0m, 0.5m, 179.7m, 19944.127420750m, "Near-antipodal pair")
        };

        foreach (var (lat1, lon1, lat2, lon2, reference, name) in testCases)
        {
            var actual = _subject.SphericalKm(lat1, lon1, lat2, lon2);
            actual.Should().BeApproximately(reference, reference * 0.006m, because: name);
        }
    }

    [TestMethod]
    public void CalculateKm_IdenticalPoints_ReturnsZero()
    {
        var actual = _subject.CalculateKm(50.5549m, 9.6738m, 50.5549m, 9.6738m);
        actual.Should().Be(0m, because: "Identical points");

        var actualSpherical = _subject.SphericalKm(50.5549m, 9.6738m, 50.5549m, 9.6738m);
        actualSpherical.Should().Be(0m, because: "Identical points spherical");
    }

    [TestMethod]
    public void CalculateKm_IsSymmetric()
    {
        var testCases = new[]
        {
            (50.5549m, 9.6738m, 52.52m, 13.405m, "German pair"),
            (50m, 179.9m, 50m, -179.9m, "Antimeridian pair"),
            (50.5549m, 9.6738m, -33.8688m, 151.2093m, "Long pair")
        };

        foreach (var (lat1, lon1, lat2, lon2, name) in testCases)
        {
            var result1 = _subject.CalculateKm(lat1, lon1, lat2, lon2);
            var result2 = _subject.CalculateKm(lat2, lon2, lat1, lon1);
            result1.Should().BeApproximately(result2, 0.000001m, because: name);
        }
    }

    [TestMethod]
    public void CalculateKm_AcrossAntimeridian_ReturnsShortDistance()
    {
        var actual = _subject.CalculateKm(50m, 179.9m, 50m, -179.9m);
        actual.Should().BeLessThan(15m, because: "Antimeridian distance");
        actual.Should().BeApproximately(14.339146451m, 0.000001m, because: "Antimeridian reference");
    }

    [TestMethod]
    public void CalculateKm_NeverThrowsForValidCoordinates()
    {
        var testCases = new[]
        {
            (90m, 180m, -90m, -180m),
            (0m, 0m, 0m, 180m),
            (0m, 0m, 0.5m, 179.7m),
            (-90m, 0m, 90m, 0m),
            (0m, -180m, 0m, 180m),
            (89.9999m, 0m, -89.9999m, 180m)
        };

        foreach (var (lat1, lon1, lat2, lon2) in testCases)
        {
            var distance = FluentActions.Invoking(() => _subject.CalculateKm(lat1, lon1, lat2, lon2))
                .Should().NotThrow(because: $"Valid coordinates ({lat1}, {lon1}, {lat2}, {lon2})").Subject;
            distance.Should().BeGreaterThanOrEqualTo(0m, because: $"Distance should be non-negative for valid coordinates ({lat1}, {lon1}, {lat2}, {lon2})");
        }
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesDistanceCalculator()
    {
        using var provider = new ServiceCollection()
            .AddWeatherParsing()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var actual = provider.GetRequiredService<IDistanceCalculator>();
        actual.Should().BeOfType<DistanceCalculator>();
    }

    [TestMethod]
    public void CalculateKm_EquivalentLongitudes_ReturnZero()
    {
        // Test pairs (0m, -180m, 0m, 180m) and (50m, 180m, 50m, -180m)
        var testCases = new[]
        {
            (0m, -180m, 0m, 180m, "Antimeridian pair"),
            (50m, 180m, 50m, -180m, "Antimeridian pair")
        };

        foreach (var (lat1, lon1, lat2, lon2, name) in testCases)
        {
            var actual = _subject.CalculateKm(lat1, lon1, lat2, lon2);
            actual.Should().Be(0m, because: name);

            var actualSpherical = _subject.SphericalKm(lat1, lon1, lat2, lon2);
            actualSpherical.Should().Be(0m, because: name);
        }
    }

    [TestMethod]
    public void CalculateKm_SamePoleDifferentLongitudes_ReturnZero()
    {
        // Test pairs (90m, 0m, 90m, 180m) and (-90m, 10m, -90m, -170m)
        var testCases = new[]
        {
            (90m, 0m, 90m, 180m, "Same pole pair"),
            (-90m, 10m, -90m, -170m, "Same pole pair")
        };

        foreach (var (lat1, lon1, lat2, lon2, name) in testCases)
        {
            var actual = _subject.CalculateKm(lat1, lon1, lat2, lon2);
            actual.Should().Be(0m, because: name);

            var actualSpherical = _subject.SphericalKm(lat1, lon1, lat2, lon2);
            actualSpherical.Should().Be(0m, because: name);
        }
    }
}
