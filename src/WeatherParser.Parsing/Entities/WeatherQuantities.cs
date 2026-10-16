using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// Shared set of normalized weather quantities defined in REQ-052/REQ-106, parameterized by the
/// per-quantity value type. Used as the base for <see cref="WeatherMeasurements"/>
/// (<typeparamref name="TValue"/> = <see cref="MeasuredValue"/>) and
/// <see cref="ForecastWeatherValues"/> (<typeparamref name="TValue"/> = <see cref="ForecastValue"/>).
/// Each quantity is optional and must be omitted, not defaulted to zero, when not available (REQ-053).
/// </summary>
/// <typeparam name="TValue">The per-quantity value type carrying the actual measurement or forecast value.</typeparam>
[Serializable]
public abstract class WeatherQuantities<TValue>
    where TValue : class
{
    /// <summary>Air temperature in degrees Celsius.</summary>
    [JsonPropertyName("temperature_c")]
    public TValue? Temperature { get; set; }

    /// <summary>Relative humidity in percent, 0-100.</summary>
    [JsonPropertyName("humidity_percent")]
    public TValue? Humidity { get; set; }

    /// <summary>Meteorological wind origin direction in degrees, 0 = north, 90 = east.</summary>
    [JsonPropertyName("wind_direction_deg")]
    public TValue? WindDirection { get; set; }

    /// <summary>Wind speed in km/h; this is not an unmarked gust value.</summary>
    [JsonPropertyName("wind_speed_kmh")]
    public TValue? WindSpeed { get; set; }

    /// <summary>Illuminance in lux.</summary>
    [JsonPropertyName("illuminance_lux")]
    public TValue? Illuminance { get; set; }

    /// <summary>Solar irradiance in W/m2.</summary>
    [JsonPropertyName("solar_irradiance_w_m2")]
    public TValue? SolarIrradiance { get; set; }

    /// <summary>Rain intensity in mm/h; this is not an accumulated rain amount.</summary>
    [JsonPropertyName("rain_rate_mm_h")]
    public TValue? RainRate { get; set; }
}
