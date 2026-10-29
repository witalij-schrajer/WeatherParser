namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Validates MQTT topic options (contract MQ-09).
/// </summary>
public interface IMqttTopicOptionsValidator
{
    /// <summary>
    /// Validates the specified options.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>A validated configuration snapshot.</returns>
    /// <exception cref="ArgumentNullException">For null options.</exception>
    /// <exception cref="MqttMessageException">Every validation violation: ErrorId OptionsInvalid.
    /// MessageType = the message type of the failing mapping, null for MaxPayloadBytes, a null Mappings dictionary or a cross-type collision.</exception>
    ValidatedMqttTopicConfiguration Validate(MqttTopicOptions options);
}
