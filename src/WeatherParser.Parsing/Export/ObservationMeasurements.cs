using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents the measurements collected for an observation.
/// </summary>
[Serializable]
public sealed class ObservationMeasurements
{
    /// <summary>
    /// Gets or sets the temperature measurement in Celsius. Omitted from both formats when the reading is missing.
    /// </summary>
    [XmlElement("temperature_c")]
    [JsonPropertyName("temperature_c")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MeasurementValue? TemperatureC { get; set; }
}
