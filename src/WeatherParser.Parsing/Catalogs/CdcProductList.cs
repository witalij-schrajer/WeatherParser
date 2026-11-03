namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// One parsed CDC <c>now</c> station list with its product code (<c>tu</c>, <c>ff</c>, <c>rr</c> or <c>sd</c>) and the configured endpoint name used in diagnostics.
/// </summary>
/// <param name="ProductCode">The product code.</param>
/// <param name="EndpointName">The endpoint name used in diagnostics.</param>
/// <param name="Catalog">The catalog of station records.</param>
public sealed record CdcProductList(string ProductCode, string EndpointName, CdcStationCatalog Catalog);
