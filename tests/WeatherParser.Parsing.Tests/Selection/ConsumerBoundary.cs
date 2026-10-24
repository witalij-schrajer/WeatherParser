using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;

namespace WeatherParser.Parsing.Tests.Selection;

internal static class ConsumerBoundary
{
    public static CdcCatalogEndpoints CdcOptions(string baseUri) => new()
    {
        AirTemperatureStationList = new Uri(baseUri + "cdc/air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt?version=2"),
        AirTemperatureNowDirectory = new Uri(baseUri + "cdc/air_temperature/now/"),
        WindStationList = new Uri(baseUri + "cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt?version=2"),
        WindNowDirectory = new Uri(baseUri + "cdc/wind/now/"),
        PrecipitationStationList = new Uri(baseUri + "cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt?version=2"),
        PrecipitationNowDirectory = new Uri(baseUri + "cdc/precipitation/now/"),
        SolarStationList = new Uri(baseUri + "cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt?version=2"),
        SolarNowDirectory = new Uri(baseUri + "cdc/solar/now/"),
        MaxEvidenceCandidates = 100,
        SelectionDeadline = TimeSpan.FromSeconds(120)
    };

    public static MosmixCatalogEndpoints MosmixOptions(string baseUri, string evidenceDirectory) => new()
    {
        CatalogUri = new Uri(baseUri + "mosmix/mosmix_stationskatalog.cfg?view=nasPublication"),
        MosmixLSingleStationsDirectory = new Uri(baseUri + "mosmix/single_stations/"),
        MosmixSAllStationsUri = new Uri(baseUri + "mosmix/all_stations/kml/MOSMIX_S_LATEST_240.kmz"),
        EvidenceCacheDirectory = evidenceDirectory,
        MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
        MosmixSRefreshDeadline = TimeSpan.FromMinutes(10),
        MaxEvidenceCandidates = 100,
        SelectionDeadline = TimeSpan.FromSeconds(120)
    };

    public static IServiceCollection AddConsumerBoundary(
        this IServiceCollection services,
        CdcCatalogEndpoints cdc,
        MosmixCatalogEndpoints mosmix,
        HttpMessageHandler handler,
        TimeProvider clock)
    {
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(new HttpClient(handler));
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(cdc);
        services.AddSingleton(mosmix);
        return services;
    }

    public static ServiceProvider Build(IServiceCollection services) => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
}
