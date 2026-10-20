using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents the measurements collected for an observation.
/// </summary>
[Serializable]
public sealed class ObservationMeasurements
{
    /// <summary>
    /// Gets or sets the temperature measurement in Celsius. Omitted from both formats when the reading is missing.
    /// </summary>
    [XmlElement("temperature_c")]
    [JsonPropertyName("temperature_c")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeasurementValue? TemperatureC { get; set; }

    /// <summary>
    /// Gets or sets the pressure measurement in hectopascals.
    /// </summary>
    [XmlElement("pressure_hpa")]
    [JsonPropertyName("pressure_hpa")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PressureMeasurementValue? PressureHpa { get; set; }

    /// <summary>
    /// Gets or sets the pressure measurement in millimeters of mercury.
    /// </summary>
    [XmlElement("pressure_mmhg")]
    [JsonPropertyName("pressure_mmhg")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PressureMeasurementValue? PressureMmHg { get; set; }

    /// <summary>
    /// Gets or sets the humidity percentage.
    /// </summary>
    [XmlElement("humidity_percent")]
    [JsonPropertyName("humidity_percent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeasurementValue? HumidityPercent { get; set; }

    /// <summary>
    /// Gets or sets the wind direction in degrees.
    /// </summary>
    [XmlElement("wind_direction_deg")]
    [JsonPropertyName("wind_direction_deg")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeasurementValue? WindDirectionDeg { get; set; }

    /// <summary>
    /// Gets or sets the wind speed in kilometers per hour.
    /// </summary>
    [XmlElement("wind_speed_kmh")]
    [JsonPropertyName("wind_speed_kmh")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeasurementValue? WindSpeedKmh { get; set; }

    /// <summary>
    /// Gets or sets the illuminance in lux.
    /// </summary>
    [XmlElement("illuminance_lux")]
    [JsonPropertyName("illuminance_lux")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeasurementValue? IlluminanceLux { get; set; }

    /// <summary>
    /// Gets or sets the solar irradiance in watts per square meter.
    /// </summary>
    [XmlElement("solar_irradiance_w_m2")]
    [JsonPropertyName("solar_irradiance_w_m2")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeasurementValue? SolarIrradianceWm2 { get; set; }

    /// <summary>
    /// Gets or sets the rain rate in millimeters per hour.
    /// </summary>
    [XmlElement("rain_rate_mm_h")]
    [JsonPropertyName("rain_rate_mm_h")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeasurementValue? RainRateMmH { get; set; }
}
