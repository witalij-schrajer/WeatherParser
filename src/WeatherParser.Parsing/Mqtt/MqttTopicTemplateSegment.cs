namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// One segment of a parsed MQTT topic template: literal text or a placeholder name (contract MQ-04).
/// </summary>
public sealed class MqttTopicTemplateSegment
{
    /// <summary>
    /// Gets a value indicating whether this segment is a placeholder.
    /// </summary>
    public bool IsPlaceholder { get; }

    /// <summary>
    /// Gets the text of this segment.
    /// </summary>
    public string Text { get; }

    internal MqttTopicTemplateSegment(bool isPlaceholder, string text)
    {
        IsPlaceholder = isPlaceholder;
        Text = text;
    }
}
