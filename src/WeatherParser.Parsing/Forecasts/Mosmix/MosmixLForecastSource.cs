using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Retrieves the MOSMIX_L per-location file below the consumer's single_stations/ directory once per call (contracts MF-04, MF-05, MF-14).
/// </summary>
public sealed class MosmixLForecastSource : IMosmixLForecastSource
{
    /// <summary>
    /// The endpoint name used in requests and diagnostics.
    /// </summary>
    public const string EndpointName = "MosmixLSingleStationsDirectory";

    private readonly Uri _directory;
    private readonly ICatalogDownloader _downloader;
    private readonly string _sanitizedEndpoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixLForecastSource"/> class.
    /// </summary>
    /// <param name="endpoints">The endpoints.</param>
    /// <param name="options">The options.</param>
    /// <param name="uriValidator">The URI validator.</param>
    /// <param name="downloader">The downloader.</param>
    /// <exception cref="ArgumentNullException">When an argument is null.</exception>
    /// <exception cref="MosmixForecastException">When the options are invalid (contract MF-04, MF-05, MF-09).</exception>
    public MosmixLForecastSource(MosmixLForecastEndpoints endpoints, MosmixLForecastOptions options, IEndpointUriValidator uriValidator, ICatalogDownloader downloader)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(uriValidator);
        ArgumentNullException.ThrowIfNull(downloader);

        _downloader = downloader;

        var uri = endpoints.SingleStationsDirectory;
        if (uri is null || !uriValidator.IsValid(uri) || !string.IsNullOrEmpty(uri.UserInfo) || uri.AbsolutePath[^1] != '/' || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw Invalid("MosmixLForecastEndpoints.SingleStationsDirectory must be an absolute HTTP or HTTPS directory URI ending with '/' without user information, query or fragment (contracts MF-04, SC-12).");
        }

        var deadline = options.ParseDeadline;
        if (deadline <= TimeSpan.Zero || deadline > TimeSpan.FromHours(1))
        {
            throw Invalid("MosmixLForecastOptions.ParseDeadline must be greater than zero and at most one hour (contract MF-05).");
        }

        var maxRunAge = options.MaxRunAge;
        if (!maxRunAge.HasValue || maxRunAge.Value <= TimeSpan.Zero || maxRunAge.Value > TimeSpan.FromHours(240))
        {
            throw Invalid("MosmixLForecastOptions.MaxRunAge must be set, greater than zero and at most 240 hours (contract MF-09).");
        }

        _directory = uri;
        _sanitizedEndpoint = CatalogDownloader.Sanitize(_directory);
    }

    /// <inheritdoc />
    public string SanitizedEndpoint => _sanitizedEndpoint;

    /// <inheritdoc />
    public async Task<T> RetrieveAsync<T>(string mosmixId, Func<Stream, CancellationToken, Task<T>> read, CancellationToken deadline, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(mosmixId);
        ArgumentNullException.ThrowIfNull(read);

        if (!IsValidMosmixId(mosmixId))
        {
            throw new MosmixForecastException(MosmixForecastErrorIds.IdInvalid, MosmixProduct.MosmixL, "The MOSMIX ID must consist of 4 or 5 characters A-Z or 0-9 (contract MF-03).");
        }

        var escaped = Uri.EscapeDataString(mosmixId);
        var uri = new Uri(_directory, escaped + "/kml/MOSMIX_L_LATEST_" + escaped + ".kmz");
        var endpoint = CatalogDownloader.Sanitize(uri);
        var request = new CatalogRequest(EndpointName, uri, CatalogRequest.PerStationFileMaxBytes, true, deadline, CatalogDeadlineKind.MosmixL);

        try
        {
            var result = await _downloader.DownloadAsync(request, read, cancellationToken);
            if (result is null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (deadline.IsCancellationRequested)
                {
                    throw new MosmixForecastException(MosmixForecastErrorIds.DeadlineExceeded, MosmixProduct.MosmixL, $"The MOSMIX_L file could not be retrieved within the parse deadline (endpoint '{EndpointName}', {endpoint}) (contract MF-05).");
                }

                throw new MosmixForecastException(MosmixForecastErrorIds.IdUnknown, MosmixProduct.MosmixL, $"The MOSMIX_L file of the requested location is not published (HTTP 404, endpoint '{EndpointName}', {endpoint}) (contract MF-07).");
            }

            return result;
        }
        catch (CatalogRetrievalException exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (deadline.IsCancellationRequested)
            {
                throw new MosmixForecastException(MosmixForecastErrorIds.DeadlineExceeded, MosmixProduct.MosmixL, $"The MOSMIX_L file could not be retrieved within the parse deadline (endpoint '{EndpointName}', {endpoint}): {exception.ErrorId} (contract MF-05).");
            }

            var errorId = exception.ErrorId switch
            {
                CatalogRetrievalException.TooLargeErrorId => MosmixForecastErrorIds.TooLarge,
                _ => MosmixForecastErrorIds.RetrievalFailed
            };

            throw new MosmixForecastException(errorId, MosmixProduct.MosmixL, $"The MOSMIX_L file could not be retrieved (endpoint '{EndpointName}', {endpoint}): {exception.ErrorId} (contract MF-14).");
        }
    }

    private static bool IsValidMosmixId(string id)
    {
        if (id.Length is < 4 or > 5)
        {
            return false;
        }

        foreach (var c in id)
        {
            if ((c < 'A' || c > 'Z') && (c < '0' || c > '9'))
            {
                return false;
            }
        }

        return true;
    }

    private static MosmixForecastException Invalid(string reason) => new(MosmixForecastErrorIds.OptionsInvalid, MosmixProduct.MosmixL, reason);
}
