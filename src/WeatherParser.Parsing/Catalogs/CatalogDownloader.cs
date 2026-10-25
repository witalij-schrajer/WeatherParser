using System.Globalization;
using System.Net;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Downloads catalogs, lists and evidence files within the SC-09 bounds.
/// </summary>
public sealed class CatalogDownloader : ICatalogDownloader
{
    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;
    private const int RequestTimeoutSeconds = 30;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogDownloader"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="timeProvider">The time provider.</param>
    public CatalogDownloader(HttpClient httpClient, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _httpClient = httpClient;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<T?> DownloadAsync<T>(CatalogRequest request, Func<Stream, CancellationToken, Task<T>> read, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(read);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(RequestTimeoutSeconds), _timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, request.DeadlineToken, timeout.Token);
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, request.Uri);
            using var response = await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, linked.Token);
            linked.Token.ThrowIfCancellationRequested();
            if (response.StatusCode == HttpStatusCode.NotFound && request.NotFoundIsAbsent)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw Failed(request, $"HTTP status {(int)response.StatusCode}");
            }

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength is long length && length > request.MaxBytes)
            {
                throw TooLarge(request);
            }

            // The response owns its content stream and disposes it when the response is disposed.
            var body = await response.Content.ReadAsStreamAsync(linked.Token);
            var bounded = new BoundedStream(body, request, contentLength, linked.Token);
            var result = await read(bounded, linked.Token);
            linked.Token.ThrowIfCancellationRequested();
            return result;
        }
        catch (OperationCanceledException)
        {
            throw Classify(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw Failed(request, exception.GetType().Name);
        }
    }

    private static string Sanitize(Uri uri) =>
        uri.Scheme + "://" + uri.Host + (uri.IsDefaultPort ? string.Empty : ":" + uri.Port.ToString(CultureInfo.InvariantCulture)) + uri.AbsolutePath;

    private static CatalogRetrievalException TooLarge(CatalogRequest request) =>
        new(CatalogRetrievalException.TooLargeErrorId, request.EndpointName, $"The response of endpoint '{request.EndpointName}' ({Sanitize(request.Uri)}) exceeds the bound of {request.MaxBytes} bytes (contract SC-09).");

    private static Exception Classify(CatalogRequest request, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new OperationCanceledException("The retrieval was cancelled by the caller.", cancellationToken);
        }

        if (request.DeadlineToken.IsCancellationRequested)
        {
            var message = $"The deadline expired while retrieving endpoint '{request.EndpointName}' ({Sanitize(request.Uri)}).";
            switch (request.DeadlineKind)
            {
                case CatalogDeadlineKind.Selection:
                    return new SelectionLimitException(SelectionLimitException.DeadlineExceededErrorId, message + " The selection deadline was exceeded (contract SC-18).");
                case CatalogDeadlineKind.Observation:
                    return new ObservationLimitException(ObservationLimitException.DeadlineExceededErrorId, message + " The observation deadline was exceeded (contract OC-09).");
                case CatalogDeadlineKind.OsthessenWetter:
                    return new OsthessenWetterException(OsthessenWetterException.DeadlineExceededErrorId, message + " The OsthessenWetter page deadline was exceeded (contract OW-10).");
                default: // Refresh
                    return new EvidenceCacheException(EvidenceCacheException.RefreshDeadlineExceededErrorId, message + " The MOSMIX_S refresh deadline was exceeded (contract SC-22).");
            }
        }

        return new CatalogRetrievalException(CatalogRetrievalException.TimeoutErrorId, request.EndpointName, $"The retrieval of endpoint '{request.EndpointName}' ({Sanitize(request.Uri)}) timed out (contract SC-09).");
    }

    private static CatalogRetrievalException Failed(CatalogRequest request, string reason) =>
        new(CatalogRetrievalException.RetrievalFailedErrorId, request.EndpointName, $"The retrieval of endpoint '{request.EndpointName}' ({Sanitize(request.Uri)}) failed: {reason} (contract SC-09).");

    private sealed class BoundedStream(Stream inner, CatalogRequest request, long? contentLength, CancellationToken token) : ReadOnlyStreamBase
    {
        private long _total;

        public override int Read(byte[] buffer, int offset, int count)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                return Count(inner.Read(buffer, offset, count));
            }
            catch (Exception exception) when (exception is IOException or HttpRequestException)
            {
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException(token);
                }

                throw Failed(request, exception.GetType().Name);
            }
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            token.ThrowIfCancellationRequested();
            int read;
            try
            {
                read = await inner.ReadAsync(buffer, token);
            }
            catch (Exception exception) when (exception is IOException or HttpRequestException)
            {
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException(token);
                }

                throw Failed(request, exception.GetType().Name);
            }

            return Count(read);
        }

        private int Count(int read)
        {
            token.ThrowIfCancellationRequested();
            _total += read;
            if (_total > request.MaxBytes)
            {
                throw TooLarge(request);
            }

            if (read == 0 && contentLength is long expected && _total < expected)
            {
                throw Failed(request, "the body is shorter than its Content-Length");
            }

            return read;
        }
    }
}
