using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents the availability information for a MOSMIX product.
/// </summary>
[Serializable]
public sealed class MosmixProductAvailability
{
    private RequiredMember<MosmixProduct> _product;

    /// <summary>
    /// Gets or sets the MOSMIX product type.
    /// Reading an unassigned value throws, so no default is exported.
    /// Undefined enum values are rejected.
    /// </summary>
    [JsonPropertyName("product")]
    [XmlElement("product")]
    public MosmixProduct Product
    {
        get => _product.Get("product");
        set => _product = Enum.IsDefined(value)
            ? RequiredMember<MosmixProduct>.Of(value)
            : throw new ArgumentException($"Undefined product value '{value}'.", nameof(Product));
    }

    private RequiredMember<AvailabilityState> _availability;

    /// <summary>
    /// Gets or sets the availability state of the product.
    /// Reading an unassigned value throws, so no default is exported.
    /// Undefined enum values are rejected.
    /// </summary>
    [JsonPropertyName("availability")]
    [XmlElement("availability")]
    public AvailabilityState Availability
    {
        get => _availability.Get("availability");
        set => _availability = Enum.IsDefined(value)
            ? RequiredMember<AvailabilityState>.Of(value)
            : throw new ArgumentException($"Undefined availability value '{value}'.", nameof(Availability));
    }

    /// <summary>
    /// Gets or sets the list of available forecast elements for this product.
    /// </summary>
    [JsonPropertyName("available_forecast_elements")]
    [XmlArray("available_forecast_elements")]
    [XmlArrayItem("element")]
    public List<string> AvailableForecastElements { get; set; } = new();
}
