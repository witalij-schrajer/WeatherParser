using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// Shared station/location fields used by <see cref="DwdStationListEntry"/> and
/// <see cref="MosmixStationListEntry"/>, per REQ-023 and REQ-101.
/// </summary>
[Serializable]
public abstract class StationListEntryBase
{
    /// <summary>Station or location name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Latitude in decimal degrees.</summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    /// <summary>Longitude in decimal degrees.</summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    /// <summary>
    /// Altitude in meters. Null when not proven by the source catalog (REQ-102); DWD entries
    /// always populate this value.
    /// </summary>
    [JsonPropertyName("altitude_m")]
    public double? AltitudeM { get; set; }

    /// <summary>Distance to the reference location in kilometers.</summary>
    [JsonPropertyName("distance_km")]
    public double DistanceKm { get; set; }

    /// <summary>
    /// Signed difference: station/location altitude minus reference altitude, in meters.
    /// Null when <see cref="AltitudeM"/> is not proven (REQ-102).
    /// </summary>
    [JsonPropertyName("altitude_difference_m")]
    public double? AltitudeDifferenceM { get; set; }
}
