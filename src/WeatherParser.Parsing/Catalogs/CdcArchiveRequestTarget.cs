namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// A CDC now-archive request target: endpoint name and URI.
/// </summary>
public sealed record CdcArchiveRequestTarget(string EndpointName, Uri Uri);
