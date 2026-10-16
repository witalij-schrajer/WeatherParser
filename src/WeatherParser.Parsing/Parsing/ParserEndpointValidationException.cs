namespace WeatherParser.Parsing.Parsing;

/// <summary>
/// Thrown when one or more parser endpoint definitions fail validation, either because a URI
/// is invalid or because the same endpoint name is defined more than once.
/// </summary>
public sealed class ParserEndpointValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParserEndpointValidationException"/> class.
    /// </summary>
    /// <param name="message">The English error message describing the validation failure.</param>
    public ParserEndpointValidationException(string message)
        : base(message)
    {
    }
}
