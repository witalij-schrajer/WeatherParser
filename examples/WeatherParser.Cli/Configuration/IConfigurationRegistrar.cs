using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Cli.Arguments;

namespace WeatherParser.Cli.Configuration;

public interface IConfigurationRegistrar
{
    void Register(IServiceCollection services, CliConfiguration configuration, CommandRequest request);
}
