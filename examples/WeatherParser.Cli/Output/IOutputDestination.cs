namespace WeatherParser.Cli.Output;

using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Commands;

/// <summary>
/// Writes a complete serialized document to standard output or to a new file, and checks a file destination before any retrieval (CL-12, REQ-066, REQ-068, REQ-069).
/// </summary>
public interface IOutputDestination
{
    /// <summary>
    /// Fails with CLI-OUTPUT-EXISTS when the output file exists and with CLI-OUTPUT-FAILED when its directory does not exist; does nothing without --OutputFile.
    /// </summary>
    /// <param name="options">The common options of the command line.</param>
    void Check(CommonOptions options);

    /// <summary>
    /// Writes the UTF-8 document without byte-order mark in one write to standard output, or through a temporary file in the destination directory that is moved to the destination without overwriting.
    /// </summary>
    /// <param name="document">The UTF-8 document to write.</param>
    /// <param name="context">The command context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task WriteAsync(ReadOnlyMemory<byte> document, CommandContext context, CancellationToken cancellationToken);
}
