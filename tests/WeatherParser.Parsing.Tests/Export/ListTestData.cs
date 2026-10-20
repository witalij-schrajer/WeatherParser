using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Tests.Export;

/// <summary>
/// Builds CDC and MOSMIX lists that are complete under the export contract, so tests change only the member they examine.
/// </summary>
internal static class ListTestData
{
    public static readonly DateTime RetrievedAtUtc = new(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc);

    public static SearchParameters Search() => new()
    {
        Latitude = 50.5549m,
        Longitude = 9.6738m,
        AltitudeM = 250m,
        MaxDistanceKm = 30m,
        MaxAltitudeDifferenceM = 200m
    };

    public static CdcStationList ValidCdcList() => new()
    {
        Source = "dwd-cdc",
        RetrievedAtUtc = RetrievedAtUtc,
        SearchParameters = Search(),
        Stations =
        {
            new CdcStationListEntry
            {
                StationId = "00044",
                Name = "Fulda-Horas",
                Latitude = 50.567m,
                Longitude = 9.653m,
                AltitudeM = 242m,
                DistanceKm = 2.1m,
                AltitudeDifferenceM = -8m,
                AvailableMeasurements = { "temperature_c" }
            }
        }
    };

    public static MosmixLocationList ValidMosmixList() => new()
    {
        Source = "dwd-mosmix",
        RetrievedAtUtc = RetrievedAtUtc,
        SearchParameters = Search(),
        Locations =
        {
            new MosmixLocationListEntry
            {
                MosmixId = "X123A",
                Name = "FULDA",
                Latitude = 50.55m,
                Longitude = 9.67m,
                AltitudeM = 250m,
                DistanceKm = 0.5m,
                AltitudeDifferenceM = 0m,
                Products =
                {
                    new MosmixProductAvailability
                    {
                        Product = MosmixProduct.MosmixL,
                        Availability = AvailabilityState.Available,
                        AvailableForecastElements = { "TTT" }
                    },
                    new MosmixProductAvailability
                    {
                        Product = MosmixProduct.MosmixS,
                        Availability = AvailabilityState.Unknown,
                        AvailableForecastElements = { }
                    }
                }
            }
        }
    };
}
