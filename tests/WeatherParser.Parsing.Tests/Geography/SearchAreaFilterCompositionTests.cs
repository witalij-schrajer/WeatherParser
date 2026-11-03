using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Geography;

[TestClass]
public sealed class SearchAreaFilterCompositionTests
{
    private ServiceProvider _provider = null!;
    private IDistanceCalculator _calculator = null!;
    private ISearchAreaFilter _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _provider = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        _calculator = _provider.GetRequiredService<IDistanceCalculator>();
        _subject = _provider.GetRequiredService<ISearchAreaFilter>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _provider.Dispose();
    }

    [TestMethod]
    public void Evaluate_WithRealCalculator_LimitsAroundReturnedDistance()
    {
        // Arrange
        var referenceLatitude = 50.5549m;
        var referenceLongitude = 9.6738m;
        var referenceAltitude = 300m;
        var candidateLatitude = 52.52m;
        var candidateLongitude = 13.405m;
        var candidateAltitude = 310m;
        var maxAltitudeDifferenceM = 150m;

        var distanceKm = _calculator.CalculateKm(referenceLatitude, referenceLongitude, candidateLatitude, candidateLongitude);

        // Act & Assert
        {
            // because: distance is just below the limit
            var parameters = new SearchParameters
            {
                Latitude = referenceLatitude,
                Longitude = referenceLongitude,
                AltitudeM = referenceAltitude,
                MaxDistanceKm = distanceKm - 0.000001m,
                MaxAltitudeDifferenceM = maxAltitudeDifferenceM
            };
            var result = _subject.Evaluate(parameters, candidateLatitude, candidateLongitude, candidateAltitude);
            result.Match.Should().Be(SearchAreaMatch.Outside);
        }

        {
            // because: distance is exactly at the limit
            var parameters = new SearchParameters
            {
                Latitude = referenceLatitude,
                Longitude = referenceLongitude,
                AltitudeM = referenceAltitude,
                MaxDistanceKm = distanceKm,
                MaxAltitudeDifferenceM = maxAltitudeDifferenceM
            };
            var result = _subject.Evaluate(parameters, candidateLatitude, candidateLongitude, candidateAltitude);
            result.Match.Should().Be(SearchAreaMatch.Inside);
            result.DistanceKm.Should().Be(distanceKm);
            result.AltitudeDifferenceM.Should().Be(10m);
        }

        {
            // because: distance is just above the limit
            var parameters = new SearchParameters
            {
                Latitude = referenceLatitude,
                Longitude = referenceLongitude,
                AltitudeM = referenceAltitude,
                MaxDistanceKm = distanceKm + 0.000001m,
                MaxAltitudeDifferenceM = maxAltitudeDifferenceM
            };
            var result = _subject.Evaluate(parameters, candidateLatitude, candidateLongitude, candidateAltitude);
            result.Match.Should().Be(SearchAreaMatch.Inside);
            result.DistanceKm.Should().Be(distanceKm);
            result.AltitudeDifferenceM.Should().Be(10m);
        }
    }

    [TestMethod]
    public void Evaluate_EquivalentPointsWithZeroLimits_IsInside()
    {
        // Test equivalent points with zero limits
        var reference = new SearchParameters
        {
            Latitude = 0m,
            Longitude = -180m,
            AltitudeM = 300m,
            MaxDistanceKm = 0m,
            MaxAltitudeDifferenceM = 0m
        };

        var candidate = (0m, 180m, 300m); // Equivalent point

        var result = _subject.Evaluate(reference, candidate.Item1, candidate.Item2, candidate.Item3);
        result.Match.Should().Be(SearchAreaMatch.Inside);
        result.DistanceKm.Should().Be(0m);

        // Test same pole different longitude
        reference = new SearchParameters
        {
            Latitude = 90m,
            Longitude = 0m,
            AltitudeM = 300m,
            MaxDistanceKm = 0m,
            MaxAltitudeDifferenceM = 0m
        };

        candidate = (90m, 180m, 300m); // Equivalent point

        result = _subject.Evaluate(reference, candidate.Item1, candidate.Item2, candidate.Item3);
        result.Match.Should().Be(SearchAreaMatch.Inside);
        result.DistanceKm.Should().Be(0m);
    }
}
