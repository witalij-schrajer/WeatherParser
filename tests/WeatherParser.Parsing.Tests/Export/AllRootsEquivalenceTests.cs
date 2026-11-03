using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Export;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class AllRootsEquivalenceTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    [TestMethod]
    [DynamicData(nameof(AllRootsTestData.Roots), typeof(AllRootsTestData))]
    public async Task AllRoots_JsonAndXml_AreSemanticallyEqual(string rootName)
    {
        var root = AllRootsTestData.Build(rootName);
        var json = await AllRootsTestData.JsonBytesAsync(root, _json);
        var xml = await AllRootsTestData.XmlBytesAsync(root, _xml);

        var xmlRoot = XDocument.Load(new MemoryStream(xml)).Root!;
        xmlRoot.Name.LocalName.Should().Be(rootName);

        using var document = JsonDocument.Parse(json);
        ExportEquivalence.AssertEquivalent(document.RootElement, xmlRoot, rootName);
    }

    [TestMethod]
    public async Task Envelope_KeepsSpecialTextZeroOmissionAndLeadingZerosInBothFormats()
    {
        var root = AllRootsTestData.RichDocument();
        var json = await AllRootsTestData.JsonBytesAsync(root, _json);
        var xml = await AllRootsTestData.XmlBytesAsync(root, _xml);

        // JSON assertions
        using var document = JsonDocument.Parse(json);
        var observations = document.RootElement.GetProperty("observations");
        var firstObservation = observations[0];
        firstObservation.GetProperty("location").GetProperty("name").GetString().Should().Be(AllRootsTestData.SpecialText);
        firstObservation.GetProperty("location").TryGetProperty("altitude_m", out _).Should().BeFalse();
        firstObservation.GetProperty("measurements").GetProperty("temperature_c").GetProperty("value").GetRawText().Should().Be("0.0");
        firstObservation.GetProperty("location").GetProperty("station_id").GetString().Should().Be("00044");
        document.RootElement.GetProperty("errors")[0].GetProperty("message").GetString().Should().Be("Station <Ä&Ö> \"02000\" failed.");

        // XML assertions
        var xmlDocument = XDocument.Load(new MemoryStream(xml));
        var firstObsXml = xmlDocument.Root!.Element(Ns + "observations")!.Elements(Ns + "observation_dataset").First();
        firstObsXml.Element(Ns + "location")!.Element(Ns + "name")!.Value.Should().Be(AllRootsTestData.SpecialText);
        firstObsXml.Element(Ns + "location")!.Element(Ns + "altitude_m").Should().BeNull();
        firstObsXml.Element(Ns + "measurements")!.Element(Ns + "temperature_c")!.Element(Ns + "value")!.Value.Should().Be("0.0");
        firstObsXml.Element(Ns + "location")!.Element(Ns + "station_id")!.Value.Should().Be("00044");
        xmlDocument.Root!.Element(Ns + "errors")!.Elements(Ns + "error").First().Element(Ns + "message")!.Value.Should().Be("Station <Ä&Ö> \"02000\" failed.");

        // UTF-8 text checks
        Encoding.UTF8.GetString(json).Should().Contain("Großenkneten");
        Encoding.UTF8.GetString(json).Should().Contain("\\u003CA\\u0026B\\u003E");
        var xmlString = Encoding.UTF8.GetString(xml);
        xmlString.Should().Contain("Großenkneten &lt;A&amp;B&gt;");
    }
}
