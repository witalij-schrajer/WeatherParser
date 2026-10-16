using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// A complete MOSMIX forecast run for one location and product, per REQ-105.
/// Published as a single retained message so time steps do not replace each other (REQ-062).
/// </summary>
[Serializable]
public sealed class ForecastDataset
{
    /// <summary>Schema version of this export contract.</summary>
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>Always "forecast" to distinguish this dataset from measurement data.</summary>
    [JsonPropertyName("dataset_type")]
    public string DatasetType { get; set; } = "forecast";

    /// <summary>Identifier that uniquely identifies provider, product and MOSMIX id.</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>MOSMIX location id; leading zeros and alphanumeric ids are preserved.</summary>
    [JsonPropertyName("mosmix_id")]
    public string MosmixId { get; set; } = string.Empty;

    /// <summary>Location of the forecast point.</summary>
    [JsonPropertyName("location")]
    public GeoLocation Location { get; set; } = new();

    /// <summary>Forecast product, either "MOSMIX_S" or "MOSMIX_L".</summary>
    [JsonPropertyName("product")]
    public string Product { get; set; } = string.Empty;

    /// <summary>UTC source issue timestamp of this forecast run, without offset (REQ-051).</summary>
    [JsonPropertyName("issued_at")]
    public DateTime IssuedAt { get; set; }

    /// <summary>UTC timestamp when this forecast run was retrieved.</summary>
    [JsonPropertyName("retrieved_at")]
    public DateTime RetrievedAt { get; set; }

    /// <summary>Forecast time steps, sorted ascending by <see cref="ForecastStep.ValidAt"/>.</summary>
    [JsonPropertyName("forecasts")]
    public List<ForecastStep> Forecasts { get; set; } = [];
}
