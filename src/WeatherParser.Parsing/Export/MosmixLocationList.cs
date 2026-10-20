using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a list of MOSMIX locations.
/// </summary>
[Serializable]
[XmlRoot("mosmix_location_list", Namespace = ExportNamespaces.V1)]
public sealed class MosmixLocationList : IExportRootValidation
{
    /// <summary>
    /// Gets or sets the schema version of this list.
    /// </summary>
    [XmlElement("schema_version")]
    [JsonPropertyName("schema_version")]
    public string? SchemaVersion
    {
        get => ExportNamespaces.SchemaVersionV1;
        set
        {
            if (value != ExportNamespaces.SchemaVersionV1)
            {
                throw new ArgumentException($"Unsupported schema_version '{value}'; contract v1 requires '{ExportNamespaces.SchemaVersionV1}'.", nameof(SchemaVersion));
            }
        }
    }

    private RequiredMember<string> _source;

    /// <summary>
    /// Gets or sets the source of this list.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("source")]
    [JsonPropertyName("source")]
    public string Source
    {
        get => _source.Get("source");
        set => _source = RequiredMember<string>.Of(value);
    }

    private ExportTimestamp _retrievedAt;

    /// <summary>
    /// Gets or sets the UTC timestamp when this list was retrieved.
    /// Rejects non-UTC kinds and fractional seconds.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public DateTime RetrievedAtUtc
    {
        get => _retrievedAt.Utc;
        set => _retrievedAt = ExportTimestamp.FromUtc(value, nameof(RetrievedAtUtc));
    }

    /// <summary>
    /// Gets or sets the suffix-free UTC wire form (YYYY-MM-DDTHH:mm:ss) used by both serializers.
    /// </summary>
    [JsonPropertyName("retrieved_at")]
    [XmlElement("retrieved_at")]
    public string? RetrievedAtWire
    {
        get => _retrievedAt.ToWire("retrieved_at");
        set => _retrievedAt = ExportTimestamp.ParseWire(value, nameof(RetrievedAtWire));
    }

    /// <summary>
    /// Gets or sets the search parameters used to generate this list.
    /// </summary>
    [XmlElement("search_parameters")]
    [JsonPropertyName("search_parameters")]
    public SearchParameters SearchParameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of locations in this list.
    /// </summary>
    [XmlArray("locations")]
    [XmlArrayItem("location")]
    [JsonPropertyName("locations")]
    public List<MosmixLocationListEntry> Locations { get; set; } = new();

    private static InvalidOperationException Violation(string message) => new($"The MOSMIX location list violates contract C-91: {message}");

    void IExportRootValidation.ValidateForExport()
    {
        if (SearchParameters is null)
            throw Violation("search_parameters must not be null");

        if (Locations is null)
            throw Violation("locations must not be null");

        if (Locations.Any(item => item is null))
            throw Violation("locations must not contain a null item");

        foreach (var location in Locations)
        {
            if (location.Products is null)
                throw Violation("products must not be null");

            if (location.Products.Any(item => item is null))
                throw Violation("products must not contain a null item");

            var seenProducts = new HashSet<MosmixProduct>();
            foreach (var availability in location.Products)
            {
                if (!seenProducts.Add(availability.Product))
                    throw Violation($"product '{availability.Product}' occurs more than once in products (contract C-23)");

                if (availability.AvailableForecastElements is null)
                    throw Violation("available_forecast_elements must not be null");

                if (availability.AvailableForecastElements.Any(item => item is null))
                    throw Violation("available_forecast_elements must not contain a null item");

                if (availability.Availability is AvailabilityState.Unavailable or AvailabilityState.Unknown)
                {
                    if (availability.AvailableForecastElements.Count != 0)
                        throw Violation("available_forecast_elements must be empty unless availability is available (contract C-23)");
                }
            }
        }
    }
}
