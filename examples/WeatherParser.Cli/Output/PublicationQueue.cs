namespace WeatherParser.Cli.Output;

using WeatherParser.Parsing.Mqtt;

/// <summary>
/// Collects the serialized MQTT messages of one invocation in result order until they are published together (CL-13, CL-15).
/// </summary>
public sealed class PublicationQueue : IPublicationQueue
{
    private readonly List<MqttMessage> _messages = new();

    /// <inheritdoc/>
    public void Add(MqttMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _messages.Add(message);
    }

    /// <inheritdoc/>
    public IReadOnlyList<MqttMessage> Messages => _messages;
}
