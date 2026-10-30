namespace WeatherParser.Cli.Output;

using WeatherParser.Parsing.Mqtt;

/// <summary>
/// Publishes MQTT messages.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes one serialized MQTT message and completes after the broker acknowledged it (REQ-062); implemented in P3.08.
    /// </summary>
    /// <param name="message">The MQTT message to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PublishAsync(MqttMessage message, CancellationToken cancellationToken);
}
