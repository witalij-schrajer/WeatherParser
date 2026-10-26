using System;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Parses the OsthessenWetter measurement page into one observation dataset per usable station whose overview name matches the pattern (contract OW-01 to OW-16).
/// </summary>
public interface IOsthessenWetterParser
{
    /// <summary>
    /// Validates the pattern before any request, retrieves the page once under the page deadline, reads, maps and matches the stations and builds a dataset per usable matched station; notices, omissions and station failures are logged with stable IDs.
    /// </summary>
    /// <param name="stationNamePattern">The station name pattern to match.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The datasets and the station failures.</returns>
    /// <exception cref="OsthessenWetterException">OSTHESSEN-PATTERN-INVALID, OSTHESSEN-PATTERN-NO-MATCH, OSTHESSEN-PAGE-UNAVAILABLE, OSTHESSEN-DEADLINE-EXCEEDED, OSTHESSEN-FORMAT-CHANGED, OSTHESSEN-STATION-ID-COLLISION or OSTHESSEN-NO-USABLE-STATION (with the station failures).</exception>
    /// <exception cref="OperationCanceledException">The caller cancelled.</exception>
    Task<OsthessenWetterParseResult> ParseAsync(string stationNamePattern, CancellationToken cancellationToken);
}
