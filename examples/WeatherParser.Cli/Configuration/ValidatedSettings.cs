namespace WeatherParser.Cli.Configuration;

/// <summary>
/// The values the configuration-validation phase resolved for the command (CL-04 to CL-06).
/// </summary>
/// <param name="StationId">The resolved DWD station ID for --ParseDWD; otherwise null.</param>
/// <param name="Credentials">The broker credentials in MQTT mode when configured; otherwise null.</param>
public sealed record ValidatedSettings(string? StationId, BrokerCredentials? Credentials);
