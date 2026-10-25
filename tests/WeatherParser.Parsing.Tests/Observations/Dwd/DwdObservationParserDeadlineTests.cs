using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Selection;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations.Dwd;

[TestClass]
public sealed class DwdObservationParserDeadlineTests
{
    private MockRepository _mocks = null!;
    private Mock<IDwdStationResolver> _resolver = null!;
    private Mock<IDwdObservationSource> _source = null!;
    private Mock<IDwdObservationMapper> _mapper = null!;
    private Mock<IObservationDatasetBuilder> _builder = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<DwdObservationParser> _logger = null!;
    private CancellationTokenSource _caller = null!;
    private DwdObservationParser _subject = null!;

    private static readonly DateTime Retrieved = new(2026, 10, 24, 12, 20, 30, DateTimeKind.Utc);
    private static readonly IReadOnlyList<CdcProductList> Lists = new[] { new CdcProductList("tu", "AirTemperatureStationList", new CdcStationCatalog(Array.Empty<CdcStationRecord>(), Array.Empty<CatalogDiagnostic>())) };

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _resolver = _mocks.Create<IDwdStationResolver>();
        _source = _mocks.Create<IDwdObservationSource>();
        _mapper = _mocks.Create<IDwdObservationMapper>();
        _builder = _mocks.Create<IObservationDatasetBuilder>();
        _clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 24, 12, 20, 30, 750, TimeSpan.Zero));
        _logger = new CapturingLogger<DwdObservationParser>();
        _caller = new CancellationTokenSource();
        _subject = new DwdObservationParser(new DwdObservationOptions(), _resolver.Object, _source.Object, _mapper.Object, _builder.Object, _clock, _logger);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _caller.Dispose();
        _mocks.VerifyAll();
    }

    private static ObservationLocation Location() => new() { StationId = "00044", Name = "Station", Latitude = 50.1m, Longitude = 8.6m, AltitudeM = 100m };

    private static DwdResolvedStation Station(params string[] products) => new("00044", Location(), products);

    private static CdcNowEvidence Evidence(string code) => new("00044", code, Array.Empty<CdcNowEvidenceRow>());

    private void ArrangeUntilArchives(DwdResolvedStation station, IReadOnlyDictionary<string, CdcNowEvidence?> archives)
    {
        _resolver.Setup(r => r.ValidateId("00044")).Returns("00044");
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(Lists);
        _resolver.Setup(r => r.Resolve("00044", Lists)).Returns(station);
        _source.Setup(s => s.ReadArchivesAsync("00044", station.ProductCodes, It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(archives);
    }

    private static DwdObservationParser RealBuilderParser(Mock<IDwdStationResolver> resolver, Mock<IDwdObservationSource> source, Mock<IDwdObservationMapper> mapper, ManualTimeProvider clock, CapturingLogger<DwdObservationParser> logger) =>
        new(new DwdObservationOptions(), resolver.Object, source.Object, mapper.Object, new ObservationDatasetBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter()), clock, logger);

    private static ObservationReading Temperature(double value, DateTime time) => new() { Kind = QuantityKind.Temperature, Value = value, Time = new NormalizedSourceTime(time, TimestampQuality.Source, null) };

    private void ExpectValidate() => _resolver.Setup(r => r.ValidateId("00044")).Returns("00044");

    private void ExpectLists(Action? during = null) => _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).Returns(() => { during?.Invoke(); return Task.FromResult(Lists); });

    private void ExpectResolve() => _resolver.Setup(r => r.Resolve("00044", Lists)).Returns(Tu);

    private static readonly DwdResolvedStation Tu = Station("tu");

    private void ExpectArchives(Action? during = null) => _source.Setup(s => s.ReadArchivesAsync("00044", Tu.ProductCodes, It.IsAny<CancellationToken>(), _caller.Token)).Returns(() => { during?.Invoke(); return Task.FromResult<IReadOnlyDictionary<string, CdcNowEvidence?>>(new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") }); });

    private void ExpectMap(Action? during = null) => _mapper.Setup(m => m.Map(It.IsAny<IReadOnlyList<CdcNowEvidence>>())).Returns(() => { during?.Invoke(); return new DwdMappingResult(new[] { Temperature(12.3, Retrieved) }, Array.Empty<DwdMappingOmission>()); });

    private void ExpectBuild(Action? during = null) => _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Returns(() => { during?.Invoke(); return new ObservationBuildResult(new ObservationDataset(), Array.Empty<OmittedReading>()); });

    private void Expire() => _clock.Advance(TimeSpan.FromSeconds(121));

    private Task Parse() => _subject.ParseAsync("00044", _caller.Token);

    private async Task ShouldExceedDeadline() => await FluentActions.Awaiting(Parse).Should().ThrowAsync<ObservationLimitException>().Where(ex => ex.ErrorId == ObservationLimitException.DeadlineExceededErrorId);

    private async Task ShouldBeCancelledByCaller() => await FluentActions.Awaiting(Parse).Should().ThrowAsync<OperationCanceledException>().Where(ex => ex.CancellationToken == _caller.Token);

    [TestMethod]
    public async Task ParseAsync_DeadlineExpiresDuringLists_ThrowsObservationDeadlineExceeded()
    {
        ExpectValidate();
        ExpectLists(Expire);
        await ShouldExceedDeadline();
    }

    [TestMethod]
    public async Task ParseAsync_DeadlineExpiresDuringArchives_ThrowsObservationDeadlineExceeded()
    {
        ExpectValidate();
        ExpectLists();
        ExpectResolve();
        _source.Setup(s => s.ReadArchivesAsync("00044", Tu.ProductCodes, It.IsAny<CancellationToken>(), _caller.Token)).ThrowsAsync(new ObservationLimitException(ObservationLimitException.DeadlineExceededErrorId, "Deadline."));
        await ShouldExceedDeadline();
    }

    [TestMethod]
    public async Task ParseAsync_DeadlineExpiresBeforeMapping_ThrowsObservationDeadlineExceeded()
    {
        ExpectValidate();
        ExpectLists();
        ExpectResolve();
        ExpectArchives(Expire);
        await ShouldExceedDeadline();
    }

    [TestMethod]
    public async Task ParseAsync_CallerCancelsDuringMapping_NoSuccessfulReturn()
    {
        ExpectValidate();
        ExpectLists();
        ExpectResolve();
        ExpectArchives();
        ExpectMap(() => _caller.Cancel());
        await ShouldBeCancelledByCaller();
    }

    [TestMethod]
    public async Task ParseAsync_DeadlineExpiresDuringBuild_ThrowsBeforeReturning()
    {
        ExpectValidate();
        ExpectLists();
        ExpectResolve();
        ExpectArchives();
        ExpectMap();
        ExpectBuild(Expire);
        await ShouldExceedDeadline();
    }

    [TestMethod]
    public async Task ParseAsync_CallerCancelsDuringLists_ThrowsOperationCanceledWithCallerToken()
    {
        ExpectValidate();
        ExpectLists(() => _caller.Cancel());
        await ShouldBeCancelledByCaller();
    }

    [TestMethod]
    public async Task ParseAsync_CallerCancelsAndDeadlineExpired_CallerWins()
    {
        ExpectValidate();
        ExpectLists(() => { Expire(); _caller.Cancel(); });
        await ShouldBeCancelledByCaller();
    }

    [TestMethod]
    public async Task ParseAsync_DeadlineResourcesReleasedAfterSuccessAndFailure()
    {
        ExpectValidate();
        ExpectLists();
        ExpectResolve();
        ExpectArchives();
        ExpectMap();
        ExpectBuild();
        await Parse();
        _clock.CreatedTimers.Should().Be(1);
        _clock.ActiveTimers.Should().Be(0);

        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).ThrowsAsync(new CatalogRetrievalException(CatalogRetrievalException.RetrievalFailedErrorId, "AirTemperatureStationList", "Failed."));
        await FluentActions.Awaiting(Parse).Should().ThrowAsync<CatalogRetrievalException>();
        _clock.CreatedTimers.Should().Be(2);
        _clock.ActiveTimers.Should().Be(0);
    }

    [TestMethod]
    public async Task ParseAsync_SecondParseHasIndependentDeadline()
    {
        ExpectValidate();
        ExpectLists(() => _clock.Advance(TimeSpan.FromSeconds(100)));
        ExpectResolve();
        ExpectArchives();
        ExpectMap();
        ExpectBuild();
        await Parse();
        await Parse();
    }

    [TestMethod]
    public async Task ParseAsync_PassesDeadlineTokenToBothSourceCalls()
    {
        ExpectValidate();
        _source.Setup(s => s.ReadStationListsAsync(It.Is<CancellationToken>(d => d.CanBeCanceled), _caller.Token)).ReturnsAsync(Lists);
        ExpectResolve();
        _source.Setup(s => s.ReadArchivesAsync("00044", Tu.ProductCodes, It.Is<CancellationToken>(d => d.CanBeCanceled), _caller.Token)).ReturnsAsync(new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") });
        ExpectMap();
        ExpectBuild();
        await Parse();
    }

    [TestMethod]
    public async Task ParseAsync_CallerCancelsThenSourceFails_CallerWins()
    {
        ExpectValidate();
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).Returns(() => { _caller.Cancel(); return Task.FromException<IReadOnlyList<CdcProductList>>(new CatalogRetrievalException(CatalogRetrievalException.RetrievalFailedErrorId, "AirTemperatureStationList", "Failed.")); });
        await ShouldBeCancelledByCaller();
    }

    [TestMethod]
    public async Task ParseAsync_DeadlineExpiresThenArchiveFormatFailure_DeadlineWins()
    {
        ExpectValidate();
        ExpectLists();
        ExpectResolve();
        _source.Setup(s => s.ReadArchivesAsync("00044", Tu.ProductCodes, It.IsAny<CancellationToken>(), _caller.Token)).Returns(() => { Expire(); return Task.FromException<IReadOnlyDictionary<string, CdcNowEvidence?>>(new CatalogFormatException(CatalogFormatException.CdcEvidenceInvalidErrorId, "Bad archive.")); });
        await ShouldExceedDeadline();
    }

    [TestMethod]
    public async Task ParseAsync_DeadlineExpiresThenBuilderRejects_DeadlineWinsAfterLoggingOmissions()
    {
        ExpectValidate();
        ExpectLists();
        ExpectResolve();
        ExpectArchives();
        ExpectMap();
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Callback(() => Expire()).Throws(new ObservationDatasetRejectedException("No reading.", new[] { new OmittedReading(QuantityKind.Temperature, OmissionReason.Missing) }));
        await ShouldExceedDeadline();
        _logger.Count(DwdObservationNoticeIds.ReadingOmitted).Should().Be(1);
    }

    [TestMethod]
    public async Task ParseAsync_CancelledDuringFinalOmissionLogging_NoSuccessfulReturn()
    {
        var parser = new DwdObservationParser(new DwdObservationOptions(), _resolver.Object, _source.Object, _mapper.Object, _builder.Object, _clock, new CancellingLogger(_caller));
        ExpectValidate();
        ExpectLists();
        ExpectResolve();
        ExpectArchives();
        ExpectMap();
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Returns(new ObservationBuildResult(new ObservationDataset(), new[] { new OmittedReading(QuantityKind.RelativeHumidity, OmissionReason.Missing) }));
        await FluentActions.Awaiting(() => parser.ParseAsync("00044", _caller.Token)).Should().ThrowAsync<OperationCanceledException>().Where(ex => ex.CancellationToken == _caller.Token);
    }

    [TestMethod]
    public async Task ParseAsync_DeadlineExpiresDuringFinalOmissionLogging_NoSuccessfulReturn()
    {
        var parser = new DwdObservationParser(new DwdObservationOptions(), _resolver.Object, _source.Object, _mapper.Object, _builder.Object, _clock, new ExpiringLogger(_clock));
        ExpectValidate();
        ExpectLists();
        ExpectResolve();
        ExpectArchives();
        ExpectMap();
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Returns(new ObservationBuildResult(new ObservationDataset(), new[] { new OmittedReading(QuantityKind.RelativeHumidity, OmissionReason.Missing) }));
        await FluentActions.Awaiting(() => parser.ParseAsync("00044", _caller.Token)).Should().ThrowAsync<ObservationLimitException>().Where(ex => ex.ErrorId == ObservationLimitException.DeadlineExceededErrorId);
    }

    private sealed class CancellingLogger(CancellationTokenSource source) : ILogger<DwdObservationParser>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId.Name == DwdObservationNoticeIds.ReadingOmitted)
            {
                source.Cancel();
            }
        }
    }

    private sealed class ExpiringLogger(ManualTimeProvider clock) : ILogger<DwdObservationParser>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (eventId.Name == DwdObservationNoticeIds.ReadingOmitted)
            {
                clock.Advance(TimeSpan.FromSeconds(121));
            }
        }
    }
}
