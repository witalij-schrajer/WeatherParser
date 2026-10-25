namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Consumer-supplied endpoint of the OsthessenWetter parser (contract OW-10).
/// </summary>
public sealed class OsthessenWetterEndpoints
{
    /// <summary>
    /// The absolute HTTP or HTTPS URI of the measurement page, without user information.
    /// </summary>
    public Uri? PageUri { get; set; }
}
