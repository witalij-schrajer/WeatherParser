using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Quantities;

using System.Globalization;

namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// Default implementation of <see cref="IDwdObservationParser"/>; one parse per call, no internal scheduling.
/// </summary>
public sealed class DwdObservationParser : IDwdObservationParser
{
    private readonly DwdObservationOptions _options;
    private readonly IDwdStationResolver _resolver;
    private readonly IDwdObservationSource _source;
    private readonly IDwdObservationMapper _mapper;
    private readonly IObservationDatasetBuilder _builder;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DwdObservationParser> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DwdObservationParser"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="resolver">The station resolver.</param>
    /// <param name="source">The observation source.</param>
    /// <param name="mapper">The observation mapper.</param>
    /// <param name="builder">The dataset builder.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="logger">The logger.</param>
    public DwdObservationParser(DwdObservationOptions options, IDwdStationResolver resolver, IDwdObservationSource source, IDwdObservationMapper mapper, IObservationDatasetBuilder builder, TimeProvider timeProvider, ILogger<DwdObservationParser> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ObservationDataset> ParseAsync(string stationId, CancellationToken cancellationToken)
    {
        var id = _resolver.ValidateId(stationId);
        var reference = _timeProvider.GetUtcNow();
        var retrievedAt = new DateTime(reference.UtcDateTime.Ticks - (reference.UtcDateTime.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);

        using var deadline = new CancellationTokenSource(_options.ObservationDeadline, _timeProvider);

        try
        {
            var lists = await _source.ReadStationListsAsync(deadline.Token, cancellationToken);
            Checkpoint(cancellationToken, deadline);

            foreach (var diagnostic in lists.SelectMany(l => l.Catalog.Diagnostics).Where(d => !d.AlreadyLogged))
            {
                LogDiagnostic(diagnostic, retrievedAt);
            }

            DwdResolvedStation station;
            try
            {
                station = _resolver.Resolve(id, lists);
            }
            catch (DwdStationException exception) when (exception.Diagnostic is not null)
            {
                LogDiagnostic(exception.Diagnostic, retrievedAt);
                throw;
            }

            var archives = await _source.ReadArchivesAsync(id, station.ProductCodes, deadline.Token, cancellationToken);
            Checkpoint(cancellationToken, deadline);
            var evidence = new List<CdcNowEvidence>();
            foreach (var code in station.ProductCodes)
            {
                if (archives.TryGetValue(code, out var item) && item is not null)
                {
                    evidence.Add(item);
                }
                else
                {
                    _logger.LogWarning(new EventId(0, DwdObservationNoticeIds.ProductMissing), "{NoticeId}: the now archive of product {ProductCode} for station {StationId} was not found (HTTP 404); the product contributes nothing (contract OC-03). Retrieved at {RetrievedAtUtc} UTC.", DwdObservationNoticeIds.ProductMissing, code, id, Format(retrievedAt));
                }
            }

            var mapping = _mapper.Map(evidence);
            Checkpoint(cancellationToken, deadline);

            foreach (var omission in mapping.Omissions)
            {
                LogOmission(id, omission.Kind, omission.Reason.ToString(), retrievedAt);
            }

            ObservationBuildResult built;
            try
            {
                built = _builder.Build(new ObservationInput { Provider = "dwd", Location = station.Location, RetrievedAtUtc = retrievedAt, Readings = mapping.Readings });
            }
            catch (ObservationDatasetRejectedException exception)
            {
                foreach (var omitted in exception.OmittedReadings)
                {
                    LogOmission(id, omitted.Kind, omitted.Reason.ToString(), retrievedAt);
                }
                throw;
            }

            Checkpoint(cancellationToken, deadline);

            foreach (var omitted in built.OmittedReadings)
            {
                LogOmission(id, omitted.Kind, omitted.Reason.ToString(), retrievedAt);
            }

            Checkpoint(cancellationToken, deadline);
            return built.Dataset;
        }
        catch (Exception exception) when (Reclassify(exception, cancellationToken, deadline))
        {
            throw Interruption(cancellationToken, deadline)!;
        }
    }

    /// <summary>
    /// True when a failure must be reported as caller cancellation or deadline expiry instead (contract OC-09: caller cancellation, then the observation deadline, then every other failure).
    /// </summary>
    private static bool Reclassify(Exception exception, CancellationToken cancellationToken, CancellationTokenSource deadline)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return exception is not OperationCanceledException canceled || canceled.CancellationToken != cancellationToken;
        }

        return deadline.IsCancellationRequested && exception is not ObservationLimitException;
    }

    /// <summary>
    /// The failure that wins when the caller cancelled (first) or the observation deadline expired (contract OC-09); null when neither happened.
    /// </summary>
    private static Exception? Interruption(CancellationToken cancellationToken, CancellationTokenSource deadline)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new OperationCanceledException(cancellationToken);
        }

        return deadline.IsCancellationRequested
            ? new ObservationLimitException(ObservationLimitException.DeadlineExceededErrorId, "The DWD observation parse exceeded its observation deadline (contract OC-09).")
            : null;
    }

    /// <summary>
    /// Throws when the caller cancelled (which wins) or the observation deadline expired (contract OC-09).
    /// </summary>
    private static void Checkpoint(CancellationToken cancellationToken, CancellationTokenSource deadline)
    {
        var interruption = Interruption(cancellationToken, deadline);
        if (interruption is not null)
        {
            throw interruption;
        }
    }

    private void LogOmission(string stationId, QuantityKind kind, string reason, DateTime retrievedAt) =>
        _logger.LogWarning(new EventId(0, DwdObservationNoticeIds.ReadingOmitted), "{NoticeId}: the reading {Kind} of station {StationId} was omitted ({Reason}) (contract OC-04 to OC-06, C-59). Retrieved at {RetrievedAtUtc} UTC.", DwdObservationNoticeIds.ReadingOmitted, kind, stationId, reason, Format(retrievedAt));

    private void LogDiagnostic(CatalogDiagnostic diagnostic, DateTime retrievedAt) =>
        _logger.LogWarning(new EventId(0, diagnostic.Id), "{DiagnosticId}: {Reason} Endpoint {EndpointName}, line {LineNumber}, station {StationId}. Retrieved at {RetrievedAtUtc} UTC.", diagnostic.Id, diagnostic.Reason, diagnostic.EndpointName, diagnostic.LineNumber, diagnostic.StationId, Format(retrievedAt));

    private static string Format(DateTime utc) => utc.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
}
