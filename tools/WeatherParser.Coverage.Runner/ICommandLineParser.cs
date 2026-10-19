namespace WeatherParser.Coverage.Runner;

/// <summary>
/// Parses command-line arguments into runner commands.
/// </summary>
public interface ICommandLineParser
{
    /// <summary>
    /// Parses the provided command-line arguments into a runner command.
    /// </summary>
    /// <param name="args">The command-line arguments to parse.</param>
    /// <returns>The parsed runner command.</returns>
    RunnerCommand Parse(IReadOnlyList<string> args);
}
