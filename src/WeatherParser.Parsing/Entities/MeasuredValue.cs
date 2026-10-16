using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// A single measured weather quantity together with its own measurement timestamp,
/// per REQ-056. Missing or invalid quantities must be omitted rather than represented
/// by a zero or default value (REQ-053). Timing fields are inherited from <see cref="TimedValue"/>.
/// </summary>
[Serializable]
public sealed class MeasuredValue : TimedValue
{
    /// <summary>The measured value in the unit documented by the containing field (REQ-052).</summary>
    [JsonPropertyName("value")]
    public double Value { get; set; }
}
