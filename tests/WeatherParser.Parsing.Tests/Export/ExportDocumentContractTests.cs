using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ExportDocumentContractTests
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

    private static ObservationDataset SecondDataset() 
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Source = "dwd:01766";
        dataset.Location.StationId = "01766";
        return dataset;
    }

    private static ExportError RetrievalError() => new() { ErrorId = "DWD-RETRIEVAL-FAILED", Message = "Retrieval of station 01766 failed: HTTP 503.", Source = "dwd:01766" };

    private async Task<JsonElement> JsonOf(ExportDocument document)
    {
        using var stream = new MemoryStream();
        await _json.SerializeAsync(document, stream, CancellationToken.None);
        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
    }

    private async Task<XElement> XmlOf(ExportDocument document)
    {
        using var stream = new MemoryStream();
        await _xml.SerializeAsync(document, stream, CancellationToken.None);
        return XDocument.Load(new MemoryStream(stream.ToArray())).Root!;
    }

    [TestMethod]
    public async Task Serialize_EnvelopeWithTwoObservationDatasets_IsOneCompleteDocument()
    {
        var document = new ExportDocument
        {
            Observations = new List<ObservationDataset>
            {
                ObservationTestData.ValidDataset(),
                SecondDataset()
            },
            ForecastRuns = new List<ForecastRun>()
        };

        var jsonRoot = await JsonOf(document);
        jsonRoot.EnumerateObject().Select(p => p.Name).Should().Equal("schema_version", "observations", "forecast_runs", "errors");
        jsonRoot.GetProperty("schema_version").GetString().Should().Be("1");
        jsonRoot.GetProperty("observations").EnumerateArray().Select(e => e.GetProperty("source").GetString()).Should().Equal("dwd:00044", "dwd:01766");
        jsonRoot.GetProperty("forecast_runs").ValueKind.Should().Be(JsonValueKind.Array);
        jsonRoot.GetProperty("forecast_runs").GetArrayLength().Should().Be(0);
        jsonRoot.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Array);
        jsonRoot.GetProperty("errors").GetArrayLength().Should().Be(0);

        var xmlRoot = await XmlOf(document);
        xmlRoot.Name.Should().Be(Ns + "export_document");
        xmlRoot.Elements().Select(e => e.Name.LocalName).Should().Equal("schema_version", "observations", "forecast_runs", "errors");
        xmlRoot.Element(Ns + "schema_version")!.Value.Should().Be("1");
        xmlRoot.Element(Ns + "observations")!.Elements().Select(e => e.Name).Should().Equal(Ns + "observation_dataset", Ns + "observation_dataset");
        xmlRoot.Element(Ns + "observations")!.Elements(Ns + "observation_dataset").Select(e => e.Element(Ns + "source")!.Value).Should().Equal("dwd:00044", "dwd:01766");
        xmlRoot.Element(Ns + "forecast_runs")!.HasElements.Should().BeFalse();
        xmlRoot.Element(Ns + "errors")!.HasElements.Should().BeFalse();
    }

    [TestMethod]
    public async Task Serialize_EmptyEnvelope_WritesEmptyCollections()
    {
        var document = new ExportDocument
        {
            Observations = new List<ObservationDataset>(),
            ForecastRuns = new List<ForecastRun>()
        };

        var jsonRoot = await JsonOf(document);
        jsonRoot.GetProperty("observations").ValueKind.Should().Be(JsonValueKind.Array);
        jsonRoot.GetProperty("observations").GetArrayLength().Should().Be(0);
        jsonRoot.GetProperty("forecast_runs").ValueKind.Should().Be(JsonValueKind.Array);
        jsonRoot.GetProperty("forecast_runs").GetArrayLength().Should().Be(0);
        jsonRoot.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Array);
        jsonRoot.GetProperty("errors").GetArrayLength().Should().Be(0);

        var xmlRoot = await XmlOf(document);
        xmlRoot.Element(Ns + "observations")!.HasElements.Should().BeFalse();
        xmlRoot.Element(Ns + "forecast_runs")!.HasElements.Should().BeFalse();
        xmlRoot.Element(Ns + "errors")!.HasElements.Should().BeFalse();
        xmlRoot.Element(Ns + "observations")!.Attributes().Where(a => !a.IsNamespaceDeclaration).Should().BeEmpty();
        xmlRoot.Element(Ns + "forecast_runs")!.Attributes().Where(a => !a.IsNamespaceDeclaration).Should().BeEmpty();
        xmlRoot.Element(Ns + "errors")!.Attributes().Where(a => !a.IsNamespaceDeclaration).Should().BeEmpty();
    }

    [TestMethod]
    public async Task Serialize_PartialResult_ContainsDatasetAndTypedError()
    {
        var document = new ExportDocument
        {
            Observations = new List<ObservationDataset> { ObservationTestData.ValidDataset() },
            Errors = new List<ExportError> { RetrievalError() }
        };

        var jsonRoot = await JsonOf(document);
        jsonRoot.GetProperty("errors")[0].EnumerateObject().Select(p => p.Name).Should().Equal("error_id", "message", "source");
        jsonRoot.GetProperty("errors").EnumerateArray().Select(e => e.GetProperty("error_id").GetString()).Should().ContainSingle().And.Equal("DWD-RETRIEVAL-FAILED");
        jsonRoot.GetProperty("errors").EnumerateArray().Select(e => e.GetProperty("message").GetString()).Should().ContainSingle().And.Equal("Retrieval of station 01766 failed: HTTP 503.");
        jsonRoot.GetProperty("errors").EnumerateArray().Select(e => e.GetProperty("source").GetString()).Should().ContainSingle().And.Equal("dwd:01766");
        jsonRoot.GetProperty("observations").GetArrayLength().Should().Be(1);

        var xmlRoot = await XmlOf(document);
        xmlRoot.Element(Ns + "errors")!.Elements().Select(e => e.Name).Should().ContainSingle().And.Equal(Ns + "error");
        var errorElement = xmlRoot.Element(Ns + "errors")!.Element(Ns + "error")!;
        errorElement.Elements().Select(e => e.Name).Should().Equal(Ns + "error_id", Ns + "message", Ns + "source");
        errorElement.Element(Ns + "error_id")!.Value.Should().Be("DWD-RETRIEVAL-FAILED");
        errorElement.Element(Ns + "message")!.Value.Should().Be("Retrieval of station 01766 failed: HTTP 503.");
        errorElement.Element(Ns + "source")!.Value.Should().Be("dwd:01766");
        xmlRoot.Element(Ns + "observations")!.Elements().Should().HaveCount(1);
    }

    [TestMethod]
    public async Task Serialize_Error_IsNeverWrittenAsObservation()
    {
        var document = new ExportDocument
        {
            Observations = new List<ObservationDataset> { ObservationTestData.ValidDataset() },
            Errors = new List<ExportError> { RetrievalError() }
        };

        var jsonRoot = await JsonOf(document);
        jsonRoot.GetProperty("observations").EnumerateArray().Select(e => e.GetProperty("source").GetString()).Should().ContainSingle().And.Equal("dwd:00044");

        var xmlRoot = await XmlOf(document);
        xmlRoot.Element(Ns + "observations")!.Elements(Ns + "observation_dataset").Select(e => e.Element(Ns + "source")!.Value).Should().ContainSingle().And.Equal("dwd:00044");
        xmlRoot.Descendants(Ns + "error").Should().HaveCount(1);
        var error = xmlRoot.Descendants(Ns + "error").First();
        error.Ancestors().Select(a => a.Name).Should().Equal(Ns + "errors", Ns + "export_document");
    }

    [TestMethod]
    public async Task Serialize_UnsortedItems_KeepCallerOrderInBothFormats()
    {
        var runS = ForecastTestData.ValidRun();
        runS.Product = MosmixProduct.MosmixS;
        runS.Source = "dwd-mosmix-s:10637";
        var runL = ForecastTestData.ValidRun();

        var document = new ExportDocument
        {
            Observations = { SecondDataset(), ObservationTestData.ValidDataset() },
            ForecastRuns = { runS, runL },
            Errors = { new ExportError { ErrorId = "E1", Message = "Failed.", Source = "dwd:03000" }, new ExportError { ErrorId = "E1", Message = "Failed.", Source = "dwd:02000" } }
        };

        var jsonRoot = await JsonOf(document);
        jsonRoot.GetProperty("observations").EnumerateArray().Select(e => e.GetProperty("source").GetString()).Should().Equal("dwd:01766", "dwd:00044");
        jsonRoot.GetProperty("forecast_runs").EnumerateArray().Select(e => e.GetProperty("source").GetString()).Should().Equal("dwd-mosmix-s:10637", "dwd-mosmix-l:10637");
        jsonRoot.GetProperty("errors").EnumerateArray().Select(e => e.GetProperty("source").GetString()).Should().Equal("dwd:03000", "dwd:02000");

        var xmlRoot = await XmlOf(document);
        xmlRoot.Element(Ns + "observations")!.Elements(Ns + "observation_dataset").Select(e => e.Element(Ns + "source")!.Value).Should().Equal("dwd:01766", "dwd:00044");
        xmlRoot.Element(Ns + "forecast_runs")!.Elements(Ns + "forecast_run").Select(e => e.Element(Ns + "source")!.Value).Should().Equal("dwd-mosmix-s:10637", "dwd-mosmix-l:10637");
        xmlRoot.Element(Ns + "errors")!.Elements(Ns + "error").Select(e => e.Element(Ns + "source")!.Value).Should().Equal("dwd:03000", "dwd:02000");
    }
}
