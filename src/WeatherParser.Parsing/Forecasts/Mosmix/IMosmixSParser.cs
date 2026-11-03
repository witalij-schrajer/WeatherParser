using System;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Parses the MOSMIX_S forecast of one location from the all-stations file: one request per call, the whole file streamed to its end, no internal scheduling (contracts MF-02, MF-03, MF-05, MF-06).
/// </summary>
public interface IMosmixSParser
{
    /// <summary>
    /// Parses the MOSMIX_S forecast for the given ID.
    /// </summary>
    /// <param name="mosmixId">The MOSMIX ID to parse.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parse result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="mosmixId"/> is null.</exception>
    /// <exception cref="MosmixForecastException">Thrown when parsing fails, with IDs:
    /// <list type="bullet">
    /// <item><description>MOSMIX-ID-INVALID (MF-03)</description></item>
    /// <item><description>MOSMIX-ID-UNKNOWN (MF-06)</description></item>
    /// <item><description>MOSMIX-DEADLINE-EXCEEDED (MF-03)</description></item>
    /// <item><description>MOSMIX-NO-USABLE-VALUE (MF-05)</description></item>
    /// </list>
    /// </exception>
    Task<MosmixParseResult> ParseAsync(string mosmixId, CancellationToken cancellationToken);
}
