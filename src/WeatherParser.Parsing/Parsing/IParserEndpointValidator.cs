namespace WeatherParser.Parsing.Parsing;

/// <summary>
/// Validates a set of parser endpoint definitions and maps them by name.
/// </summary>
public interface IParserEndpointValidator
{
    /// <summary>
    /// Validates the given endpoint definitions and returns them mapped by their unique name.
    /// </summary>
    /// <param name="endpoints">The endpoint definitions to validate.</param>
    /// <returns>A read-only map from endpoint name to its definition.</returns>
    /// <exception cref="ParserEndpointValidationException">
    /// Thrown when an endpoint URI is invalid or when an endpoint name is defined more than once.
    /// </exception>
    IReadOnlyDictionary<string, ParserEndpointDefinition> ValidateAndMap(
        IReadOnlyCollection<ParserEndpointDefinition> endpoints);
}
