using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Maps the raw station rows of an OsthessenWetter page to station IDs, locations and observation readings (contract OW-03 to OW-07, OW-11).
/// </summary>
public interface IOsthessenWetterObservationMapper
{
    /// <summary>
    /// Checks the station IDs of all overview names first, then maps every station in page order.
    /// </summary>
    /// <param name="page">The page read by IOsthessenWetterPageReader.</param>
    /// <param name="retrievedAtUtc">The retrieval instant in UTC used for the date inference.</param>
    /// <exception cref="ArgumentNullException">When page is null.</exception>
    /// <exception cref="ArgumentException">When retrievedAtUtc is not of kind UTC.</exception>
    /// <exception cref="OsthessenWetterException">OSTHESSEN-STATION-ID-COLLISION or OSTHESSEN-FORMAT-CHANGED from the station identity.</exception>
    IReadOnlyList<OsthessenWetterMappedStation> Map(OsthessenWetterPage page, DateTime retrievedAtUtc);
}
