using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Observations.Dwd;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Default implementation of <see cref="IOsthessenWetterParser"/>; one parse per call, no internal scheduling.
/// </summary>
public sealed class OsthessenWetterParser : IOsthessenWetterParser
{
    private readonly OsthessenWetterOptions _options;
    private readonly IStationNamePatternMatcher _matcher;
    private readonly IOsthessenWetterPageSource _source;
    private readonly IOsthessenWetterPageReader _reader;
    private readonly IOsthessenWetterObservationMapper _mapper;
    private readonly IObservationDatasetBuilder _builder;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OsthessenWetterParser> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OsthessenWetterParser"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="matcher">The station name pattern matcher.</param>
    /// <param name="source">The page source.</param>
    /// <param name="reader">The page reader.</param>
    /// <param name="mapper">The observation mapper.</param>
    /// <param name="builder">The dataset builder.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="logger">The logger.</param>
    public OsthessenWetterParser(OsthessenWetterOptions options, IStationNamePatternMatcher matcher, IOsthessenWetterPageSource source, IOsthessenWetterPageReader reader, IOsthessenWetterObservationMapper mapper, IObservationDatasetBuilder builder, TimeProvider timeProvider, ILogger<OsthessenWetterParser> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private const string Provider = "osthessenwetter";

    /// <inheritdoc />
    public async Task<OsthessenWetterParseResult> ParseAsync(string stationNamePattern, CancellationToken cancellationToken)
    {
        var pattern = _matcher.Validate(stationNamePattern);
        using var deadline = new CancellationTokenSource(_options.PageDeadline, _timeProvider);

        try
        {
            var page = await _source.ReadAsync((stream, token) => _reader.ReadAsync(stream, token), deadline.Token, cancellationToken);

            // Contract OW-16: sampled once after the page has arrived, before mapping and date inference.
            var reference = _timeProvider.GetUtcNow().UtcDateTime;
            var retrievedAt = new DateTime(reference.Ticks - (reference.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
            Checkpoint(cancellationToken, deadline);

            // Page-level notices are logged before mapping, so a later failure cannot hide them.
            foreach (var diagnostic in page.Diagnostics.Where(d => d.StationName is null))
            {
                LogPageNotice(diagnostic, retrievedAt);
            }

            var mapped = _mapper.Map(page, retrievedAt);
            Checkpoint(cancellationToken, deadline);

            var matched = mapped.Where(station => _matcher.IsMatch(pattern, station.Name)).ToList();
            if (matched.Count == 0)
            {
                throw new OsthessenWetterException(OsthessenWetterException.PatternNoMatchErrorId, "No overview station name matches the station name pattern (contract OW-08).");
            }

            var matchedNames = new HashSet<string>(matched.Select(station => station.Name), StringComparer.Ordinal);
            foreach (var diagnostic in page.Diagnostics)
            {
                if (diagnostic.StationName is not null && matchedNames.Contains(diagnostic.StationName))
                {
                    LogPageNotice(diagnostic, retrievedAt);
                }
            }

            var datasets = new List<ObservationDataset>();
            var failures = new List<OsthessenWetterStationFailure>();
            foreach (var station in matched)
            {
                Checkpoint(cancellationToken, deadline);
                foreach (var notice in station.Notices)
                {
                    LogMappingNotice(station.Name, notice, retrievedAt);
                }

                if (station.Status == OsthessenWetterStationStatus.Offline)
                {
                    failures.Add(Failure(station.Name, OsthessenWetterNoticeIds.StationOffline, "The page marks the station offline (contract OW-12).", retrievedAt));
                    continue;
                }

                if (station.Location is null)
                {
                    failures.Add(Failure(station.Name, OsthessenWetterNoticeIds.StationMetadataMissing, "No station metadata is configured for the station (contract OW-04).", retrievedAt));
                    continue;
                }

                ObservationBuildResult built;
                try
                {
                    built = _builder.Build(new ObservationInput { Provider = Provider, Location = station.Location, RetrievedAtUtc = retrievedAt, Readings = station.Readings });
                }
                catch (ObservationDatasetRejectedException exception)
                {
                    foreach (var omitted in exception.OmittedReadings)
                    {
                        LogOmission(station.Name, omitted, retrievedAt);
                    }

                    Checkpoint(cancellationToken, deadline);
                    failures.Add(Failure(station.Name, OsthessenWetterNoticeIds.StationNoReading, "The station yields no usable reading (contract OW-08, C-60).", retrievedAt));
                    continue;
                }

                Checkpoint(cancellationToken, deadline);
                foreach (var omitted in built.OmittedReadings)
                {
                    LogOmission(station.Name, omitted, retrievedAt);
                }

                datasets.Add(built.Dataset);
            }

            Checkpoint(cancellationToken, deadline);
            if (datasets.Count == 0)
            {
                throw new OsthessenWetterException(OsthessenWetterException.NoUsableStationErrorId, "No matched station yields a dataset (contract OW-08).", failures);
            }

            return new OsthessenWetterParseResult(datasets, failures);
        }
        catch (Exception exception) when (Reclassify(exception, cancellationToken, deadline))
        {
            throw Interruption(cancellationToken, deadline)!;
        }
    }

    /// <summary>
    /// True when a failure must be reported as caller cancellation or deadline expiry instead (contract OW-10: caller cancellation, then the page deadline, then every other failure).
    /// </summary>
    private static bool Reclassify(Exception exception, CancellationToken cancellationToken, CancellationTokenSource deadline)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return exception is not OperationCanceledException canceled || canceled.CancellationToken != cancellationToken;
        }

        return deadline.IsCancellationRequested && !(exception is OsthessenWetterException { ErrorId: OsthessenWetterException.DeadlineExceededErrorId });
    }

    /// <summary>
    /// The failure that wins when the caller cancelled (first) or the page deadline expired (contract OW-10); null when neither happened.
    /// </summary>
    private static Exception? Interruption(CancellationToken cancellationToken, CancellationTokenSource deadline)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new OperationCanceledException(cancellationToken);
        }

