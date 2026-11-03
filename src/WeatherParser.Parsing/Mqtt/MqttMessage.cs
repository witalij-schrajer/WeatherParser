namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// One MQTT message produced by the library: topic, UTF-8 JSON payload, QoS, retain flag and message type (contract MQ-02).
/// It contains no broker settings or credentials and is not an export entity.
/// </summary>
public sealed class MqttMessage
{
    /// <summary>
    /// Gets the topic of the MQTT message.
    /// </summary>
    public string Topic { get; }

    /// <summary>
    /// Gets the UTF-8 JSON payload of the MQTT message, as a private copy of the caller's bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    /// Gets the quality of service level of the MQTT message.
    /// </summary>
    public MqttQualityOfService QualityOfService { get; }

    /// <summary>
    /// Gets a value indicating whether the MQTT message is retained.
    /// </summary>
    public bool Retain { get; }

    /// <summary>
    /// Gets the type of the MQTT message.
    /// </summary>
    public MqttMessageType MessageType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttMessage"/> class.
    /// </summary>
    /// <param name="topic">The topic of the MQTT message.</param>
    /// <param name="payload">The UTF-8 JSON payload of the MQTT message.</param>
    /// <param name="qualityOfService">The quality of service level of the MQTT message.</param>
    /// <param name="retain">A value indicating whether the MQTT message is retained.</param>
    /// <param name="messageType">The type of the MQTT message.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="topic"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="topic"/> is empty, or when <paramref name="qualityOfService"/> or <paramref name="messageType"/> is not defined.
    /// </exception>
    public MqttMessage(string topic, ReadOnlyMemory<byte> payload, MqttQualityOfService qualityOfService, bool retain, MqttMessageType messageType)
    {
        ArgumentNullException.ThrowIfNull(topic);

        if (topic.Length == 0)
        {
            throw new ArgumentException("The topic must not be empty.", nameof(topic));
        }

        if (!Enum.IsDefined(qualityOfService))
        {
            throw new ArgumentException("Unknown quality of service.", nameof(qualityOfService));
        }

        if (!Enum.IsDefined(messageType))
        {
            throw new ArgumentException("Unknown message type.", nameof(messageType));
        }

        Topic = topic;
        Payload = payload.ToArray();
        QualityOfService = qualityOfService;
        Retain = retain;
        MessageType = messageType;
    }
}
