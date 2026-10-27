namespace WeatherParser.Parsing.Forecasts.Mosmix;

using System.Globalization;

/// <summary>
/// Scales MOSMIX source units to the units the forecast-run builder expects; scaling only, never rounding (REQ-057, contracts MF-10, MF-11).
/// </summary>
public sealed class MosmixUnitScaler : IMosmixUnitScaler
{
    /// <inheritdoc />
    public double KelvinToCelsius(double kelvin)
    {
        return (double)(Finite(kelvin, nameof(kelvin)) - 273.15m);
    }

    /// <inheritdoc />
    public double PascalToHectopascal(double pascal)
    {
        return (double)(Finite(pascal, nameof(pascal)) / 100m);
    }

    /// <inheritdoc />
    public double KilojoulesToJoules(double kilojoules)
    {
        return (double)(Finite(kilojoules, nameof(kilojoules)) * 1000m);
    }

    /// <inheritdoc />
    public double KilogramsPerSquareMetreToMillimetres(double kilogramsPerSquareMetre)
    {
        return (double)Finite(kilogramsPerSquareMetre, nameof(kilogramsPerSquareMetre));
    }

    private static decimal Finite(double value, string paramName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "The value must be finite.");
        }

        return (decimal)value;
    }
}
