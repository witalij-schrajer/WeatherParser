using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;

namespace WeatherParser.Cli;

/// <summary>
/// Executes one validated command: configuration, the one validated container, the configuration-validation phase and the command (CL-04, CL-05, CL-07).
/// </summary>
public sealed class CommandExecutor : ICommandExecutor
{
    private readonly IConfigurationLoader _loader;
    private readonly IConfigurationRegistrar _registrar;
    private readonly IConfigurationValidator _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecutor"/> class.
    /// </summary>
    /// <param name="loader">The configuration loader.</param>
    /// <param name="registrar">The configuration registrar.</param>
    /// <param name="validator">The configuration validator.</param>
    public CommandExecutor(IConfigurationLoader loader, IConfigurationRegistrar registrar, IConfigurationValidator validator)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(validator);

        _loader = loader;
        _registrar = registrar;
        _validator = validator;
    }

    /// <inheritdoc/>
    public int Execute(CommandRequest request, TextWriter standardOutput, TextWriter standardError)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);

        try
        {
            var configuration = _loader.Load(request.Common.ConfigPath);
            using var provider = CompositionRoot.Build(services => _registrar.Register(services, configuration, request));
            _validator.Validate(provider, configuration, request);
            standardError.WriteLine("The command is not implemented yet.");
            return 1;
        }
        catch (CliException exception)
        {
            standardError.WriteLine($"{exception.ErrorId}: {exception.Message}");
            return 1;
        }
    }
}
