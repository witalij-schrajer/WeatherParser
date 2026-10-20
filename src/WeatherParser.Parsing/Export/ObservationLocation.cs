using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents the location of an observation.
/// </summary>
[Serializable]
public sealed class ObservationLocation
{
    private RequiredMember<string> _stationId;

    /// <summary>
    /// Gets or sets the station ID.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("station_id")]
    [JsonPropertyName("station_id")]
    public string StationId
    {
        get => _stationId.Get("station_id");
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("station_id must be non-empty (contract C-51).", nameof(StationId));
            }

            _stationId = RequiredMember<string>.Of(value);
        }
    }

    private RequiredMember<string> _name;

    /// <summary>
    /// Gets or sets the name of the location.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("name")]
    [JsonPropertyName("name")]
    public string Name
    {
        get => _name.Get("name");
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("name must be non-empty (contract C-51).", nameof(Name));
            }

            _name = RequiredMember<string>.Of(value);
        }
    }

    private RequiredMember<decimal> _latitude;

    /// <summary>
    /// Gets or sets the latitude in decimal degrees.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("latitude")]
    [JsonPropertyName("latitude")]
    public decimal Latitude
    {
        get => _latitude.Get("latitude");
        set
        {
            if (value < -90 || value > 90)
            {
                throw new ArgumentException("latitude must be between -90 and 90 inclusive (contract C-51).", nameof(Latitude));
            }

            _latitude = RequiredMember<decimal>.Of(value);
        }
    }

    private RequiredMember<decimal> _longitude;

    /// <summary>
    /// Gets or sets the longitude in decimal degrees.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("longitude")]
    [JsonPropertyName("longitude")]
    public decimal Longitude
    {
        get => _longitude.Get("longitude");
        set
        {
            if (value < -180 || value > 180)
            {
                throw new ArgumentException("longitude must be between -180 and 180 inclusive (contract C-51).", nameof(Longitude));
            }

            _longitude = RequiredMember<decimal>.Of(value);
        }
    }

    /// <summary>
    /// Gets or sets the altitude in meters.
    /// </summary>
    [XmlElement("altitude_m")]
    [JsonPropertyName("altitude_m")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? AltitudeM { get; set; }

    /// <summary>
    /// Controls XML output of altitude_m; true exactly when AltitudeM has a value.
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public bool AltitudeMSpecified
    {
        get => AltitudeM.HasValue;
        set
        {
            if (!value)
            {
                AltitudeM = null;
            }
        }
    }
}
