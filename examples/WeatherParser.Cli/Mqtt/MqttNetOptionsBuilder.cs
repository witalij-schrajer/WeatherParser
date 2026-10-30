using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Mqtt;

public sealed class MqttNetOptionsBuilder : IMqttNetOptionsBuilder
{
    private readonly IFileOperations _files;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttNetOptionsBuilder"/> class.
    /// </summary>
    /// <param name="files">The file operations to use.</param>
    public MqttNetOptionsBuilder(IFileOperations files)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    /// <inheritdoc/>
    public MqttClientOptions BuildClientOptions(MqttConnectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var builder = new MqttClientOptionsBuilder()
            .WithTcpServer(settings.Host, settings.Port)
            .WithProtocolVersion(settings.Protocol == MqttProtocol.V311 ? MqttProtocolVersion.V311 : MqttProtocolVersion.V500)
            .WithClientId(settings.ClientId)
            .WithKeepAlivePeriod(settings.KeepAlive)
            .WithCleanSession(true);
        if (settings.Credentials is { } credentials)
        {
            builder = builder.WithCredentials(credentials.Username, credentials.Password);
        }

        // Certificate and host-name validation are never disabled; a configured CA is the only trust anchor (CL-13, REQ-062).
        var trustChain = settings.UseTls && settings.CaFile is { } caFile ? LoadTrustChain(caFile) : null;
        builder = builder.WithTlsOptions(tls =>
        {
            tls.UseTls(settings.UseTls);
            if (trustChain is not null)
            {
                tls.WithTrustChain(trustChain);
            }
        });
        return builder.Build();
    }

    /// <inheritdoc/>
    public MqttApplicationMessage BuildMessage(MqttMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new MqttApplicationMessageBuilder()
            .WithTopic(message.Topic)
            .WithPayload(message.Payload.ToArray())
            .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)(int)message.QualityOfService)
            .WithRetainFlag(message.Retain)
            .Build();
    }

    private X509Certificate2Collection LoadTrustChain(string caFile)
    {
        var certificates = new X509Certificate2Collection();
        try
        {
            certificates.ImportFromPem(_files.ReadAllText(caFile));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException)
        {
            throw new MqttTransportException(MqttTransportFailure.ConnectionFailed, $"The file named by 'Mqtt.Broker.Tls.CaFile' could not be read as PEM certificates: {exception.Message}");
        }

        if (certificates.Count == 0)
        {
            throw new MqttTransportException(MqttTransportFailure.ConnectionFailed, "The file named by 'Mqtt.Broker.Tls.CaFile' contains no PEM certificate.");
        }

        return certificates;
    }
}
