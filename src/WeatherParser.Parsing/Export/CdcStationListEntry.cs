using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a weather station entry in the CDC station list.
/// </summary>
[Serializable]
public sealed class CdcStationListEntry
{
    private RequiredMember<string> _stationId;

    /// <summary>
    /// Gets or sets the unique identifier of the station, including leading zeros.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("station_id")]
    [JsonPropertyName("station_id")]
    public string StationId
    {
        get => _stationId.Get("station_id");
        set => _stationId = RequiredMember<string>.Of(value);
    }

    private RequiredMember<string> _name;

    /// <summary>
    /// Gets or sets the name of the station.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("name")]
    [JsonPropertyName("name")]
    public string Name
    {
        get => _name.Get("name");
        set => _name = RequiredMember<string>.Of(value);
    }

    private RequiredMember<decimal> _latitude;

    /// <summary>
    /// Gets or sets the latitude of the station.
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
    /// Gets or sets the longitude of the station.
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
    /// Gets or sets the altitude of the station in meters.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("altitude_m")]
    [JsonPropertyName("altitude_m")]
    public decimal AltitudeM
    {
        get => _altitudeM.Get("altitude_m");
        set => _altitudeM = RequiredMember<decimal>.Of(value);
    }

    private RequiredMember<decimal> _distanceKm;

    /// <summary>
    /// Gets or sets the distance to the station in kilometers.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("distance_km")]
    [JsonPropertyName("distance_km")]
    public decimal DistanceKm
    {
        get => _distanceKm.Get("distance_km");
        set => _distanceKm = RequiredMember<decimal>.Of(value);
    }

    private RequiredMember<decimal> _altitudeDifferenceM;

    /// <summary>
    /// Gets or sets the altitude difference between the search center and the station in meters.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("altitude_difference_m")]
    [JsonPropertyName("altitude_difference_m")]
    public decimal AltitudeDifferenceM
    {
        get => _altitudeDifferenceM.Get("altitude_difference_m");
        set => _altitudeDifferenceM = RequiredMember<decimal>.Of(value);
    }

    /// <summary>
    /// Gets or sets the list of available measurements for this station.
    /// </summary>
    [XmlArray("available_measurements")]
    [XmlArrayItem("measurement")]
    [JsonPropertyName("available_measurements")]
    public List<string> AvailableMeasurements { get; set; } = new();
}
