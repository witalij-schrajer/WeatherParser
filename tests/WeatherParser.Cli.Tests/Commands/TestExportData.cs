using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using System.Collections.Generic;
using System.Linq;

namespace WeatherParser.Cli.Tests.Commands;

internal static class TestExportData
{
    public static SearchParameters Search() => new SearchParameterFactory().Create(50.5m, 9.7m, 300m, 50m, 200m);

    public static readonly DateTime RetrievedAt = new(2026, 10, 30, 6, 0, 0, DateTimeKind.Utc);

    public static CdcStationList CdcStations(params string[] stationIds)
    {
        return new CdcStationList
        {
            Source = "dwd:cdc-10min-now",
            RetrievedAtUtc = RetrievedAt,
            SearchParameters = Search(),
            Stations = stationIds.Select((id, index) => new CdcStationListEntry
            {
                StationId = id,
                Name = "Station Ä & <" + index + ">",
                Latitude = 50.5m,
                Longitude = 9.7m,
                AltitudeM = 300m,
                DistanceKm = index,
                AltitudeDifferenceM = 0m,
                AvailableMeasurements = new List<string> { "air_temperature" }
            }).ToList()
        };
    }

    public static MosmixLocationList MosmixLocations(params string[] mosmixIds)
    {
        return new MosmixLocationList
        {
            Source = "dwd:mosmix-stationskatalog",
            RetrievedAtUtc = RetrievedAt,
            SearchParameters = Search(),
            Locations = mosmixIds.Select((id, index) => new MosmixLocationListEntry
            {
                MosmixId = id,
                Name = "Ort Ü " + index,
                Latitude = 50.5m,
                Longitude = 9.7m,
                AltitudeM = 300m,
                DistanceKm = index,
                AltitudeDifferenceM = 0m,
                Products = new List<MosmixProductAvailability>
                {
                    new()
                    {
                        Product = MosmixProduct.MosmixL,
                        Availability = AvailabilityState.Available,
                        AvailableForecastElements = new List<string> { "TTT" }
                    }
                }
            }).ToList()
        };
    }
}
