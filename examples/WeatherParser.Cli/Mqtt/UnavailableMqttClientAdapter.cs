using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Mqtt;

/// <summary>
/// The client adapter until the MQTTnet adapter exists (P3.08 S4): every connection fails, so MQTT output never reports a simulated success (REQ-062).
/// </summary>
public sealed class UnavailableMqttClientAdapter : IMqttClientAdapter
{
    /// <summary>
    /// Disposes the resources used by the adapter.
    /// </summary>
    public void Dispose()
    {
        // Nothing to dispose.
    }

    /// <summary>
    /// Opens the connection with clean session or clean start; fails: the transport is not available yet.
    /// </summary>
    /// <param name="settings">The settings to use for the connection.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConnectAsync(MqttConnectionSettings settings, CancellationToken cancellationToken)
    {
        throw new MqttTransportException(MqttTransportFailure.ConnectionFailed, "The MQTT transport is not available in this version of the example.");
    }

    /// <summary>
    /// Publishes the message unchanged and completes only with the broker's acknowledgment; fails: the transport is not available yet.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task<MqttPublishAcknowledgment> PublishAsync(MqttMessage message, CancellationToken cancellationToken)
    {
        throw new MqttTransportException(MqttTransportFailure.ConnectionFailed, "The MQTT transport is not available in this version of the example.");
    }

    /// <summary>
    /// Closes the connection with a normal disconnect; completes without action.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public bool IsConnected => false;
}
