using System.Xml;
using System.Xml.Schema;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Package.Tests;

[TestClass]
public sealed class PackageSchemaDeliveryTests
{
    private ServiceProvider _provider = null!;
    private IXmlExportSerializer _xml = null!;

    private static readonly string PackagedSchema = Path.Combine(AppContext.BaseDirectory, "Schemas", "weatherparser-export-v1.xsd");
    private static readonly string ReferenceSchema = Path.Combine(AppContext.BaseDirectory, "ReferenceSchemas", "weatherparser-export-v1.xsd");

    [TestInitialize]
    public void TestInitialize()
    {
        _provider = new ServiceCollection().AddWeatherParsing().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        _xml = _provider.GetRequiredService<IXmlExportSerializer>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _provider.Dispose();
    }

    private static IReadOnlyList<string> Validate(byte[] document)
    {
        var events = new List<string>();
        var schemas = new XmlSchemaSet { XmlResolver = null };
        schemas.ValidationEventHandler += (_, e) => events.Add(e.Severity + ": " + e.Message);
        using (var reader = XmlReader.Create(PackagedSchema, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
        {
            schemas.Add(null, reader);
        }
        schemas.Compile();
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, ValidationType = ValidationType.Schema, ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings, Schemas = schemas };
        settings.ValidationEventHandler += (_, e) => events.Add(e.Severity + ": " + e.Message);
        using (var documentReader = XmlReader.Create(new MemoryStream(document), settings))
        {
            while (documentReader.Read())
            {
            }
        }
        return events;
    }

    private async Task<byte[]> ExportAsync<TRoot>(TRoot root) where TRoot : class
    {
        using var stream = new MemoryStream();
        await _xml.SerializeAsync(root, stream, CancellationToken.None);
        return stream.ToArray();
    }

    [TestMethod]
    public void Package_DeliversTheSchemaFile()
    {
        File.Exists(PackagedSchema).Should().BeTrue("the package must contain schemas/export/v1/weatherparser-export-v1.xsd");
        File.Exists(ReferenceSchema).Should().BeTrue("the committed schema of this clone must be copied for comparison");
    }

    [TestMethod]
    public void Package_Schema_IsByteIdenticalToTheCommittedFile()
    {
        File.Exists(PackagedSchema).Should().BeTrue();
        File.Exists(ReferenceSchema).Should().BeTrue();

        File.ReadAllBytes(PackagedSchema).Should().Equal(File.ReadAllBytes(ReferenceSchema));
    }

    [DataRow("export_document")]
    [DataRow("cdc_station_list")]
    [DataRow("mosmix_location_list")]
    [DataRow("observation_dataset")]
    [DataRow("forecast_run")]
    [TestMethod]
    public async Task Package_Schema_ValidatesConsumerExports(string rootName)
    {
        File.Exists(PackagedSchema).Should().BeTrue();

        byte[] bytes;
        switch (rootName)
        {
            case "export_document": bytes = await ExportAsync(ConsumerTestData.ValidDocument()); break;
            case "cdc_station_list": bytes = await ExportAsync(ConsumerTestData.ValidCdcList()); break;
            case "mosmix_location_list": bytes = await ExportAsync(ConsumerTestData.ValidMosmixList()); break;
            case "observation_dataset": bytes = await ExportAsync(ConsumerTestData.ValidDataset()); break;
            case "forecast_run": bytes = await ExportAsync(ConsumerTestData.ValidRun()); break;
            default: Assert.Fail(rootName); return;
        }

        Validate(bytes).Should().BeEmpty();
    }
}
