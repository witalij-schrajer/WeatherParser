namespace WeatherParser.Cli;

public interface IEnvironmentReader
{
    string? Get(string name);
}
