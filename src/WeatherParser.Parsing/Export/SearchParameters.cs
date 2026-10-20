using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents the search parameters used to find weather stations.
/// </summary>
[Serializable]
public sealed class SearchParameters
{
    private RequiredMember<decimal> _latitude;

    /// <summary>
    /// Gets or sets the latitude of the search center point.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("latitude")]
    [JsonPropertyName("latitude")]
    public decimal Latitude
    {
        get => _latitude.Get("latitude");
        set => _latitude = RequiredMember<decimal>.Of(value);
    }

    private RequiredMember<decimal> _longitude;

    /// <summary>
    /// Gets or sets the longitude of the search center point.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("longitude")]
    [JsonPropertyName("longitude")]
    public decimal Longitude
    {
        get => _longitude.Get("longitude");
        set => _longitude = RequiredMember<decimal>.Of(value);
    }

    private RequiredMember<decimal> _altitudeM;

    /// <summary>
    /// Gets or sets the altitude of the search center point in meters.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("altitude_m")]
    [JsonPropertyName("altitude_m")]
    public decimal AltitudeM
    {
        get => _altitudeM.Get("altitude_m");
        set => _altitudeM = RequiredMember<decimal>.Of(value);
    }

    private RequiredMember<decimal> _maxDistanceKm;

    /// <summary>
    /// Gets or sets the maximum distance in kilometers to search for stations.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("max_distance_km")]
    [JsonPropertyName("max_distance_km")]
    public decimal MaxDistanceKm
    {
        get => _maxDistanceKm.Get("max_distance_km");
        set => _maxDistanceKm = RequiredMember<decimal>.Of(value);
    }

    private RequiredMember<decimal> _maxAltitudeDifferenceM;

    /// <summary>
    /// Gets or sets the maximum altitude difference in meters allowed between the search center and stations.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("max_altitude_difference_m")]
    [JsonPropertyName("max_altitude_difference_m")]
    public decimal MaxAltitudeDifferenceM
    {
        get => _maxAltitudeDifferenceM.Get("max_altitude_difference_m");
        set => _maxAltitudeDifferenceM = RequiredMember<decimal>.Of(value);
    }
}
