namespace WeatherParser.SchemaGeneration;

/// <summary>
/// Generates the XML Schema of the WeatherParser export roots from their XmlSerializer mappings (contract C-95).
/// </summary>
public interface IExportSchemaGenerator
{
    /// <summary>
    /// Returns the complete schema file content: UTF-8 without BOM, LF line endings (contract C-97).
    /// </summary>
    byte[] Generate();
}
