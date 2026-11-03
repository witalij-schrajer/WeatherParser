using System.Security.Cryptography;
using WeatherParser.Cli.Configuration;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Mqtt;

/// <summary>
/// Builds the publication settings from the validated configuration (CL-13, CL-14).
/// </summary>
public sealed class MqttPublicationSettingsFactory : IMqttPublicationSettingsFactory
{
    /// <inheritdoc/>
    public MqttPublicationSettings Create(CliConfiguration configuration, BrokerCredentials? credentials)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var broker = configuration.Mqtt?.Broker ?? new BrokerSection();
        var limits = configuration.Limits;
        var useTls = !broker.AllowUnencrypted;
        var connection = new MqttConnectionSettings(
            broker.Host ?? string.Empty,
            broker.Port ?? (useTls ? 8883 : 1883),
            broker.ProtocolVersion == "3.1.1" ? MqttProtocol.V311 : MqttProtocol.V500,
            broker.ClientId ?? "weatherparser" + RandomNumberGenerator.GetHexString(10, lowercase: true),
            broker.KeepAlive ?? TimeSpan.FromSeconds(30),
            useTls,
            broker.Tls?.CaFile,
            credentials);
        // The default client ID is new per invocation, so parallel runs never take over each other's session (CL-13).
        var defaults = MqttPublicationLimits.Default;
        return new MqttPublicationSettings(connection, new MqttPublicationLimits(
            limits?.ConnectTimeout ?? defaults.ConnectTimeout,
            limits?.PublishTimeout ?? defaults.PublishTimeout,
            limits?.MaxPublishAttempts ?? defaults.MaxPublishAttempts,
            limits?.RetryDelay ?? defaults.RetryDelay));
    }
}
