using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Provides access to MOSMIX_L forecast data (contract MF-05, MF-07, MF-14).
/// </summary>
public interface IMosmixLForecastSource
{
    /// <summary>
    /// Gets the configured single-stations directory as scheme, host, non-default port and path, without user information, query or fragment, for diagnostics (contract MF-15).
    /// </summary>
    string SanitizedEndpoint { get; }

    /// <summary>
    /// Retrieves the forecast data for a specific station.
    /// One request is made, no retries are performed.
    /// The deadline and cancellation token are passed to the downloader.
    /// The ID is validated first against the grammar ^[A-Z0-9]{4,5}$ (contract MF-03); an invalid ID throws an exception with error ID <see cref="MosmixForecastErrorIds.IdInvalid"/> before any request, and the message never contains the ID.
    /// If the file of the location is not published (HTTP 404), an exception with error ID <see cref="MosmixForecastErrorIds.IdUnknown"/> is thrown (contract MF-07).
    /// The file URI is composed beneath the configured directory as <c>&lt;id&gt;/kml/MOSMIX_L_LATEST_&lt;id&gt;.kmz</c>; the request carries the per-station size bound and the downloader's 30-second request bound.
    /// If the file exceeds its size bound, an exception with error ID <see cref="MosmixForecastErrorIds.TooLarge"/> is thrown.
    /// If the downloader throws a <see cref="WeatherParser.Parsing.Exceptions.CatalogRetrievalException"/> with error ID <see cref="WeatherParser.Parsing.Exceptions.CatalogRetrievalException.RetrievalFailedErrorId"/>,
    /// an exception with error ID <see cref="MosmixForecastErrorIds.RetrievalFailed"/> is thrown.
    /// If the downloader throws a <see cref="WeatherParser.Parsing.Exceptions.CatalogRetrievalException"/> with error ID <see cref="WeatherParser.Parsing.Exceptions.CatalogRetrievalException.TooLargeErrorId"/>,
    /// an exception with error ID <see cref="MosmixForecastErrorIds.TooLarge"/> is thrown.
    /// If the downloader throws a <see cref="WeatherParser.Parsing.Exceptions.CatalogRetrievalException"/> with error ID <see cref="WeatherParser.Parsing.Exceptions.CatalogRetrievalException.TimeoutErrorId"/>,
    /// an exception with error ID <see cref="MosmixForecastErrorIds.RetrievalFailed"/> is thrown.
    /// If the downloader throws a <see cref="MosmixForecastException"/> or <see cref="OperationCanceledException"/>,
    /// these exceptions are propagated unchanged.
    /// Caller cancellation takes precedence (an <see cref="OperationCanceledException"/> with the caller's token), then the parse deadline (<see cref="MosmixForecastErrorIds.DeadlineExceeded"/>), then every other failure (contract MF-14).
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="mosmixId">The MOSMIX ID of the station.</param>
    /// <param name="read">The function to read the data.</param>
    /// <param name="deadline">The deadline for the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<T> RetrieveAsync<T>(string mosmixId, Func<Stream, CancellationToken, Task<T>> read, CancellationToken deadline, CancellationToken cancellationToken) where T : class;
}
