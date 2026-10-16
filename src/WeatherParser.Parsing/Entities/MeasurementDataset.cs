using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// A complete measurement data record for a single source and station, per REQ-050.
/// Used for MQTT messages and for JSON/XML export with the same contract (REQ-065, REQ-067).
/// </summary>
[Serializable]
public sealed class MeasurementDataset
{
    /// <summary>Schema version of this export contract.</summary>
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>Identifier that uniquely identifies provider and station.</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>Location of the originating station.</summary>
    [JsonPropertyName("location")]
    public GeoLocation Location { get; set; } = new();

    /// <summary>Primary UTC measurement timestamp for this record (REQ-051).</summary>
    [JsonPropertyName("measured_at")]
    public DateTime MeasuredAt { get; set; }

    /// <summary>UTC timestamp when the data was retrieved from the source.</summary>
    [JsonPropertyName("retrieved_at")]
    public DateTime RetrievedAt { get; set; }

    /// <summary>Timestamp quality, either "source" or "estimated_date" (REQ-042).</summary>
    [JsonPropertyName("timestamp_quality")]
    public string TimestampQuality { get; set; } = "source";

    /// <summary>Source time zone used to derive an estimated date (REQ-042).</summary>
    [JsonPropertyName("source_timezone")]
    public string? SourceTimezone { get; set; }

    /// <summary>The normalized weather quantities available for this record.</summary>
    [JsonPropertyName("measurements")]
    public WeatherMeasurements Measurements { get; set; } = new();
}
