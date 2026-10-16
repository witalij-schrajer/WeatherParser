using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// The set of normalized weather quantities defined in REQ-052. Each quantity is optional
/// and must be omitted, not defaulted to zero, when not available (REQ-053). The seven
/// quantities shared with <see cref="ForecastWeatherValues"/> are inherited from
/// <see cref="WeatherQuantities{TValue}"/>.
/// </summary>
[Serializable]
public sealed class WeatherMeasurements : WeatherQuantities<MeasuredValue>
{
    /// <summary>Air pressure in hPa and mmHg with pressure reference.</summary>
    [JsonPropertyName("pressure")]
    public PressureValue? Pressure { get; set; }
}
