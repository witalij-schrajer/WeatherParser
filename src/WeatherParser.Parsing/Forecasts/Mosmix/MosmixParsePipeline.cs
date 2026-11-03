using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Stateless shared parse flow of both MOSMIX parsers (validation, deadline, retrieval, sampling, freshness, mapping, building, notices, omissions, precedence, logging); one registered singleton.
/// </summary>
public sealed class MosmixParsePipeline : IMosmixParsePipeline
{
    private readonly IMosmixElementMapper _mapper;
    private readonly IForecastRunBuilder _builder;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixParsePipeline"/> class.
    /// </summary>
    /// <param name="mapper">The element mapper.</param>
    /// <param name="builder">The forecast run builder.</param>
    /// <param name="timeProvider">The time provider.</param>
    public MosmixParsePipeline(IMosmixElementMapper mapper, IForecastRunBuilder builder, TimeProvider timeProvider)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<MosmixParseResult> ParseAsync(MosmixParseRequest request, string mosmixId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(mosmixId);

        var label = request.Product == MosmixProduct.MosmixS ? "MOSMIX_S" : "MOSMIX_L";

        if (!IsValidMosmixId(mosmixId))
        {
            var utc = _timeProvider.GetUtcNow().UtcDateTime;
            request.Logger.LogError(new EventId(0, MosmixForecastErrorIds.IdInvalid), "{ErrorId}: the {Product} parse was rejected because the MOSMIX ID is invalid (contract MF-03). Endpoint {Endpoint}. At {Utc} UTC.", MosmixForecastErrorIds.IdInvalid, label, request.SanitizedEndpoint, Format(utc));
            throw new MosmixForecastException(MosmixForecastErrorIds.IdInvalid, request.Product, "The MOSMIX ID must consist of 4 or 5 characters A-Z or 0-9 (contract MF-03).");
        }

        using var deadline = new CancellationTokenSource(request.ParseDeadline, _timeProvider);

        try
        {
            var document = await request.Retrieve(mosmixId, (stream, token) => request.Read(stream, mosmixId, token), deadline.Token, cancellationToken);

            // Contract MF-13: sampled once after the file has been retrieved and read (OW-16 pattern); truncated to whole seconds.
            var reference = _timeProvider.GetUtcNow().UtcDateTime;
            var retrievedAt = new DateTime(reference.Ticks - (reference.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
            Checkpoint(cancellationToken, deadline, request.Product, request.SanitizedEndpoint, request.Logger);

            // Run age = retrieved_at - document.IssuedAtUtc; stale when the age is above MaxRunAge (at the limit Fresh; a negative age is Fresh, returned unchanged).
            var runAge = retrievedAt - document.IssuedAtUtc;
            var freshness = runAge > request.MaxRunAge ? MosmixRunFreshness.Stale : MosmixRunFreshness.Fresh;

            var mapping = _mapper.Map(document, request.Product, retrievedAt, freshness == MosmixRunFreshness.Stale);
            Checkpoint(cancellationToken, deadline, request.Product, request.SanitizedEndpoint, request.Logger);

            if (freshness == MosmixRunFreshness.Stale)
            {
                request.Logger.LogWarning(new EventId(0, MosmixNoticeIds.RunStale), "{NoticeId}: the {Product} run of location {MosmixId} issued at {IssuedAtUtc} UTC is {RunAge} old, above the limit {MaxRunAge} (contract MF-09). Endpoint {Endpoint}. Retrieved at {RetrievedAtUtc} UTC.", MosmixNoticeIds.RunStale, label, mosmixId, Format(document.IssuedAtUtc), runAge.ToString("c", CultureInfo.InvariantCulture), request.MaxRunAge.ToString("c", CultureInfo.InvariantCulture), request.SanitizedEndpoint, Format(retrievedAt));
            }

            foreach (var element in mapping.AbsentElements)
            {
                request.Logger.LogWarning(new EventId(0, MosmixNoticeIds.ElementAbsent), "{NoticeId}: the element {ElementName} is absent from the {Product} run of location {MosmixId} (contract MF-12). Endpoint {Endpoint}. Retrieved at {RetrievedAtUtc} UTC.", MosmixNoticeIds.ElementAbsent, element, label, mosmixId, request.SanitizedEndpoint, Format(retrievedAt));
            }

            foreach (var element in mapping.NotExportedElements)
            {
                request.Logger.LogWarning(new EventId(0, MosmixNoticeIds.ElementNotExported), "{NoticeId}: the element {ElementName} of the {Product} run of location {MosmixId} is not mapped and not exported (contract MF-02). Endpoint {Endpoint}. Retrieved at {RetrievedAtUtc} UTC.", MosmixNoticeIds.ElementNotExported, element, label, mosmixId, request.SanitizedEndpoint, Format(retrievedAt));
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
                    LogOmission(omitted, mosmixId, retrievedAt, request.Product, request.SanitizedEndpoint, request.Logger);
                }

                Checkpoint(cancellationToken, deadline, request.Product, request.SanitizedEndpoint, request.Logger);
                throw new MosmixForecastException(MosmixForecastErrorIds.NoUsableValue, request.Product, $"The {label} run has no usable value (contracts MF-12, C-72).", rejection.OmittedValues, mapping.NotExportedElements);
            }

            Checkpoint(cancellationToken, deadline, request.Product, request.SanitizedEndpoint, request.Logger);
            foreach (var omitted in built.OmittedValues)
            {
                LogOmission(omitted, mosmixId, retrievedAt, request.Product, request.SanitizedEndpoint, request.Logger);
            }

            // Contract MF-14: the final omission warnings may have run a slow or interrupting logging provider; check once more before the successful return.
            var result = new MosmixParseResult(built.Run, built.OmittedValues, mapping.NotExportedElements, mapping.AbsentElements, freshness, runAge);
            Checkpoint(cancellationToken, deadline, request.Product, request.SanitizedEndpoint, request.Logger);
            return result;
        }
        catch (Exception exception) when (Reclassify(exception, cancellationToken, deadline))
        {
            var interruption = Interruption(cancellationToken, deadline, request.Product)!;
            if (interruption is not OperationCanceledException)
            {
                // Log the deadline expiry only once.
                LogFailure((MosmixForecastException)interruption, mosmixId, request.Product, request.SanitizedEndpoint, request.Logger);
            }

            throw interruption;
        }
        catch (MosmixForecastFormatException exception)
        {
            var failure = new MosmixForecastException(exception.ErrorId, request.Product, exception.Message);
            LogFailure(failure, mosmixId, request.Product, request.SanitizedEndpoint, request.Logger);
            throw failure;
        }
        catch (MosmixForecastException exception)
        {
            LogFailure(exception, mosmixId, request.Product, request.SanitizedEndpoint, request.Logger);
            throw;
        }
    }

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

    private static bool Reclassify(Exception exception, CancellationToken cancellationToken, CancellationTokenSource deadline)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return exception is not OperationCanceledException canceled || canceled.CancellationToken != cancellationToken;
        }

