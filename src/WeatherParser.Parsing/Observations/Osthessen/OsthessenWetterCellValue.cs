namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// The value read from one cell; Value is set only when Status is Present and may be non-finite for a number that overflows.
/// </summary>
/// <param name="Status">The outcome of reading the cell.</param>
/// <param name="Value">The number in the cell, or null if Status is not Present.</param>
public sealed record OsthessenWetterCellValue(OsthessenWetterCellValueStatus Status, double? Value);
