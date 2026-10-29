using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Cli.Tests.Configuration;

[TestClass]
[TestCategory("T-P3-05")]
public sealed class ConfigurationRegistrarTests
{
    private IConfigurationRegistrar _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new ConfigurationRegistrar();
    }

    private ServiceProvider Register(CliConfiguration configuration, OutputFormat output)
    {
        var services = new ServiceCollection();
        _subject.Register(services, configuration, new ParseDwdRequest(new CommonOptions(output, null, null, false), null));
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Verifies the mapping of the configuration to the library's consumer registrations (CL-05, T-P3-05).
    /// </summary>
    [TestMethod]
    public void Map_EveryConsumerRegistration_ForwardedUnchanged()
    {
        var catalog = new CdcCatalogEndpoints
        {
            AirTemperatureStationList = new Uri("https://example.invalid/air/list.txt"),
            MaxEvidenceCandidates = 7
        };
        var observation = new DwdObservationOptions
        {
            ObservationDeadline = TimeSpan.FromSeconds(45)
        };
        var mosmixCatalog = new MosmixCatalogEndpoints
        {
            CatalogUri = new Uri("https://example.invalid/cfg")
        };

        var configuration = new CliConfiguration
        {
            Dwd = new DwdSection
            {
                Catalog = catalog,
                Observation = observation
            },
            OsthessenWetter = new OsthessenWetterSection
            {
                PageUri = new Uri("https://example.invalid/page/"),
                PageDeadline = TimeSpan.FromSeconds(30),
                Stations = new List<OsthessenWetterStationMetadata>
                {
                    new()
                    {
                        Name = "Test",
                        Latitude = 50.5m,
                        Longitude = 9.7m
                    }
                }
            },
            Mosmix = new MosmixSection
            {
                Catalog = mosmixCatalog,
                L = new MosmixLSection
                {
                    SingleStationsDirectory = new Uri("https://example.invalid/l/"),
                    ParseDeadline = TimeSpan.FromSeconds(50),
                    MaxRunAge = TimeSpan.FromHours(8)
                },
                S = new MosmixSSection
                {
                    AllStationsUri = new Uri("https://example.invalid/s.kmz"),
                    ParseDeadline = TimeSpan.FromMinutes(4),
                    MaxRunAge = TimeSpan.FromHours(2)
                }
            }
        };

        var provider = Register(configuration, OutputFormat.Json);

        provider.GetRequiredService<CdcCatalogEndpoints>().Should().BeSameAs(catalog);
        provider.GetRequiredService<DwdObservationOptions>().Should().BeSameAs(observation);
        provider.GetRequiredService<MosmixCatalogEndpoints>().Should().BeSameAs(mosmixCatalog);
        provider.GetRequiredService<OsthessenWetterEndpoints>().PageUri.Should().Be(new Uri("https://example.invalid/page/"));
        provider.GetRequiredService<OsthessenWetterOptions>().PageDeadline.Should().Be(TimeSpan.FromSeconds(30));
        provider.GetRequiredService<OsthessenWetterOptions>().Stations.Should().ContainSingle().Which.Name.Should().Be("Test");
        provider.GetRequiredService<MosmixLForecastEndpoints>().SingleStationsDirectory.Should().Be(new Uri("https://example.invalid/l/"));
        provider.GetRequiredService<MosmixLForecastOptions>().ParseDeadline.Should().Be(TimeSpan.FromSeconds(50));
        provider.GetRequiredService<MosmixLForecastOptions>().MaxRunAge.Should().Be(TimeSpan.FromHours(8));
        provider.GetRequiredService<MosmixSForecastEndpoints>().AllStationsUri.Should().Be(new Uri("https://example.invalid/s.kmz"));
        provider.GetRequiredService<MosmixSForecastOptions>().ParseDeadline.Should().Be(TimeSpan.FromMinutes(4));
        provider.GetRequiredService<MosmixSForecastOptions>().MaxRunAge.Should().Be(TimeSpan.FromHours(2));
        provider.GetRequiredService<ILogger<ConfigurationRegistrarTests>>().Should().NotBeNull();
    }

    [TestMethod]
    public void Map_EmptyConfiguration_LibraryDefaults()
    {
        var configuration = new CliConfiguration();

        var provider = Register(configuration, OutputFormat.Json);

        provider.GetRequiredService<CdcCatalogEndpoints>().Should().NotBeNull();
        provider.GetRequiredService<DwdObservationOptions>().Should().NotBeNull();
        provider.GetRequiredService<OsthessenWetterOptions>().Should().NotBeNull();
        provider.GetRequiredService<OsthessenWetterEndpoints>().Should().NotBeNull();
        provider.GetRequiredService<MosmixCatalogEndpoints>().Should().NotBeNull();
        provider.GetRequiredService<MosmixLForecastEndpoints>().Should().NotBeNull();
        provider.GetRequiredService<MosmixLForecastOptions>().Should().NotBeNull();
        provider.GetRequiredService<MosmixSForecastEndpoints>().Should().NotBeNull();
        provider.GetRequiredService<MosmixSForecastOptions>().Should().NotBeNull();
        provider.GetRequiredService<MqttTopicOptions>().Should().NotBeNull();

        provider.GetRequiredService<OsthessenWetterOptions>().PageDeadline.Should().Be(new OsthessenWetterOptions().PageDeadline);
        provider.GetRequiredService<MosmixLForecastOptions>().ParseDeadline.Should().Be(new MosmixLForecastOptions().ParseDeadline);
        provider.GetRequiredService<MosmixSForecastOptions>().ParseDeadline.Should().Be(new MosmixSForecastOptions().ParseDeadline);
        provider.GetRequiredService<OsthessenWetterOptions>().Stations.Should().BeEmpty();
        provider.GetRequiredService<MosmixLForecastOptions>().MaxRunAge.Should().BeNull();
    }

    [TestMethod]
    public void Map_JsonMode_EmptyTopicOptions()
    {
        var configuration = new CliConfiguration
        {
            Mqtt = new MqttSection
            {
                Topics = new TopicsSection
                {
                    Observation = new TopicSection { TopicTemplate = "a/{source}" }
                }
            }
        };

        var provider = Register(configuration, OutputFormat.Json);

        provider.GetRequiredService<MqttTopicOptions>().Mappings.Should().BeEmpty();
        provider.GetRequiredService<MqttTopicOptions>().MaxPayloadBytes.Should().BeNull();
    }

    [TestMethod]
    public void Map_TopicTemplates_ToMqttTopicOptions()
    {
        var configuration = new CliConfiguration
        {
            Mqtt = new MqttSection
            {
                MaxPayloadBytes = 4096,
                Topics = new TopicsSection
                {
                    Observation = new TopicSection { TopicTemplate = "haus/weather/measurements/{source}/state" },
                    CdcStationList = new TopicSection { TopicTemplate = "haus/weather/stations/dwd/selection" }
                }
            }
        };

        var provider = Register(configuration, OutputFormat.Mqtt);

        var options = provider.GetRequiredService<MqttTopicOptions>();
        options.MaxPayloadBytes.Should().Be(4096);
        options.Mappings.Should().ContainKey(MqttMessageType.Observation)
            .WhoseValue.TopicTemplate.Should().Be("haus/weather/measurements/{source}/state");
        options.Mappings.Should().ContainKey(MqttMessageType.CdcStationList)
            .WhoseValue.TopicTemplate.Should().Be("haus/weather/stations/dwd/selection");
        options.Mappings[MqttMessageType.Observation].QualityOfService.Should().BeNull();
        options.Mappings[MqttMessageType.Observation].Retain.Should().BeNull();
        options.Mappings[MqttMessageType.CdcStationList].QualityOfService.Should().BeNull();
        options.Mappings[MqttMessageType.CdcStationList].Retain.Should().BeNull();
    }

    [TestMethod]
    public void Map_AllFourTopics()
    {
        var configuration = new CliConfiguration
        {
            Mqtt = new MqttSection
            {
                Topics = new TopicsSection
                {
                    Observation = new TopicSection { TopicTemplate = "obs/{source}" },
                    Forecast = new TopicSection { TopicTemplate = "fc/{source}" },
                    CdcStationList = new TopicSection { TopicTemplate = "stations/dwd" },
                    MosmixLocationList = new TopicSection { TopicTemplate = "locations/mosmix" }
                }
            }
        };

        var provider = Register(configuration, OutputFormat.Mqtt);

        var options = provider.GetRequiredService<MqttTopicOptions>();
        options.Mappings.Should().HaveCount(4);
        options.Mappings.Should().ContainKey(MqttMessageType.Observation)
            .WhoseValue.TopicTemplate.Should().Be("obs/{source}");
        options.Mappings.Should().ContainKey(MqttMessageType.Forecast)
            .WhoseValue.TopicTemplate.Should().Be("fc/{source}");
        options.Mappings.Should().ContainKey(MqttMessageType.CdcStationList)
            .WhoseValue.TopicTemplate.Should().Be("stations/dwd");
        options.Mappings.Should().ContainKey(MqttMessageType.MosmixLocationList)
            .WhoseValue.TopicTemplate.Should().Be("locations/mosmix");
    }
}
