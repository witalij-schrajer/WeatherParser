namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// The records of one station list after exclusions, and the diagnostics of skipped or excluded records.
/// </summary>
/// <param name="Records">The station records.</param>
/// <param name="Diagnostics">The diagnostics for skipped or excluded records.</param>
public sealed record CdcStationCatalog(IReadOnlyList<CdcStationRecord> Records, IReadOnlyList<CatalogDiagnostic> Diagnostics);
