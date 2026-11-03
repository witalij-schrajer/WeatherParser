using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Export;

namespace WeatherParser.Parsing.Tests.Serialization;

[TestClass]
public sealed class ExportGraphValidationTests
{
    private JsonExportSerializer _json = null!;
    private XmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    private static ExportError Error(string source) => new()
    {
        ErrorId = "DWD-RETRIEVAL-FAILED",
        Message = "Retrieval failed.",
        Source = source
    };

    private static ObservationDataset Dataset(string stationId)
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Source = "dwd:" + stationId;
        dataset.Location.StationId = stationId;
        return dataset;
    }

    private static ExportDocument ValidDocument() => new()
    {
        Observations = { Dataset("00044"), Dataset("01766") },
        ForecastRuns = { ForecastTestData.ValidRun() },
        Errors = { Error("dwd:02000") }
    };

    private static ExportDocument Build(string scenario)
    {
        var document = ValidDocument();
        switch (scenario)
        {
            case "Valid":
                break;
            case "NullObservations":
                document.Observations = null!;
                break;
            case "NullForecastRuns":
                document.ForecastRuns = null!;
                break;
            case "NullErrors":
                document.Errors = null!;
                break;
            case "NullObservationItem":
                document.Observations.Add(null!);
                break;
            case "NullForecastRunItem":
                document.ForecastRuns.Add(null!);
                break;
            case "NullErrorItem":
                document.Errors.Add(null!);
                break;
            case "InvalidNestedObservation":
                document.Observations[0].Measurements.TemperatureC = null;
                document.Observations[0].Measurements.HumidityPercent = null;
                break;
            case "InvalidNestedForecastRun":
                document.ForecastRuns[0].Forecasts.Clear();
                break;
            case "DuplicateObservationSource":
                document.Observations.Add(Dataset("00044"));
                break;
            case "DuplicateForecastRunSource":
                document.ForecastRuns.Add(ForecastTestData.ValidRun());
                break;
            case "DuplicateErrorSource":
                document.Errors.Add(Error("dwd:02000"));
                break;
            case "ErrorSourceEqualsObservation":
                document.Errors.Add(Error("dwd:00044"));
                break;
            case "ErrorSourceEqualsForecastRun":
                document.Errors.Add(Error("dwd-mosmix-l:10637"));
                break;
            default:
                throw new ArgumentException(scenario);
        }
        return document;
    }

    [TestMethod]
    public async Task Serialize_ValidEnvelopeWithAllKinds_SucceedsInBothFormats()
    {
        var document = Build("Valid");
        using var jsonStream = new MemoryStream();
        using var xmlStream = new MemoryStream();

        await _json.SerializeAsync(document, jsonStream, CancellationToken.None);
        await _xml.SerializeAsync(document, xmlStream, CancellationToken.None);

        jsonStream.Length.Should().BeGreaterThan(0);
        xmlStream.Length.Should().BeGreaterThan(0);
    }

    [DataRow("NullObservations")]
    [DataRow("NullForecastRuns")]
    [DataRow("NullErrors")]
    [DataRow("NullObservationItem")]
    [DataRow("NullForecastRunItem")]
    [DataRow("NullErrorItem")]
    [DataRow("InvalidNestedObservation")]
    [DataRow("InvalidNestedForecastRun")]
    [DataRow("DuplicateObservationSource")]
    [DataRow("DuplicateForecastRunSource")]
    [DataRow("DuplicateErrorSource")]
    [DataRow("ErrorSourceEqualsObservation")]
    [DataRow("ErrorSourceEqualsForecastRun")]
    [TestMethod]
    public async Task Serialize_EnvelopeViolatingGraphRule_IsRejectedInBothFormatsAndWritesNothing(string scenario)
    {
        var document = Build(scenario);
        
        using var jsonStream = new MemoryStream();
        Func<Task> jsonAct = () => _json.SerializeAsync(document, jsonStream, CancellationToken.None);
        await jsonAct.Should().ThrowAsync<ExportSerializationException>();
        jsonStream.Length.Should().Be(0);

        using var xmlStream = new MemoryStream();
        Func<Task> xmlAct = () => _xml.SerializeAsync(document, xmlStream, CancellationToken.None);
        await xmlAct.Should().ThrowAsync<ExportSerializationException>();
        xmlStream.Length.Should().Be(0);
    }
}
