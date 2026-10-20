#nullable enable
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class EscapingContractTests
{
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    private JsonExportSerializer? _json;
    private XmlExportSerializer? _xml;

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    [TestMethod]
    public async Task SerializeAsync_Json_SpecialCharacters_RoundTrip()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Location.Name = "Fulda-Horas (Ä<&>\"')";

        using var stream = new MemoryStream();
        await _json!.SerializeAsync(dataset, stream, CancellationToken.None);
        stream.Position = 0;

        var json = JsonDocument.Parse(stream);
        var name = json.RootElement.GetProperty("location").GetProperty("name").GetString()!;

        name.Should().Be(dataset.Location.Name);
    }

    [TestMethod]
    public async Task SerializeAsync_Json_Umlaut_IsWrittenUnescaped()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Location.Name = "Fulda-Horas (Ä<&>\"')";

        using var stream = new MemoryStream();
        await _json!.SerializeAsync(dataset, stream, CancellationToken.None);
        stream.Position = 0;

        var rawText = Encoding.UTF8.GetString(stream.ToArray());

        rawText.Should().Contain("Ä");
        rawText.Should().NotContain("\\u00C4");
        rawText.Should().Contain("\\u003C");
        rawText.Should().Contain("\\u0026");
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_SpecialCharacters_RoundTrip()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Location.Name = "Fulda-Horas (Ä<&>\"')";

        using var stream = new MemoryStream();
        await _xml!.SerializeAsync(dataset, stream, CancellationToken.None);
        stream.Position = 0;

        var doc = XDocument.Load(stream);
        var name = doc.Root!.Element(Ns + "location")!.Element(Ns + "name")!.Value;

        name.Should().Be(dataset.Location.Name);
    }

    [TestMethod]
    public async Task SerializeAsync_Json_ControlCharacter_IsEscapedAndRoundTrips()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Location.Name = "bad\u0001value";

        using var stream = new MemoryStream();
        await _json!.SerializeAsync(dataset, stream, CancellationToken.None);
        stream.Position = 0;

        var rawText = Encoding.UTF8.GetString(stream.ToArray());

        rawText.Should().Contain("\\u0001");

        using var json = JsonDocument.Parse(stream);
        var name = json.RootElement.GetProperty("location").GetProperty("name").GetString()!;

        name.Should().Be(dataset.Location.Name);
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_InvalidXmlCharacter_ThrowsExportSerializationException()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Location.Name = "bad\u0001value";

        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _xml!.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
    }
}
