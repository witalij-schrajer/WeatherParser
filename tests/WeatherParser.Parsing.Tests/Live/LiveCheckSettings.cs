using WeatherParser.Parsing.Export;
using System;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;

namespace WeatherParser.Parsing.Tests.Live;

internal sealed record LiveCheckSettings(CdcCatalogEndpoints Cdc, MosmixCatalogEndpoints Mosmix, SearchParameters Parameters, bool RefreshFirst = true)
{
    public int RequestBudget => 1 + 1 + Mosmix.MaxEvidenceCandidates + 4 + (4 * Cdc.MaxEvidenceCandidates);

    public const int ObservationRequestBudget = 8; // Four CDC station lists and at most four now archives (contract OC-13).

    public const int OsthessenRequestBudget = 1; // One request for the measurement page (contract OW-15).

    public const int MosmixRequestBudget = 1; // One request per product and run (contract MF-17).

    public string ObservationStationId { get; init; } = "01526"; // Fulda-Horas, the nearest CDC station of the P4 live check.

    public Uri OsthessenPageUri { get; init; } = new("https://osthessen-wetter.de/messwerte/");

    public string OsthessenPattern { get; init; } = "*";

    public string MosmixId { get; init; } = "10637";

    public Uri MosmixLSingleStationsDirectory { get; init; } = new("https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/");

    public Uri MosmixSAllStationsUri { get; init; } = new("https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz");

    public TimeSpan MosmixLMaxRunAge { get; init; } = TimeSpan.FromHours(8);

    public TimeSpan MosmixSMaxRunAge { get; init; } = TimeSpan.FromHours(2);

    public static LiveCheckSettings Dwd(string evidenceCacheDirectory)
    {
        const string Cdc = "https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/";

        var cdc = new CdcCatalogEndpoints
        {
            AirTemperatureStationList = new Uri(Cdc + "air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt"),
            AirTemperatureNowDirectory = new Uri(Cdc + "air_temperature/now/"),
            WindStationList = new Uri(Cdc + "wind/now/zehn_now_ff_Beschreibung_Stationen.txt"),
            WindNowDirectory = new Uri(Cdc + "wind/now/"),
            PrecipitationStationList = new Uri(Cdc + "precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt"),
            PrecipitationNowDirectory = new Uri(Cdc + "precipitation/now/"),
            SolarStationList = new Uri(Cdc + "solar/now/zehn_now_sd_Beschreibung_Stationen.txt"),
            SolarNowDirectory = new Uri(Cdc + "solar/now/"),
            MaxEvidenceCandidates = 10,
            SelectionDeadline = TimeSpan.FromSeconds(120)
        };

        var mosmix = new MosmixCatalogEndpoints
        {
            CatalogUri = new Uri("https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_stationskatalog.cfg?view=nasPublication&nn=16102"),
            MosmixLSingleStationsDirectory = new Uri("https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/"),
            MosmixSAllStationsUri = new Uri("https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz"),
            EvidenceCacheDirectory = evidenceCacheDirectory,
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
            MosmixSRefreshDeadline = TimeSpan.FromMinutes(10),
            MaxEvidenceCandidates = 10,
            SelectionDeadline = TimeSpan.FromSeconds(120)
        };

        var parameters = new SearchParameters
        {
            Latitude = 50.55m,
            Longitude = 9.68m,
            AltitudeM = 280m,
            MaxDistanceKm = 15m,
            MaxAltitudeDifferenceM = 300m
        };

        return new LiveCheckSettings(cdc, mosmix, parameters);
    }
}
