namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Result of the list pre-filter; stations in ordinal order of their IDs; diagnostics of skipped records, conflicts and missing altitudes for the caller to log.
/// </summary>
/// <param name="Stations">The candidate stations.</param>
/// <param name="Diagnostics">The diagnostics for skipped or excluded records.</param>
public sealed record CdcCandidates(IReadOnlyList<CdcCandidateStation> Stations, IReadOnlyList<CatalogDiagnostic> Diagnostics);
