namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Reference level of an air pressure value (REQ-054).
/// </summary>
public enum PressureReference
{
    /// <summary>
    /// Pressure at sensor/station level.
    /// </summary>
    Sensor,

    /// <summary>
    /// Pressure reduced to mean sea level.
    /// </summary>
    SeaLevel,

    /// <summary>
    /// Reference not verified.
    /// </summary>
    Unknown
}
