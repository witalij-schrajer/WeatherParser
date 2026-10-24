namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Identifies the evidence of one station and product.
/// </summary>
/// <param name="StationId">The station ID.</param>
/// <param name="ProductCode">The product code.</param>
public readonly record struct CdcEvidenceKey(string StationId, string ProductCode);
