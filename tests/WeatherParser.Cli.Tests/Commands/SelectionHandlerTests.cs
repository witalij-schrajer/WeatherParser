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
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Output;
using WeatherParser.Cli.Tests.Commands;
using WeatherParser.Cli.Tests.Configuration;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Cli.Tests.Commands;

[TestClass]
[TestCategory("T-P3-10")]
public class SelectionHandlerTests
{
    private MockRepository _mocks = null!;
    private Mock<ICdcStationSelector> _cdcSelector = null!;
    private Mock<IMosmixLocationSelector> _mosmixSelector = null!;
    private Mock<IMosmixSEvidenceRefresher> _refresher = null!;
    private StringWriter _stdout = null!;
    private DocumentResultWriter _documentWriter = null!;
    private ServiceProvider _mqttServices = null!;
    private MqttResultWriter _mqttWriter = null!;
    private PublicationQueue _queue = null!;

    private SelectDwdHandler _selectDwdDocument = null!;
    private SelectDwdHandler _selectDwdMqtt = null!;
    private SelectMosmixHandler _selectMosmixDocument = null!;
    private SelectMosmixHandler _selectMosmixMqtt = null!;
    private SelectMosmixHandler _selectMosmixRefresh = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _cdcSelector = _mocks.Create<ICdcStationSelector>();
        _mosmixSelector = _mocks.Create<IMosmixLocationSelector>();
        _refresher = _mocks.Create<IMosmixSEvidenceRefresher>();
        _stdout = new StringWriter();
        _documentWriter = new DocumentResultWriter(new JsonExportSerializer(), new XmlExportSerializer(), new OutputDestination(new FileOperations(), NullLogger<OutputDestination>.Instance));
        _mqttServices = TestConfigurations.Build(TestConfigurations.Valid(), TestConfigurations.Request(CliCommand.SelectDwd, OutputFormat.Mqtt));
        _queue = new PublicationQueue();
        _mqttWriter = new MqttResultWriter(_mqttServices.GetRequiredService<IMqttMessageSerializer>(), _queue);
        
        _selectDwdDocument = new SelectDwdHandler(_cdcSelector.Object, _documentWriter);
        _selectDwdMqtt = new SelectDwdHandler(_cdcSelector.Object, _mqttWriter);
        _selectMosmixDocument = new SelectMosmixHandler(_mosmixSelector.Object, _documentWriter, null);
        _selectMosmixMqtt = new SelectMosmixHandler(_mosmixSelector.Object, _mqttWriter, null);
        _selectMosmixRefresh = new SelectMosmixHandler(_mosmixSelector.Object, _documentWriter, _refresher.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
        _mqttServices.Dispose();
        _stdout.Dispose();
    }

    private CommandContext Context(OutputFormat output) => new(new CommonOptions(output, null, null, false), null, _stdout);

    private static SelectDwdRequest DwdRequest(OutputFormat output) => new(new CommonOptions(output, null, null, false), TestExportData.Search());

    private static SelectMosmixRequest MosmixRequest(OutputFormat output, bool refresh) => new(new CommonOptions(output, null, null, false), TestExportData.Search(), refresh);