        return deadline.IsCancellationRequested && !(exception is MosmixForecastException { ErrorId: MosmixForecastErrorIds.DeadlineExceeded });
    }

    private static Exception? Interruption(CancellationToken cancellationToken, CancellationTokenSource deadline, MosmixProduct product)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new OperationCanceledException(cancellationToken);
        }

        var label = product == MosmixProduct.MosmixS ? "MOSMIX_S" : "MOSMIX_L";
        return deadline.IsCancellationRequested
            ? new MosmixForecastException(MosmixForecastErrorIds.DeadlineExceeded, product, $"The {label} parse exceeded its parse deadline (contract MF-05).")
            : null;
    }

    private static void Checkpoint(CancellationToken cancellationToken, CancellationTokenSource deadline, MosmixProduct product, string endpoint, ILogger logger)
    {
        var interruption = Interruption(cancellationToken, deadline, product);
        if (interruption is not null)
        {
            throw interruption;
        }
    }

    private void LogFailure(MosmixForecastException exception, string mosmixId, MosmixProduct product, string endpoint, ILogger logger)
    {
        var label = product == MosmixProduct.MosmixS ? "MOSMIX_S" : "MOSMIX_L";
        logger.LogError(new EventId(0, exception.ErrorId), "{ErrorId}: the {Product} parse of location {MosmixId} failed: {Reason} Endpoint {Endpoint}. At {Utc} UTC.", exception.ErrorId, label, mosmixId, exception.Message, endpoint, Format(_timeProvider.GetUtcNow().UtcDateTime));
    }

    private static void LogOmission(OmittedForecastValue omitted, string mosmixId, DateTime retrievedAt, MosmixProduct product, string endpoint, ILogger logger)
    {
        var label = product == MosmixProduct.MosmixS ? "MOSMIX_S" : "MOSMIX_L";
        logger.LogWarning(new EventId(0, MosmixNoticeIds.ValueOmitted), "{NoticeId}: the value {Kind} valid at {ValidAtUtc} UTC of the {Product} run of location {MosmixId} was omitted ({Reason}) (contracts MF-02, C-78). Endpoint {Endpoint}. Retrieved at {RetrievedAtUtc} UTC.", MosmixNoticeIds.ValueOmitted, omitted.Kind, Format(omitted.ValidAtUtc), label, mosmixId, omitted.Reason.ToString(), endpoint, Format(retrievedAt));
    }

    private static string Format(DateTime utc) => utc.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
}
