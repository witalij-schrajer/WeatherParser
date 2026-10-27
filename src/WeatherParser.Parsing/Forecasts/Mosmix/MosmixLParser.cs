using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Default implementation of <see cref="IMosmixLParser"/>; one parse per call, no internal scheduling.
/// Every log entry names the product, the MOSMIX ID (except for an invalid ID), the sanitized endpoint and the UTC time (contract MF-15).
/// </summary>
public sealed class MosmixLParser : IMosmixLParser
{
    private readonly TimeSpan _parseDeadline;
    private readonly TimeSpan _maxRunAge;
    private readonly IMosmixLForecastSource _source;
    private readonly IMosmixForecastDocumentReader _reader;
    private readonly IMosmixElementMapper _mapper;
    private readonly IForecastRunBuilder _builder;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MosmixLParser> _logger;
    private readonly string _endpoint;

    private const string Product = "MOSMIX_L";

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixLParser"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="source">The forecast source.</param>
    /// <param name="reader">The document reader.</param>
    /// <param name="mapper">The element mapper.</param>
    /// <param name="builder">The forecast run builder.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="MosmixForecastException">MOSMIX-OPTIONS-INVALID when the parse deadline is not greater than zero or exceeds one hour, or when MaxRunAge is not set or not in (0, 240 h].</exception>
    public MosmixLParser(MosmixLForecastOptions options, IMosmixLForecastSource source, IMosmixForecastDocumentReader reader, IMosmixElementMapper mapper, IForecastRunBuilder builder, TimeProvider timeProvider, ILogger<MosmixLParser> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _endpoint = source.SanitizedEndpoint;

        // The deadline is validated and kept at construction, so a later change of the options object cannot bypass the bounds (contract MF-05).
        var parseDeadline = options.ParseDeadline;
        if (parseDeadline <= TimeSpan.Zero || parseDeadline > TimeSpan.FromHours(1))
        {
            throw new MosmixForecastException(MosmixForecastErrorIds.OptionsInvalid, MosmixProduct.MosmixL, "MosmixLForecastOptions.ParseDeadline must be greater than zero and at most one hour (contract MF-05).");
        }

        _parseDeadline = parseDeadline;

        // The max run age is validated and kept at construction, so a later change of the options object cannot bypass the bounds (contract MF-09).
        if (options.MaxRunAge is not { } maxRunAge || maxRunAge <= TimeSpan.Zero || maxRunAge > TimeSpan.FromHours(240))
        {
            throw new MosmixForecastException(MosmixForecastErrorIds.OptionsInvalid, MosmixProduct.MosmixL, "MosmixLForecastOptions.MaxRunAge must be set, greater than zero and at most 240 hours (contract MF-09).");
        }

        _maxRunAge = maxRunAge;
    }

    /// <inheritdoc />
    public async Task<MosmixParseResult> ParseAsync(string mosmixId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mosmixId);

        if (!IsValidMosmixId(mosmixId))
        {
            var utc = _timeProvider.GetUtcNow().UtcDateTime;
            _logger.LogError(new EventId(0, MosmixForecastErrorIds.IdInvalid), "{ErrorId}: the {Product} parse was rejected because the MOSMIX ID is invalid (contract MF-03). Endpoint {Endpoint}. At {Utc} UTC.", MosmixForecastErrorIds.IdInvalid, Product, _endpoint, Format(utc));
            throw new MosmixForecastException(MosmixForecastErrorIds.IdInvalid, MosmixProduct.MosmixL, "The MOSMIX ID must consist of 4 or 5 characters A-Z or 0-9 (contract MF-03).");
        }

        using var deadline = new CancellationTokenSource(_parseDeadline, _timeProvider);

