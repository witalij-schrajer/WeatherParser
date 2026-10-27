using System;
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
    private readonly MosmixParseRequest _request;
    private readonly IMosmixParsePipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixLParser"/> class.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="source">The forecast source.</param>
    /// <param name="reader">The document reader.</param>
    /// <param name="pipeline">The parse pipeline.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="MosmixForecastException">MOSMIX-OPTIONS-INVALID when the parse deadline is not greater than zero or exceeds one hour, or when MaxRunAge is not set or not in (0, 240 h].</exception>
    public MosmixLParser(MosmixLForecastOptions options, IMosmixLForecastSource source, IMosmixForecastDocumentReader reader, IMosmixParsePipeline pipeline, ILogger<MosmixLParser> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(logger);

        // The deadline is validated and kept at construction, so a later change of the options object cannot bypass the bounds (contract MF-05).
        var parseDeadline = options.ParseDeadline;
        if (parseDeadline <= TimeSpan.Zero || parseDeadline > TimeSpan.FromHours(1))
        {
            throw new MosmixForecastException(MosmixForecastErrorIds.OptionsInvalid, MosmixProduct.MosmixL, "MosmixLForecastOptions.ParseDeadline must be greater than zero and at most one hour (contract MF-05).");
        }

        // The max run age is validated and kept at construction, so a later change of the options object cannot bypass the bounds (contract MF-09).
        if (options.MaxRunAge is not { } maxRunAge || maxRunAge <= TimeSpan.Zero || maxRunAge > TimeSpan.FromHours(240))
        {
            throw new MosmixForecastException(MosmixForecastErrorIds.OptionsInvalid, MosmixProduct.MosmixL, "MosmixLForecastOptions.MaxRunAge must be set, greater than zero and at most 240 hours (contract MF-09).");
        }

        _request = new MosmixParseRequest
        {
            Product = MosmixProduct.MosmixL,
            ParseDeadline = parseDeadline,
            MaxRunAge = maxRunAge,
            SanitizedEndpoint = source.SanitizedEndpoint,
            Retrieve = (id, read, deadline, ct) => source.RetrieveAsync(id, read, deadline, ct),
            Read = (stream, id, token) => reader.ReadSingleLocationAsync(stream, id, token),
            Logger = logger
        };

        _pipeline = pipeline;
    }

    /// <inheritdoc />
    public Task<MosmixParseResult> ParseAsync(string mosmixId, CancellationToken cancellationToken)
        => _pipeline.ParseAsync(_request, mosmixId, cancellationToken);
}
