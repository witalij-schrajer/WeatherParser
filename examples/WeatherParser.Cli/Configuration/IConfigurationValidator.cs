using WeatherParser.Cli.Arguments;

namespace WeatherParser.Cli.Configuration;

public interface IConfigurationValidator
{
    void Validate(IServiceProvider services, CliConfiguration configuration, CommandRequest request);
}
