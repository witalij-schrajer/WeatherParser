using System.Text.Json.Serialization;
using System.Xml.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a measurement value with its timestamp.
/// </summary>
[Serializable]
public class MeasurementValue
{
    private RequiredMember<decimal> _value;

    /// <summary>
    /// Gets or sets the measured value.
    /// Reading an unassigned value throws, so a missing value is never exported as a fabricated zero; an assigned zero is a real value.
    /// </summary>
    [XmlElement("value")]
    [JsonPropertyName("value")]
    [JsonPropertyOrder(0)]
    public decimal Value
    {
        get => _value.Get("value");
        set => _value = RequiredMember<decimal>.Of(value);
    }

    private ExportTimestamp _measuredAt;

    /// <summary>
    /// Gets or sets the UTC instant at which the value was measured.
    /// Rejects non-UTC kinds and fractional seconds.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public DateTime MeasuredAtUtc
    {
        get => _measuredAt.Utc;
        set => _measuredAt = ExportTimestamp.FromUtc(value, nameof(MeasuredAtUtc));
    }

    /// <summary>
    /// Gets or sets the suffix-free UTC wire form (YYYY-MM-DDTHH:mm:ss) used by both serializers.
    /// </summary>
    [JsonPropertyName("measured_at")]
    [XmlElement("measured_at")]
    [JsonPropertyOrder(1)]
    public string MeasuredAtWire
    {
        get => _measuredAt.ToWire("measured_at");
        set => _measuredAt = ExportTimestamp.ParseWire(value, nameof(MeasuredAtWire));
    }

    private RequiredMember<TimestampQuality> _timestampQuality;

    /// <summary>
    /// Gets or sets the quality of the timestamp.
    /// Reading an unassigned value throws, so no default is exported.
    /// Undefined enum values are rejected.
    /// </summary>
    [XmlElement("timestamp_quality")]
    [JsonPropertyName("timestamp_quality")]
    [JsonPropertyOrder(2)]
    public TimestampQuality TimestampQuality
    {
        get => _timestampQuality.Get("timestamp_quality");
        set => _timestampQuality = Enum.IsDefined(value)
            ? RequiredMember<TimestampQuality>.Of(value)
            : throw new ArgumentException($"Undefined timestamp_quality value '{value}'.", nameof(TimestampQuality));
    }

    private string? _sourceTimezone;

    /// <summary>
    /// Gets or sets the source timezone.
    /// </summary>
    [XmlElement("source_timezone")]
    [JsonPropertyName("source_timezone")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(3)]
    public string? SourceTimezone
    {
        get => _sourceTimezone;
        set
        {
            if (value is not null && value != "Europe/Berlin")
            {
                throw new ArgumentException("source_timezone must be Europe/Berlin or absent (contract C-32, C-53).", nameof(SourceTimezone));
            }

            _sourceTimezone = value;
        }
    }

    /// <summary>
    /// Gets or sets the measurement period.
    /// </summary>
    [XmlElement("period")]
    [JsonPropertyName("period")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(4)]
    public MeasurementPeriod? Period { get; set; }

    private string? _quality;

    /// <summary>
    /// Gets or sets the quality of the measurement.
    /// </summary>
    [XmlElement("quality")]
    [JsonPropertyName("quality")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyOrder(5)]
    public string? Quality
    {
        get => _quality;
        set
        {
            if (value is not null && string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("quality must be absent or a non-empty source code (contract C-53).", nameof(Quality));
            }

            _quality = value;
        }
    }
}
