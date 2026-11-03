using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Reads CDC station lists and evidence files.
/// </summary>
public sealed class CdcCatalogSource : ICdcCatalogSource
{
    private readonly CdcCatalogEndpoints _options;
    private readonly ICdcCatalogEndpointsValidator _validator;
    private readonly ICatalogDownloader _downloader;
    private readonly ICdcStationCatalogReader _listReader;
    private readonly ICdcNowEvidenceReader _evidenceReader;
    private readonly ICdcRequestComposer _composer;
    private const int MaxParallelRequests = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdcCatalogSource"/> class.
    /// </summary>
    /// <param name="options">The CDC catalog endpoints options.</param>
    /// <param name="validator">The validator to use.</param>
    /// <param name="downloader">The catalog downloader to use.</param>
    /// <param name="listReader">The station list reader to use.</param>
    /// <param name="evidenceReader">The evidence reader to use.</param>
    /// <param name="composer">The CDC request composer to use.</param>
    public CdcCatalogSource(
        CdcCatalogEndpoints options,
        ICdcCatalogEndpointsValidator validator,
        ICatalogDownloader downloader,
        ICdcStationCatalogReader listReader,
        ICdcNowEvidenceReader evidenceReader,
        ICdcRequestComposer composer)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _listReader = listReader ?? throw new ArgumentNullException(nameof(listReader));
        _evidenceReader = evidenceReader ?? throw new ArgumentNullException(nameof(evidenceReader));
        _composer = composer ?? throw new ArgumentNullException(nameof(composer));
        validator.Validate(options);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CdcProductList>> ReadStationListsAsync(CancellationToken selectionDeadline, CancellationToken cancellationToken)
    {
        var lists = new List<CdcProductList>();
        foreach (var target in _composer.StationLists(_options))
        {
            var request = new CatalogRequest(target.EndpointName, target.Uri, CatalogRequest.ListOrCatalogMaxBytes, false, selectionDeadline, CatalogDeadlineKind.Selection);
            var catalog = await _downloader.DownloadAsync(request, (stream, token) => _listReader.ReadAsync(stream, target.EndpointName, token), cancellationToken);
            lists.Add(new CdcProductList(target.ProductCode, target.EndpointName, catalog!));
        }

        return lists;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<CdcEvidenceKey, CdcNowEvidence?>> ReadEvidenceAsync(IReadOnlyCollection<CdcEvidenceKey> keys, CancellationToken selectionDeadline, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keys);
        var requests = new List<(CdcEvidenceKey Key, CatalogRequest Request)>();
        var seen = new HashSet<CdcEvidenceKey>();
        foreach (var key in keys)
        {
            if (!seen.Add(key))
            {
                throw new ArgumentException($"The evidence key {key.StationId}/{key.ProductCode} occurs more than once.", nameof(keys));
            }

            var target = _composer.Archive(_options, key.StationId, key.ProductCode);
            requests.Add((key, new CatalogRequest(target.EndpointName, target.Uri, CatalogRequest.PerStationFileMaxBytes, true, selectionDeadline, CatalogDeadlineKind.Selection)));
        }

        var results = new Dictionary<CdcEvidenceKey, CdcNowEvidence?>();
        using var gate = new SemaphoreSlim(MaxParallelRequests);
        var tasks = requests.Select(async item =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var evidence = await _downloader.DownloadAsync(item.Request, (stream, token) => _evidenceReader.ReadAsync(stream, item.Key.StationId, item.Key.ProductCode, token), cancellationToken);
                lock (results)
                {
                    results[item.Key] = evidence;
                }
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
