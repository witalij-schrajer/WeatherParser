using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Geography;

/// <summary>
/// Applies the inclusive distance and altitude limits of the search area (SC-06, REQ-022).
/// </summary>
public interface ISearchAreaFilter
{
    /// <summary>
    /// Evaluates a candidate against the search parameters.
    /// Both limits inclusive and applied separately (no 3-D distance).
    /// A missing candidate altitude gives <see cref="SearchAreaMatch.NotFilterable"/> without any distance calculation.
    /// Otherwise <see cref="IDistanceCalculator.SphericalKm"/> is computed first and a spherical distance larger than
    /// <paramref name="parameters"/>.<see cref="SearchParameters.MaxDistanceKm"/> × 1.01 + 1 km (computed in <see cref="double"/>) gives
    /// <see cref="SearchAreaMatch.Outside"/> without calling <see cref="IDistanceCalculator.CalculateKm"/>.
    /// Otherwise <see cref="IDistanceCalculator.CalculateKm"/> is called exactly once and its <see cref="decimal"/> value is compared with
    /// <paramref name="parameters"/>.<see cref="SearchParameters.MaxDistanceKm"/> using &lt;= (SC-06).
    /// The absolute altitude difference is compared with <paramref name="parameters"/>.<see cref="SearchParameters.MaxAltitudeDifferenceM"/> using &lt;= in <see cref="decimal"/>.
    /// </summary>
    /// <param name="parameters">The search parameters to evaluate against.</param>
    /// <param name="latitude">The latitude of the candidate point.</param>
    /// <param name="longitude">The longitude of the candidate point.</param>
    /// <param name="altitudeM">The altitude of the candidate point in meters, or null if not available.</param>
    /// <returns>The evaluation result.</returns>
    SearchAreaEvaluation Evaluate(SearchParameters parameters, decimal latitude, decimal longitude, decimal? altitudeM);
}
