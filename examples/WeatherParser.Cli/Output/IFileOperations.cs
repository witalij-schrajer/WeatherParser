namespace WeatherParser.Cli.Output;

/// <summary>
/// The filesystem operations of the file output, behind an injectable boundary (CL-12).
/// </summary>
public interface IFileOperations
{
    /// <summary>
    /// Resolves a path against the working directory.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns>The full path.</returns>
    string GetFullPath(string path);

    /// <summary>
    /// Determines whether a file exists.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise, <see langword="false"/>.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Determines whether a directory exists.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns><see langword="true"/> if the directory exists; otherwise, <see langword="false"/>.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Creates a new file, failing if it exists, writes the content, flushes it to disk and closes it.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task WriteNewAsync(string path, ReadOnlyMemory<byte> content, CancellationToken cancellationToken);

    /// <summary>
    /// Moves a file without overwriting an existing destination.
    /// </summary>
    /// <param name="source">The source path.</param>
    /// <param name="destination">The destination path.</param>
    void Move(string source, string destination);

    /// <summary>
    /// Deletes a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    void Delete(string path);
}
