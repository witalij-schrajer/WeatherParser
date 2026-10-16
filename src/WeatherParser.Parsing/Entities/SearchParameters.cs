using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// Search parameters used for a station selection request, echoed back in the response
/// envelope per REQ-023 and REQ-101. Latitude, longitude and altitude are inherited from
/// <see cref="GeoLocation"/>; the reference altitude is always set for search parameters.
/// </summary>
[Serializable]
public sealed class SearchParameters : GeoLocation
{
    /// <summary>Maximum horizontal distance in kilometers.</summary>
    [JsonPropertyName("max_distance_km")]
    public double MaxDistanceKm { get; set; }

    /// <summary>Maximum absolute altitude difference in meters.</summary>
    [JsonPropertyName("max_alt_difference_m")]
    public double MaxAltDifferenceM { get; set; }
}
