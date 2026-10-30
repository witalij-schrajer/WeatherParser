namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Provides functionality for resolving reference station IDs.
/// </summary>
public interface IReferenceStationResolver
{
    /// <summary>
    /// Resolves the reference station ID based on the provided arguments and configuration.
    /// </summary>
    /// <param name="stationIdArgument">The station ID argument, or null if not provided.</param>
    /// <param name="configuration">The CLI configuration to use for resolution.</param>
    /// <returns>The resolved reference station ID.</returns>
    string Resolve(string? stationIdArgument, CliConfiguration configuration);
}
