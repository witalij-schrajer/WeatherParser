using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Time;
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

    /// <summary>
    /// Creates a valid synthetic observation dataset.
    /// </summary>
    public static ObservationDataset Observation(string provider, string stationId)
    {
        var measuredAt = new DateTime(2026, 10, 30, 5, 50, 0, DateTimeKind.Utc);
        return new ObservationDataset
        {
            Source = provider + ":" + stationId,
            Location = new ObservationLocation { StationId = stationId, Name = "Station Ö & <" + stationId + ">", Latitude = 50.5m, Longitude = 9.7m },
            MeasuredAtUtc = measuredAt,
            RetrievedAtUtc = RetrievedAt,
            TimestampQuality = TimestampQuality.Source,
            Measurements = new ObservationMeasurements
            {
                TemperatureC = new MeasurementValue { Value = 0.0m, MeasuredAtUtc = measuredAt, TimestampQuality = TimestampQuality.Source },
                HumidityPercent = new MeasurementValue { Value = 87m, MeasuredAtUtc = measuredAt, TimestampQuality = TimestampQuality.Source }
            }
        };
    }

    /// <summary>
    /// Creates a valid synthetic forecast run.
    /// </summary>
    public static ForecastRun Forecast(MosmixProduct product, string mosmixId)
    {
        return new ForecastRun
        {
            Source = (product == MosmixProduct.MosmixS ? "dwd-mosmix-s:" : "dwd-mosmix-l:") + mosmixId,
            MosmixId = mosmixId,
            Location = new ForecastLocation { Name = "Ort Ü", Latitude = 50.5m, Longitude = 9.7m },
            Product = product,
            IssuedAtUtc = new DateTime(2026, 10, 30, 3, 0, 0, DateTimeKind.Utc),
            RetrievedAtUtc = RetrievedAt,
            Forecasts = new List<ForecastStep>
            {
                new() { ValidAtUtc = new DateTime(2026, 10, 30, 7, 0, 0, DateTimeKind.Utc), Values = new ForecastValues { TemperatureC = 12.3m } },
                new() { ValidAtUtc = new DateTime(2026, 10, 30, 8, 0, 0, DateTimeKind.Utc), Values = new ForecastValues { TemperatureC = 0.0m } }
            }
        };
    }
}
