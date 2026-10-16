using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// A single MOSMIX forecast location found by <c>--SelectMOSMIX</c>, per REQ-101. Name,
/// coordinates, distance and altitude difference are inherited from
/// <see cref="StationListEntryBase"/>.
/// </summary>
[Serializable]
public sealed class MosmixStationListEntry : StationListEntryBase
{
    /// <summary>
    /// MOSMIX id as a standalone string; leading zeros and alphanumeric ids are preserved.
    /// This is not a DWD CDC measurement station id (REQ-101).
    /// </summary>
    [JsonPropertyName("mosmix_id")]
    public string MosmixId { get; set; } = string.Empty;

    /// <summary>
    /// Forecast elements proven available for this location and product. Empty when not yet known;
    /// unknown availability must not be represented as "all elements available" (REQ-102).
    /// </summary>
    [JsonPropertyName("available_forecast_elements")]
    public List<string> AvailableForecastElements { get; set; } = [];
}
