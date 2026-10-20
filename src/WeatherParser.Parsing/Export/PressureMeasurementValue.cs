using System.Text.Json.Serialization;
using System.Xml.Serialization;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a pressure measurement value.
/// </summary>
[Serializable]
public sealed class PressureMeasurementValue : MeasurementValue
{
    private RequiredMember<PressureReference> _pressureReference;

    /// <summary>
    /// Gets or sets the reference point for the pressure measurement.
    /// Reading an unassigned value throws, so no default is exported.
    /// Undefined enum values are rejected.
    /// </summary>
    [XmlElement("pressure_reference")]
    [JsonPropertyName("pressure_reference")]
    [JsonPropertyOrder(6)]
    public PressureReference PressureReference
    {
        get => _pressureReference.Get("pressure_reference");
        set => _pressureReference = Enum.IsDefined(value)
            ? RequiredMember<PressureReference>.Of(value)
            : throw new ArgumentException($"Undefined pressure_reference value '{value}'.", nameof(PressureReference));
    }
}
