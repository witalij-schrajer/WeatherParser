using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.SchemaGeneration;

namespace WeatherParser.SchemaGeneration.Tests;

[TestClass]
public sealed class ExportSchemaGeneratorTests
{
    private IExportSchemaGenerator _generator = null!;
    private byte[] _bytes = null!;
    private string _text = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _generator = new ExportSchemaGenerator(new ExportSchemaRefiner());
        _bytes = _generator.Generate();
        _text = new UTF8Encoding(false, true).GetString(_bytes);
    }

    private static readonly XNamespace Xs = "http://www.w3.org/2001/XMLSchema";
    private const string ExpectedComment = "Generated from the WeatherParser export entities by ExportSchemaGenerator; do not edit. Contract: docs/p2/P2_EXPORT_CONTRACT.md";

    [TestMethod]
    public void Generate_WritesUtf8WithoutBomAndOnlyLfLineEndings()
    {
        _bytes.Should().NotBeEmpty();
        _bytes.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
        _text.Should().NotContain("\r");
        _text.Should().StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<xs:schema ");
    }

    [TestMethod]
    public void Generate_EndsWithExactlyOneNewline()
    {
        _text.Should().EndWith("</xs:schema>\n");
        _text.Should().NotEndWith("\n\n");
    }

    [TestMethod]
    public void Generate_FirstChildIsTheFixedComment()
    {
        var root = XDocument.Parse(_text).Root!;
        root.Nodes().First().Should().BeOfType<XComment>().Which.Value.Trim().Should().Be(ExpectedComment);
        _text.Split('\n')[2].Should().StartWith("  <!--");
    }

    [TestMethod]
    public void Generate_DeclaresTheFiveRootsInFixedOrder()
    {
        var root = XDocument.Parse(_text).Root!;
        var elements = root.Elements(Xs + "element").Select(e => e.Attribute("name")!.Value).ToArray();
        elements.Should().Equal("export_document", "cdc_station_list", "mosmix_location_list", "observation_dataset", "forecast_run");
        root.Attribute("targetNamespace")!.Value.Should().Be("urn:weatherparser:export:v1");
    }
}
