using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;

namespace WeatherParser.Parsing.Selection;

/// <summary>
/// Selects MOSMIX locations from the configured catalog, the stored MOSMIX_S evidence index and the MOSMIX_L files (contract SC-04, SC-05, SC-13, SC-18, SC-22, SC-23).
/// </summary>
public sealed class MosmixLocationSelector : IMosmixLocationSelector
{
    private readonly MosmixCatalogEndpoints _options;
    private readonly IMosmixCatalogSource _source;
    private readonly IMosmixEvidenceCache _evidenceCache;
    private readonly IMosmixAvailabilityResolver _availabilityResolver;
    private readonly ISearchParameterFactory _parameterFactory;
    private readonly ISearchAreaFilter _searchAreaFilter;
    private readonly IListEntryOrdering _ordering;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MosmixLocationSelector> _logger;

    private const string Selection = "MOSMIX location";

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixLocationSelector"/> class.
    /// </summary>
    /// <param name="options">The MOSMIX catalog endpoint options.</param>
    /// <param name="source">The MOSMIX catalog source.</param>
    /// <param name="evidenceCache">The MOSMIX_S evidence cache.</param>
    /// <param name="availabilityResolver">The MOSMIX availability resolver.</param>
    /// <param name="parameterFactory">The search parameter factory.</param>
    /// <param name="searchAreaFilter">The search area filter.</param>
    /// <param name="ordering">The list entry ordering.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="logger">The logger.</param>
    public MosmixLocationSelector(
        MosmixCatalogEndpoints options,
        IMosmixCatalogSource source,
        IMosmixEvidenceCache evidenceCache,
        IMosmixAvailabilityResolver availabilityResolver,
        ISearchParameterFactory parameterFactory,
        ISearchAreaFilter searchAreaFilter,
        IListEntryOrdering ordering,
        TimeProvider timeProvider,
        ILogger<MosmixLocationSelector> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _evidenceCache = evidenceCache ?? throw new ArgumentNullException(nameof(evidenceCache));
        _availabilityResolver = availabilityResolver ?? throw new ArgumentNullException(nameof(availabilityResolver));
        _parameterFactory = parameterFactory ?? throw new ArgumentNullException(nameof(parameterFactory));
        _searchAreaFilter = searchAreaFilter ?? throw new ArgumentNullException(nameof(searchAreaFilter));
        _ordering = ordering ?? throw new ArgumentNullException(nameof(ordering));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<MosmixLocationList> SelectAsync(SearchParameters parameters, CancellationToken cancellationToken)
    {
        var snapshot = SelectionSupport.Snapshot(parameters, _parameterFactory);
        var reference = _timeProvider.GetUtcNow();
        using var deadline = new CancellationTokenSource(_options.SelectionDeadline, _timeProvider);

        try
        {
            // The index read is bounded by the selection deadline too (SC-18); a caller cancellation keeps the caller's token (SC-09 precedence).
            MosmixSEvidenceIndex? index;
            var indexRead = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
            try
            {
                index = await _evidenceCache.ReadAsync(indexRead.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("The MOSMIX location selection was cancelled by the caller.", cancellationToken);
            }
            finally
            {
                indexRead.Dispose();
            }
            SelectionSupport.ThrowIfCancelledOrDeadline(cancellationToken, deadline, Selection);
            if (index is null)
            {
                throw new EvidenceCacheException(EvidenceCacheException.EvidenceMissingErrorId, "No usable MOSMIX_S evidence index is available; run the MOSMIX_S evidence refresh first (contract SC-22).");
            }

            var age = reference.UtcDateTime - index.DownloadedAtUtc;
            var maxAge = _options.MosmixSEvidenceMaxAge!.Value;
            if (age > maxAge)
            {
                _logger.LogWarning(new EventId(0, SelectionNoticeIds.MosmixSEvidenceStale), "The MOSMIX_S evidence index downloaded at {DownloadedAt} is {Age} old, older than the limit of {MaxAge}; it is still used (contract SC-22).", index.DownloadedAtUtc, age, maxAge);
            }

            var catalog = await _source.ReadCatalogAsync(deadline.Token, cancellationToken);
            SelectionSupport.ThrowIfCancelledOrDeadline(cancellationToken, deadline, Selection);
            SelectionSupport.LogDiagnostics(_logger, catalog.Diagnostics);

            var inside = new List<(MosmixLocationRecord Record, SearchAreaEvaluation Evaluation)>();
            foreach (var record in catalog.SelectableRecords)
            {
                var evaluation = _searchAreaFilter.Evaluate(snapshot, record.Latitude, record.Longitude, record.AltitudeM);
                if (evaluation.Match == SearchAreaMatch.Inside)
                {
                    inside.Add((record, evaluation));
                }
            }

            SelectionSupport.ThrowIfCancelledOrDeadline(cancellationToken, deadline, Selection);

            if (inside.Count > _options.MaxEvidenceCandidates)
            {
                throw SelectionSupport.TooManyCandidates(Selection, "locations", inside.Count, _options.MaxEvidenceCandidates);
            }

            IReadOnlyDictionary<string, MosmixLRetrieval?> retrievals;
            if (inside.Count == 0)
            {
                retrievals = new Dictionary<string, MosmixLRetrieval?>(StringComparer.Ordinal);
            }
            else
            {
                var ids = inside.Select(item => item.Record.MosmixId).ToList();
                retrievals = await _source.ReadMosmixLEvidenceAsync(ids, deadline.Token, cancellationToken);
                SelectionSupport.ThrowIfCancelledOrDeadline(cancellationToken, deadline, Selection);
            }

            var entries = new List<MosmixLocationListEntry>();
            foreach (var item in inside)
            {
                entries.Add(new MosmixLocationListEntry
                {
                    MosmixId = item.Record.MosmixId,
                    Name = item.Record.Name,
                    Latitude = item.Record.Latitude,
                    Longitude = item.Record.Longitude,
                    AltitudeM = item.Record.AltitudeM!.Value,
                    DistanceKm = item.Evaluation.DistanceKm!.Value,
                    AltitudeDifferenceM = item.Evaluation.AltitudeDifferenceM!.Value,
                    Products = _availabilityResolver.Resolve(item.Record.MosmixId, index, retrievals[item.Record.MosmixId]).ToList(),
                });
            }

            var ordered = _ordering.Order(entries);
            SelectionSupport.ThrowIfCancelledOrDeadline(cancellationToken, deadline, Selection);
            var result = new MosmixLocationList
            {
                Source = "dwd:mosmix-stationskatalog",
                RetrievedAtUtc = SelectionSupport.TruncateToSeconds(reference),
                SearchParameters = snapshot,
                Locations = ordered.ToList()
            };

            if (result.Locations.Count == 0)
            {
                _logger.LogInformation(new EventId(0, SelectionNoticeIds.SelectionEmpty), "The MOSMIX location selection found no location inside the search area (latitude {Latitude}, longitude {Longitude}, maximum distance {MaxDistanceKm} km, maximum altitude difference {MaxAltitudeDifferenceM} m).", snapshot.Latitude, snapshot.Longitude, snapshot.MaxDistanceKm, snapshot.MaxAltitudeDifferenceM);
            }

            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            throw SelectionSupport.DeadlineExceeded(Selection);
        }
    }
}
