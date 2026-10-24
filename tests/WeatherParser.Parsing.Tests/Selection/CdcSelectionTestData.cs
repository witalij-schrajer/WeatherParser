using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Selection;

namespace WeatherParser.Parsing.Tests.Selection;

internal static class CdcSelectionTestData
{
    public static readonly DateTimeOffset Reference = new(2026, 10, 21, 12, 34, 56, 789, TimeSpan.Zero);

    public static SearchParameters Parameters() => new()
    {
        Latitude = 50.5m,
        Longitude = 9.5m,
        AltitudeM = 300m,
        MaxDistanceKm = 25m,
        MaxAltitudeDifferenceM = 200m
    };

    public static CdcCandidateStation Station(string id, decimal? altitudeM, decimal latitude, params string[] products) => new(
        id,
        altitudeM,
        latitude,
        9.25m,
        "Name " + id,
        "Hessen",
        products);

    public static IReadOnlyList<CdcProductList> Lists(params CatalogDiagnostic[] diagnostics) => new[]
    {
        new CdcProductList("tu", "AirTemperatureStationList", new CdcStationCatalog(Array.Empty<CdcStationRecord>(), diagnostics))
    };

    public static CatalogDiagnostic Diagnostic(string id, string stationId) => new(
        id,
        "WindStationList",
        7,
        stationId,
        "Synthetic reason.");

    public static CdcStationSelector Create(
        CdcCatalogEndpoints options,
        ICdcCatalogSource source,
        ICdcAvailabilityDeriver deriver,
        ISearchAreaFilter filter,
        IListEntryOrdering ordering,
        TimeProvider clock,
        ILogger<CdcStationSelector> logger) => new(
            options,
            source,
            deriver,
            new SearchParameterFactory(),
            filter,
            ordering,
            clock,
            logger);
}
