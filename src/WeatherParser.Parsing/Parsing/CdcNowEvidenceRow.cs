namespace WeatherParser.Parsing.Parsing;

/// <summary>
/// One validated data row of a CDC <c>now</c> evidence file.
/// </summary>
/// <param name="MeasuredAtUtc">The measurement date and time in UTC.</param>
/// <param name="Values">The trimmed raw tokens of every data column of the header, by column name, uninterpreted; only the product's own columns are validated (SC-19, OC-14).</param>
public sealed record CdcNowEvidenceRow(DateTime MeasuredAtUtc, IReadOnlyDictionary<string, string> Values)
{
    /// <summary>
    /// The trimmed QN quality level token of the row, unchanged (for example "3"); empty when the row was constructed without it (contract OC-07, OC-14).
    /// </summary>
    public string QualityLevel { get; init; } = string.Empty;
}
