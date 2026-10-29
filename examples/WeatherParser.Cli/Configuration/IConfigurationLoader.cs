using System;

namespace WeatherParser.Cli.Configuration;

public interface IConfigurationLoader
{
    /// <summary>
    /// Loads the one configuration file of the invocation: --Config, otherwise WEATHERPARSER_CONFIG, never an implicit path (CL-04);
    /// throws CliException with CLI-CONFIG-MISSING or CLI-CONFIG-INVALID.
    /// </summary>
    CliConfiguration Load(string? configPath);
}
