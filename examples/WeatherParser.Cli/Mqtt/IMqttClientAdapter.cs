using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Mqtt;

/// <summary>
/// The thin seam over the MQTT client package (CL-13): connect, publish with acknowledgment, disconnect.
/// </summary>
public interface IMqttClientAdapter : IDisposable
{
    /// <summary>
    /// Opens the connection with clean session or clean start; throws <see cref="MqttTransportException"/> on failure.
    /// </summary>
    /// <param name="settings">The settings to use for the connection.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ConnectAsync(MqttConnectionSettings settings, CancellationToken cancellationToken);

    /// <summary>
    /// Publishes the message unchanged and completes only with the broker's acknowledgment; throws <see cref="MqttTransportException"/> when the connection is lost.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task<MqttPublishAcknowledgment> PublishAsync(MqttMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Closes the connection with a normal disconnect.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets a value indicating whether the connection is currently open; read before a retry to decide whether to reconnect (CL-14).
    /// </summary>
    bool IsConnected { get; }
}
