namespace WeatherParser.Cli.Arguments;

/// <summary>
/// Turns the argument vector into exactly one command, a help request or a usage error, before any configuration, network, file or broker access (CL-02, CL-03).
/// </summary>
public interface ICommandLineParser
{
    /// <summary>
    /// Parses the given arguments.
    /// </summary>
    /// <param name="args">The arguments to parse.</param>
    /// <returns>The result of parsing the arguments.</returns>
    CommandLineResult Parse(IReadOnlyList<string> args);
}
