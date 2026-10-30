using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Cli.Tests.Configuration;

internal static class TestConfigurations
{
    public static CliConfiguration Valid()
    {
        return new CliConfiguration
        {
            Dwd = new DwdSection
            {
                ReferenceStationId = "00044",
                Catalog = new CdcCatalogEndpoints
                {
                    AirTemperatureStationList = new Uri("https://example.invalid/cdc/air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt"),
                    AirTemperatureNowDirectory = new Uri("https://example.invalid/cdc/air_temperature/now/"),
                    WindStationList = new Uri("https://example.invalid/cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt"),
                    WindNowDirectory = new Uri("https://example.invalid/cdc/wind/now/"),
                    PrecipitationStationList = new Uri("https://example.invalid/cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt"),
                    PrecipitationNowDirectory = new Uri("https://example.invalid/cdc/precipitation/now/"),
                    SolarStationList = new Uri("https://example.invalid/cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt"),
                    SolarNowDirectory = new Uri("https://example.invalid/cdc/solar/now/")
                },
                Observation = new DwdObservationOptions()
            },
            OsthessenWetter = new OsthessenWetterSection
            {
                PageUri = new Uri("https://example.invalid/osthessen/"),
                Stations = new List<OsthessenWetterStationMetadata>
                {
                    new()
                    {
                        Name = "Teststation",
                        Latitude = 50.5m,
                        Longitude = 9.7m
                    }
                },
                PublicationPermitted = true
            },
            Mosmix = new MosmixSection
            {
                Catalog = new MosmixCatalogEndpoints
                {
                    CatalogUri = new Uri("https://example.invalid/mosmix/stations.cfg"),
                    MosmixLSingleStationsDirectory = new Uri("https://example.invalid/mosmix/single_stations/"),
                    MosmixSAllStationsUri = new Uri("https://example.invalid/mosmix/MOSMIX_S_LATEST_240.kmz"),
                    EvidenceCacheDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-cli-test-evidence"),
                    MosmixSEvidenceMaxAge = TimeSpan.FromHours(6)
                },
                L = new MosmixLSection
                {
                    SingleStationsDirectory = new Uri("https://example.invalid/mosmix/single_stations/"),
                    MaxRunAge = TimeSpan.FromHours(8)
                },
                S = new MosmixSSection
                {
                    AllStationsUri = new Uri("https://example.invalid/mosmix/MOSMIX_S_LATEST_240.kmz"),
                    MaxRunAge = TimeSpan.FromHours(2)
                }
            },
            Mqtt = new MqttSection
            {
                Broker = new BrokerSection { Host = "broker.example.invalid" },
                Topics = new TopicsSection
                {
                    Observation = new TopicSection { TopicTemplate = "haus/weather/measurements/{source}/state" },
                    Forecast = new TopicSection { TopicTemplate = "haus/weather/forecasts/{source}/state" },
                    CdcStationList = new TopicSection { TopicTemplate = "haus/weather/stations/dwd/selection" },
                    MosmixLocationList = new TopicSection { TopicTemplate = "haus/weather/stations/mosmix/selection" }
                }
            }
        };
    }

    public static CommandRequest Request(CliCommand command, OutputFormat output)
    {
        var common = new CommonOptions(output, null, null, false);
        return command switch
        {
            CliCommand.SelectDwd => new SelectDwdRequest(common, new SearchParameterFactory().Create(50.5m, 9.7m, 300m, 50m, 200m)),
            CliCommand.SelectMosmix => new SelectMosmixRequest(common, new SearchParameterFactory().Create(50.5m, 9.7m, 300m, 50m, 200m), false),
            CliCommand.ParseDwd => new ParseDwdRequest(common, "00044"),
            CliCommand.ParseOstHessen => new ParseOstHessenRequest(common, "Test*"),
            CliCommand.ParseMosmix => new ParseMosmixRequest(common, MosmixProduct.MosmixL, "10637"),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };
    }

    public static ServiceProvider Build(CliConfiguration configuration, CommandRequest request) => 
        CompositionRoot.Build(services => { new ConfigurationRegistrar().Register(services, configuration, request); services.AddLogging(); });
}
