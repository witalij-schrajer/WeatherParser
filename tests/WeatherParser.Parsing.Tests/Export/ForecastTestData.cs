using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Export;

/// <summary>
/// Builds forecast runs that are complete and valid under the export contract, so tests change only the member they examine.
/// </summary>
internal static class ForecastTestData
{
    public static readonly DateTime IssuedAtUtc = new(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime RetrievedAtUtc = new(2026, 7, 15, 10, 12, 0, DateTimeKind.Utc);

    public static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);

    public static ForecastRun ValidRun() => new()
    {
        Source = "dwd-mosmix-l:10637",
        MosmixId = "10637",
        Location = new ForecastLocation { Name = "FRANKFURT/M", Latitude = 50.05m, Longitude = 8.6m, AltitudeM = 111m },
        Product = MosmixProduct.MosmixL,
        IssuedAtUtc = IssuedAtUtc,
        RetrievedAtUtc = RetrievedAtUtc,
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
}