using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Reads the overview and wind tables of the documented OsthessenWetter page revision into raw station rows (contract OW-01, OW-02, OW-05, OW-11, OW-12).
/// </summary>
public interface IOsthessenWetterPageReader
{
    /// <summary>
    /// Reads the page body with asynchronous reads only and parses it without scripts or resource loading.
    /// </summary>
    /// <param name="html">The HTML stream to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed page data.</returns>
    /// <exception cref="ArgumentNullException">
    /// When html is null.
    /// </exception>
    /// <exception cref="OsthessenWetterException">
    /// OSTHESSEN-FORMAT-CHANGED when the overview table, its header, a documented cell or a status is missing or changed.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// When the token is cancelled.
    /// </exception>
    Task<OsthessenWetterPage> ReadAsync(Stream html, CancellationToken cancellationToken);
}
