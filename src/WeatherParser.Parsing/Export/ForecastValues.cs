using System.Text.Json.Serialization;
using System.Xml.Serialization;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents forecast values.
/// </summary>
[Serializable]
public sealed class ForecastValues
{
    /// <summary>
    /// Gets or sets the temperature in Celsius.
    /// </summary>
    [XmlElement("temperature_c")]
    [JsonPropertyName("temperature_c")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TemperatureC { get; set; }

    /// <summary>
    /// Gets or sets the pressure in hectopascals.
    /// </summary>
    [XmlElement("pressure_hpa")]
    [JsonPropertyName("pressure_hpa")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? PressureHpa { get; set; }

    /// <summary>
    /// Gets or sets the pressure in millimeters of mercury.
    /// </summary>
    [XmlElement("pressure_mmhg")]
    [JsonPropertyName("pressure_mmhg")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? PressureMmHg { get; set; }

    private PressureReference? _pressureReference;

    /// <summary>
    /// Gets or sets the reference level of the pressure value.
    /// Reading an unassigned value throws, so no default is exported.
    /// Undefined enum values are rejected.
    /// </summary>
    [XmlElement("pressure_reference")]
    [JsonPropertyName("pressure_reference")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PressureReference? PressureReference
    {
        get => _pressureReference;
        set
        {
            if (value is not null && !Enum.IsDefined(value.Value))
            {
                throw new ArgumentException($"Undefined pressure_reference value '{value}'.", nameof(PressureReference));
            }

            _pressureReference = value;
        }
    }

    /// <summary>
    /// Gets or sets the humidity in percent.
    /// </summary>
    [XmlElement("humidity_percent")]
    [JsonPropertyName("humidity_percent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? HumidityPercent { get; set; }

    /// <summary>
    /// Gets or sets the wind direction in degrees.
    /// </summary>
    [XmlElement("wind_direction_deg")]
    [JsonPropertyName("wind_direction_deg")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? WindDirectionDeg { get; set; }

    /// <summary>
    /// Gets or sets the wind speed in kilometers per hour.
    /// </summary>
    [XmlElement("wind_speed_kmh")]
    [JsonPropertyName("wind_speed_kmh")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? WindSpeedKmh { get; set; }

    /// <summary>
    /// Gets or sets the illuminance in lux.
    /// </summary>
    [XmlElement("illuminance_lux")]
    [JsonPropertyName("illuminance_lux")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? IlluminanceLux { get; set; }

    /// <summary>
    /// Gets or sets the solar irradiance in watts per square meter.
    /// </summary>
    [XmlElement("solar_irradiance_w_m2")]
    [JsonPropertyName("solar_irradiance_w_m2")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IntervalValue? SolarIrradianceWm2 { get; set; }

    /// <summary>
    /// Gets or sets the rain rate in millimeters.
    /// </summary>
    [XmlElement("rain_rate_mm_h")]
    [JsonPropertyName("rain_rate_mm_h")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IntervalValue? RainRateMmH { get; set; }

    /// <summary>
    /// Gets or sets the maximum wind gust over the period in km/h (aggregation maximum, contract MF-11).
    /// </summary>
    [XmlElement("wind_gust_kmh")]
    [JsonPropertyName("wind_gust_kmh")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IntervalValue? WindGustKmh { get; set; }

    /// <summary>
    /// Gets or sets the precipitation total over the period in millimetres (aggregation sum, contract MF-11).
    /// </summary>
    [XmlElement("precipitation_amount_mm")]
    [JsonPropertyName("precipitation_amount_mm")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IntervalValue? PrecipitationAmountMm { get; set; }

    /// <summary>
    /// Controls XML output of temperature_c; true exactly when TemperatureC has a value.
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public bool TemperatureCSpecified
    {
        get => TemperatureC.HasValue;
        set
        {
            if (!value)
            {
                TemperatureC = null;
            }
        }
    }

    /// <summary>
    /// Controls XML output of pressure_hpa; true exactly when PressureHpa has a value.
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public bool PressureHpaSpecified
    {
        get => PressureHpa.HasValue;
        set
        {
            if (!value)
            {
                PressureHpa = null;
            }
        }
    }

    /// <summary>
    /// Controls XML output of pressure_mmhg; true exactly when PressureMmHg has a value.
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public bool PressureMmHgSpecified
    {
        get => PressureMmHg.HasValue;
        set
        {
            if (!value)
            {
                PressureMmHg = null;
            }
        }
    }

    /// <summary>
    /// Controls XML output of pressure_reference; true exactly when PressureReference has a value.
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public bool PressureReferenceSpecified
    {
        get => PressureReference.HasValue;
        set
        {
            if (!value)
            {
                PressureReference = null;
            }
        }
    }

    /// <summary>
    /// Controls XML output of humidity_percent; true exactly when HumidityPercent has a value.
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public bool HumidityPercentSpecified
    {
        get => HumidityPercent.HasValue;
        set
        {
            if (!value)
            {
                HumidityPercent = null;
            }
        }
    }

    /// <summary>
    /// Controls XML output of wind_direction_deg; true exactly when WindDirectionDeg has a value.
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public bool WindDirectionDegSpecified
    {
        get => WindDirectionDeg.HasValue;
        set
        {
            if (!value)
            {
                WindDirectionDeg = null;
            }
        }
    }

    /// <summary>
    /// Controls XML output of wind_speed_kmh; true exactly when WindSpeedKmh has a value.
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public bool WindSpeedKmhSpecified
    {
        get => WindSpeedKmh.HasValue;
        set
        {
            if (!value)
            {
                WindSpeedKmh = null;
            }
        }
    }

    /// <summary>
    /// Controls XML output of illuminance_lux; true exactly when IlluminanceLux has a value.
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public bool IlluminanceLuxSpecified
    {
        get => IlluminanceLux.HasValue;
        set
        {
            if (!value)
            {
                IlluminanceLux = null;
            }
        }
    }
}
