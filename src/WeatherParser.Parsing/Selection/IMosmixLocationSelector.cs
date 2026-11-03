using System;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Selection;

/// <summary>
/// Selects the MOSMIX locations inside a search area with their MOSMIX_S and MOSMIX_L availability (REQ-101, REQ-102, contract SC-04, SC-05, SC-11, SC-13, SC-18, SC-22, SC-23).
/// </summary>
public interface IMosmixLocationSelector
{
    /// <summary>
    /// Selects the MOSMIX locations inside a search area.
    /// Copies and validates the five search parameters before any request (SC-11);
    /// reads the reference instant once from the <see cref="TimeProvider"/> (SC-13);
    /// reads the stored MOSMIX_S evidence index before any request (missing → failure; older than <see cref="WeatherParser.Parsing.Evidence.MosmixS.MosmixCatalogEndpoints.MosmixSEvidenceMaxAge"/> → used with one <c>MOSMIX-S-EVIDENCE-STALE</c> notice, SC-22);
    /// downloads the MOSMIX location catalog, applies the search-area filter and the candidate limit,
    /// downloads the MOSMIX_L file of each location inside the area (HTTP 404 → <c>unavailable</c>, SC-05 option A) and resolves one availability item per product with its evidence timestamps (SC-23);
    /// orders the entries through <see cref="IListEntryOrdering"/>;
    /// an empty result is a valid empty list with one <see cref="SelectionNoticeIds.SelectionEmpty"/> notice.
    /// </summary>
    /// <param name="parameters">The search parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The selected locations.</returns>
    /// <exception cref="SearchParameterException">
    /// Thrown when any of the parameters violate the validation policy.
    /// </exception>
    /// <exception cref="EvidenceCacheException">
    /// Thrown when the MOSMIX_S evidence cache fails or is missing.
    /// </exception>
    /// <exception cref="SelectionLimitException">
    /// Thrown when the selection limit is exceeded.
    /// </exception>
    /// <exception cref="CatalogRetrievalException">
    /// Thrown when a catalog retrieval fails.
    /// </exception>
    /// <exception cref="CatalogFormatException">
    /// Thrown when a catalog format is invalid.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the caller cancellation token is cancelled.
    /// </exception>
    Task<MosmixLocationList> SelectAsync(SearchParameters parameters, CancellationToken cancellationToken);
}
