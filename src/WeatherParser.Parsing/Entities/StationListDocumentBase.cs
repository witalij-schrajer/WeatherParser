using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// Shared document envelope fields used by <see cref="DwdStationListDocument"/> and
/// <see cref="MosmixStationListDocument"/>, per REQ-023 and REQ-101.
/// </summary>
/// <typeparam name="TEntry">The station/location entry type contained in <see cref="Stations"/>.</typeparam>
[Serializable]
public abstract class StationListDocumentBase<TEntry>
    where TEntry : StationListEntryBase
{
    /// <summary>Schema version of this export contract.</summary>
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>Identifier of the data source.</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the catalog was retrieved.</summary>
    [JsonPropertyName("retrieved_at")]
    public DateTime RetrievedAt { get; set; }

    /// <summary>Search parameters used for the selection.</summary>
    [JsonPropertyName("search_parameters")]
    public SearchParameters SearchParameters { get; set; } = new();

    /// <summary>Matching entries; a valid empty list indicates no match (REQ-023/REQ-101).</summary>
    [JsonPropertyName("stations")]
    public List<TEntry> Stations { get; set; } = [];
}
