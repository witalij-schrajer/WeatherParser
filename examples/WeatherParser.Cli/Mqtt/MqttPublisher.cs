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
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttPublisher"/> class.
    /// </summary>
    /// <param name="adapter">The MQTT client adapter.</param>
    /// <param name="timeProvider">The time provider for timeouts and retry delays (CL-14).</param>
    /// <param name="logger">The logger.</param>
    public MqttPublisher(IMqttClientAdapter adapter, TimeProvider timeProvider, ILogger<MqttPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        
        _adapter = adapter;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private enum MessageResult
    {
        Published,
        Failed,
        ConnectionGone,
    }

    /// <inheritdoc/>
    public async Task<PublicationReport> PublishAllAsync(IReadOnlyList<MqttMessage> messages, MqttPublicationSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(settings);
        var published = 0;
        var failed = 0;
        var connected = false;
        try
        {
            if (messages.Count == 0)
            {
                return new PublicationReport(0, 0, 0);
            }

            // Every message is checked before the connection opens (REQ-062, CL-13).
            foreach (var message in messages)
            {
                CheckMessage(message);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!settings.Connection.UseTls)
            {
                _logger.LogWarning("TLS is disabled by 'Mqtt.Broker.AllowUnencrypted'; the broker connection is unencrypted.");
            }

            await ConnectFirstAsync(settings, cancellationToken).ConfigureAwait(false);
            connected = true;
            var suspect = false;
            foreach (var message in messages)
            {
                var (result, stillSuspect) = await PublishOneAsync(message, settings, suspect, cancellationToken).ConfigureAwait(false);
                suspect = stillSuspect;
                if (result == MessageResult.Published)
                {
                    published++;
                    continue;
                }

                failed++;
                if (result == MessageResult.ConnectionGone)
                {
                    // The connection could not be re-established: the remaining messages are never sent (CL-15).
                    break;
                }
            }

            return new PublicationReport(published, failed, messages.Count - published - failed);
        }
        finally
        {
            if (connected)
            {
                await DisconnectAsync(settings).ConfigureAwait(false);
            }

            // One summary line per publication run on every exit path; messages neither published nor failed are not attempted (CL-15).
            _logger.LogInformation(new EventId(0, CliErrorIds.PublicationSummary), "Publication summary: published={Published} failed={Failed} not_attempted={NotAttempted}.", published, failed, messages.Count - published - failed);
        }
    }

    // The normal disconnect has its own bound, so that it also runs after a cancellation; a failure does not change the result.
    private async Task DisconnectAsync(MqttPublicationSettings settings)
    {
        using var timer = new CancellationTokenSource(settings.Limits.ConnectTimeout, _timeProvider);
        try
        {
            await _adapter.DisconnectAsync(timer.Token).WaitAsync(timer.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is MqttTransportException or OperationCanceledException)
        {
            _logger.LogDebug("The MQTT disconnect failed: {Message}", Redact(exception.Message, settings.Connection));
        }
    }

    // Transport texts are forwarded only without the configured credentials (CL-06, REQ-072).
    private static string Redact(string text, MqttConnectionSettings connection)
    {
        if (connection.Credentials is { } credentials)
        {
            foreach (var secret in new[] { credentials.Password, credentials.Username })
            {
                if (!string.IsNullOrEmpty(secret))
                {
                    text = text.Replace(secret, "[redacted]", StringComparison.Ordinal);
                }
            }
        }

        return text;
    }

    // The first connection gets at most MaxPublishAttempts attempts; a refusal is final (CL-14).
    private async Task ConnectFirstAsync(MqttPublicationSettings settings, CancellationToken cancellationToken)
    {
        var limits = settings.Limits;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await ConnectOnceAsync(settings, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (MqttTransportException exception) when (exception.Failure == MqttTransportFailure.ConnectionRefused || attempt >= limits.MaxPublishAttempts)
            {
                // The adapter's message never contains credentials (CL-06).
                throw new CliException(CliErrorIds.MqttConnectFailed, $"The connection to the MQTT broker {settings.Connection.Host}:{settings.Connection.Port} failed after {attempt} attempt(s) ({exception.Failure}): {Redact(exception.Message, settings.Connection)}");
            }
            catch (MqttTransportException exception)
            {
                _logger.LogWarning(new EventId(0, CliErrorIds.MqttConnectFailed), "Connection attempt {Attempt} of {Attempts} failed ({Failure}); retrying.", attempt, limits.MaxPublishAttempts, exception.Failure);
                await Task.Delay(limits.RetryDelay, _timeProvider, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    // One connection attempt bounded by ConnectTimeout; a timeout is reported as a failed connection.
    private async Task ConnectOnceAsync(MqttPublicationSettings settings, CancellationToken cancellationToken)
    {
        var timeout = settings.Limits.ConnectTimeout;
        using var timer = new CancellationTokenSource(timeout, _timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timer.Token);
        try
        {
            await _adapter.ConnectAsync(settings.Connection, linked.Token).WaitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MqttTransportException(MqttTransportFailure.ConnectionFailed, $"The connection attempt timed out after {timeout}.");
        }
    }

    // One message with at most MaxPublishAttempts attempts; an acknowledged message is never sent again and a refusal is final (CL-14).
    private async Task<(MessageResult Result, bool Suspect)> PublishOneAsync(MqttMessage message, MqttPublicationSettings settings, bool suspect, CancellationToken cancellationToken)
    {
        var limits = settings.Limits;
        var errorId = CliErrorIds.MqttPublishFailed;
        var reason = string.Empty;
        var reconnectFailed = false;
        for (var attempt = 1; attempt <= limits.MaxPublishAttempts; attempt++)
        {
            if (attempt > 1)
            {
                await Task.Delay(limits.RetryDelay, _timeProvider, cancellationToken).ConfigureAwait(false);
            }

            reconnectFailed = false;
            if (suspect && !_adapter.IsConnected)
            {
                try
                {
                    await ConnectOnceAsync(settings, cancellationToken).ConfigureAwait(false);
                }
                catch (MqttTransportException exception)
                {
                    errorId = CliErrorIds.MqttPublishFailed;
                    reason = $"reconnection failed ({exception.Failure}): {Redact(exception.Message, settings.Connection)}";
                    reconnectFailed = true;
                    if (exception.Failure == MqttTransportFailure.ConnectionRefused)
                    {
                        break;
                    }

                    continue;
                }
            }

            suspect = false;
            using var timer = new CancellationTokenSource(limits.PublishTimeout, _timeProvider);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timer.Token);
            try
            {
                var acknowledgment = await _adapter.PublishAsync(message, linked.Token).WaitAsync(linked.Token).ConfigureAwait(false);
                if (acknowledgment.IsSuccess)
                {
                    return (MessageResult.Published, false);
                }

                // A failure reason code is final and never retried (CL-14).
                _logger.LogError(new EventId(0, CliErrorIds.MqttMessageRefused), "The broker refused the message for topic {Topic} with reason code {ReasonCode}.", message.Topic, acknowledgment.ReasonCode);
                return (MessageResult.Failed, false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                errorId = CliErrorIds.MqttPublishTimeout;
                reason = $"no acknowledgment within {limits.PublishTimeout}";
                suspect = true;
            }
            catch (MqttTransportException exception)
            {
                errorId = CliErrorIds.MqttPublishFailed;
                reason = $"{exception.Failure}: {Redact(exception.Message, settings.Connection)}";
                suspect = true;
            }

            if (attempt < limits.MaxPublishAttempts)
            {
                _logger.LogWarning(new EventId(0, errorId), "Attempt {Attempt} of {Attempts} for topic {Topic} failed ({Reason}); retrying.", attempt, limits.MaxPublishAttempts, message.Topic, reason);
            }
        }

        _logger.LogError(new EventId(0, errorId), "The message for topic {Topic} was not published: {Reason}.", message.Topic, reason);
        return (reconnectFailed ? MessageResult.ConnectionGone : MessageResult.Failed, true);
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
