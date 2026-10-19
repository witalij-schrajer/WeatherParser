namespace WeatherParser.Coverage;

/// <summary>
/// Provides asynchronous file reading operations.
/// </summary>
public interface IFileReader
{
    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The file to check.</param>
    /// <returns>true if the file exists; otherwise, false.</returns>
    bool Exists(string path);

    /// <summary>
    /// Reads the entire contents of a file as a string asynchronously.
    /// </summary>
    /// <param name="path">The file to read.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous read operation. The value of the TResult parameter contains the contents of the file.</returns>
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken);
}
