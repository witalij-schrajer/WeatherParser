namespace WeatherParser.Cli.Mqtt;

/// <summary>
/// The connection data and limits of one publication run.
/// </summary>
public sealed record MqttPublicationSettings(
    MqttConnectionSettings Connection,
    MqttPublicationLimits Limits);
