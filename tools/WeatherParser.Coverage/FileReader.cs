using System.IO;

namespace WeatherParser.Coverage;

/// <summary>
/// Thin wrapper over System.IO.File for asynchronous file reading.
/// </summary>
public sealed class FileReader : IFileReader
{
    /// <inheritdoc/>
    public bool Exists(string path)
    {
        return File.Exists(path);
    }

    /// <inheritdoc/>
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
    {
        return File.ReadAllTextAsync(path, cancellationToken);
    }
}
