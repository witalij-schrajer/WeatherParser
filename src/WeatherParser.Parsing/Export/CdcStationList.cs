using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents the complete CDC station list with search parameters and station entries.
/// </summary>
[Serializable]
[XmlRoot("cdc_station_list", Namespace = ExportNamespaces.V1)]
public sealed class CdcStationList
{
    /// <summary>
    /// Gets or sets the schema version of the data format.
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

    private RequiredMember<string> _source;

    /// <summary>
    /// Gets or sets the source of the data.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("source")]
    [JsonPropertyName("source")]
    public string Source
    {
        get => _source.Get("source");
        set => _source = RequiredMember<string>.Of(value);
    }

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
    /// Gets or sets the search parameters used to find the stations.
    /// </summary>
    [XmlElement("search_parameters")]
    [JsonPropertyName("search_parameters")]
    public SearchParameters SearchParameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of weather stations found.
    /// </summary>
    [XmlArray("stations")]
    [XmlArrayItem("station")]
    [JsonPropertyName("stations")]
    public List<CdcStationListEntry> Stations { get; set; } = new();
}
