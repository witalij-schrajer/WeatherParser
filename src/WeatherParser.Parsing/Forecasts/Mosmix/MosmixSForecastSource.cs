using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Retrieves the MOSMIX_S all-stations file from the consumer's URI once per call, with a request bound equal to the parse deadline (contracts MF-04, MF-05, MF-14).
/// </summary>
public sealed class MosmixSForecastSource : IMosmixSForecastSource
{
    /// <summary>
    /// The endpoint name used in requests and diagnostics.
    /// </summary>
    public const string EndpointName = "MosmixSAllStationsUri";

    private readonly Uri _uri;
    private readonly TimeSpan _requestTimeout;
    private readonly ICatalogDownloader _downloader;

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixSForecastSource"/> class.
    /// </summary>
    /// <param name="endpoints">The endpoints.</param>
    /// <param name="options">The options.</param>
    /// <param name="uriValidator">The URI validator.</param>
    /// <param name="downloader">The downloader.</param>
    /// <exception cref="ArgumentNullException">When an argument is null.</exception>
    public MosmixSForecastSource(MosmixSForecastEndpoints endpoints, MosmixSForecastOptions options, IEndpointUriValidator uriValidator, ICatalogDownloader downloader)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(uriValidator);
        ArgumentNullException.ThrowIfNull(downloader);

        _downloader = downloader;

        var uri = endpoints.AllStationsUri;
        if (uri is null || !uriValidator.IsValid(uri) || !string.IsNullOrEmpty(uri!.UserInfo))
        {
            throw Invalid("MosmixSForecastEndpoints.AllStationsUri must be an absolute HTTP or HTTPS URI without user information (contract MF-04).");
        }

        var parseDeadline = options.ParseDeadline;
        if (parseDeadline <= TimeSpan.Zero || parseDeadline > TimeSpan.FromHours(1))
        {
            throw Invalid("MosmixSForecastOptions.ParseDeadline must be greater than zero and at most one hour (contract MF-05).");
        }

        var maxRunAge = options.MaxRunAge;
        if (!maxRunAge.HasValue || maxRunAge.Value <= TimeSpan.Zero || maxRunAge.Value > TimeSpan.FromHours(240))
        {
            throw Invalid("MosmixSForecastOptions.MaxRunAge must be set, greater than zero and at most 240 hours (contract MF-09).");
        }

        _uri = uri!;
        _requestTimeout = parseDeadline;
    }

    /// <inheritdoc />
    public async Task<T> RetrieveAsync<T>(Func<Stream, CancellationToken, Task<T>> read, CancellationToken deadline, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(read);
        var endpoint = CatalogDownloader.Sanitize(_uri);
        var request = new CatalogRequest(EndpointName, _uri, CatalogRequest.MosmixSAllStationsMaxBytes, false, deadline, CatalogDeadlineKind.MosmixS)
        {
            RequestTimeout = _requestTimeout
        };
        try
        {
            var result = await _downloader.DownloadAsync(request, read, cancellationToken);
            // NotFoundIsAbsent is false, so the downloader never returns null here.
            return result!;
        }
        catch (CatalogRetrievalException exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (deadline.IsCancellationRequested)
            {
                throw new MosmixForecastException(MosmixForecastErrorIds.DeadlineExceeded, MosmixProduct.MosmixS, $"The MOSMIX_S all-stations file could not be retrieved within the parse deadline (endpoint '{EndpointName}', {endpoint}): {exception.ErrorId} (contract MF-05).");
            }

            var errorId = exception.ErrorId switch
            {
                CatalogRetrievalException.TooLargeErrorId => MosmixForecastErrorIds.TooLarge,
                _ => MosmixForecastErrorIds.RetrievalFailed
            };
            throw new MosmixForecastException(errorId, MosmixProduct.MosmixS, $"The MOSMIX_S all-stations file could not be retrieved (endpoint '{EndpointName}', {endpoint}): {exception.ErrorId} (contract MF-14).");
        }
    }

    private static MosmixForecastException Invalid(string reason) => new(MosmixForecastErrorIds.OptionsInvalid, MosmixProduct.MosmixS, reason);
}
