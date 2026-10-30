using MQTTnet;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Mqtt;

/// <summary>
/// Maps the connection data and the library's messages to MQTTnet types (CL-13, MQ-02).
/// </summary>
public interface IMqttNetOptionsBuilder
{
    /// <summary>
    /// Builds the client options: TCP server, protocol version, client ID, keep-alive, clean session, credentials and TLS with the system trust store or the configured CA as the only trust anchor; certificate and host-name validation are never disabled. Throws <see cref="MqttTransportException"/> when the CA file cannot be used.
    /// </summary>
    /// <param name="settings">The settings to use for the connection.</param>
    /// <returns>The client options.</returns>
    MqttClientOptions BuildClientOptions(MqttConnectionSettings settings);

    /// <summary>
    /// Builds the application message with the topic, payload, QoS and retain flag unchanged.
    /// </summary>
    /// <param name="message">The message to build.</param>
    /// <returns>The application message.</returns>
    MqttApplicationMessage BuildMessage(MqttMessage message);
}
