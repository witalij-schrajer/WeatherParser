using System.Text.Json.Serialization;
using System.Xml.Serialization;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Reference level of an air pressure value (REQ-054).
/// </summary>
[Serializable]
[JsonConverter(typeof(StrictStringEnumConverter<PressureReference>))]
public enum PressureReference
{
    /// <summary>
    /// Pressure at sensor/station level.
    /// </summary>
    [XmlEnum("sensor")]
    [JsonStringEnumMemberName("sensor")]
    Sensor,

    /// <summary>
    /// Pressure reduced to mean sea level.
    /// </summary>
    [XmlEnum("sea_level")]
    [JsonStringEnumMemberName("sea_level")]
    SeaLevel,

    /// <summary>
    /// Reference not verified.
    /// </summary>
    [XmlEnum("unknown")]
    [JsonStringEnumMemberName("unknown")]
    Unknown
}
