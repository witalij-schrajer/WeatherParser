using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using FluentAssertions;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Tests.Schemas;

[TestClass]
public sealed class SchemaExporterSpikeTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void Spike_ExportSchemas_ForAllFiveRoots()
    {
        var importer = new XmlReflectionImporter();
        var schemas = new XmlSchemas();
        var exporter = new XmlSchemaExporter(schemas);

        foreach (var type in new[] { typeof(ExportDocument), typeof(CdcStationList), typeof(MosmixLocationList), typeof(ObservationDataset), typeof(ForecastRun) })
        {
            exporter.ExportTypeMapping(importer.ImportTypeMapping(type));
        }

        TestContext.WriteLine(RuntimeInformation.OSDescription);
        TestContext.WriteLine(RuntimeInformation.ProcessArchitecture.ToString());
        TestContext.WriteLine(RuntimeInformation.FrameworkDescription);
        TestContext.WriteLine($"schemas.Count = {schemas.Count}");

        var dir = Environment.GetEnvironmentVariable("WEATHERPARSER_SPIKE_OUT");
        for (var i = 0; i < schemas.Count; i++)
        {
            var schema = schemas[i];
            using var stream = new MemoryStream();
            using var writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace
            });
            schema.Write(writer);
            writer.Flush();
            var bytes = stream.ToArray();
            var sha256 = Convert.ToHexString(SHA256.HashData(bytes));
            TestContext.WriteLine($"schema {i} targetNamespace={schema.TargetNamespace} sha256={sha256}");
            TestContext.WriteLine(Encoding.UTF8.GetString(bytes));

            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "spike-schema-" + i + ".xsd");
                File.WriteAllBytes(path, bytes);
            }
        }

        schemas.Cast<XmlSchema>().Should().Contain(s => s.TargetNamespace == "urn:weatherparser:export:v1");
    }
}
