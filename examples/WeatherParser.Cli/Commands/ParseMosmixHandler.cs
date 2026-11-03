namespace WeatherParser.Cli.Commands;

using Microsoft.Extensions.Logging;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Runs --ParseMOSMIX with the parser of the requested product, never the other one (CL-09, MF-01).
/// </summary>
public sealed class ParseMosmixHandler : ICommandHandler<ParseMosmixRequest>
{
    private readonly IMosmixLParser? _mosmixLParser;
    private readonly IMosmixSParser? _mosmixSParser;
    private readonly IResultWriter _writer;
    private readonly IExportErrorMessages _messages;
    private readonly ILibraryErrors _libraryErrors;
    private readonly IFailureClassifier _classifier;
    private readonly ILogger<ParseMosmixHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseMosmixHandler"/> class.
    /// </summary>
    /// <param name="mosmixLParser">The MOSMIX_L parser. Registered only for --Product MOSMIX_L (MOSMIX_S); otherwise null.</param>
    /// <param name="mosmixSParser">The MOSMIX_S parser. Registered only for --Product MOSMIX_S (MOSMIX_L); otherwise null.</param>
    /// <param name="writer">The result writer.</param>
    /// <param name="messages">The export error messages.</param>
    /// <param name="libraryErrors">The library errors.</param>
    /// <param name="classifier">The failure classifier.</param>
    /// <param name="logger">The logger.</param>
    public ParseMosmixHandler(
        IMosmixLParser? mosmixLParser,
        IMosmixSParser? mosmixSParser,
        IResultWriter writer,
        IExportErrorMessages messages,
        ILibraryErrors libraryErrors,
        IFailureClassifier classifier,
        ILogger<ParseMosmixHandler> logger)
    {
        _mosmixLParser = mosmixLParser;
        _mosmixSParser = mosmixSParser;
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        _libraryErrors = libraryErrors ?? throw new ArgumentNullException(nameof(libraryErrors));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CommandOutcome> HandleAsync(ParseMosmixRequest request, CommandContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var source = (request.Product == MosmixProduct.MosmixS ? "dwd-mosmix-s:" : "dwd-mosmix-l:") + request.MosmixId;
        ForecastRun run;
        try
        {
            var result = request.Product == MosmixProduct.MosmixS
                ? await (_mosmixSParser ?? throw NotRegistered("MOSMIX_S")).ParseAsync(request.MosmixId, cancellationToken).ConfigureAwait(false)
                : await (_mosmixLParser ?? throw NotRegistered("MOSMIX_L")).ParseAsync(request.MosmixId, cancellationToken).ConfigureAwait(false);
            run = result.Run;
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
            await _writer.WriteAsync(run, context, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _writer.WriteAsync(new ExportDocument { ForecastRuns = { run } }, context, cancellationToken).ConfigureAwait(false);
        }

        return new CommandOutcome(1, 0, false);
    }

    // CL-09 lists the exception types that are item failures of --ParseMOSMIX; a pre-request ID such as MOSMIX-ID-INVALID stays command-level.
    private bool IsItemFailure(Exception exception, out string errorId)
    {
        errorId = string.Empty;
        return exception is MosmixForecastException or MosmixForecastFormatException
            && _libraryErrors.TryGetErrorId(exception, out errorId)
            && _classifier.Classify(CliCommand.ParseMosmix, errorId) == FailureKind.Item;
    }

    private static InvalidOperationException NotRegistered(string product) => new($"The {product} parser is not registered although --Product {product} was given; the other product is never used instead.");
}
