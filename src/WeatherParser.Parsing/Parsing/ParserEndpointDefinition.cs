namespace WeatherParser.Parsing.Parsing;

/// <summary>
/// Describes a single named parser endpoint and the URI it is configured to use.
/// </summary>
/// <param name="Name">The unique, human-readable name of the parser endpoint (e.g. "Mosmix").</param>
/// <param name="Uri">The absolute URI the endpoint is configured to use.</param>
public sealed record ParserEndpointDefinition(string Name, Uri Uri);
