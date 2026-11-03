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
    Mean,

    /// <summary>
    /// The maximum over the period (wind_gust_kmh, contract MF-11).
    /// </summary>
    [XmlEnum("maximum")]
    [JsonStringEnumMemberName("maximum")]
    Maximum,

    /// <summary>
    /// The total over the period (precipitation_amount_mm, contract MF-11).
    /// </summary>
    [XmlEnum("sum")]
    [JsonStringEnumMemberName("sum")]
    Sum
}
