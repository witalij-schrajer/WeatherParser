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

    private ExportTimestamp? _evidenceRetrievedAt;

    /// <summary>
    /// UTC companion of evidence_retrieved_at (C-117): when the evidence file was downloaded; null when the item was not derived from an evidence file.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public DateTime? EvidenceRetrievedAtUtc
    {
        get => _evidenceRetrievedAt?.Utc;
        set => _evidenceRetrievedAt = value is null ? null : ExportTimestamp.FromUtc(value.Value, nameof(EvidenceRetrievedAtUtc));
    }

    /// <summary>
    /// Suffix-free UTC wire form of evidence_retrieved_at; omitted when absent (C-05, C-119).
    /// </summary>
    [JsonPropertyName("evidence_retrieved_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [XmlElement("evidence_retrieved_at")]
    public string? EvidenceRetrievedAtWire
    {
        get => _evidenceRetrievedAt?.ToWire("evidence_retrieved_at");
        set => _evidenceRetrievedAt = value is null ? null : ExportTimestamp.ParseWire(value, nameof(EvidenceRetrievedAtWire));
    }

    /// <summary>
    /// Controls XML output of evidence_retrieved_at; true exactly when a value is set.
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public bool EvidenceRetrievedAtWireSpecified
    {
        get => _evidenceRetrievedAt.HasValue;
        set
        {
            if (!value)
            {
                _evidenceRetrievedAt = null;
            }
        }
    }

    private ExportTimestamp? _evidenceIssuedAt;

    /// <summary>
    /// UTC companion of evidence_issued_at (C-118): the evidence file's dwd:IssueTime; present exactly when evidence_retrieved_at is present.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public DateTime? EvidenceIssuedAtUtc
    {
        get => _evidenceIssuedAt?.Utc;
        set => _evidenceIssuedAt = value is null ? null : ExportTimestamp.FromUtc(value.Value, nameof(EvidenceIssuedAtUtc));
    }

    /// <summary>
    /// Suffix-free UTC wire form of evidence_issued_at; omitted when absent (C-05, C-118).
    /// </summary>
    [JsonPropertyName("evidence_issued_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [XmlElement("evidence_issued_at")]
    public string? EvidenceIssuedAtWire
    {
        get => _evidenceIssuedAt?.ToWire("evidence_issued_at");
        set => _evidenceIssuedAt = value is null ? null : ExportTimestamp.ParseWire(value, nameof(EvidenceIssuedAtWire));
    }

    /// <summary>
    /// Controls XML output of evidence_issued_at; true exactly when a value is set.
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public bool EvidenceIssuedAtWireSpecified
    {
        get => _evidenceIssuedAt.HasValue;
        set
        {
            if (!value)
            {
                _evidenceIssuedAt = null;
            }
        }
    }
}