        try
        {
            var document = await _source.RetrieveAsync<MosmixForecastDocument>(mosmixId, (stream, token) => _reader.ReadSingleLocationAsync(stream, mosmixId, token), deadline.Token, cancellationToken);

            // Contract MF-13: sampled once after the file has been retrieved and read (OW-16 pattern); truncated to whole seconds.
            var reference = _timeProvider.GetUtcNow().UtcDateTime;
            var retrievedAt = new DateTime(reference.Ticks - (reference.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
            Checkpoint(cancellationToken, deadline);

            // Run age = retrieved_at - document.IssuedAtUtc; stale when the age is above MaxRunAge (at the limit Fresh; a negative age is Fresh, returned unchanged).
            var runAge = retrievedAt - document.IssuedAtUtc;
            var freshness = runAge > _maxRunAge ? MosmixRunFreshness.Stale : MosmixRunFreshness.Fresh;

            var mapping = _mapper.Map(document, MosmixProduct.MosmixL, retrievedAt, freshness == MosmixRunFreshness.Stale);
            Checkpoint(cancellationToken, deadline);

            if (freshness == MosmixRunFreshness.Stale)
            {
                _logger.LogWarning(new EventId(0, MosmixNoticeIds.RunStale), "{NoticeId}: the {Product} run of location {MosmixId} issued at {IssuedAtUtc} UTC is {RunAge} old, above the limit {MaxRunAge} (contract MF-09). Endpoint {Endpoint}. Retrieved at {RetrievedAtUtc} UTC.", MosmixNoticeIds.RunStale, Product, mosmixId, Format(document.IssuedAtUtc), runAge.ToString("c", CultureInfo.InvariantCulture), _maxRunAge.ToString("c", CultureInfo.InvariantCulture), _endpoint, Format(retrievedAt));
            }

            foreach (var element in mapping.AbsentElements)
            {
                _logger.LogWarning(new EventId(0, MosmixNoticeIds.ElementAbsent), "{NoticeId}: the element {ElementName} is absent from the {Product} run of location {MosmixId} (contract MF-12). Endpoint {Endpoint}. Retrieved at {RetrievedAtUtc} UTC.", MosmixNoticeIds.ElementAbsent, element, Product, mosmixId, _endpoint, Format(retrievedAt));
            }

            foreach (var element in mapping.NotExportedElements)
            {
                _logger.LogWarning(new EventId(0, MosmixNoticeIds.ElementNotExported), "{NoticeId}: the element {ElementName} of the {Product} run of location {MosmixId} is not mapped and not exported (contract MF-02). Endpoint {Endpoint}. Retrieved at {RetrievedAtUtc} UTC.", MosmixNoticeIds.ElementNotExported, element, Product, mosmixId, _endpoint, Format(retrievedAt));
            }

            ForecastRunBuildResult built;
            try
            {
                built = _builder.Build(mapping.Input);
            }
            catch (ForecastRunRejectedException rejection)
            {
                foreach (var omitted in rejection.OmittedValues)
                {
                    LogOmission(omitted, mosmixId, retrievedAt);
                }

                Checkpoint(cancellationToken, deadline);
                throw new MosmixForecastException(MosmixForecastErrorIds.NoUsableValue, MosmixProduct.MosmixL, "The MOSMIX_L run has no usable value (contracts MF-12, C-72).", rejection.OmittedValues, mapping.NotExportedElements);
            }

            Checkpoint(cancellationToken, deadline);
            foreach (var omitted in built.OmittedValues)
            {
                LogOmission(omitted, mosmixId, retrievedAt);
            }

            return new MosmixParseResult(built.Run, built.OmittedValues, mapping.NotExportedElements, mapping.AbsentElements, freshness, runAge);
        }
        catch (Exception exception) when (Reclassify(exception, cancellationToken, deadline))
        {
            var interruption = Interruption(cancellationToken, deadline)!;
            if (interruption is not OperationCanceledException)
            {
                // Log the deadline expiry only once.
                LogFailure((MosmixForecastException)interruption, mosmixId);
            }

            throw interruption;
        }
        catch (MosmixForecastFormatException exception)
        {
            var failure = new MosmixForecastException(exception.ErrorId, MosmixProduct.MosmixL, exception.Message);
            LogFailure(failure, mosmixId);
            throw failure;
        }
        catch (MosmixForecastException exception)
        {
            LogFailure(exception, mosmixId);
            throw;
        }
    }

    /// <summary>
    /// Validates the MOSMIX ID.
    /// </summary>
    private static bool IsValidMosmixId(string id)
    {
        if (id is null || id.Length is not (4 or 5))
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

    /// <summary>
    /// True when a failure must be reported as caller cancellation or deadline expiry instead (contract MF-14: caller cancellation, then the parse deadline, then every other failure).
    /// </summary>
    private static bool Reclassify(Exception exception, CancellationToken cancellationToken, CancellationTokenSource deadline)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return exception is not OperationCanceledException canceled || canceled.CancellationToken != cancellationToken;
        }

        return deadline.IsCancellationRequested && !(exception is MosmixForecastException { ErrorId: MosmixForecastErrorIds.DeadlineExceeded });
    }

    /// <summary>
    /// The failure that wins when the caller cancelled (first) or the parse deadline expired (contract MF-14); null when neither happened.
    /// </summary>
    private static Exception? Interruption(CancellationToken cancellationToken, CancellationTokenSource deadline)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new OperationCanceledException(cancellationToken);
        }

        return deadline.IsCancellationRequested
            ? new MosmixForecastException(MosmixForecastErrorIds.DeadlineExceeded, MosmixProduct.MosmixL, "The MOSMIX_L parse exceeded its parse deadline (contract MF-05).")
            : null;
    }

    /// <summary>
    /// Throws when the caller cancelled (which wins) or the parse deadline expired (contract MF-14).
    /// </summary>
    private static void Checkpoint(CancellationToken cancellationToken, CancellationTokenSource deadline)
    {
        var interruption = Interruption(cancellationToken, deadline);
        if (interruption is not null)
        {
            throw interruption;
        }
    }

    private void LogFailure(MosmixForecastException exception, string mosmixId)
    {
        _logger.LogError(new EventId(0, exception.ErrorId), "{ErrorId}: the {Product} parse of location {MosmixId} failed: {Reason} Endpoint {Endpoint}. At {Utc} UTC.", exception.ErrorId, Product, mosmixId, exception.Message, _endpoint, Format(_timeProvider.GetUtcNow().UtcDateTime));
    }

    private void LogOmission(OmittedForecastValue omitted, string mosmixId, DateTime retrievedAt)
    {
        _logger.LogWarning(new EventId(0, MosmixNoticeIds.ValueOmitted), "{NoticeId}: the value {Kind} valid at {ValidAtUtc} UTC of the {Product} run of location {MosmixId} was omitted ({Reason}) (contracts MF-02, C-78). Endpoint {Endpoint}. Retrieved at {RetrievedAtUtc} UTC.", MosmixNoticeIds.ValueOmitted, omitted.Kind, Format(omitted.ValidAtUtc), Product, mosmixId, omitted.Reason.ToString(), _endpoint, Format(retrievedAt));
    }

    private static string Format(DateTime utc) => utc.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
}
