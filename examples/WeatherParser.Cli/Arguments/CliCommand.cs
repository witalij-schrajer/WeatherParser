namespace WeatherParser.Cli.Arguments;

/// <summary>
/// Represents a command for the weather parser CLI.
/// </summary>
public enum CliCommand
{
    /// <summary>
    /// The --SelectDWD command.
    /// </summary>
    SelectDwd,

    /// <summary>
    /// The --ParseDWD command.
    /// </summary>
    ParseDwd,

    /// <summary>
    /// The --ParseOstHessen command.
    /// </summary>
    ParseOstHessen,

    /// <summary>
    /// The --SelectMOSMIX command.
    /// </summary>
    SelectMosmix,

    /// <summary>
    /// The --ParseMOSMIX command.
    /// </summary>
    ParseMosmix
}
