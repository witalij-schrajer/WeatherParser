using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Forecasts.Mosmix;

namespace WeatherParser.Parsing.Tests;

/// <summary>
/// Registers what a consumer must register next to AddWeatherParsing (SC-08, SC-09, SC-12, SC-13): logging (a no-op logger here), an HttpClient that is never used to send in these tests, the system TimeProvider and valid CDC and MOSMIX catalog endpoint options with placeholder URIs and the default DWD observation options, OsthessenWetter options with one synthetic station and a placeholder OsthessenWetter page endpoint, and placeholder MOSMIX_L and MOSMIX_S forecast endpoints with valid forecast options.
/// Tests that observe log calls use strict mocks instead.
/// </summary>
internal static class TestConsumerServices
{
    public static IServiceCollection AddTestConsumerServices(this IServiceCollection services)
    {
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(new HttpClient());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(new MosmixCatalogEndpoints
        {
            CatalogUri = new Uri("https://example.org/mosmix/stations.cfg"),
            MosmixLSingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/"),
            MosmixSAllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz"),
            EvidenceCacheDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-test-evidence"),
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
        });
        services.AddSingleton(new CdcCatalogEndpoints
        {
            AirTemperatureStationList = new Uri("https://example.org/cdc/air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt"),
            AirTemperatureNowDirectory = new Uri("https://example.org/cdc/air_temperature/now/"),
            WindStationList = new Uri("https://example.org/cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt"),
            WindNowDirectory = new Uri("https://example.org/cdc/wind/now/"),
            PrecipitationStationList = new Uri("https://example.org/cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt"),
            PrecipitationNowDirectory = new Uri("https://example.org/cdc/precipitation/now/"),
            SolarStationList = new Uri("https://example.org/cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt"),
            SolarNowDirectory = new Uri("https://example.org/cdc/solar/now/"),
        });
        services.AddSingleton(new DwdObservationOptions());
        services.AddSingleton(new OsthessenWetterOptions { Stations = { new OsthessenWetterStationMetadata { Name = "Teststation", Latitude = 50.5m, Longitude = 9.7m } } });
        services.AddSingleton(new OsthessenWetterEndpoints { PageUri = new Uri("https://osthessen.example/messwerte/") });
        services.AddSingleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://mosmix.example/single_stations/") });
        services.AddSingleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://mosmix.example/MOSMIX_S_LATEST_240.kmz") });
        services.AddSingleton(new MosmixLForecastOptions { MaxRunAge = TimeSpan.FromHours(8) });
        services.AddSingleton(new MosmixSForecastOptions { MaxRunAge = TimeSpan.FromHours(2) });
        return services;
    }
}
