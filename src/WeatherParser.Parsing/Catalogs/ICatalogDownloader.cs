namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Downloads catalogs, lists and evidence files within the SC-09 bounds.
/// </summary>
public interface ICatalogDownloader
{
    /// <summary>
    /// Sends one GET request (no retries) with the consumer's <see cref="HttpClient"/>, bounded by 30 s per request through a token linked to the caller's token and the request's deadline token; checks <see cref="System.Net.Http.Headers.HttpContentHeaders.ContentLength"/> against <see cref="CatalogRequest.MaxBytes"/> before reading and enforces the bound while <paramref name="read"/> consumes the body; keeps the response alive while <paramref name="read"/> runs and disposes it afterwards on every path; returns <see langword="null"/> for HTTP 404 when <see cref="CatalogRequest.NotFoundIsAbsent"/> is true.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="request">The catalog request.</param>
    /// <param name="read">The function to read the response body.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the read operation, or <see langword="null"/> for HTTP 404 when <see cref="CatalogRequest.NotFoundIsAbsent"/> is true.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the caller cancellation token is cancelled.</exception>
    /// <exception cref="WeatherParser.Parsing.Exceptions.SelectionLimitException">Thrown when the selection deadline is exceeded.</exception>
    /// <exception cref="WeatherParser.Parsing.Exceptions.EvidenceCacheException">Thrown when the refresh deadline is exceeded.</exception>
    /// <exception cref="WeatherParser.Parsing.Exceptions.CatalogRetrievalException">Thrown for timeouts, non-success status, connection failure or a body shorter than <see cref="System.Net.Http.Headers.HttpContentHeaders.ContentLength"/>.</exception>
    Task<T?> DownloadAsync<T>(CatalogRequest request, Func<Stream, CancellationToken, Task<T>> read, CancellationToken cancellationToken) where T : class;
}
