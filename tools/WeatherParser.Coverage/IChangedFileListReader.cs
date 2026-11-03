namespace WeatherParser.Coverage;

/// <summary>
/// Reads list of changed files from text files.
/// </summary>
public interface IChangedFileListReader
{
    /// <summary>
    /// Asynchronously reads and parses a list of changed files from the specified file path.
    /// </summary>
    /// <param name="path">The path to the changed files list.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous read operation. The value of the TResult parameter contains the parsed list of changed files.</returns>
    Task<ChangedFileList> ReadAsync(string path, CancellationToken cancellationToken);
}
