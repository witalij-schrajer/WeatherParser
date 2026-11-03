using WeatherParser.Cli.Configuration;

namespace WeatherParser.Cli.Mqtt;

/// <summary>
/// The broker connection data passed to the client adapter (CL-13); the credentials are never logged (CL-06).
/// </summary>
public sealed record MqttConnectionSettings(
    string Host,
    int Port,
    MqttProtocol Protocol,
    string ClientId,
    TimeSpan KeepAlive,
    bool UseTls,
    string? CaFile,
    BrokerCredentials? Credentials)
{
    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return $"{Host}:{Port} {Protocol} tls={UseTls}";
    }
}
