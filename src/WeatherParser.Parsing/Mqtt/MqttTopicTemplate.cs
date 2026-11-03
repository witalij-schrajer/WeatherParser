namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// A topic template parsed and checked by IMqttTopicTemplateParser (contract MQ-04, MQ-07); it has no behavior.
/// </summary>
public sealed class MqttTopicTemplate
{
    /// <summary>
    /// Gets the message type of this template.
    /// </summary>
    public MqttMessageType MessageType { get; }

    /// <summary>
    /// Gets the template as configured.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the segments of this template, in order; adjacent literals merged; never an empty literal segment.
    /// </summary>
    public IReadOnlyList<MqttTopicTemplateSegment> Segments { get; }

    internal MqttTopicTemplate(MqttMessageType messageType, string text, IReadOnlyList<MqttTopicTemplateSegment> segments)
    {
        MessageType = messageType;
        Text = text;
        Segments = Array.AsReadOnly(segments.ToArray());
    }
}
