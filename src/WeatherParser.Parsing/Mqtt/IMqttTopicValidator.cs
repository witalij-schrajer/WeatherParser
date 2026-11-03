using System;

namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Checks placeholder values and resolved MQTT topic names against the protocol and the contract rules (MQ-06, MQ-07).
/// </summary>
public interface IMqttTopicValidator
{
    /// <summary>
    /// Validates the specified placeholder value.
    /// </summary>
    /// <param name="placeholderName">The placeholder name.</param>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="placeholderName"/> or <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="MqttMessageException">
    /// <paramref name="value"/> is invalid.
    /// </exception>
    public void ValidatePlaceholderValue(string placeholderName, string value);

    /// <summary>
    /// Validates the specified topic.
    /// </summary>
    /// <param name="topic">The topic to validate.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="topic"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="MqttMessageException">
    /// <paramref name="topic"/> is invalid.
    /// </exception>
    public void ValidateTopic(string topic);
}
