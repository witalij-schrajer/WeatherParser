using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Diagnostics;

namespace WeatherParser.Cli;

/// <summary>
/// Executes one validated command: configuration, the one validated container, the configuration-validation phase and the command (CL-04, CL-05, CL-07).
/// </summary>
public sealed class CommandExecutor : ICommandExecutor
{
    private readonly IConfigurationLoader _loader;
    private readonly IConfigurationRegistrar _registrar;
    private readonly IConfigurationValidator _validator;
    private readonly IExitCodePolicy _exitCodePolicy;
    private readonly ILogLineFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecutor"/> class.
    /// </summary>
    /// <param name="loader">The configuration loader.</param>
    /// <param name="registrar">The configuration registrar.</param>
    /// <param name="validator">The configuration validator.</param>
    /// <param name="exitCodePolicy">The exit code policy.</param>
    /// <param name="formatter">The log line formatter.</param>
    public CommandExecutor(IConfigurationLoader loader, IConfigurationRegistrar registrar, IConfigurationValidator validator, IExitCodePolicy exitCodePolicy, ILogLineFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(exitCodePolicy);
        ArgumentNullException.ThrowIfNull(formatter);

        _loader = loader;
        _registrar = registrar;
        _validator = validator;
        _exitCodePolicy = exitCodePolicy;
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
                services.AddLogging(builder => builder.ClearProviders().AddProvider(loggerProvider).SetMinimumLevel(level));
            });
            _validator.Validate(provider, configuration, request);
            logger.LogError("The command is not implemented yet.");
            return ExitCodes.Failure;
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
}
