namespace WeatherParser.Cli.Output;

using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Arguments;
using WeatherParser.Parsing.Serialization;

/// <summary>
/// Writes one export root as a complete JSON or XML document to standard output or to a file (CL-12).
/// </summary>
public sealed class DocumentResultWriter : IResultWriter
{
    private readonly IJsonExportSerializer _jsonSerializer;
    private readonly IXmlExportSerializer _xmlSerializer;
    private readonly IOutputDestination _destination;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentResultWriter"/> class.
    /// </summary>
    /// <param name="jsonSerializer">The JSON serializer.</param>
    /// <param name="xmlSerializer">The XML serializer.</param>
    /// <param name="destination">The destination of the serialized document.</param>
    public DocumentResultWriter(IJsonExportSerializer jsonSerializer, IXmlExportSerializer xmlSerializer, IOutputDestination destination)
    {
        _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _xmlSerializer = xmlSerializer ?? throw new ArgumentNullException(nameof(xmlSerializer));
        _destination = destination ?? throw new ArgumentNullException(nameof(destination));
    }

    /// <inheritdoc/>
    public async Task WriteAsync<TRoot>(TRoot root, CommandContext context, CancellationToken cancellationToken) where TRoot : class
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(context);

        // The whole document is serialized into memory before anything is written, so the output never holds a partial document (CL-12, REQ-069).
        using var buffer = new MemoryStream();
        switch (context.Options.Output)
        {
            case OutputFormat.Json:
                await _jsonSerializer.SerializeAsync(root, buffer, cancellationToken).ConfigureAwait(false);
                break;
            case OutputFormat.Xml:
                await _xmlSerializer.SerializeAsync(root, buffer, cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new InvalidOperationException("The document writer handles only JSON and XML output.");
        }

        await _destination.WriteAsync(buffer.GetBuffer().AsMemory(0, (int)buffer.Length), context, cancellationToken).ConfigureAwait(false);
    }
}
