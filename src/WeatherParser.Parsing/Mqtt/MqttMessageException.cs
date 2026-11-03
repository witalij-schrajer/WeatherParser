using System;

namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Raised when an MQTT message cannot be created or the MQTT topic configuration is invalid (contract MQ-13).
/// </summary>
public sealed class MqttMessageException : Exception
{
    /// <summary>
    /// Gets the error identifier for this exception.
    /// </summary>
    public string ErrorId { get; }

    /// <summary>
    /// Gets the message type of this exception.
    /// </summary>
    public MqttMessageType? MessageType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttMessageException"/> class.
    /// </summary>
    /// <param name="errorId">The error identifier.</param>
    /// <param name="messageType">The message type.</param>
    /// <param name="message">The message that describes the error.</param>
    public MqttMessageException(string errorId, MqttMessageType? messageType, string message) : base(message)
    {
        Validate(errorId, messageType);
        ErrorId = errorId;
        MessageType = messageType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttMessageException"/> class.
    /// </summary>
    /// <param name="errorId">The error identifier.</param>
    /// <param name="messageType">The message type.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MqttMessageException(string errorId, MqttMessageType? messageType, string message, Exception innerException) : base(message, innerException)
    {
        Validate(errorId, messageType);
        ErrorId = errorId;
        MessageType = messageType;
    }

    private static void Validate(string errorId, MqttMessageType? messageType)
    {
        if (!IsKnown(errorId))
        {
            throw new ArgumentException("Unknown error ID.", nameof(errorId));
        }

        if (messageType.HasValue && !Enum.IsDefined(messageType.Value))
        {
            throw new ArgumentException("Unknown message type.", nameof(messageType));
        }
    }

    private static bool IsKnown(string id) => id == MqttMessageErrorIds.MessageTypeUnsupported ||
        id == MqttMessageErrorIds.MappingMissing ||
        id == MqttMessageErrorIds.SerializationFailed ||
        id == MqttMessageErrorIds.PlaceholderValueInvalid ||
        id == MqttMessageErrorIds.TopicInvalid ||
        id == MqttMessageErrorIds.PayloadTooLarge ||
        id == MqttMessageErrorIds.OptionsInvalid;
}
