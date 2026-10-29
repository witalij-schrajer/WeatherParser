namespace WeatherParser.Cli;

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
}
