using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Default implementation of <see cref="IOsthessenWetterPageSource"/>; validates the endpoint and the page deadline at construction (contract OW-10).
/// </summary>
public sealed class OsthessenWetterPageSource : IOsthessenWetterPageSource
{
    /// <summary>
    /// The endpoint name used in requests and diagnostics.
    /// </summary>
    public const string PageEndpointName = "OsthessenWetterPage";

    /// <summary>
    /// The maximum page size in bytes, 2 MiB (contract OW-10).
    /// </summary>
    public const long PageMaxBytes = 2097152;

    private readonly Uri _pageUri;
    private readonly ICatalogDownloader _downloader;

    /// <summary>
    /// Initializes a new instance of the <see cref="OsthessenWetterPageSource"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="endpoints">The endpoints.</param>
    /// <param name="uriValidator">The URI validator.</param>
    /// <param name="downloader">The downloader.</param>
    /// <exception cref="ArgumentNullException">When an argument is null.</exception>
    /// <exception cref="OsthessenWetterException">OSTHESSEN-OPTIONS-INVALID when the page URI is missing, relative, not HTTP or HTTPS or carries user information, or when the page deadline is not greater than zero or exceeds one hour.</exception>
    public OsthessenWetterPageSource(OsthessenWetterOptions options, OsthessenWetterEndpoints endpoints, IEndpointUriValidator uriValidator, ICatalogDownloader downloader)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(uriValidator);
        ArgumentNullException.ThrowIfNull(downloader);

        _downloader = downloader;

        var uri = endpoints.PageUri;
        if (!uriValidator.IsValid(uri) || !string.IsNullOrEmpty(uri!.UserInfo))
        {
            throw new OsthessenWetterException(OsthessenWetterException.OptionsInvalidErrorId, "The OsthessenWetter page URI must be an absolute HTTP or HTTPS URI without user information (contract OW-10).");
        }

        var deadline = options.PageDeadline;
        if (deadline <= TimeSpan.Zero || deadline > TimeSpan.FromHours(1))
        {
            throw new OsthessenWetterException(OsthessenWetterException.OptionsInvalidErrorId, "PageDeadline must be greater than zero and at most one hour (contract OW-10).");
        }

        _pageUri = uri!;
    }

    /// <inheritdoc />
    public async Task<T> ReadAsync<T>(Func<Stream, CancellationToken, Task<T>> read, CancellationToken deadline, CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(read);
        var request = new CatalogRequest(PageEndpointName, _pageUri, PageMaxBytes, false, deadline, CatalogDeadlineKind.OsthessenWetter);
        try
        {
            var result = await _downloader.DownloadAsync(request, read, cancellationToken);
            // NotFoundIsAbsent is false, so the downloader never returns null here.
            return result!;
        }
        catch (CatalogRetrievalException exception)
        {
            throw new OsthessenWetterException(OsthessenWetterException.PageUnavailableErrorId, $"The OsthessenWetter page (endpoint '{PageEndpointName}', host '{_pageUri.Host}') could not be retrieved: {exception.ErrorId} (contract OW-10).");
        }
    }
}
