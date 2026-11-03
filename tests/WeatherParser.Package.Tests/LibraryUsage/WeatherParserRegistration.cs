using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;

namespace MyWeatherApp;

/// <summary>
/// Registers WeatherParser and every endpoint and option type it needs. AddWeatherParsing registers the services of all sources, and container validation checks all of them, so every type below is required even when the application uses only one source.
/// </summary>
public static class WeatherParserRegistration
{
    /// <summary>
    /// Registers WeatherParser with the public DWD endpoints.
    /// </summary>
    /// <param name="services">The service collection to register the services with.</param>
    /// <param name="httpClient">The HTTP client owned by the application; the container does not dispose it.</param>
    /// <param name="timeProvider">TimeProvider.System in production.</param>
    /// <param name="evidenceCacheDirectory">An existing, writable absolute directory used only by the MOSMIX_S evidence cache.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddMyWeatherParsing(this IServiceCollection services, HttpClient httpClient, TimeProvider timeProvider, string evidenceCacheDirectory)
    {
        services.AddWeatherParsing();

        // Application-owned infrastructure; the library registers neither. Register ILogger<T> with AddLogging in the host.
        services.AddSingleton(httpClient);
        services.AddSingleton(timeProvider);

        // DWD 10-minute observations and CDC station selection.
        const string cdc = "https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/";
        services.AddSingleton(new CdcCatalogEndpoints
        {
            AirTemperatureStationList = new Uri(cdc + "air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt"),
            AirTemperatureNowDirectory = new Uri(cdc + "air_temperature/now/"),
            WindStationList = new Uri(cdc + "wind/now/zehn_now_ff_Beschreibung_Stationen.txt"),
            WindNowDirectory = new Uri(cdc + "wind/now/"),
            PrecipitationStationList = new Uri(cdc + "precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt"),
            PrecipitationNowDirectory = new Uri(cdc + "precipitation/now/"),
            SolarStationList = new Uri(cdc + "solar/now/zehn_now_sd_Beschreibung_Stationen.txt"),
            SolarNowDirectory = new Uri(cdc + "solar/now/"),
        });
        services.AddSingleton(new DwdObservationOptions { ObservationDeadline = TimeSpan.FromMinutes(2) });

        // MOSMIX location selection.
        const string mosmix = "https://opendata.dwd.de/weather/local_forecasts/mos/";
        services.AddSingleton(new MosmixCatalogEndpoints
        {
            CatalogUri = new Uri("https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_stationskatalog.cfg?view=nasPublication&nn=16102"),
            MosmixLSingleStationsDirectory = new Uri(mosmix + "MOSMIX_L/single_stations/"),
            MosmixSAllStationsUri = new Uri(mosmix + "MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz"),
            EvidenceCacheDirectory = evidenceCacheDirectory,
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
        });

        // MOSMIX_L and MOSMIX_S forecasts; MaxRunAge has no default.
        services.AddSingleton(new MosmixLForecastEndpoints
        {
            SingleStationsDirectory = new Uri(mosmix + "MOSMIX_L/single_stations/"),
        });
        services.AddSingleton(new MosmixLForecastOptions
        {
            ParseDeadline = TimeSpan.FromSeconds(60),
            MaxRunAge = TimeSpan.FromHours(8),
        });
        services.AddSingleton(new MosmixSForecastEndpoints
        {
            AllStationsUri = new Uri(mosmix + "MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz"),
        });
        services.AddSingleton(new MosmixSForecastOptions
        {
            ParseDeadline = TimeSpan.FromMinutes(5),
            MaxRunAge = TimeSpan.FromHours(2),
        });

        // OsthessenWetter observations: the values must not be redistributed until the source conditions are clarified. Add one Stations entry per station you parse.
        services.AddSingleton(new OsthessenWetterEndpoints
        {
            PageUri = new Uri("https://osthessen-wetter.de/messwerte/"),
        });
        services.AddSingleton(new OsthessenWetterOptions());

        // MQTT messages: add one mapping per message type you publish; without mappings every serialization fails with MQTT-MAPPING-MISSING.
        services.AddSingleton(new MqttTopicOptions());

        return services;
    }
}
