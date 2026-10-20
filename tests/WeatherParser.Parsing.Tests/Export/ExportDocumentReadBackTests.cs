using System.Text.Json;
using System.Xml.Serialization;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ExportDocumentReadBackTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    [TestMethod]
    public async Task Json_ReadBack_RestoresTheEnvelope()
    {
        var original = AllRootsTestData.RichDocument();

        using var stream = new MemoryStream();
        await _json.SerializeAsync(original, stream, CancellationToken.None);
        var bytes = stream.ToArray();

        var copy = JsonSerializer.Deserialize<ExportDocument>(bytes)!;

        copy.SchemaVersion.Should().Be("1");
        copy.Observations.Select(o => o.Source).Should().Equal("dwd:00044", "dwd:01766");
        copy.Observations[0].Location.Name.Should().Be(AllRootsTestData.SpecialText);
        copy.ForecastRuns.Should().HaveCount(1);
        copy.ForecastRuns[0].Source.Should().Be("dwd-mosmix-l:10637");
        copy.Errors.Should().HaveCount(1);
        copy.Errors[0].ErrorId.Should().Be("DWD-RETRIEVAL-FAILED");
        copy.Errors[0].Message.Should().Be("Station <Ä&Ö> \"02000\" failed.");
        copy.Errors[0].Source.Should().Be("dwd:02000");

        using var stream2 = new MemoryStream();
        await _json.SerializeAsync(copy, stream2, CancellationToken.None);
        var bytes2 = stream2.ToArray();

        bytes2.Should().Equal(bytes);
    }

    [TestMethod]
    public async Task Xml_ReadBack_RestoresTheEnvelope()
    {
        var original = AllRootsTestData.RichDocument();

        using var stream = new MemoryStream();
        await _xml.SerializeAsync(original, stream, CancellationToken.None);
        var bytes = stream.ToArray();

        var copy = (ExportDocument)new XmlSerializer(typeof(ExportDocument)).Deserialize(new MemoryStream(bytes))!;

        copy.SchemaVersion.Should().Be("1");
        copy.Observations.Select(o => o.Source).Should().Equal("dwd:00044", "dwd:01766");
        copy.Observations[0].Location.Name.Should().Be(AllRootsTestData.SpecialText);
        copy.ForecastRuns.Should().HaveCount(1);
        copy.ForecastRuns[0].Source.Should().Be("dwd-mosmix-l:10637");
        copy.Errors.Should().HaveCount(1);
        copy.Errors[0].ErrorId.Should().Be("DWD-RETRIEVAL-FAILED");
        copy.Errors[0].Message.Should().Be("Station <Ä&Ö> \"02000\" failed.");
        copy.Errors[0].Source.Should().Be("dwd:02000");

        using var stream2 = new MemoryStream();
        await _xml.SerializeAsync(copy, stream2, CancellationToken.None);
        var bytes2 = stream2.ToArray();

        bytes2.Should().Equal(bytes);
    }

    [TestMethod]
    public void SchemaVersion_OtherThanOne_IsRejected()
    {
        FluentActions.Invoking(() => new ExportDocument().SchemaVersion = "2")
            .Should().Throw<ArgumentException>()
            .WithParameterName("SchemaVersion");
    }

    [TestMethod]
    public void SchemaVersion_One_IsAccepted()
    {
        var document = new ExportDocument();
        document.SchemaVersion = "1";
        document.SchemaVersion.Should().Be("1");
    }
}
