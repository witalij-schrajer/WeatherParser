using System.Xml;
using System.Xml.Schema;

namespace WeatherParser.Parsing.Tests.Schemas;

/// <summary>
/// Validates XML documents against the committed export schema, which the test project copies to its output.
/// </summary>
internal static class ExportSchemaValidation
{
    /// <summary>
    /// The committed schema copied to the test output.
    /// </summary>
    public static string SchemaPath { get; } = Path.Combine(AppContext.BaseDirectory, "Schemas", "weatherparser-export-v1.xsd");

    /// <summary>
    /// Reader settings that prohibit DTDs and resolve nothing (contract C-104); every document and schema in these tests is read with them.
    /// </summary>
    public static XmlReaderSettings SecureReaderSettings() => new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };

    /// <summary>
    /// Loads and compiles a local schema file without resolving any import, include or DTD (contract C-104).
    /// </summary>
    public static XmlSchemaSet LoadSchemas(string schemaPath, ValidationEventHandler handler)
    {
        var schemas = new XmlSchemaSet { XmlResolver = null };
        schemas.ValidationEventHandler += handler;
        using (var schemaReader = XmlReader.Create(schemaPath, SecureReaderSettings()))
        {
            schemas.Add(null, schemaReader);
        }
        schemas.Compile();
        return schemas;
    }

    /// <summary>
    /// Returns every schema and validation error and warning of the document; empty means valid.
    /// </summary>
    public static IReadOnlyList<string> Validate(byte[] document)
    {
        var events = new List<string>();
        var schemas = LoadSchemas(SchemaPath, (sender, e) => events.Add(e.Severity + ": " + e.Message));
        var settings = SecureReaderSettings();
        settings.ValidationType = ValidationType.Schema;
        settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
        settings.Schemas = schemas;
        settings.ValidationEventHandler += (_, e) => events.Add(e.Severity + ": " + e.Message);
        using (var reader = XmlReader.Create(new MemoryStream(document), settings))
        {
            while (reader.Read())
            {
            }
        }
        return events;
    }
}
