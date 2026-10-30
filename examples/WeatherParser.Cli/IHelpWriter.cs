namespace WeatherParser.Cli;

using WeatherParser.Cli.Arguments;

/// <summary>
/// Writes the help text of the example CLI (REQ-010).
/// </summary>
public interface IHelpWriter
{
    /// <summary>
    /// Writes the complete help text to the writer.
    /// </summary>
    /// <param name="writer">The destination.</param>
    void WriteHelp(TextWriter writer);

    /// <summary>
    /// Writes the help text of one command: its options with units and an example.
    /// </summary>
    /// <param name="writer">The destination.</param>
    /// <param name="command">The command.</param>
    void WriteCommandHelp(TextWriter writer, CliCommand command);
}
