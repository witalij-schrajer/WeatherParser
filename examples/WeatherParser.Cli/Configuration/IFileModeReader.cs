namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Provides functionality for reading file mode information.
/// </summary>
public interface IFileModeReader
{
    /// <summary>
    /// Determines whether the specified file has group or other access.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>true if the file has group or other access; otherwise, false.</returns>
    bool HasGroupOrOtherAccess(string path);
}
