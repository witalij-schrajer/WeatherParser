using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents the availability state of a product.
/// </summary>
[Serializable]
[JsonConverter(typeof(StrictStringEnumConverter<AvailabilityState>))]
public enum AvailabilityState
{
    /// <summary>
    /// The product is available.
    /// </summary>
    [XmlEnum("available")]
    [JsonStringEnumMemberName("available")]
    Available,

    /// <summary>
    /// The product is unavailable.
    /// </summary>
    [XmlEnum("unavailable")]
    [JsonStringEnumMemberName("unavailable")]
    Unavailable,

    /// <summary>
    /// The availability of the product is unknown.
    /// </summary>
    [XmlEnum("unknown")]
    [JsonStringEnumMemberName("unknown")]
    Unknown
}
