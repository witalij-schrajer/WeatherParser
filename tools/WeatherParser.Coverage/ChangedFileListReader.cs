using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Coverage;

/// <summary>
/// Reads list of changed files from text files.
/// </summary>
public sealed class ChangedFileListReader : IChangedFileListReader
{
    private readonly IFileReader _fileReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangedFileListReader"/> class.
    /// </summary>
    /// <param name="fileReader">The file reader to use for reading files.</param>
    public ChangedFileListReader(IFileReader fileReader)
    {
        _fileReader = fileReader ?? throw new System.ArgumentNullException(nameof(fileReader));
    }

    /// <inheritdoc/>
    public async Task<ChangedFileList> ReadAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new CoverageInputException($"Changed files list '{path}' was not found.");
        }

        if (!_fileReader.Exists(path))
        {
            throw new CoverageInputException($"Changed files list '{path}' was not found.");
        }

        var content = await _fileReader.ReadAllTextAsync(path, cancellationToken);
        return ReadFromText(content);
    }

    private ChangedFileList ReadFromText(string content)
    {
        var lines = content.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        var paths = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                // Normalize path separators to forward slashes
                var normalized = trimmed.Replace('\\', '/');
                paths.Add(normalized);
            }
        }

        return new ChangedFileList(paths);
    }
}
