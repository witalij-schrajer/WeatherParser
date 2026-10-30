namespace WeatherParser.Cli.Mqtt;

/// <summary>
/// The MQTT protocol version to use for the connection.
/// </summary>
public enum MqttProtocol
{
    /// <summary>
    /// MQTT 5.0, the default (CL-13).
    /// </summary>
    V500,

    /// <summary>
    /// MQTT 3.1.1.
    /// </summary>
    V311
}
