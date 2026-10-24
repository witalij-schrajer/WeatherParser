using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Geography;

/// <summary>
/// Creates validated search parameters (SC-11).
/// </summary>
public interface ISearchParameterFactory
{
    /// <summary>
    /// Validates the reference location and the limits (REQ-020, REQ-022, SC-11) and returns a new <see cref="SearchParameters"/>
    /// with the values unchanged (no rounding, C-26). Latitude −90 to 90 and longitude −180 to 180 inclusive; limits ≥ 0
    /// with no upper cap. The reference altitude may be negative but the difference to every accepted catalog altitude
    /// (−500 to 9000 m) must be representable in <see cref="decimal"/>. Every violation throws <see cref="SearchParameterException"/>
    /// with <see cref="SearchParameterException.ErrorId"/> <c>SEARCH-PARAMETER-INVALID</c>.
    /// </summary>
    /// <param name="latitude">The latitude of the search center point.</param>
    /// <param name="longitude">The longitude of the search center point.</param>
    /// <param name="altitudeM">The altitude of the search center point in meters.</param>
    /// <param name="maxDistanceKm">The maximum distance in kilometers to search for stations.</param>
    /// <param name="maxAltitudeDifferenceM">The maximum altitude difference in meters allowed between the search center and stations.</param>
    /// <returns>A new <see cref="SearchParameters"/> with validated values.</returns>
    /// <exception cref="SearchParameterException">
    /// Thrown when any of the parameters violate the validation policy.
    /// </exception>
    SearchParameters Create(decimal latitude, decimal longitude, decimal altitudeM, decimal maxDistanceKm, decimal maxAltitudeDifferenceM);
}
