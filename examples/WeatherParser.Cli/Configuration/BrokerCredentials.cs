namespace WeatherParser.Cli.Configuration;

public sealed class BrokerCredentials
{
    public BrokerCredentials(string username, string password)
    {
        Username = username;
        Password = password;
    }

    public string Username { get; }
    public string Password { get; }
}
