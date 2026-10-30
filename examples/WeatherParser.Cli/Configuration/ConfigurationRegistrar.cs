using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Cli.Arguments;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Registers the consumer types of the library from the configuration; values are forwarded unchanged and absent members keep the library defaults (CL-05). Logging is registered by the command executor.
/// </summary>
public sealed class ConfigurationRegistrar : IConfigurationRegistrar
{
    /// <inheritdoc />
    public void Register(IServiceCollection services, CliConfiguration configuration, CommandRequest request)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(request);

        var osthessen = configuration.OsthessenWetter;
        var mosmixL = configuration.Mosmix?.L;
        var mosmixS = configuration.Mosmix?.S;
        var osthessenOptions = new OsthessenWetterOptions { Stations = osthessen?.Stations ?? new List<OsthessenWetterStationMetadata>() };
        osthessenOptions.PageDeadline = osthessen?.PageDeadline ?? osthessenOptions.PageDeadline;
        var mosmixLOptions = new MosmixLForecastOptions { MaxRunAge = mosmixL?.MaxRunAge };
        mosmixLOptions.ParseDeadline = mosmixL?.ParseDeadline ?? mosmixLOptions.ParseDeadline;
        var mosmixSOptions = new MosmixSForecastOptions { MaxRunAge = mosmixS?.MaxRunAge };
        mosmixSOptions.ParseDeadline = mosmixS?.ParseDeadline ?? mosmixSOptions.ParseDeadline;

        services.AddSingleton(configuration.Dwd?.Catalog ?? new CdcCatalogEndpoints());
        services.AddSingleton(configuration.Dwd?.Observation ?? new DwdObservationOptions());
        services.AddSingleton(osthessenOptions);
        services.AddSingleton(new OsthessenWetterEndpoints { PageUri = osthessen?.PageUri });
        services.AddSingleton(configuration.Mosmix?.Catalog ?? new MosmixCatalogEndpoints());
        services.AddSingleton(new MosmixLForecastEndpoints { SingleStationsDirectory = mosmixL?.SingleStationsDirectory });
        services.AddSingleton(mosmixLOptions);
        services.AddSingleton(new MosmixSForecastEndpoints { AllStationsUri = mosmixS?.AllStationsUri });
        services.AddSingleton(mosmixSOptions);
        services.AddSingleton(request.Common.Output == OutputFormat.Mqtt ? CreateTopicOptions(configuration.Mqtt) : new MqttTopicOptions());
    }

    private static MqttTopicOptions CreateTopicOptions(MqttSection? mqtt)
    {
        var topics = mqtt?.Topics;
        var mappings = new Dictionary<MqttMessageType, MqttTopicMapping>();
        foreach (var (type, topic) in new[]
                 {
                     (MqttMessageType.Observation, topics?.Observation),
                     (MqttMessageType.Forecast, topics?.Forecast),
                     (MqttMessageType.CdcStationList, topics?.CdcStationList),
                     (MqttMessageType.MosmixLocationList, topics?.MosmixLocationList),
                 })
        {
            if (topic != null)
            {
                mappings[type] = new MqttTopicMapping { TopicTemplate = topic.TopicTemplate };
            }
        }

        return new MqttTopicOptions { Mappings = mappings, MaxPayloadBytes = mqtt?.MaxPayloadBytes };
    }
}
