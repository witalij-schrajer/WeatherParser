using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Export;

namespace WeatherParser.Parsing.Tests.Schemas;

[TestClass]
public sealed class SchemaNegativeTests
{
    private IXmlExportSerializer _xml = null!;
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    [TestInitialize]
    public void TestInitialize()
    {
        _xml = new XmlExportSerializer();
    }

    private Task<byte[]> ExportAsync(object root) => AllRootsTestData.XmlBytesAsync(root, _xml);

    private static byte[] Mutate(byte[] document, Action<XElement> change)
    {
        var doc = XDocument.Load(new MemoryStream(document));
        var root = doc.Root!;
        change(root);
        using var stream = new MemoryStream();
        doc.Save(stream);
        return stream.ToArray();
    }

    [TestMethod]
    public async Task Schema_RejectsWrongNamespace()
    {
        var bytes = await ExportAsync(ObservationTestData.ValidDataset());
        bytes = Mutate(bytes, root =>
        {
            foreach (var element in root.DescendantsAndSelf())
            {
                element.Name = XNamespace.Get("urn:weatherparser:export:v2") + element.Name.LocalName;
            }
            root.Attributes().Where(a => a.IsNamespaceDeclaration).Remove();
        });
        ExportSchemaValidation.Validate(bytes).Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Schema_RejectsUnknownElement()
    {
        var bytes = await ExportAsync(ObservationTestData.ValidDataset());
        bytes = Mutate(bytes, root => root.Add(new XElement(Ns + "unknown_member", "x")));
        ExportSchemaValidation.Validate(bytes).Should().NotBeEmpty();
    }

    [TestMethod]
    [DataRow("2026-07-15 12:00:00")]
    [DataRow("2026-07-15T12:00:00+02:00")]
    [DataRow("2026-07-15T12:00:00Z")]
    [DataRow("2026-07-15T12:00:00.5")]
    [DataRow("\u0662\u0660\u0662\u0666-07-15T12:00:00")]
    [DataRow("2026-07-15T12:00:00\n")]
    public async Task Schema_RejectsMalformedTimestamps(string value)
    {
        var bytes = Mutate(await ExportAsync(ObservationTestData.ValidDataset()), root => root.Element(Ns + "retrieved_at")!.Value = value);
        ExportSchemaValidation.Validate(bytes).Should().NotBeEmpty();
    }

    [TestMethod]
    [DataRow("missing")]
    [DataRow("empty")]
    [DataRow("2")]
    public async Task Schema_RejectsBadSchemaVersionInEnvelopeItem(string variant)
    {
        var document = AllRootsTestData.RichDocument();
        var bytes = await ExportAsync(document);
        bytes = Mutate(bytes, root =>
        {
            var observation = root.Element(Ns + "observations")!.Element(Ns + "observation_dataset")!;
            var schemaVersion = observation.Element(Ns + "schema_version");
            if (variant == "missing")
                schemaVersion?.Remove();
            else if (variant == "empty")
                schemaVersion!.Value = "";
            else
                schemaVersion!.Value = variant;
        });
        ExportSchemaValidation.Validate(bytes).Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Schema_RejectsEmptyDatasetTypeInEnvelopeItem()
    {
        var document = AllRootsTestData.RichDocument();
        var bytes = await ExportAsync(document);
        bytes = Mutate(bytes, root =>
        {
            root.Element(Ns + "forecast_runs")!.Element(Ns + "forecast_run")!.Element(Ns + "dataset_type")!.Value = "";
        });
        ExportSchemaValidation.Validate(bytes).Should().NotBeEmpty();
    }

    [TestMethod]
    [DataRow("standalone", "missing")]
    [DataRow("standalone", "empty")]
    [DataRow("standalone", "observation")]
    [DataRow("envelope", "missing")]
    [DataRow("envelope", "empty")]
    [DataRow("envelope", "observation")]
    public async Task Schema_RejectsBadDatasetType(string scope, string variant)
    {
        var bytes = scope == "standalone" ? await ExportAsync(ForecastTestData.ValidRun()) : await ExportAsync(AllRootsTestData.RichDocument());
        bytes = Mutate(bytes, root =>
        {
            var run = scope == "standalone" ? root : root.Element(Ns + "forecast_runs")!.Element(Ns + "forecast_run")!;
            var datasetType = run.Element(Ns + "dataset_type")!;
            if (variant == "missing")
                datasetType.Remove();
            else if (variant == "empty")
                datasetType.Value = "";
            else
                datasetType.Value = variant;
        });
        ExportSchemaValidation.Validate(bytes).Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task TimestampSemantics_RejectImpossibleCalendarDate()
    {
        var bytes = Mutate(await ExportAsync(ObservationTestData.ValidDataset()), root => root.Element(Ns + "retrieved_at")!.Value = "2026-02-30T12:00:00");
        ExportSchemaValidation.Validate(bytes).Should().BeEmpty("the pattern accepts the impossible date");
        TimestampSemantics.Violations(bytes).Should().ContainSingle();
    }

    [TestMethod]
    [DynamicData(nameof(AllRootsTestData.Roots), typeof(AllRootsTestData))]
    public async Task TimestampSemantics_AcceptsRealExports(string rootName)
    {
        var root = AllRootsTestData.Build(rootName);
        await ExportAsync(root);
        TimestampSemantics.Violations(await ExportAsync(root)).Should().BeEmpty();
    }
}
