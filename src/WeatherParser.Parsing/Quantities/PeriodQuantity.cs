namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// An unrounded value derived over a known period; the period is kept for the export (REQ-055).
/// </summary>
/// <param name="Value">The quantity value.</param>
/// <param name="Kind">The kind of quantity.</param>
/// <param name="Period">The averaging or accumulation period.</param>
public sealed record PeriodQuantity(decimal Value, QuantityKind Kind, AveragingPeriod Period);
