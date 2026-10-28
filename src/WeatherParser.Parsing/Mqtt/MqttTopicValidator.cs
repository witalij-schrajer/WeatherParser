using System.Text;

namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Checks placeholder values and resolved MQTT topic names (contract MQ-06, MQ-07; protocol facts F1 to F9).
/// </summary>
public sealed class MqttTopicValidator : IMqttTopicValidator
{
    /// <inheritdoc/>
    public void ValidatePlaceholderValue(string placeholderName, string value)
    {
        ArgumentNullException.ThrowIfNull(placeholderName);
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length == 0)
        {
            throw ValueFailure($"The value of placeholder '{placeholderName}' must not be empty.");
        }

        int index = value.IndexOfAny(['/', '+', '#']);
        if (index >= 0)
        {
            throw ValueFailure($"The value of placeholder '{placeholderName}' contains a topic level separator or wildcard character at position {index}.");
        }

        index = MqttTopicCharacters.FindDisallowed(value, 0, value.Length);
        if (index >= 0)
        {
            throw ValueFailure($"The value of placeholder '{placeholderName}' contains a disallowed character at position {index}.");
        }
    }

    /// <inheritdoc/>
    public void ValidateTopic(string topic)
    {
        ArgumentNullException.ThrowIfNull(topic);

        if (topic.Length == 0)
        {
            throw TopicFailure("The topic must not be empty.");
        }

        var index = MqttTopicCharacters.FindDisallowed(topic, 0, topic.Length);
        if (index >= 0)
        {
            throw TopicFailure($"The topic contains a disallowed character at position {index}.");
        }

        index = topic.IndexOfAny(['+', '#']);
        if (index >= 0)
        {
            throw TopicFailure($"The topic contains a wildcard character at position {index}.");
        }

        if (topic[0] == '$')
        {
            throw TopicFailure("The topic must not start with '$'.");
        }

        if (topic[0] == '/' || topic[^1] == '/' || topic.Contains("//", StringComparison.Ordinal))
        {
            throw TopicFailure("The topic contains an empty topic level.");
        }

        if (Encoding.UTF8.GetByteCount(topic) > 65535)
        {
            throw TopicFailure("The topic is longer than 65 535 UTF-8 bytes.");
        }
    }

    private static MqttMessageException ValueFailure(string message)
    {
        return new MqttMessageException(MqttMessageErrorIds.PlaceholderValueInvalid, null, message);
    }

    private static MqttMessageException TopicFailure(string message)
    {
        return new MqttMessageException(MqttMessageErrorIds.TopicInvalid, null, message);
    }
}
