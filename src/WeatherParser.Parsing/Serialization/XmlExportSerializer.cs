using System.Collections.Concurrent;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Serialization;

/// <summary>
/// Serializes export roots to XML format.
/// </summary>
public sealed class XmlExportSerializer : IXmlExportSerializer
{
    private readonly ConcurrentDictionary<Type, XmlSerializer> _serializers = new();

    /// <inheritdoc/>
    /// <exception cref="OperationCanceledException">The token was canceled before or during the copy to the destination.</exception>
    /// <exception cref="ExportSerializationException">The root could not be serialized as an XML document; nothing was written to the destination.</exception>
    public async Task SerializeAsync<TRoot>(TRoot root, Stream destination, CancellationToken cancellationToken)
        where TRoot : class
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(destination);
        cancellationToken.ThrowIfCancellationRequested();
        SupportedExportRoots.ThrowIfUnsupported(typeof(TRoot));

        using var buffer = new MemoryStream();
        try
        {
            if (root is IExportRootValidation validation)
            {
                validation.ValidateForExport();
            }
            var serializer = _serializers.GetOrAdd(typeof(TRoot), type => new XmlSerializer(type));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, ExportNamespaces.V1);
            var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), CloseOutput = false, NewLineHandling = NewLineHandling.Entitize };
            using (var writer = XmlWriter.Create(buffer, settings))
            {
                serializer.Serialize(writer, root, namespaces);
            }
        }
        catch (InvalidOperationException exception)
        {
            throw new ExportSerializationException("The export root could not be serialized as an XML document.", exception);
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(destination, cancellationToken);
    }
}
