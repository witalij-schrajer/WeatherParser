namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Represents credentials for accessing an MQTT broker.
/// </summary>
public sealed class BrokerCredentials
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BrokerCredentials"/> class.
    /// </summary>
    /// <param name="username">The username for the broker.</param>
    /// <param name="password">The password for the broker.</param>
    public BrokerCredentials(string username, string password)
    {
        Username = username;
        Password = password;
    }

    /// <summary>
    /// Gets the username for the broker.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Gets the password for the broker.
    /// </summary>
    public string Password { get; }
}
