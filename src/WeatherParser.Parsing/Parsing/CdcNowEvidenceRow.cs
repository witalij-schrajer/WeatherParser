namespace WeatherParser.Parsing.Parsing;

/// <summary>
/// One validated data row of a CDC <c>now</c> evidence file.
/// </summary>
/// <param name="MeasuredAtUtc">The measurement date and time in UTC.</param>
/// <param name="Values">The values, mapped by product column name to trimmed token, uninterpreted.</param>
public sealed record CdcNowEvidenceRow(DateTime MeasuredAtUtc, IReadOnlyDictionary<string, string> Values);
