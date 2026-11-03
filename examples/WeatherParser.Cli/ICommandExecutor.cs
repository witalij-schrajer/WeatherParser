using WeatherParser.Cli.Arguments;

namespace WeatherParser.Cli;

/// <summary>
/// Executes one validated command: configuration, the one validated container, the configuration-validation phase and the command (CL-04, CL-05, CL-07).
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// Executes the specified command request using the provided output writers.
    /// </summary>
    /// <param name="request">The validated command request to execute.</param>
    /// <param name="standardOutput">The text writer to write standard output to.</param>
    /// <param name="standardError">The text writer to write standard error to.</param>
    /// <returns>The exit code of the executed command.</returns>
    int Execute(CommandRequest request, TextWriter standardOutput, TextWriter standardError);
}
