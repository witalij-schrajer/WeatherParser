using System.Text;
using System.Xml.Linq;
using FluentAssertions;

namespace WeatherParser.SchemaGeneration.Tests;

[TestClass]
public sealed class ContractMappingGuardTests
{
    private IExportSchemaGenerator generator = new ExportSchemaGenerator(new ExportSchemaRefiner());
    private XElement _root = null!;
    private static readonly XNamespace Xs = "http://www.w3.org/2001/XMLSchema";

    [TestInitialize]
    public void TestInitialize()
    {
        _root = XDocument.Parse(Encoding.UTF8.GetString(generator.Generate())).Root!;
    }

    [TestMethod]
    public void Schema_ContainsNoXsAnyOrAnyAttribute()
    {
        _root.Descendants(Xs + "any").Should().BeEmpty();
        _root.Descendants(Xs + "anyAttribute").Should().BeEmpty();
    }

    [TestMethod]
    public void Schema_DeclaresTheContractNamespaceAndQualifiedElements()
    {
        _root.Attribute("targetNamespace")!.Value.Should().Be("urn:weatherparser:export:v1");
        _root.Attribute("elementFormDefault")!.Value.Should().Be("qualified");
        _root.Descendants(Xs + "attribute").Should().BeEmpty();
    }

    [TestMethod]
    public void Schema_EnvelopeMatchesTheContractRecord()
    {
        var exportDocument = ComplexType("ExportDocument");
        var elements = exportDocument.Descendants(Xs + "element").ToList();

        elements.Count.Should().Be(4);
        elements[0].Attribute("name")!.Value.Should().Be("schema_version");
        elements[0].Attribute("minOccurs")!.Value.Should().Be("1");
        elements[0].Attribute("maxOccurs")!.Value.Should().Be("1");

        elements[1].Attribute("name")!.Value.Should().Be("observations");
        elements[1].Attribute("minOccurs")!.Value.Should().Be("1");
        elements[1].Attribute("maxOccurs")!.Value.Should().Be("1");

        elements[2].Attribute("name")!.Value.Should().Be("forecast_runs");
        elements[2].Attribute("minOccurs")!.Value.Should().Be("1");
        elements[2].Attribute("maxOccurs")!.Value.Should().Be("1");

        elements[3].Attribute("name")!.Value.Should().Be("errors");
        elements[3].Attribute("minOccurs")!.Value.Should().Be("1");
        elements[3].Attribute("maxOccurs")!.Value.Should().Be("1");

        var arrayOfExportError = ComplexType("ArrayOfExportError");
        var errorElement = arrayOfExportError.Descendants(Xs + "element").Single();
        errorElement.Attribute("name")!.Value.Should().Be("error");
        errorElement.Attribute("type")!.Value.Should().Be("tns:ExportError");
        errorElement.Attribute("minOccurs")!.Value.Should().Be("0");
        errorElement.Attribute("maxOccurs")!.Value.Should().Be("unbounded");

        var exportError = ComplexType("ExportError");
        var errorElements = exportError.Descendants(Xs + "element").ToList();

        errorElements.Count.Should().Be(3);
        errorElements[0].Attribute("name")!.Value.Should().Be("error_id");
        errorElements[0].Attribute("minOccurs")!.Value.Should().Be("1");

        errorElements[1].Attribute("name")!.Value.Should().Be("message");
        errorElements[1].Attribute("minOccurs")!.Value.Should().Be("1");

        errorElements[2].Attribute("name")!.Value.Should().Be("source");
        errorElements[2].Attribute("minOccurs")!.Value.Should().Be("1");
    }

    [TestMethod]
    public void Schema_CollectionItemsAreOptionalAndUnbounded()
    {
        foreach (var complexType in _root.Elements(Xs + "complexType"))
        {
            var name = complexType.Attribute("name")?.Value;
            if (name != null && name.StartsWith("ArrayOf"))
            {
                var element = complexType.Descendants(Xs + "element").Single();
                element.Attribute("minOccurs")!.Value.Should().Be("0");
                element.Attribute("maxOccurs")!.Value.Should().Be("unbounded");
            }
        }
    }

    private XElement ComplexType(string name)
        => _root.Elements(Xs + "complexType").Single(t => (string?)t.Attribute("name") == name);
}
