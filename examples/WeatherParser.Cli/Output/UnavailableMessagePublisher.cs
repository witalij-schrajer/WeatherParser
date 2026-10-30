using WeatherParser.Cli.Diagnostics;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Output;

/// <summary>
/// The MQTT publisher until the broker transport exists (P3.08): every publication fails with CLI-MQTT-CONNECT-FAILED, so MQTT output never reports a simulated success (REQ-062).
/// </summary>
public sealed class UnavailableMessagePublisher : IMessagePublisher
{
    /// <inheritdoc/>
    public Task PublishAsync(MqttMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        throw new CliException(CliErrorIds.MqttConnectFailed, "The MQTT transport is not available in this version of the example.");
    }
}
