using System.Text.Json.Serialization;
using System.Xml.Serialization;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents an interval value.
/// </summary>
[Serializable]
public class IntervalValue
{
    private RequiredMember<decimal> _value;

    /// <summary>
    /// Gets or sets the value of the interval.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("value")]
    [JsonPropertyName("value")]
    public decimal Value
    {
        get => _value.Get("value");
        set => _value = RequiredMember<decimal>.Of(value);
    }

    private ExportTimestamp _periodStart;

    /// <summary>
    /// Gets or sets the start time of the interval in UTC.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public DateTime PeriodStartUtc
    {
        get => _periodStart.Utc;
        set => _periodStart = ExportTimestamp.FromUtc(value, nameof(PeriodStartUtc));
    }

    /// <summary>
    /// Gets or sets the suffix-free UTC wire form (YYYY-MM-DDTHH:mm:ss) used by both serializers.
    /// </summary>
    [JsonPropertyName("period_start")]
    [XmlElement("period_start")]
    public string PeriodStartWire
    {
        get => _periodStart.ToWire("period_start");
        set => _periodStart = ExportTimestamp.ParseWire(value, nameof(PeriodStartWire));
    }

    private ExportTimestamp _periodEnd;

    /// <summary>
    /// Gets or sets the end time of the interval in UTC.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public DateTime PeriodEndUtc
    {
        get => _periodEnd.Utc;
        set => _periodEnd = ExportTimestamp.FromUtc(value, nameof(PeriodEndUtc));
    }

    /// <summary>
    /// Gets or sets the suffix-free UTC wire form (YYYY-MM-DDTHH:mm:ss) used by both serializers.
    /// </summary>
    [JsonPropertyName("period_end")]
    [XmlElement("period_end")]
    public string PeriodEndWire
    {
        get => _periodEnd.ToWire("period_end");
        set => _periodEnd = ExportTimestamp.ParseWire(value, nameof(PeriodEndWire));
    }

    private RequiredMember<IntervalAggregation> _aggregation;

    /// <summary>
    /// Gets or sets the aggregation method of the interval.
    /// Reading an unassigned value throws, so no default is exported.
    /// Undefined enum values are rejected.
    /// </summary>
    [XmlElement("aggregation")]
    [JsonPropertyName("aggregation")]
    public IntervalAggregation Aggregation
    {
        get => _aggregation.Get("aggregation");
        set => _aggregation = Enum.IsDefined(value)
            ? RequiredMember<IntervalAggregation>.Of(value)
            : throw new ArgumentException($"Undefined aggregation value '{value}'.", nameof(Aggregation));
    }
}
