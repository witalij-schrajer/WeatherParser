using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// Default implementation of <see cref="IDwdObservationSource"/>; at most eight requests per parse, at most four in parallel, no retries (contract OC-11).
/// </summary>
public sealed class DwdObservationSource : IDwdObservationSource
{
    private readonly DwdObservationOptions _options;
    private readonly CdcCatalogEndpoints _endpoints;
    private readonly ICdcCatalogEndpointsValidator _validator;
    private readonly ICatalogDownloader _downloader;
    private readonly ICdcStationCatalogReader _listReader;
    private readonly ICdcNowEvidenceReader _evidenceReader;
    private readonly ICdcRequestComposer _composer;
    private const int MaxParallelRequests = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="DwdObservationSource"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="endpoints">The endpoints.</param>
    /// <param name="validator">The validator.</param>
    /// <param name="downloader">The downloader.</param>
    /// <param name="listReader">The list reader.</param>
    /// <param name="evidenceReader">The evidence reader.</param>
    /// <param name="composer">The CDC request composer to use.</param>
    public DwdObservationSource(DwdObservationOptions options, CdcCatalogEndpoints endpoints, ICdcCatalogEndpointsValidator validator, ICatalogDownloader downloader, ICdcStationCatalogReader listReader, ICdcNowEvidenceReader evidenceReader, ICdcRequestComposer composer)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _listReader = listReader ?? throw new ArgumentNullException(nameof(listReader));
        _evidenceReader = evidenceReader ?? throw new ArgumentNullException(nameof(evidenceReader));
        _composer = composer ?? throw new ArgumentNullException(nameof(composer));

        _validator.ValidateEndpoints(endpoints);

        var deadline = options.ObservationDeadline;
        if (deadline <= TimeSpan.Zero || deadline > TimeSpan.FromHours(1))
        {
            throw new ObservationLimitException(ObservationLimitException.LimitInvalidErrorId, "ObservationDeadline must be greater than zero and at most one hour (contract OC-09).");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CdcProductList>> ReadStationListsAsync(CancellationToken deadline, CancellationToken cancellationToken)
    {
        var lists = new List<CdcProductList>();
        foreach (var target in _composer.StationLists(_endpoints))
        {
            var request = new CatalogRequest(target.EndpointName, target.Uri, CatalogRequest.ListOrCatalogMaxBytes, false, deadline, CatalogDeadlineKind.Observation);
            var catalog = await _downloader.DownloadAsync(request, (stream, token) => _listReader.ReadAsync(stream, target.EndpointName, token), cancellationToken);
            lists.Add(new CdcProductList(target.ProductCode, target.EndpointName, catalog!));
        }

        return lists;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, CdcNowEvidence?>> ReadArchivesAsync(string stationId, IReadOnlyList<string> productCodes, CancellationToken deadline, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stationId);
        ArgumentNullException.ThrowIfNull(productCodes);

        var requests = new List<(string ProductCode, CatalogRequest Request)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var code in productCodes)
        {
            if (!seen.Add(code))
            {
                throw new ArgumentException($"The product code '{code}' occurs more than once.", nameof(productCodes));
            }

            var target = _composer.Archive(_endpoints, stationId, code);
            requests.Add((code, new CatalogRequest(target.EndpointName, target.Uri, CatalogRequest.PerStationFileMaxBytes, true, deadline, CatalogDeadlineKind.Observation)));
        }

        var results = new Dictionary<string, CdcNowEvidence?>(StringComparer.Ordinal);
        using var gate = new SemaphoreSlim(MaxParallelRequests);
        var failed = 0;
        var tasks = requests.Select(async item =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (Volatile.Read(ref failed) != 0)
                {
                    return;
                }
                var evidence = await _downloader.DownloadAsync(item.Request, (stream, token) => _evidenceReader.ReadAsync(stream, stationId, item.ProductCode, token), cancellationToken);
                lock (results)
                {
                    results[item.ProductCode] = evidence;
                }
            }
            catch
            {
                Interlocked.Exchange(ref failed, 1);
                throw;
            }
            finally
            {
                gate.Release();
            }
        }).ToList();
        await Task.WhenAll(tasks);
        return results;
    }
}
