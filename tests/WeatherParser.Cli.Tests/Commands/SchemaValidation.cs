using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Collections.Generic;

namespace WeatherParser.Cli.Tests.Commands;

internal static class SchemaValidation
{
    /// <summary>
    /// Validates an XML document against the XSD packaged with the library and returns every validation event; an empty list means the document is valid.
    /// </summary>
    public static IReadOnlyList<string> ValidateAgainstPackagedSchema(byte[] document)
    {
        var events = new List<string>();
        var schemas = new XmlSchemaSet { XmlResolver = null };
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Schemas", "weatherparser-export-v1.xsd");
        schemas.ValidationEventHandler += (_, e) => events.Add(e.Severity + ": " + e.Message);
        using (var reader = XmlReader.Create(schemaPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
        {
            schemas.Add(null, reader);
        }
        schemas.Compile();
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            ValidationType = ValidationType.Schema,
            ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings,
            Schemas = schemas
        };
        settings.ValidationEventHandler += (_, e) => events.Add(e.Severity + ": " + e.Message);
        using (var documentReader = XmlReader.Create(new MemoryStream(document), settings))
        {
            while (documentReader.Read())
            {
            }
        }
        return events;
    }
}
