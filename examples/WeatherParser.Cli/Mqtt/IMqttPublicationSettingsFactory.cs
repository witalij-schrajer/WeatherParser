using WeatherParser.Cli.Configuration;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Mqtt;

/// <summary>
/// Builds the publication settings from the validated configuration (CL-13, CL-14).
/// </summary>
public interface IMqttPublicationSettingsFactory
{
    /// <summary>
    /// Builds the settings; absent members take the CL-13 and CL-14 defaults and the default client ID is generated per call.
    /// </summary>
    /// <param name="configuration">The validated configuration.</param>
    /// <param name="credentials">The broker credentials, or null if not required.</param>
    /// <returns>The publication settings.</returns>
    MqttPublicationSettings Create(CliConfiguration configuration, BrokerCredentials? credentials);
}
