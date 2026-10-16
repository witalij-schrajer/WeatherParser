namespace WeatherParser.Parsing.Parsing;

/// <summary>
/// Validates whether a URI is acceptable for use as a parser endpoint.
/// </summary>
public interface IEndpointUriValidator
{
    /// <summary>
    /// Determines whether the given URI is a valid, absolute HTTP(S) endpoint URI.
    /// </summary>
    /// <param name="uri">The URI to validate.</param>
    /// <returns><see langword="true"/> if the URI is valid; otherwise <see langword="false"/>.</returns>
    bool IsValid(Uri? uri);
}
