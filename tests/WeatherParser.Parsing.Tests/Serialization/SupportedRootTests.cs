using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Export;

namespace WeatherParser.Parsing.Tests.Serialization;

[TestClass]
public sealed class SupportedRootTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    private async Task<(byte[] Json, byte[] Xml)> SerializeBoth<TRoot>(TRoot root) where TRoot : class
    {
        using var jsonStream = new MemoryStream();
        using var xmlStream = new MemoryStream();

        await _json.SerializeAsync(root, jsonStream, CancellationToken.None);
        await _xml.SerializeAsync(root, xmlStream, CancellationToken.None);

        return (jsonStream.ToArray(), xmlStream.ToArray());
    }

    private async Task AssertRejected<TRoot>(TRoot root) where TRoot : class
    {
        {
            using var stream = new MemoryStream();
            Func<Task> act = () => _json.SerializeAsync(root, stream, CancellationToken.None);
            (await act.Should().ThrowAsync<ExportSerializationException>())
                .WithMessage($"*{typeof(TRoot).Name}*");
            stream.Length.Should().Be(0);
        }
        {
            using var stream = new MemoryStream();
            Func<Task> act = () => _xml.SerializeAsync(root, stream, CancellationToken.None);
            (await act.Should().ThrowAsync<ExportSerializationException>())
                .WithMessage($"*{typeof(TRoot).Name}*");
            stream.Length.Should().Be(0);
        }
    }

    [TestMethod]
    [DataRow("export_document")]
    [DataRow("cdc_station_list")]
    [DataRow("mosmix_location_list")]
    [DataRow("observation_dataset")]
    [DataRow("forecast_run")]
    public async Task Serialize_EachSupportedRoot_Succeeds(string rootName)
    {
        var (Json, Xml) = rootName switch
        {
            "export_document" => await SerializeBoth(new ExportDocument { Observations = { ObservationTestData.ValidDataset() } }),
            "cdc_station_list" => await SerializeBoth(ListTestData.ValidCdcList()),
            "mosmix_location_list" => await SerializeBoth(ListTestData.ValidMosmixList()),
            "observation_dataset" => await SerializeBoth(ObservationTestData.ValidDataset()),
            "forecast_run" => await SerializeBoth(ForecastTestData.ValidRun()),
            _ => throw new ArgumentException($"Unexpected root name: {rootName}")
        };

        JsonDocument.Parse(Json).RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        XDocument.Load(new MemoryStream(Xml)).Root!.Name.LocalName.Should().Be(rootName);
    }

    [TestMethod]
    [DataRow("string")]
    [DataRow("dictionary")]
    [DataRow("anonymous")]
    [DataRow("object")]
    [DataRow("forecast_step")]
    public async Task Serialize_UnsupportedRoot_ThrowsExportSerializationException(string kind)
    {
        await (kind switch
        {
            "string" => AssertRejected("text"),
            "dictionary" => AssertRejected(new Dictionary<string, object> { ["source"] = "dwd:00044" }),
            "anonymous" => AssertRejected(new { source = "dwd:00044" }),
            "object" => AssertRejected<object>(ObservationTestData.ValidDataset()),
            "forecast_step" => AssertRejected(ForecastTestData.ValidRun().Forecasts[0]),
            _ => Task.Run(() => Assert.Fail(kind))
        });
    }

    [TestMethod]
    public async Task Serialize_UnsupportedRootWithCanceledToken_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> jsonAct = () => _json.SerializeAsync("text", new MemoryStream(), cts.Token);
        await jsonAct.Should().ThrowAsync<OperationCanceledException>();

        Func<Task> xmlAct = () => _xml.SerializeAsync("text", new MemoryStream(), cts.Token);
        await xmlAct.Should().ThrowAsync<OperationCanceledException>();
    }
}
