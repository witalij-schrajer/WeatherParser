namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Converts wind speeds from m/s to km/h with the exact factor 3.6, unrounded (contract C-46).
/// </summary>
public sealed class WindSpeedConverter : IWindSpeedConverter
{
    /// <summary>Exact factor m/s → km/h (3600 s/h ÷ 1000 m/km).</summary>
    private static readonly decimal MetresPerSecondToKilometresPerHour = 3.6m;

    /// <inheritdoc />
    public decimal ToKilometresPerHour(decimal metresPerSecond)
    {
        return metresPerSecond * MetresPerSecondToKilometresPerHour;
    }
}
