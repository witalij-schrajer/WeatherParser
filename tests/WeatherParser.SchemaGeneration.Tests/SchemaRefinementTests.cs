using System.Text;
using System.Xml.Linq;
using FluentAssertions;

namespace WeatherParser.SchemaGeneration.Tests;

[TestClass]
public sealed class SchemaRefinementTests
{
    private IExportSchemaGenerator generator = new ExportSchemaGenerator(new ExportSchemaRefiner());
    private string _text = null!;
    private XElement _root = null!;
    private static readonly XNamespace Xs = "http://www.w3.org/2001/XMLSchema";

    [TestInitialize]
    public void TestInitialize()
    {
        generator = new ExportSchemaGenerator(new ExportSchemaRefiner());
        _text = Encoding.UTF8.GetString(generator.Generate());
        _root = XDocument.Parse(_text).Root!;
    }

    private XElement ComplexType(string name) => _root.Elements(Xs + "complexType").Single(t => (string?)t.Attribute("name") == name);
    private XElement Element(string complexType, string element) => ComplexType(complexType).Descendants(Xs + "element").Single(e => (string?)e.Attribute("name") == element);
    private XElement SimpleType(string name) => _root.Elements(Xs + "simpleType").Single(t => (string?)t.Attribute("name") == name);

    [TestMethod]
    public void Generate_ContainsNoNillableAttribute()
    {
        _root.DescendantsAndSelf().Attributes("nillable").Should().BeEmpty();
    }

    [DataRow("ObservationDataset", "source", "1")]
    [DataRow("ObservationDataset", "location", "1")]
    [DataRow("ObservationDataset", "measurements", "1")]
    [DataRow("ObservationMeasurements", "temperature_c", "0")]
    [DataRow("MeasurementValue", "source_timezone", "0")]
    [DataRow("MeasurementValue", "period", "0")]
    [DataRow("ObservationLocation", "altitude_m", "0")]
    [DataRow("ExportDocument", "observations", "1")]
    [DataRow("ExportError", "message", "1")]
    [DataRow("CdcStationListEntry", "available_measurements", "1")]
    [DataRow("ForecastStep", "values", "1")]
    [DataRow("ForecastValues", "rain_rate_mm_h", "0")]
    [DataRow("PressureMeasurementValue", "pressure_reference", "1")]
    [TestMethod]
    public void Generate_SetsMinOccursFromTheC05Omission(string complexType, string element, string expectedMinOccurs)
    {
        Element(complexType, element).Attribute("minOccurs")!.Value.Should().Be(expectedMinOccurs);
    }

    [DataRow("ObservationDataset", "measured_at")]
    [DataRow("ObservationDataset", "retrieved_at")]
    [DataRow("MeasurementValue", "measured_at")]
    [DataRow("MeasurementPeriod", "start")]
    [DataRow("MeasurementPeriod", "end")]
    [DataRow("ForecastRun", "issued_at")]
    [DataRow("ForecastRun", "retrieved_at")]
    [DataRow("ForecastStep", "valid_at")]
    [DataRow("IntervalValue", "period_start")]
    [DataRow("IntervalValue", "period_end")]
    [DataRow("CdcStationList", "retrieved_at")]
    [DataRow("MosmixLocationList", "retrieved_at")]
    [TestMethod]
    public void Generate_TimestampElementsUseUtcTimestamp(string complexType, string element)
    {
        Element(complexType, element).Attribute("type")!.Value.Should().Be("tns:UtcTimestamp");
        Element(complexType, element).Attribute("minOccurs")!.Value.Should().Be("1");
    }

    [TestMethod]
    public void Generate_DeclaresUtcTimestampWithLengthAndAsciiPattern()
    {
        var restriction = SimpleType("UtcTimestamp").Element(Xs + "restriction")!;
        restriction.Attribute("base")!.Value.Should().Be("xs:string");
        restriction.Elements(Xs + "length").Single().Attribute("value")!.Value.Should().Be("19");
        restriction.Elements(Xs + "pattern").Single().Attribute("value")!.Value.Should().Be(@"[0-9]{4}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]");
    }

    [TestMethod]
    public void Generate_ConstantsUseSingleValueEnumerations()
    {
        var schemaVersionElements = _root.Descendants(Xs + "element").Where(e => (string?)e.Attribute("name") == "schema_version");
        schemaVersionElements.Should().HaveCount(5);
        schemaVersionElements.Select(e => e.Attribute("type")!.Value).Should().OnlyContain(type => type == "tns:SchemaVersionV1");

        Element("ForecastRun", "dataset_type").Attribute("type")!.Value.Should().Be("tns:ForecastDatasetType");

        var schemaVersionEnum = SimpleType("SchemaVersionV1").Descendants(Xs + "enumeration").Select(e => e.Attribute("value")!.Value);
        schemaVersionEnum.Should().Equal("1");

        var forecastDatasetTypeEnum = SimpleType("ForecastDatasetType").Descendants(Xs + "enumeration").Select(e => e.Attribute("value")!.Value);
        forecastDatasetTypeEnum.Should().Equal("forecast");

        Element("ForecastRun", "dataset_type").Attribute("minOccurs")!.Value.Should().Be("1");
        schemaVersionElements.Select(e => e.Attribute("minOccurs")!.Value).Should().OnlyContain(minOccurs => minOccurs == "1");

        _root.Descendants(Xs + "element").Where(e => e.Attribute("fixed") != null || e.Attribute("default") != null).Should().BeEmpty();
    }

    [DataRow("MosmixProductAvailability", "evidence_retrieved_at")]
    [DataRow("MosmixProductAvailability", "evidence_issued_at")]
    [TestMethod]
    public void Generate_OptionalEvidenceTimestampsUseUtcTimestampAndAreOptional(string complexType, string element)
    {
        Element(complexType, element).Attribute("type")!.Value.Should().Be("tns:UtcTimestamp");
        Element(complexType, element).Attribute("minOccurs")!.Value.Should().Be("0");
    }

    [TestMethod]
    public void Generate_EvidenceTimestampsAppendedAfterAvailableForecastElements()
    {
        var elements = ComplexType("MosmixProductAvailability").Descendants(Xs + "sequence").First().Elements(Xs + "element").Select(e => (string?)e.Attribute("name"));
        elements.Should().Equal("product", "availability", "available_forecast_elements", "evidence_retrieved_at", "evidence_issued_at");
    }
}
