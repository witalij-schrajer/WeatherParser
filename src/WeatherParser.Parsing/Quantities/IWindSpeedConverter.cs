namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Converts wind speeds from metres per second to kilometres per hour.
/// </summary>
public interface IWindSpeedConverter
{
    /// <summary>
    /// Converts a wind speed from m/s to km/h with the exact factor 3.6; the result is unrounded (round with IOutputRounding afterwards).
    /// </summary>
    /// <param name="metresPerSecond">The wind speed in metres per second.</param>
    /// <returns>The wind speed in kilometres per hour, unrounded.</returns>
    decimal ToKilometresPerHour(decimal metresPerSecond);
}