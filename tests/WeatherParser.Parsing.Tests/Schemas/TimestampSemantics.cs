using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace WeatherParser.Parsing.Tests.Schemas;

/// <summary>
/// Checks the calendar validity of every timestamp in an export document, which the schema pattern cannot (contract C-102).
/// </summary>
internal static class TimestampSemantics
{
    /// <summary>
    /// Returns one message per timestamp element whose value is not a real UTC calendar instant.
    /// </summary>
    public static IReadOnlyList<string> Violations(byte[] document)
    {
        var violations = new List<string>();
        var schemas = ExportSchemaValidation.LoadSchemas(ExportSchemaValidation.SchemaPath, (_, e) => violations.Add("schema: " + e.Message));
        using var reader = XmlReader.Create(new MemoryStream(document), ExportSchemaValidation.SecureReaderSettings());
        var xml = XDocument.Load(reader);
        xml.Validate(schemas, (_, e) => violations.Add("validation: " + e.Message), addSchemaInfo: true);

        foreach (var element in xml.Descendants())
        {
            var schemaType = element.GetSchemaInfo()?.SchemaType;
            if (schemaType?.Name == "UtcTimestamp")
            {
                if (!DateTime.TryParseExact(element.Value, "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    violations.Add($"{element.Name.LocalName} '{element.Value}' is not a real calendar instant (contract C-06).");
                }
            }
        }

        return violations;
    }
}
