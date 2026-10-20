namespace WeatherParser.Parsing.Quantities;

using System.Globalization;

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
        decimal result;
        try
        {
            result = metresPerSecond * MetresPerSecondToKilometresPerHour;
        }
        catch (OverflowException exception)
        {
            throw new QuantityConversionException($"Converting {metresPerSecond.ToString(CultureInfo.InvariantCulture)} m/s gives a value outside the decimal range (contract C-63).", exception);
        }

        return result;
    }
}
