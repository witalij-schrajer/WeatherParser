using WeatherParser.Parsing.Evidence.MosmixS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Reads MOSMIX station catalogs and evidence files.
/// </summary>
public sealed class MosmixCatalogSource : IMosmixCatalogSource
{
    private readonly MosmixCatalogEndpoints _options;
    private readonly IMosmixCatalogEndpointsValidator _validator;
    private readonly ICatalogDownloader _downloader;
    private readonly IMosmixLocationCatalogReader _catalogReader;
    private readonly IMosmixLEvidenceReader _evidenceReader;
    private readonly TimeProvider _timeProvider;
    private const int MaxParallelRequests = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixCatalogSource"/> class.
    /// </summary>
    /// <param name="options">The MOSMIX catalog endpoints options.</param>
    /// <param name="validator">The validator to use.</param>
    /// <param name="downloader">The catalog downloader to use.</param>
    /// <param name="catalogReader">The catalog reader to use.</param>
    /// <param name="evidenceReader">The evidence reader to use.</param>
    /// <param name="timeProvider">The time provider to use.</param>
    public MosmixCatalogSource(
        MosmixCatalogEndpoints options,
        IMosmixCatalogEndpointsValidator validator,
        ICatalogDownloader downloader,
        IMosmixLocationCatalogReader catalogReader,
        IMosmixLEvidenceReader evidenceReader,
        TimeProvider timeProvider)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _catalogReader = catalogReader ?? throw new ArgumentNullException(nameof(catalogReader));
        _evidenceReader = evidenceReader ?? throw new ArgumentNullException(nameof(evidenceReader));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        validator.Validate(options);
    }

    /// <inheritdoc/>
    public async Task<MosmixLocationCatalog> ReadCatalogAsync(CancellationToken selectionDeadline, CancellationToken cancellationToken)
    {
        const string name = "CatalogUri";
        var request = new CatalogRequest(name, _options.CatalogUri!, CatalogRequest.ListOrCatalogMaxBytes, false, selectionDeadline, CatalogDeadlineKind.Selection);
        var catalog = await _downloader.DownloadAsync(request, (stream, token) => _catalogReader.ReadAsync(stream, name, token), cancellationToken);
        return catalog!;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<string, MosmixLRetrieval?>> ReadMosmixLEvidenceAsync(IReadOnlyCollection<string> mosmixIds, CancellationToken selectionDeadline, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mosmixIds);
        if (mosmixIds.Distinct(StringComparer.Ordinal).Count() != mosmixIds.Count)
        {
            throw new ArgumentException("A MOSMIX ID occurs more than once.", nameof(mosmixIds));
        }

        var results = new Dictionary<string, MosmixLRetrieval?>(StringComparer.Ordinal);
        using var gate = new SemaphoreSlim(MaxParallelRequests);
        var tasks = mosmixIds.Select(async id =>
        {
            var escaped = Uri.EscapeDataString(id);
            var uri = new Uri(_options.MosmixLSingleStationsDirectory!, escaped + "/kml/MOSMIX_L_LATEST_" + escaped + ".kmz");
            var request = new CatalogRequest("MosmixLSingleStationsDirectory", uri, CatalogRequest.PerStationFileMaxBytes, true, selectionDeadline, CatalogDeadlineKind.Selection);
            await gate.WaitAsync(cancellationToken);
            try
            {
                var evidence = await _downloader.DownloadAsync(request, (stream, token) => _evidenceReader.ReadAsync(stream, id, token), cancellationToken);
                MosmixLRetrieval? retrieval = null;
                if (evidence is not null)
                {
                    var now = _timeProvider.GetUtcNow().UtcDateTime;
                    var retrievedAt = new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
                    retrieval = new MosmixLRetrieval(retrievedAt, evidence);
                }

                lock (results)
                {
                    results[id] = retrieval;
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
