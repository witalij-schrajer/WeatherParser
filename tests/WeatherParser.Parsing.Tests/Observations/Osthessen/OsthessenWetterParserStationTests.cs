using System;
using System.Collections.Generic;
using System.Linq;
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
public sealed class OsthessenWetterParserStationTests
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
    
    private static OsthessenWetterPage EmptyPage() => new(Array.Empty<OsthessenWetterStationRow>(), Array.Empty<OsthessenWetterPageDiagnostic>());

    private static ObservationLocation Location(string id, string name) => new() { StationId = id, Name = name, Latitude = 50.5m, Longitude = 9.7m };
    
    private static OsthessenWetterMappedStation Online(string id, string name, params OsthessenWetterMappingNotice[] notices) => new(id, name, OsthessenWetterStationStatus.Online, Location(id, name), new[] { new ObservationReading { Kind = QuantityKind.Temperature, Value = 10.3, Time = new NormalizedSourceTime(Retrieved, TimestampQuality.EstimatedDate, "Europe/Berlin") } }, notices);
    
    private static OsthessenWetterMappedStation Offline(string id, string name) => new(id, name, OsthessenWetterStationStatus.Offline, Location(id, name), Array.Empty<ObservationReading>(), Array.Empty<OsthessenWetterMappingNotice>());
    
    private static OsthessenWetterMappedStation NoMetadata(string id, string name) => new(id, name, OsthessenWetterStationStatus.Online, null, new[] { new ObservationReading { Kind = QuantityKind.Temperature, Value = 10.3, Time = new NormalizedSourceTime(Retrieved, TimestampQuality.EstimatedDate, "Europe/Berlin") } }, Array.Empty<OsthessenWetterMappingNotice>());

    private void ArrangeSource(OsthessenWetterPage page)
    {
        _source.Setup(s => s.ReadAsync(It.IsAny<Func<Stream, CancellationToken, Task<OsthessenWetterPage>>>(), It.IsAny<CancellationToken>(), _caller.Token))
            .Returns(async (Func<Stream, CancellationToken, Task<OsthessenWetterPage>> read, CancellationToken deadline, CancellationToken caller) =>
            {
                return await read(new MemoryStream(), deadline);
            });
        _reader.Setup(r => r.ReadAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(page);
    }

    private void Arrange(OsthessenWetterPage page, params OsthessenWetterMappedStation[] stations)
    {
        ArrangeSource(page);
        _mapper.Setup(m => m.Map(page, Retrieved)).Returns(stations);
    }

    private ObservationDataset ArrangeBuild(OsthessenWetterMappedStation station, params OmittedReading[] omitted)
    {
        var dataset = new ObservationDataset();
        _builder.Setup(b => b.Build(It.Is<ObservationInput>(i => i.Provider == "osthessenwetter" && i.Location == station.Location && i.RetrievedAtUtc == Retrieved && i.Readings == station.Readings))).Returns(new ObservationBuildResult(dataset, omitted));
        return dataset;
    }

    private void ArrangeRejected(OsthessenWetterMappedStation station, params OmittedReading[] omitted)
    {
        _builder.Setup(b => b.Build(It.Is<ObservationInput>(i => i.Provider == "osthessenwetter" && i.Location == station.Location && i.RetrievedAtUtc == Retrieved && i.Readings == station.Readings))).Throws(new ObservationDatasetRejectedException("rejected", omitted));
    }

    private int IndexOf(string eventName) => _logger.Entries.FindIndex(e => e.EventName == eventName);

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_OneMatch_OneDataset()
    {
        var bimbach = Online("bimbach", "Bimbach");
        var schlitz = Online("schlitz", "Schlitz");
        var page = EmptyPage();
        Arrange(page, bimbach, schlitz);
        var datasetBimbach = ArrangeBuild(bimbach);
        var result = await _subject.ParseAsync("Bimbach", _caller.Token);
        result.Datasets.Should().Equal(datasetBimbach);
        result.StationFailures.Should().BeEmpty();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_SeveralMatches_AllInPageOrder()
    {
        var bimbach = Online("bimbach", "Bimbach");
        var schlitz = Online("schlitz", "Schlitz");
        var burghaun = Online("burghaun", "Burghaun");
        var page = EmptyPage();
        Arrange(page, bimbach, schlitz, burghaun);
        var datasetBimbach = ArrangeBuild(bimbach);
        var datasetBurghaun = ArrangeBuild(burghaun);
        var result = await _subject.ParseAsync("B*", _caller.Token);
        result.Datasets.Should().Equal(datasetBimbach, datasetBurghaun);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_NoMatch_ThrowsPatternNoMatch()
    {
        var bimbach = Online("bimbach", "Bimbach");
        var page = EmptyPage();
        Arrange(page, bimbach);
        Func<Task> act = () => _subject.ParseAsync("Fulda", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.ErrorId.Should().Be(OsthessenWetterException.PatternNoMatchErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_OfflineStation_Failure()
    {
        var bimbach = Online("bimbach", "Bimbach");
        var langenbieber = Offline("langenbieber", "Langenbieber");
        var page = EmptyPage();
        Arrange(page, bimbach, langenbieber);
        var datasetBimbach = ArrangeBuild(bimbach);
        var result = await _subject.ParseAsync("*", _caller.Token);
        result.Datasets.Should().Equal(datasetBimbach);
        var failure = result.StationFailures.Should().ContainSingle().Subject;
        failure.StationName.Should().Be("Langenbieber");
        failure.ErrorId.Should().Be(OsthessenWetterNoticeIds.StationOffline);
        failure.Reason.Should().NotBeEmpty();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_MetadataMissing_Failure()
    {
        var bimbach = Online("bimbach", "Bimbach");
        var schlitz = NoMetadata("schlitz", "Schlitz");
        var page = EmptyPage();
        Arrange(page, bimbach, schlitz);
        var datasetBimbach = ArrangeBuild(bimbach);
        var result = await _subject.ParseAsync("*", _caller.Token);
        result.Datasets.Should().Equal(datasetBimbach);
        var failure = result.StationFailures.Should().ContainSingle().Subject;
        failure.StationName.Should().Be("Schlitz");
        failure.ErrorId.Should().Be(OsthessenWetterNoticeIds.StationMetadataMissing);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_NoUsableReading_FailureNeverDataset()
    {
        var bimbach = Online("bimbach", "Bimbach");
        var schlitz = Online("schlitz", "Schlitz");
        var page = EmptyPage();
        Arrange(page, bimbach, schlitz);
        var datasetBimbach = ArrangeBuild(bimbach);
        ArrangeRejected(schlitz);
        var result = await _subject.ParseAsync("*", _caller.Token);
        result.Datasets.Should().Equal(datasetBimbach);
        var failure = result.StationFailures.Should().ContainSingle().Subject;
        failure.StationName.Should().Be("Schlitz");
        failure.ErrorId.Should().Be(OsthessenWetterNoticeIds.StationNoReading);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_MatchesButNoDataset_ThrowsNoUsableStationWithFailures()
    {
        var langenbieber = Offline("langenbieber", "Langenbieber");
        var schlitz = NoMetadata("schlitz", "Schlitz");
        var page = EmptyPage();
        Arrange(page, langenbieber, schlitz);
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        var ex = await act.Should().ThrowAsync<OsthessenWetterException>();
        ex.Which.ErrorId.Should().Be(OsthessenWetterException.NoUsableStationErrorId);
        ex.Which.StationFailures.Select(f => (f.StationName, f.ErrorId)).Should().Equal(("Langenbieber", OsthessenWetterNoticeIds.StationOffline), ("Schlitz", OsthessenWetterNoticeIds.StationMetadataMissing));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_Collision_ThrowsIdCollision()
    {
        var page = EmptyPage();
        ArrangeSource(page);
        var collision = new OsthessenWetterException(OsthessenWetterException.StationIdCollisionErrorId, "collision");
        _mapper.Setup(m => m.Map(page, Retrieved)).Throws(collision);
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.Should().BeSameAs(collision);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_NeverFirstMatchOnly()
    {
        var bimbach = Online("bimbach", "Bimbach");
        var schlitz = Online("schlitz", "Schlitz");
        var burghaun = Online("burghaun", "Burghaun");
        var page = EmptyPage();
        Arrange(page, bimbach, schlitz, burghaun);
        var datasetBimbach = ArrangeBuild(bimbach);
        var datasetSchlitz = ArrangeBuild(schlitz);
        var datasetBurghaun = ArrangeBuild(burghaun);
        var result = await _subject.ParseAsync("*", _caller.Token);
        result.Datasets.Should().Equal(datasetBimbach, datasetSchlitz, datasetBurghaun);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_UnmatchedStations_NeitherBuiltNorFailures()
    {
        var bimbach = Online("bimbach", "Bimbach");
        var langenbieber = Offline("langenbieber", "Langenbieber");
        var schlitz = NoMetadata("schlitz", "Schlitz");
        var page = EmptyPage();
        Arrange(page, bimbach, langenbieber, schlitz);
        var datasetBimbach = ArrangeBuild(bimbach);
        var result = await _subject.ParseAsync("Bimbach", _caller.Token);
        result.Datasets.Should().Equal(datasetBimbach);
        result.StationFailures.Should().BeEmpty();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_Notices_LoggedWithIdUtcSourceStation()
    {
        var bimbach = Online("bimbach", "Bimbach", new OsthessenWetterMappingNotice(OsthessenWetterNoticeIds.ValueUnreadable, QuantityKind.Temperature), new OsthessenWetterMappingNotice(OsthessenWetterNoticeIds.TimeUnresolved, QuantityKind.MeanWindSpeed));
        var page = new OsthessenWetterPage(Array.Empty<OsthessenWetterStationRow>(), new[]
        {
            new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindTableMissing, null),
            new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Bimbach")
        });
        Arrange(page, bimbach);
        var datasetBimbach = ArrangeBuild(bimbach);
        await _subject.ParseAsync("*", _caller.Token);
        _logger.Count(OsthessenWetterNoticeIds.WindTableMissing).Should().Be(1);
        _logger.Count(OsthessenWetterNoticeIds.WindRowUnmatched).Should().Be(1);
        _logger.Count(OsthessenWetterNoticeIds.ValueUnreadable).Should().Be(1);
        _logger.Count(OsthessenWetterNoticeIds.TimeUnresolved).Should().Be(1);
        foreach (var entry in _logger.Entries)
        {
            entry.Level.Should().Be(LogLevel.Warning);
            entry.Message.Should().Contain("osthessenwetter");
            entry.Message.Should().Contain("2026-01-15T09:00:00");
        }
        _logger.Entries.Where(e => e.EventName == OsthessenWetterNoticeIds.ValueUnreadable).First().Message.Should().Contain("Temperature");
        _logger.Entries.Where(e => e.EventName == OsthessenWetterNoticeIds.TimeUnresolved).First().Message.Should().Contain("MeanWindSpeed");
        foreach (var id in new[] { OsthessenWetterNoticeIds.WindRowUnmatched, OsthessenWetterNoticeIds.ValueUnreadable, OsthessenWetterNoticeIds.TimeUnresolved })
        {
            _logger.Entries.Single(e => e.EventName == id).Message.Should().Contain("Bimbach");
        }
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_NoticesOfUnmatchedStations_NotLogged()
    {
        var bimbach = Online("bimbach", "Bimbach", new OsthessenWetterMappingNotice(OsthessenWetterNoticeIds.ValueUnreadable, QuantityKind.Temperature), new OsthessenWetterMappingNotice(OsthessenWetterNoticeIds.TimeUnresolved, QuantityKind.MeanWindSpeed));
        var schlitz = Online("schlitz", "Schlitz");
        var page = new OsthessenWetterPage(Array.Empty<OsthessenWetterStationRow>(), new[]
        {
            new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindTableMissing, null),
            new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Bimbach")
        });
        Arrange(page, bimbach, schlitz);
        var datasetSchlitz = ArrangeBuild(schlitz);
        await _subject.ParseAsync("Schlitz", _caller.Token);
        _logger.Count(OsthessenWetterNoticeIds.WindTableMissing).Should().Be(1);
        _logger.Count(OsthessenWetterNoticeIds.WindRowUnmatched).Should().Be(0);
        _logger.Count(OsthessenWetterNoticeIds.ValueUnreadable).Should().Be(0);
        _logger.Count(OsthessenWetterNoticeIds.TimeUnresolved).Should().Be(0);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_Omissions_LoggedWithKindAndReason()
    {
        var bimbach = Online("bimbach", "Bimbach");
        var page = EmptyPage();
        Arrange(page, bimbach);
        var datasetBimbach = ArrangeBuild(bimbach, new OmittedReading(QuantityKind.RelativeHumidity, OmissionReason.Missing));
        await _subject.ParseAsync("*", _caller.Token);
        _logger.Count("OBSERVATION-READING-OMITTED").Should().Be(1);
        _logger.Entries.Where(e => e.EventName == "OBSERVATION-READING-OMITTED").First().Message.Should().Contain("RelativeHumidity")
            .And.Contain("Missing")
            .And.Contain("Bimbach");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_OmissionsLoggedBeforeRejection()
    {
        var bimbach = Online("bimbach", "Bimbach");
        var schlitz = Online("schlitz", "Schlitz");
        var page = EmptyPage();
        Arrange(page, bimbach, schlitz);
        var datasetBimbach = ArrangeBuild(bimbach);
        ArrangeRejected(schlitz, new OmittedReading(QuantityKind.Temperature, OmissionReason.MissingTime));
        await _subject.ParseAsync("*", _caller.Token);
        _logger.Count("OBSERVATION-READING-OMITTED").Should().Be(1);
        _logger.Entries.Where(e => e.EventName == "OBSERVATION-READING-OMITTED").First().Message.Should().Contain("Schlitz")
            .And.Contain("MissingTime");
        IndexOf("OBSERVATION-READING-OMITTED").Should().BeLessThan(IndexOf(OsthessenWetterNoticeIds.StationNoReading));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_StationFailures_Logged()
    {
        var bimbach = Online("bimbach", "Bimbach");
        var langenbieber = Offline("langenbieber", "Langenbieber");
        var schlitz = NoMetadata("schlitz", "Schlitz");
        var page = EmptyPage();
        Arrange(page, bimbach, langenbieber, schlitz);
        var datasetBimbach = ArrangeBuild(bimbach);
        await _subject.ParseAsync("*", _caller.Token);
        _logger.Count(OsthessenWetterNoticeIds.StationOffline).Should().Be(1);
        _logger.Count(OsthessenWetterNoticeIds.StationMetadataMissing).Should().Be(1);
        _logger.Entries.Where(e => e.EventName == OsthessenWetterNoticeIds.StationOffline).First().Message.Should().Contain("Langenbieber")
            .And.Contain("osthessenwetter");
        _logger.Entries.Where(e => e.EventName == OsthessenWetterNoticeIds.StationMetadataMissing).First().Message.Should().Contain("Schlitz")
            .And.Contain("osthessenwetter");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_NoUsableStation_FailuresLoggedBeforeThrow()
    {
        var langenbieber = Offline("langenbieber", "Langenbieber");
        var schlitz = NoMetadata("schlitz", "Schlitz");
        var page = EmptyPage();
        Arrange(page, langenbieber, schlitz);
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        await act.Should().ThrowAsync<OsthessenWetterException>();
        _logger.Count(OsthessenWetterNoticeIds.StationOffline).Should().Be(1);
        _logger.Count(OsthessenWetterNoticeIds.StationMetadataMissing).Should().Be(1);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_NoMatch_PageNoticeStillLogged()
    {
        var page = new OsthessenWetterPage(Array.Empty<OsthessenWetterStationRow>(), new[]
        {
            new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindTableMissing, null),
            new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Bimbach")
        });
        Arrange(page, Online("bimbach", "Bimbach"));
        Func<Task> act = () => _subject.ParseAsync("Fulda", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.ErrorId.Should().Be(OsthessenWetterException.PatternNoMatchErrorId);
        _logger.Count(OsthessenWetterNoticeIds.WindTableMissing).Should().Be(1);
        _logger.Count(OsthessenWetterNoticeIds.WindRowUnmatched).Should().Be(0);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_Collision_PageNoticeStillLogged()
    {
        var page = new OsthessenWetterPage(Array.Empty<OsthessenWetterStationRow>(), new[]
        {
            new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindTableMissing, null),
            new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Bimbach")
        });
        ArrangeSource(page);
        var collision = new OsthessenWetterException(OsthessenWetterException.StationIdCollisionErrorId, "collision");
        _mapper.Setup(m => m.Map(page, Retrieved)).Throws(collision);
        Func<Task> act = () => _subject.ParseAsync("*", _caller.Token);
        (await act.Should().ThrowAsync<OsthessenWetterException>()).Which.Should().BeSameAs(collision);
        _logger.Count(OsthessenWetterNoticeIds.WindTableMissing).Should().Be(1);
    }
}
