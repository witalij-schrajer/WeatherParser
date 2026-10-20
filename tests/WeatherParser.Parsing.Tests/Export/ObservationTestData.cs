using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Export;

/// <summary>
/// Builds observation datasets that are complete under the export contract, so tests change only the member they examine.
/// </summary>
internal static class ObservationTestData
{
    public static readonly DateTime MeasuredAtUtc = new(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime RetrievedAtUtc = new(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc);

    public static ObservationDataset ValidDataset() => new()
    {
        Source = "dwd:00044",
        Location = new ObservationLocation { StationId = "00044", Name = "Großenkneten", Latitude = 52.9336m, Longitude = 8.2370m, AltitudeM = 44m },
        MeasuredAtUtc = MeasuredAtUtc,
        RetrievedAtUtc = RetrievedAtUtc,
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
}
