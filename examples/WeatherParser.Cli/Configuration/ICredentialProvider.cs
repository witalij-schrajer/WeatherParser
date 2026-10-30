namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Provides functionality for retrieving broker credentials.
/// </summary>
/// <remarks>
/// CL-06: Credentials handling.
/// </remarks>
public interface ICredentialProvider
{
    /// <summary>
    /// Gets the credentials for the specified broker section.
    /// </summary>
    /// <param name="broker">The broker section to get credentials for, or null if none.</param>
    /// <returns>The broker credentials, or null if not found.</returns>
    BrokerCredentials? GetCredentials(BrokerSection? broker);
}
