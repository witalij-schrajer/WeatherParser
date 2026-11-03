namespace WeatherParser.Cli.Diagnostics;

/// <summary>
/// Provides an interface for mapping exceptions to error IDs.
/// </summary>
public interface ILibraryErrors
{
    /// <summary>
    /// Tries to get the error ID for a given exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="errorId">The error ID, if found.</param>
    /// <returns>True if an error ID was found; otherwise false.</returns>
    bool TryGetErrorId(Exception exception, out string errorId);
}
