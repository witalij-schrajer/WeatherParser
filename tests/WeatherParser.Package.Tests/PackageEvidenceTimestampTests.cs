using System;
using System.IO;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Package.Tests;

[TestClass]
public sealed class PackageEvidenceTimestampTests
{
    private ServiceProvider _provider = null!;
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _provider = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        _json = _provider.GetRequiredService<IJsonExportSerializer>();
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
        using (var reader = XmlReader.Create(Path.Combine(AppContext.BaseDirectory, "Schemas", "weatherparser-export-v1.xsd"), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
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

    [TestMethod]
    public async Task PackagedLibrary_SerializesEvidenceTimestampsInJsonAndXml()
    {
        var list = ConsumerTestData.ValidMosmixList();
        list.Locations[0].Products[0].EvidenceRetrievedAtUtc = new DateTime(2026, 10, 21, 4, 12, 7, DateTimeKind.Utc);
        list.Locations[0].Products[0].EvidenceIssuedAtUtc = new DateTime(2026, 10, 21, 3, 0, 0, DateTimeKind.Utc);

        var jsonBytes = await SerializeJsonAsync(list);
        var xmlBytes = await SerializeXmlAsync(list);

        // Check JSON
        var jsonDoc = JsonDocument.Parse(jsonBytes);
        var firstProduct = jsonDoc.RootElement.GetProperty("locations")[0].GetProperty("products")[0];
        firstProduct.GetProperty("evidence_retrieved_at").GetString().Should().Be("2026-10-21T04:12:07");
        firstProduct.GetProperty("evidence_issued_at").GetString().Should().Be("2026-10-21T03:00:00");

        var secondProduct = jsonDoc.RootElement.GetProperty("locations")[0].GetProperty("products")[1];
        secondProduct.TryGetProperty("evidence_retrieved_at", out _).Should().BeFalse();
        secondProduct.TryGetProperty("evidence_issued_at", out _).Should().BeFalse();

        // Check XML
        var xdoc = XDocument.Load(new MemoryStream(xmlBytes));
        var ns = XNamespace.Get("urn:weatherparser:export:v1");
        var items = xdoc.Descendants(ns + "product_availability").ToList();
        var firstProductXml = items[0];
        firstProductXml.Element(ns + "evidence_retrieved_at")!.Value.Should().Be("2026-10-21T04:12:07");
        firstProductXml.Element(ns + "evidence_issued_at")!.Value.Should().Be("2026-10-21T03:00:00");

        var secondProductXml = items[1];
        secondProductXml.Element(ns + "evidence_retrieved_at").Should().BeNull();
        secondProductXml.Element(ns + "evidence_issued_at").Should().BeNull();
    }

    [TestMethod]
    public async Task PackagedSchema_ValidatesEvidenceTimestamps()
    {
        var list = ConsumerTestData.ValidMosmixList();
        list.Locations[0].Products[0].EvidenceRetrievedAtUtc = new DateTime(2026, 10, 21, 4, 12, 7, DateTimeKind.Utc);
        list.Locations[0].Products[0].EvidenceIssuedAtUtc = new DateTime(2026, 10, 21, 3, 0, 0, DateTimeKind.Utc);

        var xmlBytes = await SerializeXmlAsync(list);
        Validate(xmlBytes).Should().BeEmpty();

        // Modify the XML to make it invalid
        var xdoc = XDocument.Load(new MemoryStream(xmlBytes));
        var ns = XNamespace.Get("urn:weatherparser:export:v1");
        var items = xdoc.Descendants(ns + "product_availability").ToList();
        var firstProductXml = items[0];
        firstProductXml.Element(ns + "evidence_retrieved_at")!.Value = "2026-10-21T04:12:07Z";
        var modifiedStream = new MemoryStream();
        xdoc.Save(modifiedStream);
        modifiedStream.Position = 0;
        Validate(modifiedStream.ToArray()).Should().NotBeEmpty();
    }

    private async Task<byte[]> SerializeJsonAsync<T>(T root) where T : class
    {
        using var stream = new MemoryStream();
        await _json.SerializeAsync(root, stream, CancellationToken.None);
        return stream.ToArray();
    }

    private async Task<byte[]> SerializeXmlAsync<T>(T root) where T : class
    {
        using var stream = new MemoryStream();
        await _xml.SerializeAsync(root, stream, CancellationToken.None);
        return stream.ToArray();
    }
}
