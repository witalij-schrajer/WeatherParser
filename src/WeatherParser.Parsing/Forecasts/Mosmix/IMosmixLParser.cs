using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Parses the MOSMIX_L forecast of one location: one request per call, no internal scheduling (contracts MF-02, MF-03, MF-05).
/// </summary>
public interface IMosmixLParser
{
    /// <summary>
    /// Parses the MOSMIX_L forecast for the given ID.
    /// </summary>
    /// <param name="mosmixId">The MOSMIX ID to parse.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parse result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="mosmixId"/> is null.</exception>
    /// <exception cref="MosmixForecastException">Thrown when parsing fails, with IDs:
    /// <list type="bullet">
    /// <item><description>MOSMIX-ID-INVALID (MF-03)</description></item>
    /// <item><description>MOSMIX-DEADLINE-EXCEEDED (MF-03)</description></item>
    /// <item><description>MOSMIX-NO-USABLE-VALUE (MF-05)</description></item>
    /// </list>
    /// </exception>
    Task<MosmixParseResult> ParseAsync(string mosmixId, CancellationToken cancellationToken);
}
