namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Consumer-registered options for MQTT topic mappings (contract MQ-09); plain data object with public settable properties.
/// Consumers register one instance (an empty one when they do not use MQTT) and it is validated when the MQTT serializer is first resolved.
/// </summary>
public sealed class MqttTopicOptions
{
    /// <summary>
    /// Gets or sets the topic mappings by message type.
    /// Null → MQTT-OPTIONS-INVALID.
    /// </summary>
    public IDictionary<MqttMessageType, MqttTopicMapping>? Mappings { get; set; } = new Dictionary<MqttMessageType, MqttTopicMapping>();

    /// <summary>
    /// Gets or sets the maximum payload size in bytes.
    /// Null → protocol bound; otherwise 1 to 268 435 450 (MQ-11).
    /// </summary>
    public int? MaxPayloadBytes { get; set; }
}
