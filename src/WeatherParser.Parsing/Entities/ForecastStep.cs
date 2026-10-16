using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// A single forecast time step within a <see cref="ForecastDataset"/>, per REQ-105.
/// </summary>
[Serializable]
public sealed class ForecastStep
{
    /// <summary>UTC predicted timestamp this step refers to, without offset (REQ-051).</summary>
    [JsonPropertyName("valid_at")]
    public DateTime ValidAt { get; set; }

    /// <summary>Normalized forecast weather quantities available for this time step.</summary>
    [JsonPropertyName("values")]
    public ForecastWeatherValues Values { get; set; } = new();
}
