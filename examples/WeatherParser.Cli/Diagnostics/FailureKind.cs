namespace WeatherParser.Cli.Diagnostics;

/// <summary>
/// Represents the kind of failure.
/// </summary>
public enum FailureKind
{
    /// <summary>
    /// Item-level failure.
    /// </summary>
    Item,

    /// <summary>
    /// Command-level failure.
    /// </summary>
    CommandLevel
}
