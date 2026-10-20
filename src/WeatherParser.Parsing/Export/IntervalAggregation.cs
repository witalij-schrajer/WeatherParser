using System.Text.Json.Serialization;
using System.Xml.Serialization;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents the aggregation method for interval values.
/// </summary>
[Serializable]
[JsonConverter(typeof(StrictStringEnumConverter<IntervalAggregation>))]
public enum IntervalAggregation
{
    /// <summary>
    /// The mean aggregation method.
    /// </summary>
    [XmlEnum("mean")]
    [JsonStringEnumMemberName("mean")]
    Mean
}
