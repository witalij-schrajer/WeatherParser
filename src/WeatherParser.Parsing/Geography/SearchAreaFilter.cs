using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Geography;

/// <summary>
/// Applies the inclusive distance and altitude limits of the search area (SC-06, REQ-022).
/// </summary>
public sealed class SearchAreaFilter : ISearchAreaFilter
{
    private readonly IDistanceCalculator _distanceCalculator;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchAreaFilter"/> class.
    /// </summary>
    /// <param name="distanceCalculator">The distance calculator to use.</param>
    public SearchAreaFilter(IDistanceCalculator distanceCalculator)
    {
        ArgumentNullException.ThrowIfNull(distanceCalculator);
        _distanceCalculator = distanceCalculator;
    }

    /// <inheritdoc />
    public SearchAreaEvaluation Evaluate(SearchParameters parameters, decimal latitude, decimal longitude, decimal? altitudeM)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (altitudeM is null)
        {
            return new SearchAreaEvaluation(SearchAreaMatch.NotFilterable, null, null);
        }

        var altitudeDifferenceM = altitudeM.Value - parameters.AltitudeM;
        var withinAltitude = Math.Abs(altitudeDifferenceM) <= parameters.MaxAltitudeDifferenceM;

        // SC-06 pre-filter: the spherical error is below 0.6 %, so a spherical distance above max × 1.01 + 1 km is certainly outside.
        var sphericalKm = _distanceCalculator.SphericalKm(parameters.Latitude, parameters.Longitude, latitude, longitude);
        var prefilterLimitKm = (double)parameters.MaxDistanceKm * 1.01 + 1.0;
        if ((double)sphericalKm > prefilterLimitKm)
        {
            return new SearchAreaEvaluation(SearchAreaMatch.Outside, null, altitudeDifferenceM);
        }

        var distanceKm = _distanceCalculator.CalculateKm(parameters.Latitude, parameters.Longitude, latitude, longitude);
        var match = distanceKm <= parameters.MaxDistanceKm && withinAltitude ? SearchAreaMatch.Inside : SearchAreaMatch.Outside;
        return new SearchAreaEvaluation(match, distanceKm, altitudeDifferenceM);
    }
}
