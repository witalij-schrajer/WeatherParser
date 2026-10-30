namespace WeatherParser.Cli.Commands;

using Microsoft.Extensions.Logging;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// Runs --ParseDWD for the resolved station: one observation dataset or one item failure (CL-09).
/// </summary>
public sealed class ParseDwdHandler : ICommandHandler<ParseDwdRequest>
{
    private readonly IDwdObservationParser _parser;
    private readonly IResultWriter _writer;
    private readonly IExportErrorMessages _messages;
    private readonly ILibraryErrors _libraryErrors;
    private readonly IFailureClassifier _classifier;
    private readonly ILogger<ParseDwdHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseDwdHandler"/> class.
    /// </summary>
    /// <param name="parser">The DWD observation parser.</param>
    /// <param name="writer">The result writer.</param>
    /// <param name="messages">The export error messages.</param>
    /// <param name="libraryErrors">The library errors.</param>
    /// <param name="classifier">The failure classifier.</param>
    /// <param name="logger">The logger.</param>
    public ParseDwdHandler(
        IDwdObservationParser parser,
        IResultWriter writer,
        IExportErrorMessages messages,
        ILibraryErrors libraryErrors,
        IFailureClassifier classifier,
        ILogger<ParseDwdHandler> logger)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        _libraryErrors = libraryErrors ?? throw new ArgumentNullException(nameof(libraryErrors));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CommandOutcome> HandleAsync(ParseDwdRequest request, CommandContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var stationId = context.StationId ?? throw new InvalidOperationException("The DWD station is not resolved.");
        var source = "dwd:" + stationId;
        ObservationDataset dataset;
        try
        {
            dataset = await _parser.ParseAsync(stationId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsItemFailure(exception, out var errorId))
        {
            var error = new ExportError { ErrorId = errorId, Message = _messages.For(errorId), Source = source };
            _logger.LogError(new EventId(0, errorId), "{Source} failed: {Message}", source, error.Message);
            if (context.Options.Output != OutputFormat.Mqtt)
            {
                await _writer.WriteAsync(new ExportDocument { Errors = { error } }, context, cancellationToken).ConfigureAwait(false);
            }

            return new CommandOutcome(0, 1, false);
        }

        if (context.Options.Output == OutputFormat.Mqtt)
        {
            await _writer.WriteAsync(dataset, context, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _writer.WriteAsync(new ExportDocument { Observations = { dataset } }, context, cancellationToken).ConfigureAwait(false);
        }

        return new CommandOutcome(1, 0, false);
    }

    // CL-09 lists the exception types that are item failures of --ParseDWD; a pre-request ID such as STATION-ID-INVALID stays command-level.
    private bool IsItemFailure(Exception exception, out string errorId)
    {
        errorId = string.Empty;
        return exception is DwdStationException or CatalogRetrievalException or CatalogFormatException or ObservationLimitException
            && _libraryErrors.TryGetErrorId(exception, out errorId)
            && _classifier.Classify(CliCommand.ParseDwd, errorId) == FailureKind.Item;
    }
}
