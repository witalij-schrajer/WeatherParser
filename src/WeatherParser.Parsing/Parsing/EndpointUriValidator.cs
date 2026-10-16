namespace WeatherParser.Parsing.Parsing;

/// <summary>
/// Default <see cref="IEndpointUriValidator"/> implementation that requires an absolute
/// HTTP or HTTPS URI.
/// </summary>
public sealed class EndpointUriValidator : IEndpointUriValidator
{
    /// <inheritdoc />
    public bool IsValid(Uri? uri)
    {
        if (uri is null || !uri.IsAbsoluteUri)
        {
            return false;
        }

        return uri.Scheme is "http" or "https";
    }
}
