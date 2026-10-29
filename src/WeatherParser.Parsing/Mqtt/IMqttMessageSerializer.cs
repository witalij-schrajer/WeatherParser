using System;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Serializes one supported export root into one MQTT message with the configured topic, the JSON export payload, QoS and retain, without any broker access (contract MQ-02, MQ-03, MQ-11, MQ-13).
/// </summary>
public interface IMqttMessageSerializer
{
    /// <summary>
    /// Serializes one supported export root into one MQTT message.
    /// </summary>
    /// <param name="root">The root object to serialize.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The serialized MQTT message.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="root"/> is null.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is cancelled.
    /// </exception>
    /// <exception cref="MqttMessageException">
    /// Thrown when the MQTT message cannot be created.
    /// </exception>
    Task<MqttMessage> SerializeAsync<TRoot>(TRoot root, CancellationToken cancellationToken) where TRoot : class;
}
