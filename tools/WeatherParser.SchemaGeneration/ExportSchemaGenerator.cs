using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using WeatherParser.Parsing.Export;

namespace WeatherParser.SchemaGeneration;

/// <summary>
/// Exports and refines the export schema deterministically (contracts C-95 to C-102).
/// </summary>
public sealed class ExportSchemaGenerator : IExportSchemaGenerator
{
    /// <summary>
    /// The export roots in the fixed export order of contract C-97.
    /// </summary>
    public static IReadOnlyList<Type> RootTypes { get; } = new[] { typeof(ExportDocument), typeof(CdcStationList), typeof(MosmixLocationList), typeof(ObservationDataset), typeof(ForecastRun) };

    private const string GeneratedComment = "Generated from the WeatherParser export entities by ExportSchemaGenerator; do not edit. Contract: docs/p2/P2_EXPORT_CONTRACT.md";
    
    private readonly IExportSchemaRefiner _refiner;

    /// <summary>
    /// Initializes the generator with the refinement step.
    /// </summary>
    /// <param name="refiner">The refiner to use.</param>
    public ExportSchemaGenerator(IExportSchemaRefiner refiner)
    {
        ArgumentNullException.ThrowIfNull(refiner);
        _refiner = refiner;
    }

    /// <inheritdoc/>
    public byte[] Generate()
    {
        var importer = new XmlReflectionImporter();
        var schemas = new XmlSchemas();
        var exporter = new XmlSchemaExporter(schemas);

        foreach (var type in RootTypes)
        {
            exporter.ExportTypeMapping(importer.ImportTypeMapping(type));
        }

        if (schemas.Count != 1)
            throw new InvalidOperationException($"Expected one exported schema, found {schemas.Count}.");

        using var memoryStream = new MemoryStream();
        schemas[0].Write(memoryStream);
        memoryStream.Flush();
        var raw = memoryStream.ToArray();

        var document = XDocument.Load(new MemoryStream(raw));

        _refiner.Refine(document, RootTypes);

        document.Root!.AddFirst(new XComment(" " + GeneratedComment + " "));

        return Write(document);
    }

    private static byte[] Write(XDocument document)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Replace
        };

        using var memoryStream = new MemoryStream();
        using var writer = XmlWriter.Create(memoryStream, settings);
        document.Save(writer);
        writer.Flush();

        var result = memoryStream.ToArray();
        var list = new List<byte>(result)
        {
            (byte)'\n'
        };
        return list.ToArray();
    }
}
