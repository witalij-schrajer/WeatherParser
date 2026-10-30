namespace WeatherParser.Cli.Mqtt;

/// <summary>
/// The failure types that can occur during MQTT transport operations.
/// </summary>
public enum MqttTransportFailure
{
    /// <summary>
    /// The broker could not be reached or the connection attempt failed or timed out.
    /// </summary>
    ConnectionFailed,

    /// <summary>
    /// The broker refused the connection, for example for bad credentials or a missing authorization; never retried.
    /// </summary>
    ConnectionRefused,

    /// <summary>
    /// The connection was lost before the acknowledgment.
    /// </summary>
    ConnectionLost
}
