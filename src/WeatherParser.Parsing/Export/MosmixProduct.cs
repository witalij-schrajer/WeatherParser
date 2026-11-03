using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a MOSMIX product type.
/// </summary>
[Serializable]
[JsonConverter(typeof(StrictStringEnumConverter<MosmixProduct>))]
public enum MosmixProduct
{
    /// <summary>
    /// The MOSMIX_S product.
    /// </summary>
    [XmlEnum("MOSMIX_S")]
    [JsonStringEnumMemberName("MOSMIX_S")]
    MosmixS,

    /// <summary>
    /// The MOSMIX_L product.
    /// </summary>
    [XmlEnum("MOSMIX_L")]
    [JsonStringEnumMemberName("MOSMIX_L")]
    MosmixL
}
