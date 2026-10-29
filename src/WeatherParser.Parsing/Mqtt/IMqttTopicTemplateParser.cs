namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Parses MQTT topic templates (contract MQ-04, MQ-07).
/// </summary>
public interface IMqttTopicTemplateParser
{
    /// <summary>
    /// Parses the specified template for the specified message type.
    /// </summary>
    /// <param name="template">The template to parse.</param>
    /// <param name="messageType">The message type.</param>
    /// <returns>The parsed template.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="template"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="MqttMessageException">
    /// <paramref name="template"/> is invalid for the specified message type.
    /// </exception>
    MqttTopicTemplate Parse(string template, MqttMessageType messageType);
}
