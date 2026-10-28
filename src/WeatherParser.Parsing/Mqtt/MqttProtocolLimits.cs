namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Protocol limits of the MQTT contract (sections 3 and 4 of P8_MQTT_CONTRACT.md).
/// </summary>
internal static class MqttProtocolLimits
{
    /// <summary>
    /// Maximum UTF-8 length of a topic name, protocol fact F1.
    /// </summary>
    public const int MaxTopicBytes = 65535;

    /// <summary>
    /// Largest payload of a message whose topic has zero bytes: 268 435 455 − 2 − 2 − 1 (protocol facts F10 to F12); the effective bound of a message subtracts its topic bytes.
    /// </summary>
    public const int MaxPayloadBytes = 268435450;
}
