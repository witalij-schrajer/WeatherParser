using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Cli.Arguments;

namespace WeatherParser.Cli.Commands;

/// <summary>
/// Registers the command handler and the result writer of the output mode for one command (CL-09).
/// </summary>
public interface ICommandRegistrar
{
    /// <summary>
    /// Registers the command handler and the result writer of the output mode for one command (CL-09).
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="request">The command request to register handlers for.</param>
    void Register(IServiceCollection services, CommandRequest request);
}
