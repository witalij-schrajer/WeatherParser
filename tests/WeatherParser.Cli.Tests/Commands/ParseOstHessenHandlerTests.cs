using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Serialization;
using System.Xml.Linq;

[TestCategory("T-P3-11")]
[TestClass]
public sealed class ParseOstHessenHandlerTests
{
    private MockRepository _mocks = null!;
    private Mock<IOsthessenWetterParser> _parser = null!;
    private Mock<IOsthessenWetterStationIdentity> _identity = null!;
    private StringWriter _stdout = null!;
    private StringWriter _stderr = null!;
    private ILoggerFactory _loggerFactory = null!;
    private IResultWriter _documentWriter = null!;
    private ServiceProvider _mqttServices = null!;
    private IResultWriter _mqttWriter = null!;
    private PublicationQueue _queue = null!;

    private ParseOstHessenHandler _document = null!;
    private ParseOstHessenHandler _mqtt = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new(MockBehavior.Strict);
        _parser = _mocks.Create<IOsthessenWetterParser>();
        _identity = _mocks.Create<IOsthessenWetterStationIdentity>();
        _stdout = new StringWriter();
        _stderr = new StringWriter();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new StandardErrorLoggerProvider(_stderr, TimeProvider.System, new LogLineFormatter(), LogLevel.Information, "ParseOstHessen")));
        _documentWriter = new DocumentResultWriter(new JsonExportSerializer(), new XmlExportSerializer(), new OutputDestination(new FileOperations(), NullLogger<OutputDestination>.Instance));
        _mqttServices = TestConfigurations.Build(TestConfigurations.Valid(), TestConfigurations.Request(CliCommand.ParseOstHessen, OutputFormat.Mqtt));
        _queue = new PublicationQueue();
        _mqttWriter = new MqttResultWriter(_mqttServices.GetRequiredService<IMqttMessageSerializer>(), _queue);
        
        _document = new ParseOstHessenHandler(_parser.Object, _identity.Object, _documentWriter, new ExportErrorMessages(), _loggerFactory.CreateLogger<ParseOstHessenHandler>());
        _mqtt = new ParseOstHessenHandler(_parser.Object, _identity.Object, _mqttWriter, new ExportErrorMessages(), _loggerFactory.CreateLogger<ParseOstHessenHandler>());
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

    private CommandContext Context(OutputFormat output) => new(new CommonOptions(output, null, null, false), null, _stdout);
    private static ParseOstHessenRequest Request(OutputFormat output) => new(new CommonOptions(output, null, null, false), "Fulda*");
    private void SetupParse(params ObservationDataset[] datasets) => _parser.Setup(p => p.ParseAsync("Fulda*", It.IsAny<CancellationToken>())).ReturnsAsync(new OsthessenWetterParseResult(datasets, Array.Empty<OsthessenWetterStationFailure>()));
    private void SetupParse(IReadOnlyList<ObservationDataset> datasets, params OsthessenWetterStationFailure[] failures) => _parser.Setup(p => p.ParseAsync("Fulda*", It.IsAny<CancellationToken>())).ReturnsAsync(new OsthessenWetterParseResult(datasets, failures));
    private void MapName(string name, string stationId) => _identity.Setup(i => i.ToStationId(name)).Returns(stationId);
    private static async Task<string> JsonOf<TRoot>(TRoot root) where TRoot : class
    {
        using var stream = new MemoryStream();
        await new JsonExportSerializer().SerializeAsync(root, stream, CancellationToken.None);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [TestMethod]
    public async Task ParseOstHessen_SeveralMatches_SeparateDatasets()
    {
        var first = TestExportData.Observation("osthessenwetter", "fulda");
        var second = TestExportData.Observation("osthessenwetter", "fulda-sued");
        SetupParse(first, second);
        var outcome = await _document.HandleAsync(Request(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(2, 0, false));
        var expectedJson = await JsonOf(new ExportDocument { Observations = { first, second } });
        _stdout.ToString().Should().Be(expectedJson);
    }

    [TestMethod]
    public async Task ParseOstHessen_Xml_SchemaValid()
    {
        var first = TestExportData.Observation("osthessenwetter", "fulda");
        var second = TestExportData.Observation("osthessenwetter", "fulda-sued");
        SetupParse(first, second);
        var outcome = await _document.HandleAsync(Request(OutputFormat.Xml), Context(OutputFormat.Xml), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(2, 0, false));
        var xml = _stdout.ToString();
        SchemaValidation.ValidateAgainstPackagedSchema(Encoding.UTF8.GetBytes(xml)).Should().BeEmpty();
        var document = XDocument.Parse(_stdout.ToString());
        document.Root!.Name.LocalName.Should().Be("export_document");
        var ns = document.Root.Name.Namespace;
        document.Root.Element(ns + "observations")!.Elements().Should().HaveCount(2);
        _stdout.ToString().Should().Contain("osthessenwetter:fulda").And.Contain("osthessenwetter:fulda-sued");
    }

    [TestMethod]
    public async Task ParseOstHessen_SomeStationsFail_ObservationsAndErrors_ExitPartial()
    {
        var dataset = TestExportData.Observation("osthessenwetter", "fulda");
        var failure = new OsthessenWetterStationFailure("Hünfeld", "OSTHESSEN-STATION-OFFLINE", "offline-reason-7c1");
        SetupParse(new[] { dataset }, failure);
        MapName("Hünfeld", "huenfeld");
        var outcome = await _document.HandleAsync(Request(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(1, 1, false));
        new ExitCodePolicy().ForOutcome(outcome).Should().Be(ExitCodes.Partial);
        var doc = JsonDocument.Parse(_stdout.ToString());
        var observations = doc.RootElement.GetProperty("observations");
        observations.GetArrayLength().Should().Be(1);
        var errors = doc.RootElement.GetProperty("errors");
        errors.GetArrayLength().Should().Be(1);
        var error = errors[0];
        error.GetProperty("error_id").GetString().Should().Be("OSTHESSEN-STATION-OFFLINE");
        error.GetProperty("source").GetString().Should().Be("osthessenwetter:huenfeld");
        error.GetProperty("message").GetString().Should().Be("The OsthessenWetter station is reported offline.");
        _stdout.ToString().Should().NotContain("offline-reason-7c1");
    }

    [TestMethod]
    public async Task ParseOstHessen_NoUsableStation_EnvelopeWithErrorsOnly()
    {
        var failure1 = new OsthessenWetterStationFailure("Fulda", "OSTHESSEN-STATION-NO-READING", "r");
        var failure2 = new OsthessenWetterStationFailure("Hünfeld", "OSTHESSEN-STATION-OFFLINE", "r");
        _parser.Setup(p => p.ParseAsync("Fulda*", It.IsAny<CancellationToken>())).Throws(new OsthessenWetterException(OsthessenWetterException.NoUsableStationErrorId, "m", new[] { failure1, failure2 }));
        MapName("Fulda", "fulda");
        MapName("Hünfeld", "huenfeld");
        var outcome = await _document.HandleAsync(Request(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(0, 2, false));
        new ExitCodePolicy().ForOutcome(outcome).Should().Be(ExitCodes.Failure);
        var doc = JsonDocument.Parse(_stdout.ToString());
        var observations = doc.RootElement.GetProperty("observations");
        observations.GetArrayLength().Should().Be(0);
        var errors = doc.RootElement.GetProperty("errors");
        errors.GetArrayLength().Should().Be(2);
        errors[0].GetProperty("source").GetString().Should().Be("osthessenwetter:fulda");
        errors[1].GetProperty("source").GetString().Should().Be("osthessenwetter:huenfeld");
    }

    [TestMethod]
    public async Task ParseOstHessen_PageUnavailable_CommandLevel_Propagates()
    {
        _parser.Setup(p => p.ParseAsync("Fulda*", It.IsAny<CancellationToken>())).Throws(new OsthessenWetterException(OsthessenWetterException.PageUnavailableErrorId, "m"));
        Func<Task> act = async () => await _document.HandleAsync(Request(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);
        await act.Should().ThrowAsync<OsthessenWetterException>();
        _stdout.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public async Task ParseOstHessen_PatternNoMatch_CommandLevel_Propagates()
    {
        _parser.Setup(p => p.ParseAsync("Fulda*", It.IsAny<CancellationToken>())).Throws(new OsthessenWetterException(OsthessenWetterException.PatternNoMatchErrorId, "m"));
        Func<Task> act = async () => await _document.HandleAsync(Request(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);
        await act.Should().ThrowAsync<OsthessenWetterException>();
        _stdout.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public async Task ParseOstHessen_StationFailureNameUnmappable_StderrOnly_CountsAsFailed()
    {
        var dataset = TestExportData.Observation("osthessenwetter", "fulda");
        var failure = new OsthessenWetterStationFailure("???", "OSTHESSEN-STATION-OFFLINE", "r");
        SetupParse(new[] { dataset }, failure);
        _identity.Setup(i => i.ToStationId("???")).Throws(new OsthessenWetterException(OsthessenWetterException.FormatChangedErrorId, "m"));
        var outcome = await _document.HandleAsync(Request(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(1, 1, false));
        var doc = JsonDocument.Parse(_stdout.ToString());
        var errors = doc.RootElement.GetProperty("errors");
        errors.GetArrayLength().Should().Be(0);
        _stderr.ToString().Should().Contain(" ERROR OSTHESSEN-STATION-OFFLINE ");
    }

    [TestMethod]
    public async Task ParseOstHessen_DuplicateFailureSource_StderrOnly()
    {
        var dataset = TestExportData.Observation("osthessenwetter", "fulda");
        var failure = new OsthessenWetterStationFailure("Fulda", "OSTHESSEN-STATION-NO-READING", "r");
        SetupParse(new[] { dataset }, failure);
        MapName("Fulda", "fulda");
        var outcome = await _document.HandleAsync(Request(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(1, 1, false));
        var doc = JsonDocument.Parse(_stdout.ToString());
        var errors = doc.RootElement.GetProperty("errors");
        errors.GetArrayLength().Should().Be(0);
        _stderr.ToString().Should().Contain(" ERROR OSTHESSEN-STATION-NO-READING ");
        _stderr.ToString().Should().Contain("source=osthessenwetter:fulda");
    }

    [TestMethod]
    public async Task ParseOstHessen_Mqtt_OneMessagePerDataset()
    {
        var first = TestExportData.Observation("osthessenwetter", "fulda");
        var second = TestExportData.Observation("osthessenwetter", "fulda-sued");
        var failure = new OsthessenWetterStationFailure("Hünfeld", "OSTHESSEN-STATION-OFFLINE", "offline-reason-7c1");
        SetupParse(new[] { first, second }, failure);
        MapName("Hünfeld", "huenfeld");
        var outcome = await _mqtt.HandleAsync(Request(OutputFormat.Mqtt), Context(OutputFormat.Mqtt), CancellationToken.None);
        outcome.Should().Be(new CommandOutcome(2, 1, false));
        _queue.Messages.Count.Should().Be(2);
        _queue.Messages[0].MessageType.Should().Be(MqttMessageType.Observation);
        _queue.Messages[1].MessageType.Should().Be(MqttMessageType.Observation);
        var payload1 = Encoding.UTF8.GetString(_queue.Messages[0].Payload.Span);
        var payload2 = Encoding.UTF8.GetString(_queue.Messages[1].Payload.Span);
        payload1.Should().Be(await JsonOf(first));
        payload2.Should().Be(await JsonOf(second));
        _stdout.ToString().Should().BeEmpty();
        _stderr.ToString().Should().Contain(" ERROR OSTHESSEN-STATION-OFFLINE ");
    }
}
