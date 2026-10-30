using WeatherParser.Cli.Arguments;

namespace WeatherParser.Cli;

/// <summary>
/// Executes one validated command: configuration, the one validated container, the configuration-validation phase and the command (CL-04, CL-05, CL-07).
/// </summary>
public interface ICommandExecutor
{
    int Execute(CommandRequest request, TextWriter standardOutput, TextWriter standardError);
}
