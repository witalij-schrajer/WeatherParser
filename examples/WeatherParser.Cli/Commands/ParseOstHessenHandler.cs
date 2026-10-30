namespace WeatherParser.Cli.Commands;

using Microsoft.Extensions.Logging;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Runs --ParseOstHessen: one observation dataset per matched station and one item failure per failed station (CL-09).
/// </summary>
public sealed class ParseOstHessenHandler : ICommandHandler<ParseOstHessenRequest>
{
    private readonly IOsthessenWetterParser _parser;
    private readonly IOsthessenWetterStationIdentity _identity;
    private readonly IResultWriter _writer;
    private readonly IExportErrorMessages _messages;
    private readonly ILogger<ParseOstHessenHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseOstHessenHandler"/> class.
    /// </summary>
    /// <param name="parser">The OsthessenWetter parser.</param>
    /// <param name="identity">The station identity provider.</param>
    /// <param name="writer">The result writer.</param>
    /// <param name="messages">The export error messages.</param>
    /// <param name="logger">The logger.</param>
    public ParseOstHessenHandler(
        IOsthessenWetterParser parser,
        IOsthessenWetterStationIdentity identity,
        IResultWriter writer,
        IExportErrorMessages messages,
        ILogger<ParseOstHessenHandler> logger)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CommandOutcome> HandleAsync(ParseOstHessenRequest request, CommandContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        OsthessenWetterParseResult result;
        try
        {
            result = await _parser.ParseAsync(request.StationNamePattern, cancellationToken).ConfigureAwait(false);
        }
        catch (OsthessenWetterException exception) when (exception.ErrorId == OsthessenWetterException.NoUsableStationErrorId)
        {
            // Every matched station failed: an envelope with errors only (CL-09).
            result = new OsthessenWetterParseResult(Array.Empty<ObservationDataset>(), exception.StationFailures);
        }

        var document = new ExportDocument();
        document.Observations.AddRange(result.Datasets);
        var sources = new HashSet<string>(result.Datasets.Select(dataset => dataset.Source), StringComparer.Ordinal);
        foreach (var failure in result.StationFailures)
        {
            var message = _messages.For(failure.ErrorId);
            string source;
            try
            {
                source = "osthessenwetter:" + _identity.ToStationId(failure.StationName);
            }
            catch (OsthessenWetterException)
            {
                // CL-09: without a mappable station ID there is no source; the failure goes to stderr only and still counts as failed.
                _logger.LogError(new EventId(0, failure.ErrorId), "An OsthessenWetter station failed and has no mappable station ID: {Message}", message);
                continue;
            }

            _logger.LogError(new EventId(0, failure.ErrorId), "{Source} failed: {Message}", source, message);

            // C-85: a source occurs at most once in the document; a repeated source goes to stderr only.
            if (sources.Add(source))
            {
                document.Errors.Add(new ExportError { ErrorId = failure.ErrorId, Message = message, Source = source });
            }
        }

        if (context.Options.Output == OutputFormat.Mqtt)
        {
            foreach (var dataset in result.Datasets)
            {
                await _writer.WriteAsync(dataset, context, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            await _writer.WriteAsync(document, context, cancellationToken).ConfigureAwait(false);
        }

        return new CommandOutcome(result.Datasets.Count, result.StationFailures.Count, false);
    }
}
