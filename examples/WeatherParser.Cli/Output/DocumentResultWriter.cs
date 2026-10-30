namespace WeatherParser.Cli.Output;

using System.Text;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentResultWriter"/> class.
    /// </summary>
    /// <param name="jsonSerializer">The JSON serializer.</param>
    /// <param name="xmlSerializer">The XML serializer.</param>
    public DocumentResultWriter(IJsonExportSerializer jsonSerializer, IXmlExportSerializer xmlSerializer)
    {
        _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _xmlSerializer = xmlSerializer ?? throw new ArgumentNullException(nameof(xmlSerializer));
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

        var document = Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
        await context.StandardOutput.WriteAsync(document.AsMemory(), cancellationToken).ConfigureAwait(false);
        await context.StandardOutput.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
