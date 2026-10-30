using System.Net.Sockets;
using System.Security.Authentication;
using MQTTnet;
using MQTTnet.Exceptions;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Mqtt;

public sealed class MqttNetClientAdapter : IMqttClientAdapter
{
    private readonly IMqttClient _client;
    private readonly IMqttNetOptionsBuilder _optionsBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttNetClientAdapter"/> class.
    /// </summary>
    /// <param name="client">The MQTTnet client to use.</param>
    /// <param name="optionsBuilder">The options builder to use.</param>
    public MqttNetClientAdapter(IMqttClient client, IMqttNetOptionsBuilder optionsBuilder)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _optionsBuilder = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder));
    }

    private static readonly HashSet<MqttClientConnectResultCode> RefusedCodes = new()
    {
        MqttClientConnectResultCode.NotAuthorized,
        MqttClientConnectResultCode.BadUserNameOrPassword,
        MqttClientConnectResultCode.Banned,
        MqttClientConnectResultCode.BadAuthenticationMethod,
        MqttClientConnectResultCode.ClientIdentifierNotValid,
    };

    /// <inheritdoc/>
    public bool IsConnected => _client.IsConnected;

    /// <inheritdoc/>
    public async Task ConnectAsync(MqttConnectionSettings settings, CancellationToken cancellationToken)
    {
        var options = _optionsBuilder.BuildClientOptions(settings);
        MqttClientConnectResult result;
        try
        {
            result = await _client.ConnectAsync(options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsTransportFailure(exception))
        {
            throw new MqttTransportException(MqttTransportFailure.ConnectionFailed, exception.Message);
        }

        // MQTTnet returns a refused connection as a result code instead of throwing.
        if (result.ResultCode != MqttClientConnectResultCode.Success)
        {
            var failure = RefusedCodes.Contains(result.ResultCode) ? MqttTransportFailure.ConnectionRefused : MqttTransportFailure.ConnectionFailed;
            throw new MqttTransportException(failure, $"The broker refused the connection with result code {result.ResultCode}.");
        }
    }

    /// <inheritdoc/>
    public async Task<MqttPublishAcknowledgment> PublishAsync(MqttMessage message, CancellationToken cancellationToken)
    {
        var applicationMessage = _optionsBuilder.BuildMessage(message);
        try
        {
            var result = await _client.PublishAsync(applicationMessage, cancellationToken).ConfigureAwait(false);
            return new MqttPublishAcknowledgment(result.IsSuccess, result.ReasonCode.ToString());
        }
        catch (Exception exception) when (IsTransportFailure(exception))
        {
            throw new MqttTransportException(MqttTransportFailure.ConnectionLost, exception.Message);
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (!_client.IsConnected)
        {
            return;
        }

        try
        {
            await _client.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection).Build(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsTransportFailure(exception))
        {
            throw new MqttTransportException(MqttTransportFailure.ConnectionLost, exception.Message);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // The container owns and disposes the MQTTnet client.
    }

    // A TLS failure surfaces as AuthenticationException; cancellation is never a transport failure.
    private static bool IsTransportFailure(Exception exception) =>
        exception is MqttCommunicationException or SocketException or IOException or AuthenticationException;
}
