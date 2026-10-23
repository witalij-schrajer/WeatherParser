using System;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Refreshes the MOSMIX_S evidence index from the all-stations file (SC-22).
/// </summary>
public interface IMosmixSEvidenceRefresher
{
    /// <summary>
    /// Downloads the configured <see cref="MosmixCatalogEndpoints.MosmixSAllStationsUri"/> (64 MiB bound, SC-09) within <see cref="MosmixCatalogEndpoints.MosmixSRefreshDeadline"/>,
    /// stream-parses the KMZ under the SC-20 rules with the all-stations replacements
    /// (1 to 20000 placemarks with unique IDs <c>^[A-Z0-9]{4,5}$</c>, at most 200 forecasts per placemark with names <c>^[A-Za-z0-9_]{1,16}$</c>,
    /// unpacked member at most 2 GiB, never held in memory as a whole) and writes the derived index through <see cref="IMosmixEvidenceCache"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the caller cancellation token is cancelled.
    /// </exception>
    /// <exception cref="EvidenceCacheException">
    /// Thrown when the refresh deadline is exceeded.
    /// </exception>
    /// <exception cref="CatalogRetrievalException">
    /// Thrown for timeouts, non-success status, connection failure or a body shorter than <see cref="HttpContent.ContentLength"/>.
    /// </exception>
    /// <exception cref="CatalogFormatException">
    /// Thrown when the file format or bound is invalid.
    /// </exception>
    /// <exception cref="EvidenceCacheException">
    /// Thrown when the evidence cache fails.
    /// </exception>
    Task RefreshAsync(CancellationToken cancellationToken);
}
