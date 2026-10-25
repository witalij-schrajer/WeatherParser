using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// Validates a DWD station ID and resolves it against the CDC station lists supplied by the caller, without any request.
/// </summary>
public interface IDwdStationResolver
{
    /// <summary>
    /// Checks the station ID grammar (exactly five ASCII digits, contract OC-01) and returns the ID unchanged.
    /// </summary>
    /// <param name="stationId">The station ID to validate.</param>
    /// <exception cref="DwdStationException">
    /// STATION-ID-INVALID when the ID is null or does not match.
    /// </exception>
    string ValidateId(string stationId);

    /// <summary>
    /// Validates the ID first, then finds the station in the supplied lists (contract OC-02).
    /// </summary>
    /// <param name="stationId">The station ID to resolve.</param>
    /// <param name="lists">The CDC station lists to search in.</param>
    /// <exception cref="DwdStationException">
    /// STATION-ID-INVALID, STATION-UNKNOWN or STATION-METADATA-CONFLICT.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// When lists is null and the ID is valid.
    /// </exception>
    DwdResolvedStation Resolve(string stationId, IReadOnlyList<CdcProductList> lists);
}
