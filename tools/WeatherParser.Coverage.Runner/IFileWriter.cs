namespace WeatherParser.Coverage.Runner;

/// <summary>
/// Provides asynchronous file writing operations.
/// </summary>
public interface IFileWriter
{
    /// <summary>
    /// Asynchronously writes the specified content to a file.
    /// </summary>
    /// <param name="path">The path to the file to write.</param>
    /// <param name="contents">The content to write to the file.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken);
}
