namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// A CDC station-list request target: product code, endpoint name and URI.
/// </summary>
public sealed record CdcListRequestTarget(string ProductCode, string EndpointName, Uri Uri);
