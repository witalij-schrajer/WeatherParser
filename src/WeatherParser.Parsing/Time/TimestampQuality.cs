using System.Text.Json.Serialization;
using System.Xml.Serialization;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Time;

/// <summary>
/// Indicates the quality of a timestamp.
/// </summary>
[Serializable]
[JsonConverter(typeof(StrictStringEnumConverter<TimestampQuality>))]
public enum TimestampQuality
{
    /// <summary>
    /// A complete source timestamp.
    /// </summary>
    [XmlEnum("source")]
    [JsonStringEnumMemberName("source")]
    Source,

    /// <summary>
    /// Date inferred from the retrieval time, REQ-042.
    /// </summary>
    [XmlEnum("estimated_date")]
    [JsonStringEnumMemberName("estimated_date")]
    EstimatedDate
}
