using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a measurement period.
/// </summary>
[Serializable]
public sealed class MeasurementPeriod
{
    private ExportTimestamp _start;

    /// <summary>
    /// Gets or sets the start time of the measurement period in UTC.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public DateTime StartUtc
    {
        get => _start.Utc;
        set => _start = ExportTimestamp.FromUtc(value, nameof(StartUtc));
    }

    /// <summary>
    /// Gets or sets the suffix-free UTC wire form (YYYY-MM-DDTHH:mm:ss) used by both serializers.
    /// </summary>
    [JsonPropertyName("start")]
    [XmlElement("start")]
    public string StartWire
    {
        get => _start.ToWire("start");
        set => _start = ExportTimestamp.ParseWire(value, nameof(StartWire));
    }

    private ExportTimestamp _end;

    /// <summary>
    /// Gets or sets the end time of the measurement period in UTC.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public DateTime EndUtc
    {
        get => _end.Utc;
        set => _end = ExportTimestamp.FromUtc(value, nameof(EndUtc));
    }

    /// <summary>
    /// Gets or sets the suffix-free UTC wire form (YYYY-MM-DDTHH:mm:ss) used by both serializers.
    /// </summary>
    [JsonPropertyName("end")]
    [XmlElement("end")]
    public string EndWire
    {
        get => _end.ToWire("end");
        set => _end = ExportTimestamp.ParseWire(value, nameof(EndWire));
    }
}