        return deadline.IsCancellationRequested
            ? new OsthessenWetterException(OsthessenWetterException.DeadlineExceededErrorId, "The OsthessenWetter parse exceeded its page deadline (contract OW-10).")
            : null;
    }

    /// <summary>
    /// Throws when the caller cancelled (which wins) or the page deadline expired (contract OW-10).
    /// </summary>
    private static void Checkpoint(CancellationToken cancellationToken, CancellationTokenSource deadline)
    {
        var interruption = Interruption(cancellationToken, deadline);
        if (interruption is not null)
        {
            throw interruption;
        }
    }

    private OsthessenWetterStationFailure Failure(string stationName, string errorId, string reason, DateTime retrievedAt)
    {
        _logger.LogWarning(new EventId(0, errorId), "{ErrorId}: {Reason} Station {StationName}, source {Source}. Retrieved at {RetrievedAtUtc} UTC.", errorId, reason, stationName, Provider, Format(retrievedAt));
        return new OsthessenWetterStationFailure(stationName, errorId, reason);
    }

    private void LogPageNotice(OsthessenWetterPageDiagnostic diagnostic, DateTime retrievedAt) =>
        _logger.LogWarning(new EventId(0, diagnostic.NoticeId), "{NoticeId}: page notice (contract OW-05, OW-11). Station {StationName}, source {Source}. Retrieved at {RetrievedAtUtc} UTC.", diagnostic.NoticeId, diagnostic.StationName ?? "(all stations)", Provider, Format(retrievedAt));

    private void LogMappingNotice(string stationName, OsthessenWetterMappingNotice notice, DateTime retrievedAt) =>
        _logger.LogWarning(new EventId(0, notice.NoticeId), "{NoticeId}: the reading {Kind} of station {StationName} was not exported (contract OW-07, OW-11). Source {Source}. Retrieved at {RetrievedAtUtc} UTC.", notice.NoticeId, notice.Kind, stationName, Provider, Format(retrievedAt));

    private void LogOmission(string stationName, OmittedReading omitted, DateTime retrievedAt) =>
        _logger.LogWarning(new EventId(0, DwdObservationNoticeIds.ReadingOmitted), "{NoticeId}: the reading {Kind} of station {StationName} was omitted ({Reason}) (contract OC-10, C-59). Source {Source}. Retrieved at {RetrievedAtUtc} UTC.", DwdObservationNoticeIds.ReadingOmitted, omitted.Kind, stationName, omitted.Reason.ToString(), Provider, Format(retrievedAt));

    private static string Format(DateTime utc) => utc.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
}
