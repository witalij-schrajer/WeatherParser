using System;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Selection;

/// <summary>
/// Selects the active CDC stations inside a search area (REQ-023, REQ-024, contract SC-03, SC-11, SC-13, SC-18).
/// </summary>
public interface ICdcStationSelector
{
    /// <summary>
    /// Selects the active CDC stations inside a search area.
    /// Copies and validates the five search parameters before any request (SC-11);
    /// reads the reference instant once from the <see cref="TimeProvider"/> (SC-13);
    /// downloads the four station lists, applies the list pre-filter, the search-area filter and the candidate limit,
    /// reads the <c>now</c> evidence of the candidates inside the area and derives their availability (SC-03);
    /// orders the entries through <see cref="IListEntryOrdering"/>;
    /// an empty result is a valid empty list with one <see cref="SelectionNoticeIds.SelectionEmpty"/> notice.
    /// </summary>
    /// <param name="parameters">The search parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The selected stations.</returns>
    /// <exception cref="SearchParameterException">
    /// Thrown when any of the parameters violate the validation policy.
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
    Task<CdcStationList> SelectAsync(SearchParameters parameters, CancellationToken cancellationToken);
}
