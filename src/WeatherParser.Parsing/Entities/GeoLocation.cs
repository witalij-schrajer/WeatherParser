using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// Geographic location used in export entities. Coordinates are WGS84 decimal degrees.
/// Not sealed so that <see cref="SearchParameters"/> can reuse these fields by inheritance.
/// </summary>
[Serializable]
public class GeoLocation
{
    /// <summary>Latitude in decimal degrees (-90 to +90).</summary>
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    /// <summary>Longitude in decimal degrees (-180 to +180).</summary>
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    /// <summary>Altitude above sea level in meters. Null when not available for the location.</summary>
    [JsonPropertyName("altitude_m")]
    public double? AltitudeM { get; set; }
}
