using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;

namespace WeatherParser.Parsing.Selection;

/// <summary>
/// Stateless helpers shared by the CDC and MOSMIX selectors (contract SC-08, SC-11, SC-13, SC-18); no static state.
/// </summary>
internal static class SelectionSupport
{
    /// <summary>
    /// Validates the search parameters and returns a new instance with the values unchanged (no rounding, C-26).
    /// </summary>
    /// <param name="parameters">The search parameters to validate.</param>
    /// <param name="parameterFactory">The parameter factory to use for validation.</param>
    /// <returns>A new <see cref="SearchParameters"/> with validated values.</returns>
    /// <exception cref="SearchParameterException">
    /// Thrown when any of the parameters violate the validation policy.
    /// </exception>
    public static SearchParameters Snapshot(SearchParameters? parameters, ISearchParameterFactory parameterFactory)
    {
        if (parameters is null)
        {
            throw new SearchParameterException("Search parameters are required (contract SC-11).");
        }

        decimal latitude, longitude, altitudeM, maxDistanceKm, maxAltitudeDifferenceM;
        try
        {
            latitude = parameters.Latitude;
            longitude = parameters.Longitude;
            altitudeM = parameters.AltitudeM;
            maxDistanceKm = parameters.MaxDistanceKm;
            maxAltitudeDifferenceM = parameters.MaxAltitudeDifferenceM;
        }
        catch (InvalidOperationException)
        {
            throw new SearchParameterException("Every search parameter must be assigned (contract SC-11).");
        }

        return parameterFactory.Create(latitude, longitude, altitudeM, maxDistanceKm, maxAltitudeDifferenceM);
    }

    /// <summary>
    /// Truncates the given instant to the nearest second.
    /// </summary>
    /// <param name="instant">The instant to truncate.</param>
    /// <returns>The truncated instant.</returns>
    public static DateTime TruncateToSeconds(DateTimeOffset instant)
    {
        return new DateTime(instant.UtcDateTime.Ticks - (instant.UtcDateTime.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
    }

    /// <summary>
    /// Logs the diagnostics to the logger.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="diagnostics">The diagnostics to log.</param>
    public static void LogDiagnostics(ILogger logger, IEnumerable<CatalogDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics.Where(d => !d.AlreadyLogged))
        {
            logger.LogWarning(new EventId(0, diagnostic.Id), "{DiagnosticId}: {Reason} Endpoint {EndpointName}, line {LineNumber}, station {StationId}.", diagnostic.Id, diagnostic.Reason, diagnostic.EndpointName, diagnostic.LineNumber, diagnostic.StationId);
        }
    }

    /// <summary>
    /// Creates a new <see cref="SelectionLimitException"/> for a deadline exceeded error.
    /// </summary>
    /// <param name="selection">The selection type.</param>
    /// <returns>A new <see cref="SelectionLimitException"/>.</returns>
    public static SelectionLimitException DeadlineExceeded(string selection) => new(SelectionLimitException.DeadlineExceededErrorId, $"The {selection} selection exceeded its selection deadline (contract SC-18).");

    /// <summary>
    /// Throws a <see cref="SelectionLimitException"/> if the cancellation token is cancelled or the deadline is exceeded.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="deadline">The deadline cancellation source.</param>
    /// <param name="selection">The selection type.</param>
    public static void ThrowIfCancelledOrDeadline(CancellationToken cancellationToken, CancellationTokenSource deadline, string selection)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (deadline.IsCancellationRequested)
        {
            throw DeadlineExceeded(selection);
        }
    }

    /// <summary>
    /// Creates a new <see cref="SelectionLimitException"/> for too many candidates.
    /// </summary>
    /// <param name="selection">The selection type.</param>
    /// <param name="candidates">The candidate type.</param>
    /// <param name="count">The number of candidates.</param>
    /// <param name="limit">The limit of candidates.</param>
    /// <returns>A new <see cref="SelectionLimitException"/>.</returns>
    public static SelectionLimitException TooManyCandidates(string selection, string candidates, int count, int limit) => new(SelectionLimitException.TooManyCandidatesErrorId, $"The {selection} selection found {count} candidate {candidates} inside the search area, more than the limit of {limit}; use a smaller search area or a higher MaxEvidenceCandidates (contract SC-18).");
}
