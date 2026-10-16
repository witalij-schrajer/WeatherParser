using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// Air pressure measured value, published in both hPa and mmHg with a documented
/// pressure reference, per REQ-052 and REQ-054. Timing fields are inherited from
/// <see cref="TimedValue"/>.
/// </summary>
[Serializable]
public sealed class PressureValue : TimedValue
{
    /// <summary>Air pressure in hectopascal (hPa).</summary>
    [JsonPropertyName("pressure_hpa")]
    public double PressureHpa { get; set; }

    /// <summary>The same air pressure converted to millimeters of mercury (mmHg).</summary>
    [JsonPropertyName("pressure_mmhg")]
    public double PressureMmHg { get; set; }

    /// <summary>
    /// Pressure reference: "sensor", "sea_level" or "unknown" when not proven (REQ-054).
    /// </summary>
    [JsonPropertyName("pressure_reference")]
    public string PressureReference { get; set; } = "unknown";
}
