namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// The records in line order and the diagnostics of skipped records, in line order, for the caller.
/// </summary>
/// <param name="Records">The station records, every syntactically valid record in line order, including those without elevation (AltitudeM null, never 0).</param>
/// <param name="Diagnostics">The diagnostics for skipped or excluded records.</param>
public sealed record MosmixLocationCatalog(IReadOnlyList<MosmixLocationRecord> Records, IReadOnlyList<CatalogDiagnostic> Diagnostics)
{
    /// <summary>
    /// Gets the records that can be filtered by altitude: every record except those without elevation (REQ-102, SC-04).
    /// </summary>
    public IReadOnlyList<MosmixLocationRecord> SelectableRecords => Records.Where(record => record.AltitudeM is not null).ToList();
}
