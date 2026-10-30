using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Output;
using WeatherParser.Cli.Tests.Commands;
using WeatherParser.Cli.Tests.Configuration;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Cli.Tests.Commands;

[TestClass]
[TestCategory("T-P3-11")]
public sealed class ParseDwdAndMosmixHandlerTests
{
    private MockRepository _mocks = null!;
    private Mock<IDwdObservationParser> _dwdParser = null!;
    private Mock<IMosmixLParser> _mosmixL = null!;
    private Mock<IMosmixSParser> _mosmixS = null!;
    private Mock<IMessagePublisher> _publisher = null!;
    private StringWriter _stdout = null!;
    private StringWriter _stderr = null!;
    private ILoggerFactory _loggerFactory = null!;
    private DocumentResultWriter _documentWriter = null!;
    private ServiceProvider _mqttServices = null!;
    private MqttResultWriter _mqttWriter = null!;
    private List<MqttMessage> _published = null!;

    private ParseDwdHandler _dwdDocument = null!;
    private ParseDwdHandler _dwdMqtt = null!;
    private ParseMosmixHandler _mosmixDocument = null!;
    private ParseMosmixHandler _mosmixMqtt = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _dwdParser = _mocks.Create<IDwdObservationParser>();
        _mosmixL = _mocks.Create<IMosmixLParser>();
        _mosmixS = _mocks.Create<IMosmixSParser>();
        _publisher = _mocks.Create<IMessagePublisher>();

