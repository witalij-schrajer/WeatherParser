using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// A single DWD CDC station found by <c>--SelectDWD</c>, per REQ-023. Name, coordinates,
/// distance and altitude difference are inherited from <see cref="StationListEntryBase"/>.
/// </summary>
[Serializable]
public sealed class DwdStationListEntry : StationListEntryBase
{
    /// <summary>DWD station id as string; leading zeros are preserved.</summary>
    [JsonPropertyName("station_id")]
    public string StationId { get; set; } = string.Empty;

    /// <summary>Proven measurement quantities/data types available at this station.</summary>
    [JsonPropertyName("available_measurements")]
    public List<string> AvailableMeasurements { get; set; } = [];
}