    private static async Task<string> JsonOf<TRoot>(TRoot root) where TRoot : class
    {
        var serializer = new JsonExportSerializer();
        using var stream = new MemoryStream();
        await serializer.SerializeAsync(root, stream, CancellationToken.None);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private void SetupCdc(CdcStationList list)
    {
        _cdcSelector.Setup(s => s.SelectAsync(
                It.Is<SearchParameters>(p => p.Latitude == 50.5m && p.Longitude == 9.7m && p.MaxDistanceKm == 50m),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
    }

    private void SetupMosmix(MosmixLocationList list)
    {
        _mosmixSelector.Setup(s => s.SelectAsync(
                It.Is<SearchParameters>(p => p.Latitude == 50.5m && p.Longitude == 9.7m && p.MaxDistanceKm == 50m),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
    }

    [TestMethod]
    public async Task SelectDwd_Json_StandaloneCdcStationList()
    {
        var list = TestExportData.CdcStations("00044", "01234");
        SetupCdc(list);
        var outcome = await _selectDwdDocument.HandleAsync(DwdRequest(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
        _stdout.ToString().Should().Be(await JsonOf(list));

        using var doc = JsonDocument.Parse(_stdout.ToString());
        doc.RootElement.TryGetProperty("stations", out var stations).Should().BeTrue();
        stations.GetArrayLength().Should().Be(2);
        stations[0].GetProperty("station_id").GetString().Should().Be("00044");
        doc.RootElement.TryGetProperty("observations", out _).Should().BeFalse();
    }

    [TestMethod]
    public async Task SelectDwd_Xml_SchemaValidAgainstPackagedXsd()
    {
        var list = TestExportData.CdcStations("00044");
        SetupCdc(list);
        var outcome = await _selectDwdDocument.HandleAsync(DwdRequest(OutputFormat.Xml), Context(OutputFormat.Xml), CancellationToken.None);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
        SchemaValidation.ValidateAgainstPackagedSchema(Encoding.UTF8.GetBytes(_stdout.ToString())).Should().BeEmpty();
        XDocument.Parse(_stdout.ToString()).Root!.Name.LocalName.Should().Be("cdc_station_list");
        _stdout.ToString().Should().Contain("&amp;").And.Contain("&lt;").And.Contain("Ä");
    }

    [TestMethod]
    public async Task SelectDwd_EmptySelection_ValidEmptyListExitSuccess()
    {
        var list = TestExportData.CdcStations();
        SetupCdc(list);
        var outcome = await _selectDwdDocument.HandleAsync(DwdRequest(OutputFormat.Json), Context(OutputFormat.Json), CancellationToken.None);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
        new ExitCodePolicy().ForOutcome(outcome).Should().Be(ExitCodes.Success);
        _stdout.ToString().Should().Be(await JsonOf(list));

        using var doc = JsonDocument.Parse(_stdout.ToString());
        doc.RootElement.TryGetProperty("stations", out var stations).Should().BeTrue();
        stations.GetArrayLength().Should().Be(0);
    }

    [TestMethod]
    public async Task SelectMosmix_Json_StandaloneMosmixLocationList()
    {
        var list = TestExportData.MosmixLocations("10637");
        SetupMosmix(list);
        var outcome = await _selectMosmixDocument.HandleAsync(MosmixRequest(OutputFormat.Json, false), Context(OutputFormat.Json), CancellationToken.None);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
        _stdout.ToString().Should().Be(await JsonOf(list));

        using var doc = JsonDocument.Parse(_stdout.ToString());
        doc.RootElement.TryGetProperty("locations", out var locations).Should().BeTrue();
        locations.GetArrayLength().Should().Be(1);
        locations[0].GetProperty("mosmix_id").GetString().Should().Be("10637");
    }

    [TestMethod]
    public async Task SelectMosmix_Xml_SchemaValid()
    {
        var list = TestExportData.MosmixLocations("10637");
        SetupMosmix(list);
        var outcome = await _selectMosmixDocument.HandleAsync(MosmixRequest(OutputFormat.Xml, false), Context(OutputFormat.Xml), CancellationToken.None);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
        SchemaValidation.ValidateAgainstPackagedSchema(Encoding.UTF8.GetBytes(_stdout.ToString())).Should().BeEmpty();
        XDocument.Parse(_stdout.ToString()).Root!.Name.LocalName.Should().Be("mosmix_location_list");
    }

    [TestMethod]
    public async Task SelectMosmix_Refresh_RunsRefreshThenSelection()
    {
        var sequence = new MockSequence();
        _refresher.InSequence(sequence).Setup(r => r.RefreshAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mosmixSelector.InSequence(sequence).Setup(s => s.SelectAsync(It.IsAny<SearchParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestExportData.MosmixLocations("10637"));

        var outcome = await _selectMosmixRefresh.HandleAsync(MosmixRequest(OutputFormat.Json, true), Context(OutputFormat.Json), CancellationToken.None);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
    }

    [TestMethod]
    public async Task SelectMosmix_RefreshFails_NoSelection()
    {
        _refresher.Setup(r => r.RefreshAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CatalogRetrievalException(CatalogRetrievalException.RetrievalFailedErrorId, "endpoint", "m"));

        Func<Task> act = async () => await _selectMosmixRefresh.HandleAsync(MosmixRequest(OutputFormat.Json, true), Context(OutputFormat.Json), CancellationToken.None);

        await act.Should().ThrowAsync<CatalogRetrievalException>();
        _stdout.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public async Task SelectMosmix_RefreshRequestedWithoutRefresher_Throws()
    {
        Func<Task> act = async () => await _selectMosmixDocument.HandleAsync(MosmixRequest(OutputFormat.Json, true), Context(OutputFormat.Json), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _stdout.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public async Task SelectDwd_Mqtt_OneNonRetainedMessageToPublisher()
    {
        var list = TestExportData.CdcStations("00044");
        SetupCdc(list);

        var outcome = await _selectDwdMqtt.HandleAsync(DwdRequest(OutputFormat.Mqtt), Context(OutputFormat.Mqtt), CancellationToken.None);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
        _queue.Messages.Should().ContainSingle();
        var message = _queue.Messages[0];
        message.Topic.Should().Be("haus/weather/stations/dwd/selection");
        message.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        message.Retain.Should().BeFalse();
        message.MessageType.Should().Be(MqttMessageType.CdcStationList);
        Encoding.UTF8.GetString(message.Payload.Span).Should().Be(await JsonOf(list));
        _stdout.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public async Task SelectMosmix_Mqtt_OneNonRetainedMessageToPublisher()
    {
        var list = TestExportData.MosmixLocations("10637");
        SetupMosmix(list);

        var outcome = await _selectMosmixMqtt.HandleAsync(MosmixRequest(OutputFormat.Mqtt, false), Context(OutputFormat.Mqtt), CancellationToken.None);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
        _queue.Messages.Should().ContainSingle();
        var message = _queue.Messages[0];
        message.Topic.Should().Be("haus/weather/stations/mosmix/selection");
        message.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        message.Retain.Should().BeFalse();
        message.MessageType.Should().Be(MqttMessageType.MosmixLocationList);
        Encoding.UTF8.GetString(message.Payload.Span).Should().Be(await JsonOf(list));
        _stdout.ToString().Should().BeEmpty();
    }
}
