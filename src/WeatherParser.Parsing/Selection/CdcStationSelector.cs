using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Selection;

/// <summary>
/// Selects CDC stations from the configured station lists and evidence files (contract SC-03, SC-11, SC-13, SC-18).
/// </summary>
public sealed class CdcStationSelector : ICdcStationSelector
{
    private readonly CdcCatalogEndpoints _options;
    private readonly ICdcCatalogSource _source;
    private readonly ICdcAvailabilityDeriver _deriver;
    private readonly ISearchParameterFactory _parameterFactory;
    private readonly ISearchAreaFilter _searchAreaFilter;
    private readonly IListEntryOrdering _ordering;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CdcStationSelector> _logger;

    private const string Selection = "CDC station";

    /// <summary>
    /// Initializes a new instance of the <see cref="CdcStationSelector"/> class.
    /// </summary>
    /// <param name="options">The CDC catalog endpoint options.</param>
    /// <param name="source">The CDC catalog source.</param>
    /// <param name="deriver">The CDC availability deriver.</param>
    /// <param name="parameterFactory">The search parameter factory.</param>
    /// <param name="searchAreaFilter">The search area filter.</param>
    /// <param name="ordering">The list entry ordering.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="logger">The logger.</param>
    public CdcStationSelector(
        CdcCatalogEndpoints options,
        ICdcCatalogSource source,
        ICdcAvailabilityDeriver deriver,
        ISearchParameterFactory parameterFactory,
        ISearchAreaFilter searchAreaFilter,
        IListEntryOrdering ordering,
        TimeProvider timeProvider,
        ILogger<CdcStationSelector> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _deriver = deriver ?? throw new ArgumentNullException(nameof(deriver));
        _parameterFactory = parameterFactory ?? throw new ArgumentNullException(nameof(parameterFactory));
        _searchAreaFilter = searchAreaFilter ?? throw new ArgumentNullException(nameof(searchAreaFilter));
        _ordering = ordering ?? throw new ArgumentNullException(nameof(ordering));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CdcStationList> SelectAsync(SearchParameters parameters, CancellationToken cancellationToken)
    {
        var snapshot = SelectionSupport.Snapshot(parameters, _parameterFactory);
        var reference = _timeProvider.GetUtcNow();
        using var deadline = new CancellationTokenSource(_options.SelectionDeadline, _timeProvider);

        try
        {
            var lists = await _source.ReadStationListsAsync(deadline.Token, cancellationToken);
            SelectionSupport.ThrowIfCancelledOrDeadline(cancellationToken, deadline, Selection);
            SelectionSupport.LogDiagnostics(_logger, lists.SelectMany(list => list.Catalog.Diagnostics));

            var candidates = _deriver.SelectCandidates(lists, reference);

            var inside = new List<(CdcCandidateStation Station, SearchAreaEvaluation Evaluation)>();
            foreach (var station in candidates.Stations)
            {
                var evaluation = _searchAreaFilter.Evaluate(snapshot, station.Latitude, station.Longitude, station.AltitudeM);
                if (evaluation.Match == SearchAreaMatch.Inside)
                {
                    inside.Add((station, evaluation));
                }
            }

            SelectionSupport.ThrowIfCancelledOrDeadline(cancellationToken, deadline, Selection);

            if (inside.Count > _options.MaxEvidenceCandidates)
            {
                throw SelectionSupport.TooManyCandidates(Selection, "stations", inside.Count, _options.MaxEvidenceCandidates);
            }

            IReadOnlyDictionary<CdcEvidenceKey, CdcNowEvidence?> evidence;
            if (inside.Count == 0)
            {
                evidence = new Dictionary<CdcEvidenceKey, CdcNowEvidence?>();
            }
            else
            {
                var keys = inside.SelectMany(item => item.Station.CandidateProducts.Select(product => new CdcEvidenceKey(item.Station.StationId, product))).ToList();
                evidence = await _source.ReadEvidenceAsync(keys, deadline.Token, cancellationToken);
                SelectionSupport.ThrowIfCancelledOrDeadline(cancellationToken, deadline, Selection);
            }

            var availability = _deriver.Derive(new CdcCandidates(inside.Select(item => item.Station).ToList(), candidates.Diagnostics), evidence, reference);
            SelectionSupport.LogDiagnostics(_logger, availability.Diagnostics);

            var entries = new List<CdcStationListEntry>();
            foreach (var active in availability.Stations)
            {
                var evaluation = inside.First(item => item.Station.StationId == active.Station.StationId).Evaluation;
                entries.Add(new CdcStationListEntry
                {
                    StationId = active.Station.StationId,
                    Name = active.Station.Name,
                    Latitude = active.Station.Latitude,
                    Longitude = active.Station.Longitude,
                    AltitudeM = active.Station.AltitudeM!.Value,
                    DistanceKm = evaluation.DistanceKm!.Value,
                    AltitudeDifferenceM = evaluation.AltitudeDifferenceM!.Value,
                    AvailableMeasurements = active.AvailableMeasurements.ToList(),
                });
            }

            var ordered = _ordering.Order(entries);
            SelectionSupport.ThrowIfCancelledOrDeadline(cancellationToken, deadline, Selection);
            var result = new CdcStationList
            {
                Source = "dwd:cdc-10min-now",
                RetrievedAtUtc = SelectionSupport.TruncateToSeconds(reference),
                SearchParameters = snapshot,
                Stations = ordered.ToList()
            };

            if (result.Stations.Count == 0)
            {
                _logger.LogInformation(new EventId(0, SelectionNoticeIds.SelectionEmpty), "The CDC station selection found no active station inside the search area (latitude {Latitude}, longitude {Longitude}, maximum distance {MaxDistanceKm} km, maximum altitude difference {MaxAltitudeDifferenceM} m).", snapshot.Latitude, snapshot.Longitude, snapshot.MaxDistanceKm, snapshot.MaxAltitudeDifferenceM);
            }

            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            throw SelectionSupport.DeadlineExceeded(Selection);
        }
    }
}
