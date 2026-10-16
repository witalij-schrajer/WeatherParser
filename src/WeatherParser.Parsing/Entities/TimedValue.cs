using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// Shared timing information for a measured quantity, per REQ-056. Used as the base for
/// <see cref="MeasuredValue"/> and <see cref="PressureValue"/> so both carry the same
/// per-value measurement timestamp and quality contract.
/// </summary>
[Serializable]
public abstract class TimedValue
{
    /// <summary>UTC measurement timestamp in "YYYY-MM-DDTHH:mm:ss" format without offset (REQ-051).</summary>
    [JsonPropertyName("measured_at")]
    public DateTime MeasuredAt { get; set; }

    /// <summary>
    /// Timestamp quality, either "source" or "estimated_date" (REQ-042). Null when not applicable.
    /// </summary>
    [JsonPropertyName("timestamp_quality")]
    public string? TimestampQuality { get; set; }

    /// <summary>
    /// Source time zone used to derive an estimated date, e.g. "Europe/Berlin" (REQ-042).
    /// Only set when <see cref="TimestampQuality"/> is "estimated_date".
    /// </summary>
    [JsonPropertyName("source_timezone")]
    public string? SourceTimezone { get; set; }
}
