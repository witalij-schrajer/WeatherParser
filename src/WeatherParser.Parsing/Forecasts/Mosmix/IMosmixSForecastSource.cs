using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Provides access to MOSMIX_S forecast data (contract MF-05, MF-07, MF-14).
/// </summary>
public interface IMosmixSForecastSource
{
    /// <summary>
    /// Retrieves the forecast data for all stations.
    /// One request is made, no retries are performed.
    /// The deadline and cancellation token are passed to the downloader.
    /// If the file exceeds its size bound, an exception with error ID <see cref="MosmixForecastErrorIds.TooLarge"/> is thrown.
    /// If the downloader throws a <see cref="WeatherParser.Parsing.Exceptions.CatalogRetrievalException"/> with error ID <see cref="WeatherParser.Parsing.Exceptions.CatalogRetrievalException.RetrievalFailedErrorId"/>,
    /// an exception with error ID <see cref="MosmixForecastErrorIds.RetrievalFailed"/> is thrown.
    /// If the downloader throws a <see cref="WeatherParser.Parsing.Exceptions.CatalogRetrievalException"/> with error ID <see cref="WeatherParser.Parsing.Exceptions.CatalogRetrievalException.TooLargeErrorId"/>,
    /// an exception with error ID <see cref="MosmixForecastErrorIds.TooLarge"/> is thrown.
    /// If the downloader throws a <see cref="WeatherParser.Parsing.Exceptions.CatalogRetrievalException"/> with error ID <see cref="WeatherParser.Parsing.Exceptions.CatalogRetrievalException.TimeoutErrorId"/>,
    /// an exception with error ID <see cref="MosmixForecastErrorIds.RetrievalFailed"/> is thrown.
    /// If the downloader throws a <see cref="MosmixForecastException"/> or <see cref="OperationCanceledException"/>,
    /// these exceptions are propagated unchanged.
    /// The configured URI is used unchanged; the request carries the all-stations size bound and a request bound equal to <see cref="MosmixSForecastOptions.ParseDeadline"/> (contract MF-05).
    /// Caller cancellation takes precedence (an <see cref="OperationCanceledException"/> with the caller's token), then the parse deadline (<see cref="MosmixForecastErrorIds.DeadlineExceeded"/>), then every other failure (contract MF-14).
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="read">The function to read the data.</param>
    /// <param name="deadline">The deadline for the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<T> RetrieveAsync<T>(Func<Stream, CancellationToken, Task<T>> read, CancellationToken deadline, CancellationToken cancellationToken) where T : class;
}
