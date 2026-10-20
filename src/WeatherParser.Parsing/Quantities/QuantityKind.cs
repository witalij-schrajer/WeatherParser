namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Kind of a physical quantity; each kind has one meaning and unit (contract C-37).
/// </summary>
public enum QuantityKind
{
    /// <summary>
    /// Air temperature, °C
    /// </summary>
    Temperature,

    /// <summary>
    /// Air pressure, hPa or mmHg
    /// </summary>
    Pressure,

    /// <summary>
    /// Relative humidity, percent 0–100
    /// </summary>
    RelativeHumidity,

    /// <summary>
    /// Meteorological origin direction in degrees, 0 = north
    /// </summary>
    WindDirection,

    /// <summary>
    /// Mean wind speed, km/h
    /// </summary>
    MeanWindSpeed,

    /// <summary>
    /// Wind gust speed, km/h
    /// </summary>
    WindGust,

    /// <summary>
    /// Illuminance, lux
    /// </summary>
    Illuminance,

    /// <summary>
    /// Solar power per area, W/m²
    /// </summary>
    Irradiance,

    /// <summary>
    /// Radiation energy per area over a period, J/m²
    /// </summary>
    RadiationEnergy,

    /// <summary>
    /// Rain intensity, mm/h
    /// </summary>
    PrecipitationRate,

    /// <summary>
    /// Precipitation amount over a period, mm
    /// </summary>
    PrecipitationAccumulation,

    /// <summary>
    /// Probability, percent 0–100
    /// </summary>
    Probability
}
