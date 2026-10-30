using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Output;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Mqtt;

/// <summary>
/// Publishes the queued messages over the client adapter: checks every message, connects, publishes in order and counts only acknowledged messages as published (REQ-062, REQ-063, CL-13 to CL-15).
/// </summary>
public sealed class MqttPublisher : IMessagePublisher
{
    private readonly IMqttClientAdapter _adapter;
    private readonly ILogger<MqttPublisher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttPublisher"/> class.
    /// </summary>
    /// <param name="adapter">The MQTT client adapter.</param>
    /// <param name="logger">The logger.</param>
    public MqttPublisher(IMqttClientAdapter adapter, ILogger<MqttPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(logger);
        
        _adapter = adapter;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<PublicationReport> PublishAllAsync(IReadOnlyList<MqttMessage> messages, MqttPublicationSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(settings);
        if (messages.Count == 0)
        {
            return new PublicationReport(0, 0, 0);
        }

        // Every message is checked before the connection opens (REQ-062, CL-13).
        foreach (var message in messages)
        {
            CheckMessage(message);
        }

        if (!settings.Connection.UseTls)
        {
            _logger.LogWarning("TLS is disabled by 'Mqtt.Broker.AllowUnencrypted'; the broker connection is unencrypted.");
        }

        try
        {
            await _adapter.ConnectAsync(settings.Connection, cancellationToken).ConfigureAwait(false);
        }
        catch (MqttTransportException exception)
        {
            // The adapter's message never contains credentials (CL-06).
            throw new CliException(CliErrorIds.MqttConnectFailed, $"The connection to the MQTT broker {settings.Connection.Host}:{settings.Connection.Port} failed ({exception.Failure}): {exception.Message}");
        }

        var published = 0;
        var failed = 0;
        foreach (var message in messages)
        {
            var acknowledgment = await _adapter.PublishAsync(message, cancellationToken).ConfigureAwait(false);
            if (acknowledgment.IsSuccess)
            {
                published++;
            }
            else
            {
                // A failure reason code is final and never retried (CL-14).
                failed++;
                _logger.LogError(new EventId(0, CliErrorIds.MqttMessageRefused), "The broker refused the message for topic {Topic} with reason code {ReasonCode}.", message.Topic, acknowledgment.ReasonCode);
            }
        }

        try
        {
            await _adapter.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (MqttTransportException exception)
        {
            // Every message already has its acknowledgment; a failed disconnect does not change the result.
            _logger.LogDebug("The MQTT disconnect failed: {Message}", exception.Message);
        }

        return new PublicationReport(published, failed, 0);
    }

    private static void CheckMessage(MqttMessage message)
    {
        var retain = message.MessageType is MqttMessageType.Observation or MqttMessageType.Forecast;
        if (message.QualityOfService != MqttQualityOfService.AtLeastOnce || message.Retain != retain)
        {
            throw new CliException(CliErrorIds.Unexpected, $"The message for topic {message.Topic} has QoS {message.QualityOfService} and retain {message.Retain}; {message.MessageType} needs QoS AtLeastOnce and retain {retain} (REQ-062).");
        }
    }
}
