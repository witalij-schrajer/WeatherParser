using System;

namespace WeatherParser.Cli.Mqtt;

/// <summary>
/// A failure of the MQTT client adapter; the message never contains credentials (CL-06).
/// </summary>
public sealed class MqttTransportException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MqttTransportException"/> class.
    /// </summary>
    /// <param name="failure">The type of failure that occurred.</param>
    /// <param name="message">The message that describes the error.</param>
    public MqttTransportException(MqttTransportFailure failure, string message) : base(message)
    {
        Failure = failure;
    }

    /// <summary>
    /// Gets the type of failure that occurred.
    /// </summary>
    public MqttTransportFailure Failure { get; }
}
