namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Consumer-registered options for an MQTT topic mapping (contract MQ-09); plain data object with public settable properties.
/// </summary>
public sealed class MqttTopicMapping
{
    /// <summary>
    /// Gets or sets the topic template.
    /// Must not be null or empty; null or empty → MQTT-OPTIONS-INVALID.
    /// </summary>
    public string? TopicTemplate { get; set; }

    /// <summary>
    /// Gets or sets the quality of service.
    /// Null → MQ-08 default of the message type.
    /// </summary>
    public MqttQualityOfService? QualityOfService { get; set; }

    /// <summary>
    /// Gets or sets the retain flag.
    /// Null → MQ-08 default of the message type.
    /// </summary>
    public bool? Retain { get; set; }
}
