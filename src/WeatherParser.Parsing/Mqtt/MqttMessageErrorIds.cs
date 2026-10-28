namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Stable error IDs of the MQTT message serializer and its topic configuration (contract MQ-13).
/// </summary>
public static class MqttMessageErrorIds
{
    /// <summary>
    /// The message type is not supported.
    /// </summary>
    public const string MessageTypeUnsupported = "MQTT-MESSAGE-TYPE-UNSUPPORTED";

    /// <summary>
    /// The mapping for the message type is missing.
    /// </summary>
    public const string MappingMissing = "MQTT-MAPPING-MISSING";

    /// <summary>
    /// The serialization of the message failed.
    /// </summary>
    public const string SerializationFailed = "MQTT-SERIALIZATION-FAILED";

    /// <summary>
    /// A placeholder value is invalid.
    /// </summary>
    public const string PlaceholderValueInvalid = "MQTT-PLACEHOLDER-VALUE-INVALID";

    /// <summary>
    /// The topic is invalid.
    /// </summary>
    public const string TopicInvalid = "MQTT-TOPIC-INVALID";

    /// <summary>
    /// The payload is too large.
    /// </summary>
    public const string PayloadTooLarge = "MQTT-PAYLOAD-TOO-LARGE";

    /// <summary>
    /// The consumer options are invalid.
    /// </summary>
    public const string OptionsInvalid = "MQTT-OPTIONS-INVALID";
}
