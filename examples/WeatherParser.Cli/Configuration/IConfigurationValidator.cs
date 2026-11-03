using WeatherParser.Cli.Arguments;

namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Provides functionality for validating CLI configuration and command requests.
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Validates the provided services, configuration, and command request.
    /// </summary>
    /// <param name="services">The service provider to validate against.</param>
    /// <param name="configuration">The CLI configuration to validate.</param>
    /// <param name="request">The command request to validate.</param>
    /// <returns>The resolved station and credentials.</returns>
    ValidatedSettings Validate(IServiceProvider services, CliConfiguration configuration, CommandRequest request);
}
