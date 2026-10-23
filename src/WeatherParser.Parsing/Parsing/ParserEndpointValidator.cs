namespace WeatherParser.Parsing.Parsing;

/// <summary>
/// Default <see cref="IParserEndpointValidator"/> implementation that rejects invalid endpoint
/// URIs and duplicate endpoint names, and otherwise maps each endpoint definition by its name.
/// </summary>
public sealed class ParserEndpointValidator : IParserEndpointValidator
{
    private readonly IEndpointUriValidator _uriValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParserEndpointValidator"/> class.
    /// </summary>
    /// <param name="uriValidator">The URI validator used to check each endpoint's URI.</param>
    public ParserEndpointValidator(IEndpointUriValidator uriValidator)
    {
        _uriValidator = uriValidator ?? throw new ArgumentNullException(nameof(uriValidator));
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ParserEndpointDefinition> ValidateAndMap(
        IReadOnlyCollection<ParserEndpointDefinition> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var map = new Dictionary<string, ParserEndpointDefinition>(StringComparer.Ordinal);

        foreach (var endpoint in endpoints)
        {
            if (!_uriValidator.IsValid(endpoint.Uri))
            {
                throw new ParserEndpointValidationException(
                    $"The endpoint '{endpoint.Name}' has an invalid URI; an absolute HTTP(S) URI is required.");
            }

            if (!map.TryAdd(endpoint.Name, endpoint))
            {
                throw new ParserEndpointValidationException(
                    $"The endpoint name '{endpoint.Name}' is defined more than once.");
            }
        }

        return map;
    }
}
