namespace WeatherParser.Parsing.Parsing;

/// <summary>
/// The validated content of one station's <c>now</c> archive for one product.
/// </summary>
/// <param name="StationId">The five-digit station ID.</param>
/// <param name="ProductCode">The product code, one of "tu", "ff", "rr" or "sd".</param>
/// <param name="Rows">The data rows in file order.</param>
public sealed record CdcNowEvidence(
    string StationId,
    string ProductCode,
    IReadOnlyList<CdcNowEvidenceRow> Rows);
