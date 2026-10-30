namespace WeatherParser.Cli.Configuration;

public interface ICredentialProvider
{
    BrokerCredentials? GetCredentials(BrokerSection? broker);
}
