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

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _writer = _mocks.Create<IResultWriter>();
        _cancellation = new CancellationTokenSource();
        _token = _cancellation.Token;
        _stdout = new StringWriter();
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
        var selector = _mocks.Create<ICdcStationSelector>();
        var search = TestExportData.Search();
        selector.Setup(s => s.SelectAsync(search, _token)).ReturnsAsync(TestExportData.CdcStations("00044"));
        ExpectWrite<CdcStationList>();
        var subject = new SelectDwdHandler(selector.Object, _writer.Object);

        var outcome = await subject.HandleAsync(new SelectDwdRequest(Json, search), Context(null), _token);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
    }

    /// <summary>
    /// Verifies that the SelectMosmixHandler passes its cancellation token to the refresher, selector and writer when refreshing.
    /// </summary>
    [TestMethod]
    public async Task SelectMosmix_WithRefresh_TokenPassedToRefresherSelectorAndWriter()
    {
        var selector = _mocks.Create<IMosmixLocationSelector>();
        var refresher = _mocks.Create<IMosmixSEvidenceRefresher>();
        var search = TestExportData.Search();
        selector.Setup(s => s.SelectAsync(search, _token)).ReturnsAsync(TestExportData.MosmixLocations("10637"));
        refresher.Setup(r => r.RefreshAsync(_token)).Returns(Task.CompletedTask);
        ExpectWrite<MosmixLocationList>();
        var subject = new SelectMosmixHandler(selector.Object, _writer.Object, refresher.Object);

        var outcome = await subject.HandleAsync(new SelectMosmixRequest(Json, search, true), Context(null), _token);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
    }

    /// <summary>
    /// Verifies that the ParseDwdHandler passes its cancellation token to the parser and writer.
    /// </summary>
    [TestMethod]
    public async Task ParseDwd_TokenPassedToParserAndWriter()
    {
        var parser = _mocks.Create<IDwdObservationParser>();
        parser.Setup(p => p.ParseAsync("00044", _token)).ReturnsAsync(TestExportData.Observation("dwd", "00044"));
        ExpectWrite<ExportDocument>();
        var subject = new ParseDwdHandler(parser.Object, _writer.Object, new ExportErrorMessages(), new LibraryErrors(), new FailureClassifier(), NullLogger<ParseDwdHandler>.Instance);

        var outcome = await subject.HandleAsync(new ParseDwdRequest(Json, "00044"), Context("00044"), _token);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
    }

    /// <summary>
    /// Verifies that the ParseMosmixHandler passes its cancellation token to the MOSMIX_S parser and writer.
    /// </summary>
    [TestMethod]
    public async Task ParseMosmixS_TokenPassedToParserAndWriter()
    {
        var l = _mocks.Create<IMosmixLParser>();
        var s = _mocks.Create<IMosmixSParser>();
        var search = TestExportData.Search();
        s.Setup(p => p.ParseAsync("10637", _token)).ReturnsAsync(Result(TestExportData.Forecast(MosmixProduct.MosmixS, "10637")));
        ExpectWrite<ExportDocument>();
        var subject = new ParseMosmixHandler(l.Object, s.Object, _writer.Object, new ExportErrorMessages(), new LibraryErrors(), new FailureClassifier(), NullLogger<ParseMosmixHandler>.Instance);

        var outcome = await subject.HandleAsync(new ParseMosmixRequest(Json, MosmixProduct.MosmixS, "10637"), Context(null), _token);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
    }

    /// <summary>
    /// Verifies that the ParseMosmixHandler passes its cancellation token to the MOSMIX_L parser and writer.
    /// </summary>
    [TestMethod]
    public async Task ParseMosmixL_TokenPassedToParserAndWriter()
    {
        var l = _mocks.Create<IMosmixLParser>();
        var s = _mocks.Create<IMosmixSParser>();
        var search = TestExportData.Search();
        l.Setup(p => p.ParseAsync("10637", _token)).ReturnsAsync(Result(TestExportData.Forecast(MosmixProduct.MosmixL, "10637")));
        ExpectWrite<ExportDocument>();
        var subject = new ParseMosmixHandler(l.Object, s.Object, _writer.Object, new ExportErrorMessages(), new LibraryErrors(), new FailureClassifier(), NullLogger<ParseMosmixHandler>.Instance);

        var outcome = await subject.HandleAsync(new ParseMosmixRequest(Json, MosmixProduct.MosmixL, "10637"), Context(null), _token);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
    }

    /// <summary>
    /// Verifies that the ParseOstHessenHandler passes its cancellation token to the parser and writer.
    /// </summary>
    [TestMethod]
    public async Task ParseOstHessen_TokenPassedToParserAndWriter()
    {
        var parser = _mocks.Create<IOsthessenWetterParser>();
        var identity = _mocks.Create<IOsthessenWetterStationIdentity>();
        var dataset = TestExportData.Observation("osthessenwetter", "fulda");
        parser.Setup(p => p.ParseAsync("Fulda*", _token)).ReturnsAsync(new OsthessenWetterParseResult(new[] { dataset }, Array.Empty<OsthessenWetterStationFailure>()));
        ExpectWrite<ExportDocument>();
        var subject = new ParseOstHessenHandler(parser.Object, identity.Object, _writer.Object, new ExportErrorMessages(), NullLogger<ParseOstHessenHandler>.Instance);

        var outcome = await subject.HandleAsync(new ParseOstHessenRequest(Json, "Fulda*"), Context(null), _token);

        outcome.Should().Be(new CommandOutcome(1, 0, false));
    }
}
