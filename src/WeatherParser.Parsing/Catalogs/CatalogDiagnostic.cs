namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// A diagnostic returned by catalog and evidence readers for the caller to log (the readers never log).
/// </summary>
/// <param name="Id">The diagnostic identifier.</param>
/// <param name="EndpointName">The endpoint name used in diagnostics.</param>
/// <param name="LineNumber">The 1-based physical line number, or null if unknown.</param>
/// <param name="StationId">The station ID, or null if not known.</param>
/// <param name="Reason">The reason for the diagnostic, in English and never containing raw input.</param>
public sealed record CatalogDiagnostic(string Id, string EndpointName, int? LineNumber, string? StationId, string Reason)
{
    /// <summary>
    /// Identifier for a skipped catalog record.
    /// </summary>
    public const string RecordSkippedId = "CATALOG-RECORD-SKIPPED";

    /// <summary>
    /// Identifier for a station conflict across lists.
    /// </summary>
    public const string StationConflictId = "CDC-STATION-CONFLICT";

    /// <summary>
    /// Identifier for a station without altitude.
    /// </summary>
    public const string StationNoAltitudeId = "CDC-STATION-NO-ALTITUDE";
}
