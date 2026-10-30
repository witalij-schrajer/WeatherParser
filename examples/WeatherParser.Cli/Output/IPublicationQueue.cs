namespace WeatherParser.Cli.Output;

using WeatherParser.Parsing.Mqtt;

/// <summary>
/// Collects the serialized MQTT messages of one invocation in result order until they are published together (CL-13, CL-15).
/// </summary>
public interface IPublicationQueue
{
    /// <summary>
    /// Adds a message to the queue.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <param name="source">The source of the export root.</param>
    void Add(MqttMessage message, string source);

    /// <summary>
    /// Gets the messages in the queue.
    /// </summary>
    IReadOnlyList<MqttMessage> Messages { get; }

    /// <summary>
    /// Gets the queued items in result order.
    /// </summary>
    IReadOnlyList<PublicationItem> Items { get; }
}
