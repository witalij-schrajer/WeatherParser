using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Reads CDC station lists and evidence files within the SC-09 bounds.
/// </summary>
public interface ICdcCatalogSource
{
    /// <summary>
    /// Downloads (8 MiB bound) and reads the four configured station lists (air temperature <c>tu</c>, wind <c>ff</c>, precipitation <c>rr</c>, solar <c>sd</c>),
    /// each from its configured URI unchanged, and returns them with all reader diagnostics.
    /// No availability derivation; failures propagate (no partial result).
    /// </summary>
    /// <param name="selectionDeadline">The selection deadline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The station lists with diagnostics.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the caller cancellation token is cancelled.</exception>
    /// <exception cref="WeatherParser.Parsing.Exceptions.SelectionLimitException">Thrown when the selection deadline is exceeded.</exception>
    Task<IReadOnlyList<CdcProductList>> ReadStationListsAsync(CancellationToken selectionDeadline, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads each station's <c>now</c> file (<c>10minutenwerte_TU_&lt;id&gt;_now.zip</c>, <c>10minutenwerte_wind_&lt;id&gt;_now.zip</c>,
    /// <c>10minutenwerte_nieder_&lt;id&gt;_now.zip</c>, <c>10minutenwerte_SOLAR_&lt;id&gt;_now.zip</c>) from the product's configured directory,
    /// the file name appended to the directory path with the ID percent-encoded (SC-12), 1 MiB bound, at most 4 requests in parallel (SC-18);
    /// HTTP 404 gives <see langword="null"/> (no file); every other failure propagates.
    /// </summary>
    /// <param name="keys">The evidence keys to read.</param>
    /// <param name="selectionDeadline">The selection deadline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A dictionary mapping each key to its evidence or null if not found.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the caller cancellation token is cancelled.</exception>
    /// <exception cref="WeatherParser.Parsing.Exceptions.SelectionLimitException">Thrown when the selection deadline is exceeded.</exception>
    Task<IReadOnlyDictionary<CdcEvidenceKey, CdcNowEvidence?>> ReadEvidenceAsync(IReadOnlyCollection<CdcEvidenceKey> keys, CancellationToken selectionDeadline, CancellationToken cancellationToken);
}
