namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Scales MOSMIX source units to the units the forecast-run builder expects; scaling only, never rounding (REQ-057, contracts MF-10, MF-11).
/// </summary>
public interface IMosmixUnitScaler
{
    /// <summary>
    /// Converts a temperature in Kelvin to Celsius.
    /// </summary>
    /// <param name="kelvin">The temperature in Kelvin.</param>
    /// <returns>The temperature in Celsius.</returns>
    double KelvinToCelsius(double kelvin);

    /// <summary>
    /// Converts a pressure in Pascal to Hectopascal.
    /// </summary>
    /// <param name="pascal">The pressure in Pascal.</param>
    /// <returns>The pressure in Hectopascal.</returns>
    double PascalToHectopascal(double pascal);

    /// <summary>
    /// Converts an energy in Kilojoules to Joules.
    /// </summary>
    /// <param name="kilojoules">The energy in Kilojoules.</param>
    /// <returns>The energy in Joules.</returns>
    double KilojoulesToJoules(double kilojoules);

    /// <summary>
    /// Converts a precipitation accumulation in kilograms per square meter to millimeters.
    /// </summary>
    /// <param name="kilogramsPerSquareMetre">The precipitation accumulation in kilograms per square meter.</param>
    /// <returns>The precipitation accumulation in millimeters.</returns>
    double KilogramsPerSquareMetreToMillimetres(double kilogramsPerSquareMetre);
}
