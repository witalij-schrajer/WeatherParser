using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents the export envelope that carries several results of one command in one document.
/// </summary>
[Serializable]
[XmlRoot("export_document", Namespace = ExportNamespaces.V1)]
public sealed class ExportDocument : IExportRootValidation
{
    /// <summary>
    /// Gets or sets the schema version of this document.
    /// </summary>
    [XmlElement("schema_version")]
    [JsonPropertyName("schema_version")]
    public string SchemaVersion
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

    /// <summary>
    /// Gets or sets the list of observation datasets.
    /// </summary>
    [JsonPropertyName("observations")]
    [XmlArray("observations")]
    [XmlArrayItem("observation_dataset")]
    public List<ObservationDataset> Observations { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of forecast runs.
    /// </summary>
    [JsonPropertyName("forecast_runs")]
    [XmlArray("forecast_runs")]
    [XmlArrayItem("forecast_run")]
    public List<ForecastRun> ForecastRuns { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of typed error records of a partial result.
    /// </summary>
    [JsonPropertyName("errors")]
    [XmlArray("errors")]
    [XmlArrayItem("error")]
    public List<ExportError> Errors { get; set; } = new();

    private static InvalidOperationException Violation(string message) => new($"The export document violates contract C-91: {message}");

    void IExportRootValidation.ValidateForExport()
    {
        if (Observations is null)
            throw Violation("observations must not be null");

        if (ForecastRuns is null)
            throw Violation("forecast_runs must not be null");

        if (Errors is null)
            throw Violation("errors must not be null");

        if (Observations.Any(item => item is null))
            throw Violation("observations must not contain a null item");

        if (ForecastRuns.Any(item => item is null))
            throw Violation("forecast_runs must not contain a null item");

        if (Errors.Any(item => item is null))
            throw Violation("errors must not contain a null item");

        foreach (var item in Observations)
        {
            ((IExportRootValidation)item).ValidateForExport();
        }

        foreach (var item in ForecastRuns)
        {
            ((IExportRootValidation)item).ValidateForExport();
        }

        var seenSources = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in Observations)
        {
            var source = item.Source;
            if (!seenSources.Add(source))
                throw Violation($"source '{source}' occurs more than once; each requested item has exactly one outcome (contract C-85)");
        }

        foreach (var item in ForecastRuns)
        {
            var source = item.Source;
            if (!seenSources.Add(source))
                throw Violation($"source '{source}' occurs more than once; each requested item has exactly one outcome (contract C-85)");
        }

        foreach (var item in Errors)
        {
            var source = item.Source;
            if (!seenSources.Add(source))
                throw Violation($"source '{source}' occurs more than once; each requested item has exactly one outcome (contract C-85)");
        }
    }
}
