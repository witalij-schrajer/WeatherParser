using System.IO;
using System.Threading.Tasks;

namespace WeatherParser.Coverage.Runner;

/// <summary>
/// Thin wrapper over File.WriteAllTextAsync.
/// </summary>
public sealed class FileWriter : IFileWriter
{
    /// <inheritdoc/>
    public async Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(contents);

        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, contents, cancellationToken);
    }
}
