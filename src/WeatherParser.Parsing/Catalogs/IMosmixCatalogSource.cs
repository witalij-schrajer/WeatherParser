using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Evidence.MosmixL;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Reads MOSMIX station catalogs and evidence files within the SC-09 bounds.
/// </summary>
public interface IMosmixCatalogSource
{
    /// <summary>
    /// Downloads (8 MiB bound) and reads the configured MOSMIX station catalog (URI unchanged) and returns it with all reader diagnostics.
    /// No availability resolution.
    /// </summary>
    /// <param name="selectionDeadline">The selection deadline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The station catalog with diagnostics.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the caller cancellation token is cancelled.</exception>
    /// <exception cref="SelectionLimitException">Thrown when the selection deadline is exceeded.</exception>
    Task<MosmixLocationCatalog> ReadCatalogAsync(CancellationToken selectionDeadline, CancellationToken cancellationToken);

    /// <summary>
    /// Requests &lt;id&gt;/kml/MOSMIX_L_LATEST_&lt;id&gt;.kmz below the configured <c>single_stations/</c> directory (ID percent-encoded, SC-12), 1 MiB bound, at most 4 requests in parallel;
    /// each retrieval carries its completion time from the <see cref="TimeProvider"/> truncated to whole seconds (SC-13, SC-23);
    /// HTTP 404 gives <see langword="null"/>.
    /// </summary>
    /// <param name="mosmixIds">The MOSMIX IDs to retrieve.</param>
    /// <param name="selectionDeadline">The selection deadline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A dictionary mapping each ID to its retrieval or null if not found.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the caller cancellation token is cancelled.</exception>
    /// <exception cref="SelectionLimitException">Thrown when the selection deadline is exceeded.</exception>
    Task<IReadOnlyDictionary<string, MosmixLRetrieval?>> ReadMosmixLEvidenceAsync(IReadOnlyCollection<string> mosmixIds, CancellationToken selectionDeadline, CancellationToken cancellationToken);
}
