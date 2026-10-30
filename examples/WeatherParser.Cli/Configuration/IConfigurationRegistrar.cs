using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Cli.Arguments;

namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Provides functionality for registering services based on CLI configuration and command request.
/// </summary>
public interface IConfigurationRegistrar
{
    /// <summary>
    /// Registers services into the service collection based on the provided configuration and command request.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="configuration">The CLI configuration to use for registration.</param>
    /// <param name="request">The command request to use for registration.</param>
    void Register(IServiceCollection services, CliConfiguration configuration, CommandRequest request);
}
