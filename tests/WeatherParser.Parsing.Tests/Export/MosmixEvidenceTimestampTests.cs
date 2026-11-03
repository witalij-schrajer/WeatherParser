using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Schemas;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class MosmixEvidenceTimestampTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    private static readonly DateTime Retrieved = new(2026, 10, 21, 4, 12, 7, DateTimeKind.Utc);
    private static readonly DateTime Issued = new(2026, 10, 21, 3, 0, 0, DateTimeKind.Utc);

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    private static MosmixLocationList ListWith(DateTime? retrieved, DateTime? issued)
    {
        var list = ListTestData.ValidMosmixList();
        var item = list.Locations[0].Products[0];
        item.EvidenceRetrievedAtUtc = retrieved;
        item.EvidenceIssuedAtUtc = issued;
        return list;
    }

    [TestMethod]
    public async Task Json_EvidenceTimestampsPresent_AppendedAfterElementsInContractOrder()
    {
        var list = ListWith(Retrieved, Issued);
        var jsonBytes = await AllRootsTestData.JsonBytesAsync(list, _json);
        using var doc = JsonDocument.Parse(jsonBytes);
        var product = doc.RootElement.GetProperty("locations")[0].GetProperty("products")[0];
        var propertyNames = product.EnumerateObject().Select(p => p.Name).ToArray();
        propertyNames.Should().Equal("product", "availability", "available_forecast_elements", "evidence_retrieved_at", "evidence_issued_at");
        product.GetProperty("evidence_retrieved_at").GetString().Should().Be("2026-10-21T04:12:07");
        product.GetProperty("evidence_issued_at").GetString().Should().Be("2026-10-21T03:00:00");
    }

    [TestMethod]
    public async Task Xml_EvidenceTimestampsPresent_AppendedAfterElementsInContractOrder()
    {
        var list = ListWith(Retrieved, Issued);
        var xmlBytes = await AllRootsTestData.XmlBytesAsync(list, _xml);
        var doc = XDocument.Load(new MemoryStream(xmlBytes));
        var productElement = doc.Root!.Descendants(Ns + "product_availability").First();
        var elementNames = productElement.Elements().Select(e => e.Name.LocalName).ToArray();
        elementNames.Should().Equal("product", "availability", "available_forecast_elements", "evidence_retrieved_at", "evidence_issued_at");
        productElement.Element(Ns + "evidence_retrieved_at")!.Value.Should().Be("2026-10-21T04:12:07");
        productElement.Element(Ns + "evidence_issued_at")!.Value.Should().Be("2026-10-21T03:00:00");
    }

    [TestMethod]
    public async Task EvidenceTimestampsAbsent_NoMemberInJsonOrXml()
    {
        var list = ListWith(null, null);
        var jsonBytes = await AllRootsTestData.JsonBytesAsync(list, _json);
        using var doc = JsonDocument.Parse(jsonBytes);
        var product = doc.RootElement.GetProperty("locations")[0].GetProperty("products")[0];
        product.TryGetProperty("evidence_retrieved_at", out _).Should().BeFalse();
        product.TryGetProperty("evidence_issued_at", out _).Should().BeFalse();

        var xmlBytes = await AllRootsTestData.XmlBytesAsync(list, _xml);
        var docXml = XDocument.Load(new MemoryStream(xmlBytes));
        var productElement = docXml.Root!.Descendants(Ns + "product_availability").First();
        productElement.Elements(Ns + "evidence_retrieved_at").Should().BeEmpty();
        productElement.Elements(Ns + "evidence_issued_at").Should().BeEmpty();
        docXml.Root!.DescendantsAndSelf().Attributes().Where(a => a.Name.LocalName == "nil").Should().BeEmpty();
        docXml.Descendants().Where(e => e.Name.LocalName is "evidence_retrieved_at" or "evidence_issued_at").Should().BeEmpty();
    }

    [TestMethod]
    public void Companions_RejectNonUtcKinds()
    {
        var item = new MosmixProductAvailability();
        foreach (var kind in new[] { DateTimeKind.Local, DateTimeKind.Unspecified })
        {
            Action act1 = () => item.EvidenceRetrievedAtUtc = new DateTime(2026, 10, 21, 4, 12, 7, kind);
            act1.Should().Throw<ArgumentException>();
            Action act2 = () => item.EvidenceIssuedAtUtc = new DateTime(2026, 10, 21, 4, 12, 7, kind);
            act2.Should().Throw<ArgumentException>();
        }
    }

    [TestMethod]
    public void Companions_RejectFractionalSeconds()
    {
        var item = new MosmixProductAvailability();
        Action act1 = () => item.EvidenceRetrievedAtUtc = Retrieved.AddMilliseconds(500);
        act1.Should().Throw<ArgumentException>();
        Action act2 = () => item.EvidenceIssuedAtUtc = Issued.AddMilliseconds(500);
        act2.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Companions_NullClearsTheValue()
    {
        var item = new MosmixProductAvailability();
        item.EvidenceRetrievedAtUtc = Retrieved;
        item.EvidenceIssuedAtUtc = Issued;
        item.EvidenceRetrievedAtUtc.Should().Be(Retrieved);
        item.EvidenceIssuedAtUtc.Should().Be(Issued);
        item.EvidenceRetrievedAtUtc = null;
        item.EvidenceIssuedAtUtc = null;
        item.EvidenceRetrievedAtUtc.Should().BeNull();
        item.EvidenceIssuedAtUtc.Should().BeNull();
        item.EvidenceRetrievedAtWire.Should().BeNull();
        item.EvidenceIssuedAtWire.Should().BeNull();
    }

    [TestMethod]
    public void Wire_RoundTripsThroughCompanion()
    {
        var item = new MosmixProductAvailability();
        item.EvidenceIssuedAtWire = "2026-10-21T03:00:00";
        item.EvidenceIssuedAtUtc.Should().Be(Issued);
        item.EvidenceIssuedAtUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
        Action act = () => item.EvidenceIssuedAtWire = "2026-10-21T03:00:00Z";
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public async Task OnlyOneTimestampPresent_IsRejectedInBothFormatsAndWritesNothing()
    {
        using var jsonStream = new MemoryStream();
        var list1 = ListWith(Retrieved, null);
        Func<Task> jsonAct1 = () => _json.SerializeAsync(list1, jsonStream, CancellationToken.None);
        await jsonAct1.Should().ThrowAsync<ExportSerializationException>();
        jsonStream.Length.Should().Be(0);

        using var xmlStream = new MemoryStream();
        Func<Task> xmlAct1 = () => _xml.SerializeAsync(list1, xmlStream, CancellationToken.None);
        await xmlAct1.Should().ThrowAsync<ExportSerializationException>();
        xmlStream.Length.Should().Be(0);

        using var jsonStream2 = new MemoryStream();
        var list2 = ListWith(null, Issued);
        Func<Task> jsonAct2 = () => _json.SerializeAsync(list2, jsonStream2, CancellationToken.None);
        await jsonAct2.Should().ThrowAsync<ExportSerializationException>();
        jsonStream2.Length.Should().Be(0);

        using var xmlStream2 = new MemoryStream();
        Func<Task> xmlAct2 = () => _xml.SerializeAsync(list2, xmlStream2, CancellationToken.None);
        await xmlAct2.Should().ThrowAsync<ExportSerializationException>();
        xmlStream2.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task ReadBack_JsonAndXml_RestoreTheCompanions()
    {
        var list = ListWith(Retrieved, Issued);
        var jsonBytes = await AllRootsTestData.JsonBytesAsync(list, _json);
        var deserializedList = JsonSerializer.Deserialize<MosmixLocationList>(jsonBytes);
        deserializedList.Should().NotBeNull();
        deserializedList!.Locations[0].Products[0].EvidenceRetrievedAtUtc.Should().Be(Retrieved);
        deserializedList!.Locations[0].Products[0].EvidenceIssuedAtUtc.Should().Be(Issued);
        deserializedList!.Locations[0].Products[1].EvidenceRetrievedAtUtc.Should().BeNull();
        deserializedList!.Locations[0].Products[1].EvidenceIssuedAtUtc.Should().BeNull();

        var xmlBytes = await AllRootsTestData.XmlBytesAsync(list, _xml);
        var xmlDeserialized = new XmlSerializer(typeof(MosmixLocationList), "urn:weatherparser:export:v1").Deserialize(new MemoryStream(xmlBytes)) as MosmixLocationList;
        xmlDeserialized.Should().NotBeNull();
        xmlDeserialized!.Locations[0].Products[0].EvidenceRetrievedAtUtc.Should().Be(Retrieved);
        xmlDeserialized!.Locations[0].Products[0].EvidenceIssuedAtUtc.Should().Be(Issued);
        xmlDeserialized!.Locations[0].Products[1].EvidenceRetrievedAtUtc.Should().BeNull();
        xmlDeserialized!.Locations[0].Products[1].EvidenceIssuedAtUtc.Should().BeNull();
    }

    [TestMethod]
    public async Task Schema_AcceptsTheExportWithAndWithoutEvidenceTimestamps()
    {
        var list1 = ListWith(Retrieved, Issued);
        var xmlBytes1 = await AllRootsTestData.XmlBytesAsync(list1, _xml);
        ExportSchemaValidation.Validate(xmlBytes1).Should().BeEmpty();

        var list2 = ListWith(null, null);
        var xmlBytes2 = await AllRootsTestData.XmlBytesAsync(list2, _xml);
        ExportSchemaValidation.Validate(xmlBytes2).Should().BeEmpty();

        TimestampSemantics.Violations(xmlBytes1).Should().BeEmpty();
    }

    [TestMethod]
    [DataRow("2026-10-21T04:12:07Z")]
    [DataRow("2026-10-21T04:12:07+02:00")]
    [DataRow("2026-10-21T04:12:07.5")]
    public async Task Schema_RejectsMalformedEvidenceTimestamps(string malformedValue)
    {
        var list = ListWith(Retrieved, Issued);
        var xmlBytes = await AllRootsTestData.XmlBytesAsync(list, _xml);
        var doc = XDocument.Load(new MemoryStream(xmlBytes));
        doc.Root!.Descendants(Ns + "evidence_retrieved_at").First().Value = malformedValue;
        using var mutatedStream = new MemoryStream();
        doc.Save(mutatedStream);
        mutatedStream.Position = 0;
        var mutatedBytes = mutatedStream.ToArray();
        ExportSchemaValidation.Validate(mutatedBytes).Should().NotBeEmpty();
    }
}
