namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Converts pressure values between hectopascals and millimetres of mercury.
/// </summary>
public interface IPressureConverter
{
    /// <summary>
    /// Converts a pressure value in hectopascals to both hectopascals and millimetres of mercury.
    /// </summary>
    /// <param name="hpa">The pressure value in hectopascals.</param>
    /// <param name="reference">The reference level for the pressure value.</param>
    /// <returns>A record containing both rounded outputs derived from one unrounded original.</returns>
    PressureValues FromHpa(decimal hpa, PressureReference reference);

    /// <summary>
    /// Converts a pressure value in millimetres of mercury to both hectopascals and millimetres of mercury.
    /// </summary>
    /// <param name="mmHg">The pressure value in millimetres of mercury.</param>
    /// <param name="reference">The reference level for the pressure value.</param>
    /// <returns>A record containing both rounded outputs derived from one unrounded original.</returns>
    PressureValues FromMmHg(decimal mmHg, PressureReference reference);
}
