using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class LineBreakPreservationTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    private const string NameWithBreaks = "Line1\rLine2\nLine3\r\nLine4";

    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    private async Task<(byte[] Json, byte[] Xml)> SerializeBoth<TRoot>(TRoot root) where TRoot : class
    {
        using var jsonStream = new MemoryStream();
        using var xmlStream = new MemoryStream();

        await _json.SerializeAsync(root, jsonStream, CancellationToken.None);
        await _xml.SerializeAsync(root, xmlStream, CancellationToken.None);

        return (jsonStream.ToArray(), xmlStream.ToArray());
    }

    private Task<(byte[] Json, byte[] Xml)> SerializeRoot(string root, ObservationDataset dataset)
    {
        if (root == "observation_dataset")
        {
            return SerializeBoth(dataset);
        }

        return SerializeBoth(new ExportDocument { Observations = { dataset } });
    }

    [TestMethod]
    [DataRow("observation_dataset")]
    [DataRow("export_document")]
    public async Task Serialize_NameWithCrLfAndCrLf_ReadsBackUnchangedFromBothFormats(string root)
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Location.Name = NameWithBreaks;

        var (json, xml) = await SerializeRoot(root, dataset);

        using var jsonDoc = JsonDocument.Parse(json);
        var jsonDataset = root == "observation_dataset"
            ? jsonDoc.RootElement
            : jsonDoc.RootElement.GetProperty("observations").EnumerateArray().First();
        var jsonLocation = jsonDataset.GetProperty("location");
        var jsonName = jsonLocation.GetProperty("name").GetString()!;
        jsonName.Should().Be(NameWithBreaks);

        var xmlRoot = XDocument.Load(new MemoryStream(xml)).Root!;
        var xmlDataset = root == "observation_dataset"
            ? xmlRoot
            : xmlRoot.Element(Ns + "observations")!.Element(Ns + "observation_dataset")!;
        var xmlLocation = xmlDataset.Element(Ns + "location")!;
        var xmlName = xmlLocation.Element(Ns + "name")!.Value;
        xmlName.Should().Be(NameWithBreaks);
    }

    [TestMethod]
    [DataRow("observation_dataset")]
    [DataRow("export_document")]
    public async Task Serialize_Xml_WritesCarriageReturnAsCharacterReferenceIndependentOfPlatform(string root)
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Location.Name = NameWithBreaks;

        var (_, xml) = await SerializeRoot(root, dataset);

        var text = Encoding.UTF8.GetString(xml);
        text.Should().NotContain("\r");
        text.Should().Contain("Line1&#xD;Line2\nLine3&#xD;\nLine4");
    }
}
