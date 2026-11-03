namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// The station rows of the overview table in page order and the page notices.
/// </summary>
/// <param name="Stations">The station rows of the overview table in page order.</param>
/// <param name="Diagnostics">The page notices found while reading the page.</param>
public sealed record OsthessenWetterPage(
    IReadOnlyList<OsthessenWetterStationRow> Stations,
    IReadOnlyList<OsthessenWetterPageDiagnostic> Diagnostics);
