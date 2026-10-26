using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Selection;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class OsthessenWetterParserTests
{
    private MockRepository _mocks = new(MockBehavior.Strict);
    private Mock<IOsthessenWetterPageSource> _source = null!;
    private Mock<IOsthessenWetterPageReader> _reader = null!;
    private Mock<IOsthessenWetterObservationMapper> _mapper = null!;
    private Mock<IObservationDatasetBuilder> _builder = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<OsthessenWetterParser> _logger = null!;
    private CancellationTokenSource _caller = null!;
    private OsthessenWetterParser _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _source = _mocks.Create<IOsthessenWetterPageSource>();
        _reader = _mocks.Create<IOsthessenWetterPageReader>();
        _mapper = _mocks.Create<IOsthessenWetterObservationMapper>();
        _builder = _mocks.Create<IObservationDatasetBuilder>();
        _clock = new ManualTimeProvider(new DateTimeOffset(2026, 1, 15, 9, 0, 0, 0, TimeSpan.Zero));
        _logger = new();
        _caller = new();
        _subject = Create(new OsthessenWetterOptions());
    }

    private OsthessenWetterParser Create(OsthessenWetterOptions options) => new(options, new StationNamePatternMatcher(), _source.Object, _reader.Object, _mapper.Object, _builder.Object, _clock, _logger);

    [TestCleanup]
    public void TestCleanup()
    {
        _caller.Dispose();
        _mocks.VerifyAll();
    }

    private static DateTime Retrieved => new(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);
    private static readonly OsthessenWetterPage Page = new(Array.Empty<OsthessenWetterStationRow>(), Array.Empty<OsthessenWetterPageDiagnostic>());

    private static ObservationLocation Location(string id, string name) => new() { StationId = id, Name = name, Latitude = 50.5m, Longitude = 9.7m };
    private static OsthessenWetterMappedStation Online(string id, string name) => new(id, name, OsthessenWetterStationStatus.Online, Location(id, name), new[] { new ObservationReading { Kind = QuantityKind.Temperature, Value = 10.3, Time = new NormalizedSourceTime(Retrieved, TimestampQuality.EstimatedDate, "Europe/Berlin") } }, Array.Empty<OsthessenWetterMappingNotice>());

    private void ArrangeSource(Func<CancellationToken, Task>? during = null)
    {
        _source.Setup(s => s.ReadAsync(It.IsAny<Func<Stream, CancellationToken, Task<OsthessenWetterPage>>>(), It.IsAny<CancellationToken>(), _caller.Token))
            .Returns(async (Func<Stream, CancellationToken, Task<OsthessenWetterPage>> read, CancellationToken deadline, CancellationToken caller) =>
            {
                if (during is not null)
                {
                    await during(deadline);
                }
                return await read(new MemoryStream(), deadline);
            });
        _reader.Setup(r => r.ReadAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(Page);
    }

    private void ArrangeSourceThrows(Func<CancellationToken, Exception> failure)
    {
        _source.Setup(s => s.ReadAsync(It.IsAny<Func<Stream, CancellationToken, Task<OsthessenWetterPage>>>(), It.IsAny<CancellationToken>(), _caller.Token))
            .Returns((Func<Stream, CancellationToken, Task<OsthessenWetterPage>> _, CancellationToken deadline, CancellationToken _) => Task.FromException<OsthessenWetterPage>(failure(deadline)));
    }

    private static OsthessenWetterException Deadline() => new(OsthessenWetterException.DeadlineExceededErrorId, "deadline");

    private void ArrangeMap(params OsthessenWetterMappedStation[] stations)
    {
        _mapper.Setup(m => m.Map(Page, Retrieved)).Returns(stations);
    }

    private void ArrangeBuild()
    {
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Returns(new ObservationBuildResult(new ObservationDataset(), Array.Empty<OmittedReading>()));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_InvalidPattern_ThrowsBeforeRequest()
    {
        Func<Task> act = () => _subject.ParseAsync("", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.ErrorId.Should().Be(OsthessenWetterException.PatternInvalidErrorId);
        _clock.CreatedTimers.Should().Be(0);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineFromOptionsAndTimeProvider()
    {
        _subject = Create(new OsthessenWetterOptions { PageDeadline = TimeSpan.FromSeconds(45) });
        var cancelledAt44 = true;
        var cancelledAt45 = false;
        ArrangeSourceThrows(deadline =>
        {
            _clock.Advance(TimeSpan.FromSeconds(44));
            cancelledAt44 = deadline.IsCancellationRequested;
            _clock.Advance(TimeSpan.FromSeconds(1));
            cancelledAt45 = deadline.IsCancellationRequested;
            return Deadline();
        });
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.ErrorId.Should().Be(OsthessenWetterException.DeadlineExceededErrorId);
        cancelledAt44.Should().BeFalse("the deadline must not expire before the configured 45 seconds");
        cancelledAt45.Should().BeTrue("the deadline must expire at the configured 45 seconds");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_CallerCancelledDuringRetrieval_ThrowsOperationCanceled()
    {
        ArrangeSourceThrows(_ => { _caller.Cancel(); return new OperationCanceledException(_caller.Token); });
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.CancellationToken.Should().Be(_caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineDuringRetrieval_ThrowsDeadlineExceeded()
    {
        ArrangeSourceThrows(_ => { _clock.Advance(TimeSpan.FromSeconds(60)); return Deadline(); });
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.ErrorId.Should().Be(OsthessenWetterException.DeadlineExceededErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_BothAtOnce_CallerCancellationWins()
    {
        ArrangeSourceThrows(_ => { _clock.Advance(TimeSpan.FromSeconds(60)); _caller.Cancel(); return Deadline(); });
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.CancellationToken.Should().Be(_caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineAndOtherFailureAtOnce_DeadlineWins()
    {
        ArrangeSourceThrows(_ => { _clock.Advance(TimeSpan.FromSeconds(60)); return new OsthessenWetterException(OsthessenWetterException.PageUnavailableErrorId, "unavailable"); });
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.ErrorId.Should().Be(OsthessenWetterException.DeadlineExceededErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineSurfacedAsOperationCanceled_ThrowsDeadlineExceeded()
    {
        ArrangeSourceThrows(deadline => { _clock.Advance(TimeSpan.FromSeconds(60)); return new OperationCanceledException(deadline); });
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.ErrorId.Should().Be(OsthessenWetterException.DeadlineExceededErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_PageUnavailable_Propagates()
    {
        var unavailable = new OsthessenWetterException(OsthessenWetterException.PageUnavailableErrorId, "unavailable");
        ArrangeSourceThrows(_ => unavailable);
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.Should().BeSameAs(unavailable);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_FormatChanged_Propagates()
    {
        ArrangeSource();
        _reader.Setup(r => r.ReadAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ThrowsAsync(new OsthessenWetterException(OsthessenWetterException.FormatChangedErrorId, "changed"));
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_RetrievedAtSampledAfterPageSourceReturns()
    {
        ArrangeSource(_ => { _clock.Advance(TimeSpan.FromMilliseconds(7750)); return Task.CompletedTask; });
        _mapper.Setup(m => m.Map(Page, new DateTime(2026, 1, 15, 9, 0, 7, DateTimeKind.Utc))).Returns(new[] { Online("bimbach", "Bimbach") });
        var captured = new List<ObservationInput>();
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Callback<ObservationInput>(captured.Add).Returns(new ObservationBuildResult(new ObservationDataset(), Array.Empty<OmittedReading>()));
        await _subject.ParseAsync("*", _caller.Token);
        captured.Should().ContainSingle();
        captured[0].RetrievedAtUtc.Should().Be(new DateTime(2026, 1, 15, 9, 0, 7, DateTimeKind.Utc));
        captured[0].Provider.Should().Be("osthessenwetter");
        _clock.GetUtcNowCalls.Should().Be(1);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_CallerCancelledDuringMapping_ThrowsOperationCanceled()
    {
        ArrangeSource();
        _mapper.Setup(m => m.Map(Page, Retrieved)).Callback(() => _caller.Cancel()).Returns(new[] { Online("bimbach", "Bimbach") });
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.CancellationToken.Should().Be(_caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineDuringMapping_ThrowsDeadlineExceeded()
    {
        ArrangeSource();
        _mapper.Setup(m => m.Map(Page, Retrieved)).Callback(() => _clock.Advance(TimeSpan.FromSeconds(60))).Returns(new[] { Online("bimbach", "Bimbach") });
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.ErrorId.Should().Be(OsthessenWetterException.DeadlineExceededErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineBetweenStations_NoPartialResult()
    {
        ArrangeSource();
        ArrangeMap(Online("bimbach", "Bimbach"), Online("schlitz", "Schlitz"));
        _builder.Setup(b => b.Build(It.Is<ObservationInput>(i => i.Location!.StationId == "bimbach"))).Callback(() => _clock.Advance(TimeSpan.FromSeconds(60))).Returns(new ObservationBuildResult(new ObservationDataset(), Array.Empty<OmittedReading>()));
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.ErrorId.Should().Be(OsthessenWetterException.DeadlineExceededErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineDuringBuild_WinsOverBuilderRejection()
    {
        ArrangeSource();
        ArrangeMap(Online("bimbach", "Bimbach"));
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Callback(() => _clock.Advance(TimeSpan.FromSeconds(60))).Throws(new ObservationDatasetRejectedException("rejected", Array.Empty<OmittedReading>()));
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.ErrorId.Should().Be(OsthessenWetterException.DeadlineExceededErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_CancellationAndDeadlineDuringBuild_CancellationWins()
    {
        ArrangeSource();
        ArrangeMap(Online("bimbach", "Bimbach"));
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Callback(() =>
        {
            _clock.Advance(TimeSpan.FromSeconds(60));
            _caller.Cancel();
        }).Throws(new ObservationDatasetRejectedException("rejected", Array.Empty<OmittedReading>()));
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.CancellationToken.Should().Be(_caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineTimerDisposed_OnSuccess()
    {
        ArrangeSource();
        ArrangeMap(Online("bimbach", "Bimbach"));
        ArrangeBuild();
        await _subject.ParseAsync("*", _caller.Token);
        _clock.ActiveTimers.Should().Be(0);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineTimerDisposed_OnFailure()
    {
        ArrangeSourceThrows(_ => new OsthessenWetterException(OsthessenWetterException.PageUnavailableErrorId, "unavailable"));
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        await act.Should().ThrowAsync<OsthessenWetterException>();
        _clock.ActiveTimers.Should().Be(0);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineTimerDisposed_OnCancellation()
    {
        ArrangeSourceThrows(_ => { _caller.Cancel(); return new OperationCanceledException(_caller.Token); });
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        _clock.ActiveTimers.Should().Be(0);
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Action act = () => new OsthessenWetterParser(null!, new StationNamePatternMatcher(), _source.Object, _reader.Object, _mapper.Object, _builder.Object, _clock, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");

        act = () => new OsthessenWetterParser(new OsthessenWetterOptions(), null!, _source.Object, _reader.Object, _mapper.Object, _builder.Object, _clock, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("matcher");

        act = () => new OsthessenWetterParser(new OsthessenWetterOptions(), new StationNamePatternMatcher(), null!, _reader.Object, _mapper.Object, _builder.Object, _clock, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("source");

        act = () => new OsthessenWetterParser(new OsthessenWetterOptions(), new StationNamePatternMatcher(), _source.Object, null!, _mapper.Object, _builder.Object, _clock, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("reader");

        act = () => new OsthessenWetterParser(new OsthessenWetterOptions(), new StationNamePatternMatcher(), _source.Object, _reader.Object, null!, _builder.Object, _clock, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("mapper");

        act = () => new OsthessenWetterParser(new OsthessenWetterOptions(), new StationNamePatternMatcher(), _source.Object, _reader.Object, _mapper.Object, null!, _clock, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("builder");

        act = () => new OsthessenWetterParser(new OsthessenWetterOptions(), new StationNamePatternMatcher(), _source.Object, _reader.Object, _mapper.Object, _builder.Object, null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");

        act = () => new OsthessenWetterParser(new OsthessenWetterOptions(), new StationNamePatternMatcher(), _source.Object, _reader.Object, _mapper.Object, _builder.Object, _clock, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
