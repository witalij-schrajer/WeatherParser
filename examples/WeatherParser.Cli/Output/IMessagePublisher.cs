namespace WeatherParser.Cli.Output;

using WeatherParser.Parsing.Mqtt;

/// <summary>
/// Publishes the queued MQTT messages of one invocation; implemented over the broker client in P3.08.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Checks every message, then connects, publishes the messages one after another in order and reports only acknowledged messages as published (REQ-062, REQ-063, CL-13 to CL-15); an empty list never contacts the broker.
    /// </summary>
    /// <param name="messages">The messages to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<PublicationReport> PublishAllAsync(IReadOnlyList<MqttMessage> messages, CancellationToken cancellationToken);
}
