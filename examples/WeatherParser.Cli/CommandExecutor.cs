using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Output;
using WeatherParser.Cli.Mqtt;

namespace WeatherParser.Cli;

/// <summary>
/// Executes one validated command: configuration, the one validated container, the configuration-validation phase and the command handler (CL-04, CL-05, CL-07, CL-09).
/// </summary>
public sealed class CommandExecutor : ICommandExecutor
{
    private readonly IConfigurationLoader _loader;
    private readonly IConfigurationRegistrar _registrar;
    private readonly ICommandRegistrar _commandRegistrar;
    private readonly IConfigurationValidator _validator;
    private readonly IExitCodePolicy _exitCodePolicy;
    private readonly ILibraryErrors _libraryErrors;
    private readonly ILogLineFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecutor"/> class.
    /// </summary>
    /// <param name="loader">The configuration loader.</param>
    /// <param name="registrar">The configuration registrar.</param>
    /// <param name="commandRegistrar">The command registrar.</param>
    /// <param name="validator">The configuration validator.</param>
    /// <param name="exitCodePolicy">The exit code policy.</param>
    /// <param name="libraryErrors">The mapping of library exceptions to error IDs.</param>
    /// <param name="formatter">The log line formatter.</param>
    public CommandExecutor(IConfigurationLoader loader, IConfigurationRegistrar registrar, ICommandRegistrar commandRegistrar, IConfigurationValidator validator, IExitCodePolicy exitCodePolicy, ILibraryErrors libraryErrors, ILogLineFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(commandRegistrar);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(exitCodePolicy);
        ArgumentNullException.ThrowIfNull(libraryErrors);
        ArgumentNullException.ThrowIfNull(formatter);

        _loader = loader;
        _registrar = registrar;
        _commandRegistrar = commandRegistrar;
        _validator = validator;
        _exitCodePolicy = exitCodePolicy;
        _libraryErrors = libraryErrors;
        _formatter = formatter;
    }

    /// <inheritdoc/>
    public int Execute(CommandRequest request, TextWriter standardOutput, TextWriter standardError)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);

        var level = request.Common.Verbose ? LogLevel.Debug : LogLevel.Information;
        using var loggerProvider = new StandardErrorLoggerProvider(standardError, TimeProvider.System, _formatter, level, CommandName(request));
        var logger = loggerProvider.CreateLogger("WeatherParser.Cli");
        try
        {
            var configuration = _loader.Load(request.Common.ConfigPath);
            using var provider = CompositionRoot.Build(services =>
            {
                _registrar.Register(services, configuration, request);
                _commandRegistrar.Register(services, request);
                services.AddLogging(builder => builder.ClearProviders().AddProvider(loggerProvider).SetMinimumLevel(level));
            });
            var settings = _validator.Validate(provider, configuration, request);
            // The destination is checked after the validation phase and before any retrieval (CL-11, CL-12).
            if (request.Common.OutputFile is not null)
            {
                provider.GetRequiredService<IOutputDestination>().Check(request.Common);
            }
            var context = new CommandContext(request.Common, settings.StationId, standardOutput);
            try
            {
                var outcome = RunAndPublishAsync(provider, request, context, configuration, settings.Credentials, CancellationToken.None).GetAwaiter().GetResult();
                return _exitCodePolicy.ForOutcome(outcome);
            }
            catch (CliException exception)
            {
                // A CLI failure while the command runs is a run-time failure (CL-11).
                logger.LogError(new EventId(0, exception.ErrorId), "{Message}", exception.Message);
                return _exitCodePolicy.ForRuntimeError(exception.ErrorId);
            }
            catch (Exception exception) when (_libraryErrors.TryGetErrorId(exception, out var errorId))
            {
                // A command-level library failure: no document, the ID on stderr; a pre-request ID keeps the usage code (CL-09, CL-11).
                logger.LogError(new EventId(0, errorId), "{Message}", exception.Message);
                return _exitCodePolicy.ForError(errorId);
            }
        }
        catch (CliException exception)
        {
            logger.LogError(new EventId(0, exception.ErrorId), "{Message}", exception.Message);
            return _exitCodePolicy.ForError(exception.ErrorId);
        }
    }

    private static string CommandName(CommandRequest request) => request switch
    {
        SelectDwdRequest => "SelectDWD",
        SelectMosmixRequest => "SelectMOSMIX",
        ParseDwdRequest => "ParseDWD",
        ParseOstHessenRequest => "ParseOstHessen",
        _ => "ParseMOSMIX",
    };

    private static Task<CommandOutcome> RunAsync(IServiceProvider services, CommandRequest request, CommandContext context, CancellationToken cancellationToken) => request switch
    {
        SelectDwdRequest selectDwd => services.GetRequiredService<ICommandHandler<SelectDwdRequest>>().HandleAsync(selectDwd, context, cancellationToken),
        SelectMosmixRequest selectMosmix => services.GetRequiredService<ICommandHandler<SelectMosmixRequest>>().HandleAsync(selectMosmix, context, cancellationToken),
        ParseDwdRequest parseDwd => services.GetRequiredService<ICommandHandler<ParseDwdRequest>>().HandleAsync(parseDwd, context, cancellationToken),
        ParseOstHessenRequest parseOstHessen => services.GetRequiredService<ICommandHandler<ParseOstHessenRequest>>().HandleAsync(parseOstHessen, context, cancellationToken),
        _ => services.GetRequiredService<ICommandHandler<ParseMosmixRequest>>().HandleAsync((ParseMosmixRequest)request, context, cancellationToken),
    };

    // In MQTT mode the queued messages are published after the handler; an unpublished message turns a succeeded item into a failed one (CL-13, CL-15, REQ-069).
    private static async Task<CommandOutcome> RunAndPublishAsync(IServiceProvider services, CommandRequest request, CommandContext context, CliConfiguration configuration, BrokerCredentials? credentials, CancellationToken cancellationToken)
    {
        var outcome = await RunAsync(services, request, context, cancellationToken).ConfigureAwait(false);
        if (request.Common.Output != OutputFormat.Mqtt)
        {
            return outcome;
        }

        var items = services.GetRequiredService<IPublicationQueue>().Items;
        var publication = services.GetRequiredService<IMqttPublicationSettingsFactory>().Create(configuration, credentials);
        var report = await services.GetRequiredService<IMessagePublisher>().PublishAllAsync(items, publication, cancellationToken).ConfigureAwait(false);
        var unpublished = report.Failed + report.NotAttempted;
        return new CommandOutcome(Math.Max(0, outcome.Succeeded - unpublished), outcome.Failed + unpublished, outcome.Cancelled);
    }
}
