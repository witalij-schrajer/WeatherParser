namespace WeatherParser.Coverage;

/// <summary>
/// Reads baseline metadata from JSON files.
/// </summary>
public interface IBaselineMetadataReader
{
    /// <summary>
    /// Asynchronously reads and parses baseline metadata from the specified file path.
    /// </summary>
    /// <param name="path">The path to the baseline metadata file, or null/empty for no baseline.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous read operation. The value of the TResult parameter contains the parsed baseline metadata, or null if the file does not exist.</returns>
    Task<BaselineMetadata?> ReadAsync(string? path, CancellationToken cancellationToken);
}