        _stdout = new StringWriter();
        _stderr = new StringWriter();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new StandardErrorLoggerProvider(_stderr, TimeProvider.System, new LogLineFormatter(), LogLevel.Information, "ParseDWD")));

        _documentWriter = new DocumentResultWriter(new JsonExportSerializer(), new XmlExportSerializer(), new OutputDestination(new FileOperations(), NullLogger<OutputDestination>.Instance));
        _mqttServices = TestConfigurations.Build(TestConfigurations.Valid(), TestConfigurations.Request(CliCommand.ParseDwd, OutputFormat.Mqtt));
        _mqttWriter = new MqttResultWriter(_mqttServices.GetRequiredService<IMqttMessageSerializer>(), _publisher.Object);
        _published = new List<MqttMessage>();

        _dwdDocument = new ParseDwdHandler(_dwdParser.Object, _documentWriter, new ExportErrorMessages(), new LibraryErrors(), new FailureClassifier(), _loggerFactory.CreateLogger<ParseDwdHandler>());
        _dwdMqtt = new ParseDwdHandler(_dwdParser.Object, _mqttWriter, new ExportErrorMessages(), new LibraryErrors(), new FailureClassifier(), _loggerFactory.CreateLogger<ParseDwdHandler>());
        _mosmixDocument = new ParseMosmixHandler(_mosmixL.Object, _mosmixS.Object, _documentWriter, new ExportErrorMessages(), new LibraryErrors(), new FailureClassifier(), _loggerFactory.CreateLogger<ParseMosmixHandler>());
        _mosmixMqtt = new ParseMosmixHandler(_mosmixL.Object, _mosmixS.Object, _mqttWriter, new ExportErrorMessages(), new LibraryErrors(), new FailureClassifier(), _loggerFactory.CreateLogger<ParseMosmixHandler>());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
        _mqttServices.Dispose();
        _loggerFactory.Dispose();
        _stdout.Dispose();
        _stderr.Dispose();
    }

    private CommandContext Context(OutputFormat output) => new(new CommonOptions(output, null, null, false), "00044", _stdout);
    private static ParseDwdRequest DwdRequest(OutputFormat output) => new(new CommonOptions(output, null, null, false), "00044");
    private static ParseMosmixRequest MosmixRequest(OutputFormat output, MosmixProduct product) => new(new CommonOptions(output, null, null, false), product, "10637");
    private static MosmixParseResult Result(ForecastRun run) => new(run, Array.Empty<OmittedForecastValue>(), Array.Empty<string>(), Array.Empty<string>(), MosmixRunFreshness.Fresh, TimeSpan.FromHours(1));
    private static async Task<string> JsonOf<TRoot>(TRoot root) where TRoot : class
    {
        using var stream = new MemoryStream();
        await new JsonExportSerializer().SerializeAsync(root, stream, CancellationToken.None);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    private void SetupPublisher() => _publisher.Setup(p => p.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).Callback<MqttMessage, CancellationToken>((message, _) => _published.Add(message)).Returns(Task.CompletedTask);

    [TestMethod]
    public async Task ParseDwd_Json_ExportDocumentWithOneObservation()
    {
        var dataset = TestExportData.Observation("dwd", "00044");
        _dwdParser.Setup(p => p.ParseAsync("00044", It.IsAny<CancellationToken>())).ReturnsAsync(dataset);
        var outcome = await _dwdDocument.HandleAsync(DwdRequest(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(1, 0, false));
        _stdout.ToString().Should().Be(await JsonOf(new ExportDocument { Observations = { dataset } }));
        using var doc = JsonDocument.Parse(_stdout.ToString());
        doc.RootElement.GetProperty("observations")[0].GetProperty("measurements").GetProperty("temperature_c").GetProperty("value").GetDouble().Should().Be(0);
        _stdout.ToString().Should().Contain("\"forecast_runs\":[]");
    }

    [TestMethod]
    public async Task ParseDwd_Xml_SchemaValid()
    {
        var dataset = TestExportData.Observation("dwd", "00044");
        _dwdParser.Setup(p => p.ParseAsync("00044", It.IsAny<CancellationToken>())).ReturnsAsync(dataset);
        var outcome = await _dwdDocument.HandleAsync(DwdRequest(OutputFormat.Xml), Context(OutputFormat.Xml), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(1, 0, false));
        var events = SchemaValidation.ValidateAgainstPackagedSchema(Encoding.UTF8.GetBytes(_stdout.ToString()));
        events.Should().BeEmpty();
        var doc = System.Xml.Linq.XDocument.Parse(_stdout.ToString());
        doc.Root.Name.LocalName.Should().Be("export_document");
    }

    [TestMethod]
    public async Task ParseDwd_RetrievalFailure_ExportDocumentWithError_ExitFailure()
    {
        _dwdParser.Setup(p => p.ParseAsync("00044", It.IsAny<CancellationToken>())).ThrowsAsync(new CatalogRetrievalException(CatalogRetrievalException.RetrievalFailedErrorId, "endpoint", "secret-endpoint-7c1"));
        var outcome = await _dwdDocument.HandleAsync(DwdRequest(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(0, 1, false));
        new ExitCodePolicy().ForOutcome(outcome).Should().Be(ExitCodes.Failure);
        using var doc = JsonDocument.Parse(_stdout.ToString());
        doc.RootElement.GetProperty("errors").GetArrayLength().Should().Be(1);
        var error = doc.RootElement.GetProperty("errors")[0];
        error.GetProperty("error_id").GetString().Should().Be("CATALOG-RETRIEVAL-FAILED");
        error.GetProperty("source").GetString().Should().Be("dwd:00044");
        error.GetProperty("message").GetString().Should().Be("A source file could not be retrieved.");
        doc.RootElement.GetProperty("observations").GetArrayLength().Should().Be(0);
        _stdout.ToString().Should().NotContain("secret-endpoint-7c1");
        _stderr.ToString().Should().Contain(" ERROR CATALOG-RETRIEVAL-FAILED ");
    }

    [TestMethod]
    public async Task ParseDwd_StationUnknown_ItemFailure()
    {
        _dwdParser.Setup(p => p.ParseAsync("00044", It.IsAny<CancellationToken>())).ThrowsAsync(new DwdStationException(DwdStationException.StationUnknownErrorId, "m"));
        var outcome = await _dwdDocument.HandleAsync(DwdRequest(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(0, 1, false));
        using var doc = JsonDocument.Parse(_stdout.ToString());
        doc.RootElement.GetProperty("errors").GetArrayLength().Should().Be(1);
        var error = doc.RootElement.GetProperty("errors")[0];
        error.GetProperty("error_id").GetString().Should().Be("STATION-UNKNOWN");
        error.GetProperty("source").GetString().Should().Be("dwd:00044");
    }

    [TestMethod]
    public async Task ParseDwd_InvalidStationId_CommandLevel_Propagates()
    {
        _dwdParser.Setup(p => p.ParseAsync("00044", It.IsAny<CancellationToken>())).ThrowsAsync(new DwdStationException(DwdStationException.StationIdInvalidErrorId, "m"));
        Func<Task<CommandOutcome>> act = () => _dwdDocument.HandleAsync(DwdRequest(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);
        await act.Should().ThrowAsync<DwdStationException>();
        _stdout.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public async Task ParseDwd_UnlistedExceptionType_Propagates()
    {
        _dwdParser.Setup(p => p.ParseAsync("00044", It.IsAny<CancellationToken>())).ThrowsAsync(new EvidenceCacheException(EvidenceCacheException.CacheFailedErrorId, "m"));
        Func<Task<CommandOutcome>> act = () => _dwdDocument.HandleAsync(DwdRequest(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);
        await act.Should().ThrowAsync<EvidenceCacheException>();
        _stdout.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public async Task ParseDwd_Mqtt_PublishesDatasetNotEnvelope()
    {
        var dataset = TestExportData.Observation("dwd", "00044");
        _dwdParser.Setup(p => p.ParseAsync("00044", It.IsAny<CancellationToken>())).ReturnsAsync(dataset);
        SetupPublisher();
        var outcome = await _dwdMqtt.HandleAsync(DwdRequest(OutputFormat.Mqtt), Context(OutputFormat.Mqtt), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(1, 0, false));
        _published.Should().HaveCount(1);
        _published[0].MessageType.Should().Be(MqttMessageType.Observation);
        var payload = Encoding.UTF8.GetString(_published[0].Payload.ToArray());
        payload.Should().Be(await JsonOf(dataset));
        _stdout.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public async Task ParseDwd_Mqtt_Failure_NothingPublished_LoggedOnly()
    {
        _dwdParser.Setup(p => p.ParseAsync("00044", It.IsAny<CancellationToken>())).ThrowsAsync(new CatalogRetrievalException(CatalogRetrievalException.RetrievalFailedErrorId, "endpoint", "secret-endpoint-7c1"));
        var outcome = await _dwdMqtt.HandleAsync(DwdRequest(OutputFormat.Mqtt), Context(OutputFormat.Mqtt), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(0, 1, false));
        _published.Should().HaveCount(0);
        _stdout.ToString().Should().BeEmpty();
        _stderr.ToString().Should().Contain(" ERROR CATALOG-RETRIEVAL-FAILED ");
        _stderr.ToString().Should().Contain("source=dwd:00044");
    }

    [TestMethod]
    public async Task ParseMosmix_MosmixL_UsesMosmixLParserOnly()
    {
        var run = TestExportData.Forecast(MosmixProduct.MosmixL, "10637");
        _mosmixL.Setup(p => p.ParseAsync("10637", It.IsAny<CancellationToken>())).ReturnsAsync(Result(run));
        var outcome = await _mosmixDocument.HandleAsync(MosmixRequest(OutputFormat.Json, MosmixProduct.MosmixL), Context(OutputFormat.Json), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(1, 0, false));
        _stdout.ToString().Should().Be(await JsonOf(new ExportDocument { ForecastRuns = { run } }));
    }

    [TestMethod]
    public async Task ParseMosmix_MosmixS_UsesMosmixSParserOnly()
    {
        var run = TestExportData.Forecast(MosmixProduct.MosmixS, "10637");
        _mosmixS.Setup(p => p.ParseAsync("10637", It.IsAny<CancellationToken>())).ReturnsAsync(Result(run));
        var outcome = await _mosmixDocument.HandleAsync(MosmixRequest(OutputFormat.Json, MosmixProduct.MosmixS), Context(OutputFormat.Json), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(1, 0, false));
        _stdout.ToString().Should().Be(await JsonOf(new ExportDocument { ForecastRuns = { run } }));
    }

    [TestMethod]
    public async Task ParseMosmix_Json_ForecastRunWithoutMeasuredAt()
    {
        var run = TestExportData.Forecast(MosmixProduct.MosmixL, "10637");
        _mosmixL.Setup(p => p.ParseAsync("10637", It.IsAny<CancellationToken>())).ReturnsAsync(Result(run));
        var outcome = await _mosmixDocument.HandleAsync(MosmixRequest(OutputFormat.Json, MosmixProduct.MosmixL), Context(OutputFormat.Json), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(1, 0, false));
        _stdout.ToString().Should().NotContain("measured_at");
        _stdout.ToString().Should().Contain("dwd-mosmix-l:10637");
    }

    [TestMethod]
    public async Task ParseMosmix_Xml_SchemaValid()
    {
        var run = TestExportData.Forecast(MosmixProduct.MosmixL, "10637");
        _mosmixL.Setup(p => p.ParseAsync("10637", It.IsAny<CancellationToken>())).ReturnsAsync(Result(run));
        var outcome = await _mosmixDocument.HandleAsync(MosmixRequest(OutputFormat.Xml, MosmixProduct.MosmixL), Context(OutputFormat.Xml), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(1, 0, false));
        var events = SchemaValidation.ValidateAgainstPackagedSchema(Encoding.UTF8.GetBytes(_stdout.ToString()));
        events.Should().BeEmpty();
        var document = XDocument.Parse(
            _stdout.ToString());
        document.Root!.Name.LocalName.Should().Be("export_document");
        var ns = document.Root.Name.Namespace;
        document.Root.Element(ns + "forecast_runs")!.Elements().Should().ContainSingle();
        document.Root.Element(ns + "observations")!.Elements().Should().BeEmpty();
        _stdout.ToString().Should().Contain("dwd-mosmix-l:10637");
    }

    [TestMethod]
    public async Task ParseMosmix_IdUnknown_ErrorWithProductSource()
    {
        _mosmixS.Setup(p => p.ParseAsync("10637", It.IsAny<CancellationToken>())).ThrowsAsync(new MosmixForecastException(MosmixForecastErrorIds.IdUnknown, MosmixProduct.MosmixS, "m"));
        var outcome = await _mosmixDocument.HandleAsync(MosmixRequest(OutputFormat.Json, MosmixProduct.MosmixS), Context(OutputFormat.Json), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(0, 1, false));
        using var doc = JsonDocument.Parse(_stdout.ToString());
        doc.RootElement.GetProperty("errors").GetArrayLength().Should().Be(1);
        var error = doc.RootElement.GetProperty("errors")[0];
        error.GetProperty("error_id").GetString().Should().Be("MOSMIX-ID-UNKNOWN");
        error.GetProperty("source").GetString().Should().Be("dwd-mosmix-s:10637");
        error.GetProperty("message").GetString().Should().Be("The MOSMIX location is not in the forecast file.");
    }

    [TestMethod]
    public async Task ParseMosmix_FormatFailure_ErrorWithProductSource()
    {
        _mosmixL.Setup(p => p.ParseAsync("10637", It.IsAny<CancellationToken>())).ThrowsAsync(new MosmixForecastFormatException(MosmixForecastErrorIds.FormatInvalid, "m"));
        var outcome = await _mosmixDocument.HandleAsync(MosmixRequest(OutputFormat.Json, MosmixProduct.MosmixL), Context(OutputFormat.Json), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(0, 1, false));
        using var doc = JsonDocument.Parse(_stdout.ToString());
        doc.RootElement.GetProperty("errors").GetArrayLength().Should().Be(1);
        var error = doc.RootElement.GetProperty("errors")[0];
        error.GetProperty("source").GetString().Should().Be("dwd-mosmix-l:10637");
        error.GetProperty("error_id").GetString().Should().Be("MOSMIX-FORMAT-INVALID");
    }

    [TestMethod]
    public async Task ParseMosmix_InvalidId_CommandLevel_Propagates()
    {
        _mosmixL.Setup(p => p.ParseAsync("10637", It.IsAny<CancellationToken>())).ThrowsAsync(new MosmixForecastException(MosmixForecastErrorIds.IdInvalid, MosmixProduct.MosmixL, "m"));
        Func<Task<CommandOutcome>> act = () => _mosmixDocument.HandleAsync(MosmixRequest(OutputFormat.Json, MosmixProduct.MosmixL), Context(OutputFormat.Json), CancellationToken.None);
        await act.Should().ThrowAsync<MosmixForecastException>();
        _stdout.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public async Task ParseMosmix_Mqtt_PublishesRunNotEnvelope()
    {
        var run = TestExportData.Forecast(MosmixProduct.MosmixL, "10637");
        _mosmixL.Setup(p => p.ParseAsync("10637", It.IsAny<CancellationToken>())).ReturnsAsync(Result(run));
        SetupPublisher();
        var outcome = await _mosmixMqtt.HandleAsync(MosmixRequest(OutputFormat.Mqtt, MosmixProduct.MosmixL), Context(OutputFormat.Mqtt), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(1, 0, false));
        _published.Should().HaveCount(1);
        _published[0].MessageType.Should().Be(MqttMessageType.Forecast);
        var payload = Encoding.UTF8.GetString(_published[0].Payload.ToArray());
        payload.Should().Be(await JsonOf(run));
        _stdout.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public async Task ParseMosmix_ProductParserNotRegistered_Throws()
    {
        var handler = new ParseMosmixHandler(_mosmixL.Object, null, _documentWriter, new ExportErrorMessages(), new LibraryErrors(), new FailureClassifier(), _loggerFactory.CreateLogger<ParseMosmixHandler>());
        Func<Task> act = () => handler.HandleAsync(MosmixRequest(OutputFormat.Json, MosmixProduct.MosmixS), Context(OutputFormat.Json), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
        _stdout.ToString().Should().BeEmpty();
    }
}
