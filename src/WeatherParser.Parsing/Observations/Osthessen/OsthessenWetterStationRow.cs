namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// The raw texts of one overview row and its joined wind row, uninterpreted (contract OW-01, OW-05, A5, A7).
/// </summary>
/// <param name="Name">The station name as displayed in the overview table, decoded by the HTML5 rules.</param>
/// <param name="Status">The station status.</param>
/// <param name="OverviewTimeText">The trimmed time text of the overview row, for example 09:31 Uhr, or empty.</param>
/// <param name="TemperatureText">The trimmed primary temperature text with its unit, for example 10.3 followed by the degree sign and C, or empty; never the change (A7).</param>
/// <param name="HumidityText">The trimmed humidity text with its unit, or empty.</param>
/// <param name="WindTimeText">The trimmed time text of the joined wind row, or null when no wind row is joined.</param>
/// <param name="WindMeanText">The trimmed 10-minute mean wind text with its unit, or null when no wind row is joined.</param>
public sealed record OsthessenWetterStationRow(
    string Name,
    OsthessenWetterStationStatus Status,
    string OverviewTimeText,
    string TemperatureText,
    string HumidityText,
    string? WindTimeText,
    string? WindMeanText);
