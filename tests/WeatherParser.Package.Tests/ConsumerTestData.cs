using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Builds one valid graph of each of the five export roots through the public API of the package; the consumer cannot use the internal fixtures of the source-reference tests.
/// </summary>
internal static class ConsumerTestData
{
    public static readonly DateTime MeasuredAtUtc = new(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime ObservationRetrievedAtUtc = new(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc);
    public static readonly DateTime IssuedAtUtc = new(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime ForecastRetrievedAtUtc = new(2026, 7, 15, 10, 12, 0, DateTimeKind.Utc);
    public static readonly DateTime ListRetrievedAtUtc = new(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc);

    public static ObservationDataset ValidDataset() => new()
    {
        Source = "dwd:00044",
        Location = new ObservationLocation { StationId = "00044", Name = "Großenkneten", Latitude = 52.9336m, Longitude = 8.2370m, AltitudeM = 44m },
        MeasuredAtUtc = MeasuredAtUtc,
        RetrievedAtUtc = ObservationRetrievedAtUtc,
        TimestampQuality = TimestampQuality.Source,
        Measurements = new ObservationMeasurements
        {
            TemperatureC = Value(20.3m, MeasuredAtUtc),
            HumidityPercent = Value(55m, MeasuredAtUtc)
        }
    };

    public static MeasurementValue Value(decimal value, DateTime measuredAtUtc) => new()
    {
        Value = value,
        MeasuredAtUtc = measuredAtUtc,
        TimestampQuality = TimestampQuality.Source
    };

    public static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);

    public static ForecastRun ValidRun() => new()
    {
        Source = "dwd-mosmix-l:10637",
        MosmixId = "10637",
        Location = new ForecastLocation { Name = "FRANKFURT/M", Latitude = 50.05m, Longitude = 8.6m, AltitudeM = 111m },
        Product = MosmixProduct.MosmixL,
        IssuedAtUtc = IssuedAtUtc,
        RetrievedAtUtc = ForecastRetrievedAtUtc,
        Forecasts = new List<ForecastStep>
        {
            new() { ValidAtUtc = Utc(12, 0), Values = new ForecastValues { TemperatureC = 21.4m, PressureHpa = 1013m, PressureMmHg = 760m, PressureReference = PressureReference.SeaLevel, RainRateMmH = Interval(0.3m, Utc(11, 0), Utc(12, 0)) } },
            new() { ValidAtUtc = Utc(13, 0), Values = new ForecastValues { TemperatureC = 23.0m } }
        }
    };

    public static IntervalValue Interval(decimal value, DateTime startUtc, DateTime endUtc) => new()
    {
        Value = value,
        PeriodStartUtc = startUtc,
        PeriodEndUtc = endUtc,
        Aggregation = IntervalAggregation.Mean
    };

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
        RetrievedAtUtc = ListRetrievedAtUtc,
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
        RetrievedAtUtc = ListRetrievedAtUtc,
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

    public static ExportDocument ValidDocument() => new()
    {
        Observations = { ValidDataset() },
        ForecastRuns = { ValidRun() },
        Errors =
        {
            new ExportError
            {
                ErrorId = "DWD-RETRIEVAL-FAILED",
                Message = "Retrieval of station 01766 failed.",
                Source = "dwd:01766"
            }
        }
    };
}
