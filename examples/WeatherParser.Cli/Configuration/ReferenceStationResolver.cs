namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Resolves the reference station ID based on command-line arguments and configuration.
/// </summary>
public sealed class ReferenceStationResolver : IReferenceStationResolver
{
    /// <summary>
    /// Resolves the reference station ID based on the provided arguments and configuration.
    /// </summary>
    /// <param name="stationIdArgument">The station ID argument, or null if the option was omitted; a supplied value is returned unchanged.</param>
    /// <param name="configuration">The CLI configuration to use for resolution.</param>
    /// <returns>The resolved reference station ID.</returns>
    public string Resolve(string? stationIdArgument, CliConfiguration configuration)
    {
        // Only null means the option was omitted; any supplied value, also an empty one, is validated by the library parser (CL-03, CL-05).
        if (stationIdArgument is not null)
        {
            return stationIdArgument;
        }

        if (!string.IsNullOrEmpty(configuration.Dwd?.ReferenceStationId))
        {
            return configuration.Dwd.ReferenceStationId;
        }

        throw new CliException("CLI-DWD-STATION-MISSING", "No DWD station given; use --StationID <id> or the setting 'Dwd.ReferenceStationId'.");
    }
}
