namespace WeatherParser.Cli.Output;

using WeatherParser.Cli.Commands;

/// <summary>
/// Writes export roots.
/// </summary>
public interface IResultWriter
{
    /// <summary>
    /// Writes one export root in the output mode of the invocation: a complete JSON or XML document, or one MQTT message (CL-09, CL-12).
    /// </summary>
    /// <param name="root">The export root to write.</param>
    /// <param name="context">The command context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TRoot">The type of the export root.</typeparam>
    Task WriteAsync<TRoot>(TRoot root, CommandContext context, CancellationToken cancellationToken) where TRoot : class;
}
