using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a measurement value with its timestamp.
/// </summary>
[Serializable]
public sealed class MeasurementValue
{
    private decimal _value;
    private bool _valueIsSet;

    /// <summary>
    /// Gets or sets the measured value.
    /// Reading an unassigned value throws, so a missing value is never exported as a fabricated zero; an assigned zero is a real value.
    /// </summary>
    [XmlElement("value")]
    [JsonPropertyName("value")]
    public decimal Value
    {
        get => _valueIsSet
            ? _value
            : throw new InvalidOperationException("The required value 'value' is not set.");
        set
        {
            _value = value;
            _valueIsSet = true;
        }
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
    public string MeasuredAtWire
    {
        get => _measuredAt.ToWire("measured_at");
        set => _measuredAt = ExportTimestamp.ParseWire(value, nameof(MeasuredAtWire));
    }
}
