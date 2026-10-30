using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Selection;

namespace WeatherParser.Cli.Tests.Commands;

[TestClass]
[TestCategory("T-P3-18")]
public sealed class HandlerTokenTests
{
    private MockRepository _mocks = null!;
    private Mock<IResultWriter> _writer = null!;
    private CancellationTokenSource _cancellation = null!;
    private CancellationToken _token;
    private StringWriter _stdout = null!;

    private Mock<ICdcStationSelector> _cdcSelector = null!;
    private Mock<IMosmixLocationSelector> _mosmixSelector = null!;
    private Mock<IMosmixSEvidenceRefresher> _refresher = null!;
    private Mock<IDwdObservationParser> _dwdParser = null!;
    private Mock<IMosmixLParser> _mosmixL = null!;
    private Mock<IMosmixSParser> _mosmixS = null!;
    private Mock<IOsthessenWetterParser> _osthessenParser = null!;
    private Mock<IOsthessenWetterStationIdentity> _identity = null!;
    private SelectDwdHandler _selectDwd = null!;
    private SelectMosmixHandler _selectMosmix = null!;
    private ParseDwdHandler _parseDwd = null!;
    private ParseMosmixHandler _parseMosmix = null!;
    private ParseOstHessenHandler _parseOstHessen = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _writer = _mocks.Create<IResultWriter>();
        _cancellation = new CancellationTokenSource();
        _token = _cancellation.Token;
        _stdout = new StringWriter();

        _cdcSelector = _mocks.Create<ICdcStationSelector>();
        _mosmixSelector = _mocks.Create<IMosmixLocationSelector>();
        _refresher = _mocks.Create<IMosmixSEvidenceRefresher>();
        _dwdParser = _mocks.Create<IDwdObservationParser>();
        _mosmixL = _mocks.Create<IMosmixLParser>();
        _mosmixS = _mocks.Create<IMosmixSParser>();
        _osthessenParser = _mocks.Create<IOsthessenWetterParser>();
        _identity = _mocks.Create<IOsthessenWetterStationIdentity>();

        _selectDwd = new SelectDwdHandler(_cdcSelector.Object, _writer.Object);
        _selectMosmix = new SelectMosmixHandler(_mosmixSelector.Object, _writer.Object, _refresher.Object);
        _parseDwd = new ParseDwdHandler(_dwdParser.Object, _writer.Object, new ExportErrorMessages(), new LibraryErrors(), new FailureClassifier(), NullLogger<ParseDwdHandler>.Instance);
        _parseMosmix = new ParseMosmixHandler(_mosmixL.Object, _mosmixS.Object, _writer.Object, new ExportErrorMessages(), new LibraryErrors(), new FailureClassifier(), NullLogger<ParseMosmixHandler>.Instance);
        _parseOstHessen = new ParseOstHessenHandler(_osthessenParser.Object, _identity.Object, _writer.Object, new ExportErrorMessages(), NullLogger<ParseOstHessenHandler>.Instance);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
        _cancellation.Dispose();
        _stdout.Dispose();
    }

    private static CommonOptions Json => new(OutputFormat.Json, null, null, false);

    private CommandContext Context(string? stationId) => new(Json, stationId, _stdout);

    private void ExpectWrite<TRoot>() where TRoot : class =>
        _writer.Setup(w => w.WriteAsync(It.IsAny<TRoot>(), It.IsAny<CommandContext>(), _token)).Returns(Task.CompletedTask);

    private static MosmixParseResult Result(ForecastRun run) => new(run, Array.Empty<OmittedForecastValue>(), Array.Empty<string>(), Array.Empty<string>(), MosmixRunFreshness.Fresh, TimeSpan.FromHours(1));

    /// <summary>
    /// Verifies that the SelectDwdHandler passes its cancellation token to the selector and writer.
    /// </summary>
    [TestMethod]
    public async Task SelectDwd_TokenPassedToSelectorAndWriter()
    {
        var search = TestExportData.Search();
        _cdcSelector.Setup(s => s.SelectAsync(search, _token)).ReturnsAsync(TestExportData.CdcStations("00044"));
        ExpectWrite<CdcStationList>();

        var outcome = await _selectDwd.HandleAsync(new SelectDwdRequest(Json, search), Context(null), _token);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
    }

    /// <summary>
    /// Verifies that the SelectMosmixHandler passes its cancellation token to the refresher, selector and writer when refreshing.
    /// </summary>
    [TestMethod]
    public async Task SelectMosmix_WithRefresh_TokenPassedToRefresherSelectorAndWriter()
    {
        var search = TestExportData.Search();
        _mosmixSelector.Setup(s => s.SelectAsync(search, _token)).ReturnsAsync(TestExportData.MosmixLocations("10637"));
        _refresher.Setup(r => r.RefreshAsync(_token)).Returns(Task.CompletedTask);
        ExpectWrite<MosmixLocationList>();

        var outcome = await _selectMosmix.HandleAsync(new SelectMosmixRequest(Json, search, true), Context(null), _token);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
    }

    /// <summary>
    /// Verifies that the ParseDwdHandler passes its cancellation token to the parser and writer.
    /// </summary>
    [TestMethod]
    public async Task ParseDwd_TokenPassedToParserAndWriter()
    {
        _dwdParser.Setup(p => p.ParseAsync("00044", _token)).ReturnsAsync(TestExportData.Observation("dwd", "00044"));
        ExpectWrite<ExportDocument>();

        var outcome = await _parseDwd.HandleAsync(new ParseDwdRequest(Json, "00044"), Context("00044"), _token);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
    }

    /// <summary>
    /// Verifies that the ParseMosmixHandler passes its cancellation token to the MOSMIX_S parser and writer.
    /// </summary>
    [TestMethod]
    public async Task ParseMosmixS_TokenPassedToParserAndWriter()
    {
        _mosmixS.Setup(p => p.ParseAsync("10637", _token)).ReturnsAsync(Result(TestExportData.Forecast(MosmixProduct.MosmixS, "10637")));
        ExpectWrite<ExportDocument>();

        var outcome = await _parseMosmix.HandleAsync(new ParseMosmixRequest(Json, MosmixProduct.MosmixS, "10637"), Context(null), _token);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
    }

    /// <summary>
    /// Verifies that the ParseMosmixHandler passes its cancellation token to the MOSMIX_L parser and writer.
    /// </summary>
    [TestMethod]
    public async Task ParseMosmixL_TokenPassedToParserAndWriter()
    {
        _mosmixL.Setup(p => p.ParseAsync("10637", _token)).ReturnsAsync(Result(TestExportData.Forecast(MosmixProduct.MosmixL, "10637")));
        ExpectWrite<ExportDocument>();

        var outcome = await _parseMosmix.HandleAsync(new ParseMosmixRequest(Json, MosmixProduct.MosmixL, "10637"), Context(null), _token);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
    }

    /// <summary>
    /// Verifies that the ParseOstHessenHandler passes its cancellation token to the parser and writer.
    /// </summary>
    [TestMethod]
    public async Task ParseOstHessen_TokenPassedToParserAndWriter()
    {
        var dataset = TestExportData.Observation("osthessenwetter", "fulda");
        _osthessenParser.Setup(p => p.ParseAsync("Fulda*", _token)).ReturnsAsync(new OsthessenWetterParseResult(new[] { dataset }, Array.Empty<OsthessenWetterStationFailure>()));
        ExpectWrite<ExportDocument>();

        var outcome = await _parseOstHessen.HandleAsync(new ParseOstHessenRequest(Json, "Fulda*"), Context(null), _token);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
    }
}
