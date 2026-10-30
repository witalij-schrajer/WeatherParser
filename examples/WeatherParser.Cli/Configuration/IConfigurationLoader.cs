using System;

namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Provides functionality for loading CLI configuration.
/// </summary>
/// <remarks>
/// CL-06: Credentials handling.
/// </remarks>
public interface IConfigurationLoader
{
    /// <summary>
    /// Loads the one configuration file of the invocation: --Config, otherwise WEATHERPARSER_CONFIG, never an implicit path (CL-04);
    /// throws CliException with CLI-CONFIG-MISSING or CLI-CONFIG-INVALID.
    /// </summary>
    /// <param name="configPath">The path to the configuration file, or null to use the default.</param>
    /// <returns>The loaded configuration.</returns>
    CliConfiguration Load(string? configPath);
}
