namespace WeatherParser.Cli.Output;

using System.IO;
using System.Threading.Tasks;
using System.Threading;

/// <summary>
/// The filesystem operations of the file output, behind an injectable boundary (CL-12).
/// </summary>
public sealed class FileOperations : IFileOperations
{
    /// <inheritdoc/>
    public string GetFullPath(string path) => Path.GetFullPath(path);

    /// <inheritdoc/>
    public bool FileExists(string path) => File.Exists(path);

    /// <inheritdoc/>
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <inheritdoc/>
    public async Task WriteNewAsync(string path, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        // CreateNew never replaces an existing file (CL-12).
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        stream.Flush(flushToDisk: true);
    }

    /// <inheritdoc/>
    public void Move(string source, string destination) => File.Move(source, destination, overwrite: false);

    /// <inheritdoc/>
    public void Delete(string path) => File.Delete(path);

    /// <inheritdoc/>
    public string ReadAllText(string path) => File.ReadAllText(path);
}
