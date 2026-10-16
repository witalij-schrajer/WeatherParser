using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// The normalized forecast weather quantities for one forecast time step, using the same
/// field names and units as <see cref="WeatherMeasurements"/> where applicable (REQ-106).
/// Quantities not offered by the source must be omitted, not defaulted to zero. The seven
/// quantities shared with <see cref="WeatherMeasurements"/> are inherited from
/// <see cref="WeatherQuantities{TValue}"/>.
/// </summary>
[Serializable]
public sealed class ForecastWeatherValues : WeatherQuantities<ForecastValue>
{
    /// <summary>Air pressure in hPa.</summary>
    [JsonPropertyName("pressure_hpa")]
    public ForecastValue? PressureHpa { get; set; }

    /// <summary>The same air pressure converted to mmHg.</summary>
    [JsonPropertyName("pressure_mmhg")]
    public ForecastValue? PressureMmHg { get; set; }

    /// <summary>Pressure reference: "sensor", "sea_level" or "unknown" (REQ-054).</summary>
    [JsonPropertyName("pressure_reference")]
    public string? PressureReference { get; set; }
}
