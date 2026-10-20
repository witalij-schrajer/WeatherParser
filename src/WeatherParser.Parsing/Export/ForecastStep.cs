using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a forecast step.
/// </summary>
[Serializable]
public sealed class ForecastStep
{
    private ExportTimestamp _validAt;

    /// <summary>
    /// Gets or sets the valid time of the forecast step in UTC.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public DateTime ValidAtUtc
    {
        get => _validAt.Utc;
        set => _validAt = ExportTimestamp.FromUtc(value, nameof(ValidAtUtc));
    }

    /// <summary>
    /// Gets or sets the suffix-free UTC wire form (YYYY-MM-DDTHH:mm:ss) used by both serializers.
    /// </summary>
    [JsonPropertyName("valid_at")]
    [XmlElement("valid_at")]
    public string ValidAtWire
    {
        get => _validAt.ToWire("valid_at");
        set => _validAt = ExportTimestamp.ParseWire(value, nameof(ValidAtWire));
    }

    private RequiredMember<ForecastValues> _values;

    /// <summary>
    /// Gets or sets the forecast values.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("values")]
    [JsonPropertyName("values")]
    public ForecastValues Values
    {
        get => _values.Get("values");
        set => _values = RequiredMember<ForecastValues>.Of(value);
    }
}
