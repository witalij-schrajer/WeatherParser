namespace WeatherParser.Cli.Output;

using Microsoft.Extensions.Logging;
using System.Text;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Diagnostics;

/// <summary>
/// Writes a complete serialized document to standard output or to a new file, and checks a file destination before any retrieval (CL-12, REQ-066, REQ-068, REQ-069).
/// </summary>
public sealed class OutputDestination : IOutputDestination
{
    private readonly IFileOperations _files;
    private readonly ILogger<OutputDestination> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputDestination"/> class.
    /// </summary>
    /// <param name="files">The filesystem operations.</param>
    /// <param name="logger">The logger.</param>
    public OutputDestination(IFileOperations files, ILogger<OutputDestination> logger)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void Check(CommonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.OutputFile is null)
        {
            return;
        }

        string destination;
        try
        {
            destination = _files.GetFullPath(options.OutputFile);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // An invalid path is a usage error of the caller, detected before any retrieval (CL-11, CL-12).
            throw new CliException(CliErrorIds.OutputFailed, "The output file path is not a valid path.");
        }
        if (_files.FileExists(destination) || _files.DirectoryExists(destination))
        {
            throw new CliException(CliErrorIds.OutputExists, "The output file already exists; an existing file is never overwritten.");
        }

        if (!_files.DirectoryExists(Path.GetDirectoryName(destination)!))
        {
            throw new CliException(CliErrorIds.OutputFailed, "The directory of the output file does not exist.");
        }
    }

    /// <inheritdoc/>
    public async Task WriteAsync(ReadOnlyMemory<byte> document, CommandContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Options.OutputFile is null)
        {
            // One write of the complete document and nothing else on standard output (REQ-068, REQ-073).
            await context.StandardOutput.WriteAsync(Encoding.UTF8.GetString(document.Span).AsMemory(), cancellationToken).ConfigureAwait(false);
            await context.StandardOutput.FlushAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var destination = _files.GetFullPath(context.Options.OutputFile);
        var temporary = $"{destination}.{Guid.NewGuid():N}.tmp";
        var moving = false;
        try
        {
            await _files.WriteNewAsync(temporary, document, cancellationToken).ConfigureAwait(false);
            // A cancellation noticed after the write still prevents the move; the catch deletes the temporary file (CL-12).
            cancellationToken.ThrowIfCancellationRequested();
            moving = true;
            _files.Move(temporary, destination);
        }
        catch (Exception exception)
        {
            // No destination appears and no temporary file is left after a failure or cancellation (CL-12, REQ-069).
            DeleteTemporary(temporary);
            if (moving && _files.FileExists(destination))
            {
                throw new CliException(CliErrorIds.OutputExists, "The output file was created while the command ran; it is not overwritten.");
            }

            if (exception is IOException or UnauthorizedAccessException)
            {
                throw new CliException(CliErrorIds.OutputFailed, "The output file could not be written.");
            }

            throw;
        }
    }

    private void DeleteTemporary(string temporary)
    {
        try
        {
            _files.Delete(temporary);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(new EventId(0, CliErrorIds.OutputFailed), "The temporary file {Path} could not be deleted.", temporary);
        }
    }
}
