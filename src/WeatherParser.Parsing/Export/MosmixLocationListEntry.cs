using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a location entry in the MOSMIX location list.
/// </summary>
[Serializable]
public sealed class MosmixLocationListEntry
{
    private RequiredMember<string> _mosmixId;

    /// <summary>
    /// Gets or sets the MOSMIX ID of the location.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [JsonPropertyName("mosmix_id")]
    [XmlElement("mosmix_id")]
    public string MosmixId
    {
        get => _mosmixId.Get("mosmix_id");
        set => _mosmixId = RequiredMember<string>.Of(value);
    }

    private RequiredMember<string> _name;

    /// <summary>
    /// Gets or sets the name of the location.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [JsonPropertyName("name")]
    [XmlElement("name")]
    public string Name
    {
        get => _name.Get("name");
        set => _name = RequiredMember<string>.Of(value);
    }

    private RequiredMember<decimal> _latitude;

    /// <summary>
    /// Gets or sets the latitude of the location.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [JsonPropertyName("latitude")]
    [XmlElement("latitude")]
    public decimal Latitude
    {
        get => _latitude.Get("latitude");
        set => _latitude = RequiredMember<decimal>.Of(value);
    }

    private RequiredMember<decimal> _longitude;

    /// <summary>
    /// Gets or sets the longitude of the location.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [JsonPropertyName("longitude")]
    [XmlElement("longitude")]
    public decimal Longitude
    {
        get => _longitude.Get("longitude");
        set => _longitude = RequiredMember<decimal>.Of(value);
    }

    private RequiredMember<decimal> _altitudeM;

    /// <summary>
    /// Gets or sets the altitude in meters of the location.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [JsonPropertyName("altitude_m")]
    [XmlElement("altitude_m")]
    public decimal AltitudeM
    {
        get => _altitudeM.Get("altitude_m");
        set => _altitudeM = RequiredMember<decimal>.Of(value);
    }

    private RequiredMember<decimal> _distanceKm;

    /// <summary>
    /// Gets or sets the distance in kilometers from the search point to this location.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [JsonPropertyName("distance_km")]
    [XmlElement("distance_km")]
    public decimal DistanceKm
    {
        get => _distanceKm.Get("distance_km");
        set => _distanceKm = RequiredMember<decimal>.Of(value);
    }

    private RequiredMember<decimal> _altitudeDifferenceM;

    /// <summary>
    /// Gets or sets the altitude difference in meters between the search point and this location.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [JsonPropertyName("altitude_difference_m")]
    [XmlElement("altitude_difference_m")]
    public decimal AltitudeDifferenceM
    {
        get => _altitudeDifferenceM.Get("altitude_difference_m");
        set => _altitudeDifferenceM = RequiredMember<decimal>.Of(value);
    }

    /// <summary>
    /// Gets or sets the list of product availabilities for this location.
    /// </summary>
    [JsonPropertyName("products")]
    [XmlArray("products")]
    [XmlArrayItem("product_availability")]
    public List<MosmixProductAvailability> Products { get; set; } = new();
}
