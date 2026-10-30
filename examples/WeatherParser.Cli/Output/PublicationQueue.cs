namespace WeatherParser.Cli.Output;

using WeatherParser.Parsing.Mqtt;

/// <summary>
/// Collects the serialized MQTT messages of one invocation in result order until they are published together (CL-13, CL-15).
/// </summary>
public sealed class PublicationQueue : IPublicationQueue
{
    private readonly List<MqttMessage> _messages = new();
    private readonly List<PublicationItem> _items = new();

    /// <inheritdoc/>
    public void Add(MqttMessage message, string source)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(source);
        _items.Add(new PublicationItem(message, source));
        _messages.Add(message);
    }

    /// <inheritdoc/>
    public IReadOnlyList<MqttMessage> Messages => _messages;

    /// <inheritdoc/>
    public IReadOnlyList<PublicationItem> Items => _items;
}
