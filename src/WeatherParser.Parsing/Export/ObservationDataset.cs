using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a complete observation dataset.
/// </summary>
[Serializable]
[XmlRoot("observation_dataset", Namespace = ExportNamespaces.V1)]
public sealed class ObservationDataset
{
    /// <summary>
    /// Gets or sets the schema version of this dataset.
    /// </summary>
    [XmlElement("schema_version")]
    [JsonPropertyName("schema_version")]
    public string SchemaVersion
    {
        get => ExportNamespaces.SchemaVersionV1;
        set
        {
            if (value != ExportNamespaces.SchemaVersionV1)
            {
                throw new ArgumentException($"Unsupported schema_version '{value}'; contract v1 requires '{ExportNamespaces.SchemaVersionV1}'.", nameof(SchemaVersion));
            }
        }
    }

    /// <summary>
    /// Gets or sets the source identifier for this dataset.
    /// </summary>
    [XmlElement("source")]
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    private ExportTimestamp _retrievedAt;

    /// <summary>
    /// Gets or sets the UTC instant at which the dataset was retrieved.
    /// Rejects non-UTC kinds and fractional seconds.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public DateTime RetrievedAtUtc
    {
        get => _retrievedAt.Utc;
        set => _retrievedAt = ExportTimestamp.FromUtc(value, nameof(RetrievedAtUtc));
    }

    /// <summary>
    /// Gets or sets the suffix-free UTC wire form (YYYY-MM-DDTHH:mm:ss) used by both serializers.
    /// </summary>
    [JsonPropertyName("retrieved_at")]
    [XmlElement("retrieved_at")]
    public string RetrievedAtWire
    {
        get => _retrievedAt.ToWire("retrieved_at");
        set => _retrievedAt = ExportTimestamp.ParseWire(value, nameof(RetrievedAtWire));
    }

    /// <summary>
    /// Gets or sets the measurements included in this dataset.
    /// </summary>
    [XmlElement("measurements")]
    [JsonPropertyName("measurements")]
    public ObservationMeasurements Measurements { get; set; } = new();
}
