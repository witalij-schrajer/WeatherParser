using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Output;
using WeatherParser.Cli.Mqtt;
using WeatherParser.Cli.Lifetime;

namespace WeatherParser.Cli;

/// <summary>
/// Executes one validated command: configuration, the one validated container, the configuration-validation phase and the command handler (CL-04, CL-05, CL-07, CL-09).
/// </summary>
public sealed class CommandExecutor : ICommandExecutor
{
    // The CL-14 default of Limits.CommandDeadline.
    private static readonly TimeSpan DefaultCommandDeadline = TimeSpan.FromMinutes(10);

    private readonly IConfigurationLoader _loader;
    private readonly IConfigurationRegistrar _registrar;
    private readonly ICommandRegistrar _commandRegistrar;
    private readonly IConfigurationValidator _validator;
    private readonly IExitCodePolicy _exitCodePolicy;
    private readonly ILibraryErrors _libraryErrors;
    private readonly ILogLineFormatter _formatter;
    private readonly IShutdownSignal _shutdownSignal;
    private readonly ILibraryDeadlineResolver _deadlineResolver;
    private readonly TimeProvider _timeProvider;

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
    /// <param name="shutdownSignal">The shutdown signal whose token cancels the command (CL-18).</param>
    /// <param name="deadlineResolver">The resolver of the selected command's library deadline (CL-14).</param>
    /// <param name="timeProvider">The time provider of the command deadline (CL-14).</param>
    public CommandExecutor(IConfigurationLoader loader, IConfigurationRegistrar registrar, ICommandRegistrar commandRegistrar, IConfigurationValidator validator, IExitCodePolicy exitCodePolicy, ILibraryErrors libraryErrors, ILogLineFormatter formatter, IShutdownSignal shutdownSignal, ILibraryDeadlineResolver deadlineResolver, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(commandRegistrar);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(exitCodePolicy);
        ArgumentNullException.ThrowIfNull(libraryErrors);
        ArgumentNullException.ThrowIfNull(formatter);
        ArgumentNullException.ThrowIfNull(shutdownSignal);
        ArgumentNullException.ThrowIfNull(deadlineResolver);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _loader = loader;
        _registrar = registrar;
        _commandRegistrar = commandRegistrar;
        _validator = validator;
        _exitCodePolicy = exitCodePolicy;
        _libraryErrors = libraryErrors;
        _formatter = formatter;
        _shutdownSignal = shutdownSignal;
        _deadlineResolver = deadlineResolver;
        _timeProvider = timeProvider;
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

            var limits = configuration.Limits;
            _shutdownSignal.Configure(limits?.ShutdownGracePeriod ?? ShutdownSignal.DefaultGracePeriod, logger);
            var commandDeadline = limits?.CommandDeadline ?? DefaultCommandDeadline;
            var libraryDeadline = _deadlineResolver.Resolve(provider, request);
            if (commandDeadline < libraryDeadline)
            {
                // The library deadline is not shortened; the command deadline ends the work first (CL-14).
                logger.LogWarning(new EventId(0, CliErrorIds.DeadlineExceeded), "The command deadline {CommandDeadline} is shorter than the library deadline {LibraryDeadline} of the command; the command deadline ends the work first.", commandDeadline, libraryDeadline);
            }

            var context = new CommandContext(request.Common, settings.StationId, standardOutput);
            // One token for every library, output and transport call: the shutdown signal or the command deadline cancels it (CL-14, CL-18).
            using var deadline = new CancellationTokenSource(commandDeadline, _timeProvider);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_shutdownSignal.Token, deadline.Token);
            try
            {
                var outcome = RunAndPublishAsync(provider, request, context, configuration, settings.Credentials, linked.Token).GetAwaiter().GetResult();
                return _exitCodePolicy.ForOutcome(outcome);
            }
            catch (OperationCanceledException) when (_shutdownSignal.Token.IsCancellationRequested)
            {
                // A signal wins over the deadline; an interrupted export or publication is never reported as successful (CL-11, CL-18, REQ-007).
                logger.LogError(new EventId(0, CliErrorIds.Cancelled), "The command was cancelled by a shutdown signal; its output is incomplete and not reported as successful.");
                return _exitCodePolicy.ForRuntimeError(CliErrorIds.Cancelled);
            }
            catch (OperationCanceledException exception) when (deadline.IsCancellationRequested)
            {
                // Messages acknowledged before the deadline make it a partial failure (CL-11, CL-14).
                var published = (exception as PublicationCancelledException)?.Report.Published ?? 0;
                logger.LogError(new EventId(0, CliErrorIds.DeadlineExceeded), "The command did not finish within the command deadline {CommandDeadline}; the remaining work was cancelled.", commandDeadline);
                return _exitCodePolicy.ForOutcome(new CommandOutcome(published, 1, false));
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
