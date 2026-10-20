using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Export;

namespace WeatherParser.Parsing.Tests.Schemas;

[TestClass]
public sealed class SchemaRefinementValidationTests
{
    private IXmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _xml = new XmlExportSerializer();
    }

    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    private async Task<byte[]> ExportAsync<TRoot>(TRoot root) where TRoot : class
    {
        using var stream = new MemoryStream();
        await _xml.SerializeAsync(root, stream, CancellationToken.None);
        return stream.ToArray();
    }

    private static byte[] Mutate(byte[] document, Action<XElement> change)
    {
        var xml = XDocument.Load(new MemoryStream(document));
        change(xml.Root!);
        using var stream = new MemoryStream();
        xml.Save(stream);
        return stream.ToArray();
    }

    [TestMethod]
    public async Task Schema_AcceptsRealObservationExport()
    {
        var document = await ExportAsync(ObservationTestData.ValidDataset());
        ExportSchemaValidation.Validate(document).Should().BeEmpty();
    }

    [TestMethod]
    public async Task Schema_RejectsDocumentMissingRequiredString()
    {
        var document = await ExportAsync(ObservationTestData.ValidDataset());
        var mutated = Mutate(document, root => root.Element(Ns + "source")!.Remove());
        ExportSchemaValidation.Validate(mutated).Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Schema_RejectsTimestampWithZSuffix()
    {
        var document = await ExportAsync(ObservationTestData.ValidDataset());
        var mutated = Mutate(document, root => root.Element(Ns + "retrieved_at")!.Value += "Z");
        ExportSchemaValidation.Validate(mutated).Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Schema_RejectsXsiNil()
    {
        var document = await ExportAsync(ObservationTestData.ValidDataset());
        var mutated = Mutate(document, root =>
        {
            var altitude = root.Element(Ns + "location")!.Element(Ns + "altitude_m")!;
            altitude.RemoveNodes();
            altitude.SetAttributeValue(Xsi + "nil", "true");
        });
        ExportSchemaValidation.Validate(mutated).Should().NotBeEmpty();
    }

    [DataRow("missing")]
    [DataRow("empty")]
    [DataRow("2")]
    [TestMethod]
    public async Task Schema_RejectsMissingEmptyOrWrongSchemaVersion(string variant)
    {
        var document = await ExportAsync(ObservationTestData.ValidDataset());
        var mutated = Mutate(document, root =>
        {
            var schemaVersion = root.Element(Ns + "schema_version")!;
            if (variant == "missing")
                schemaVersion.Remove();
            else if (variant == "empty")
                schemaVersion.Value = "";
            else
                schemaVersion.Value = variant;
        });
        ExportSchemaValidation.Validate(mutated).Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Schema_RejectsWrongDatasetType()
    {
        var document = await ExportAsync(ForecastTestData.ValidRun());
        var mutated = Mutate(document, root => root.Element(Ns + "dataset_type")!.Value = "observation");
        ExportSchemaValidation.Validate(mutated).Should().NotBeEmpty();
    }
}
