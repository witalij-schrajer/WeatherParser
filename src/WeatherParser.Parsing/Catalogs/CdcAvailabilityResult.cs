namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Active stations with their available measurements, and all diagnostics of the derivation including those of excluded stations.
/// </summary>
/// <param name="Stations">The active stations with their available measurements.</param>
/// <param name="Diagnostics">The diagnostics for skipped or excluded records.</param>
public sealed record CdcAvailabilityResult(IReadOnlyList<CdcStationAvailability> Stations, IReadOnlyList<CatalogDiagnostic> Diagnostics);
