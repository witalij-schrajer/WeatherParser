using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
public sealed class DwdObservationParserLoggingTests
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

    private void ArrangeMap(DwdMappingResult result) => _mapper.Setup(m => m.Map(It.IsAny<IReadOnlyList<CdcNowEvidence>>())).Returns(result);

    private void ArrangeBuild(params OmittedReading[] omitted) => _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Returns(new ObservationBuildResult(new ObservationDataset(), omitted));

    private static ObservationReading Missing(QuantityKind kind) => new() { Kind = kind, Value = null };

    private IEnumerable<(LogLevel Level, string? EventName, string Message)> Omissions() => _logger.Entries.Where(e => e.EventName == DwdObservationNoticeIds.ReadingOmitted);

    [TestMethod]
    public async Task ParseAsync_LogsOmittedReadingsWithKindAndReason()
    {
        ArrangeUntilArchives(Station("tu"), new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") });
        ArrangeMap(new DwdMappingResult(new[] { Temperature(12.3, Retrieved) }, Array.Empty<DwdMappingOmission>()));
        ArrangeBuild(new OmittedReading(QuantityKind.RelativeHumidity, OmissionReason.Missing));

        await _subject.ParseAsync("00044", _caller.Token);

        var om = Omissions().Should().ContainSingle();
        om.Which.Level.Should().Be(LogLevel.Warning);
        om.Which.Message.Should().Contain("RelativeHumidity").And.Contain("Missing");
    }

    [TestMethod]
    public async Task ParseAsync_RejectedDataset_LogsEveryOmissionBeforeThrowing()
    {
        var parser = RealBuilderParser(_resolver, _source, _mapper, _clock, _logger);
        ArrangeUntilArchives(Station("tu"), new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") });
        ArrangeMap(new DwdMappingResult(new[] { Missing(QuantityKind.Temperature), Missing(QuantityKind.RelativeHumidity) }, Array.Empty<DwdMappingOmission>()));

        Func<Task> act = () => parser.ParseAsync("00044", _caller.Token);
        await act.Should().ThrowAsync<ObservationDatasetRejectedException>();

        var om = Omissions();
        om.Should().HaveCount(2);
        om.Where(o => o.Message.Contains("Temperature")).Should().ContainSingle();
        om.Where(o => o.Message.Contains("RelativeHumidity")).Should().ContainSingle();
        om.All(o => o.Message.Contains("Missing")).Should().BeTrue();
    }

    [TestMethod]
    public async Task ParseAsync_MapperOmissions_LoggedOnSuccess()
    {
        ArrangeUntilArchives(Station("tu"), new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") });
        ArrangeMap(new DwdMappingResult(new[] { Temperature(12.3, Retrieved) }, new[] { new DwdMappingOmission(QuantityKind.WindDirection, DwdMappingOmissionReason.CalmWind) }));
        ArrangeBuild();

        await _subject.ParseAsync("00044", _caller.Token);

        var om = Omissions().Should().ContainSingle();
        om.Which.Message.Should().Contain("WindDirection").And.Contain("CalmWind");
    }

    [TestMethod]
    public async Task ParseAsync_MapperOmissions_LoggedBeforeRejection()
    {
        var parser = RealBuilderParser(_resolver, _source, _mapper, _clock, _logger);
        ArrangeUntilArchives(Station("tu"), new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") });
        ArrangeMap(new DwdMappingResult(new[] { Missing(QuantityKind.RelativeHumidity) }, new[] { new DwdMappingOmission(QuantityKind.WindDirection, DwdMappingOmissionReason.ZeroDirection) }));

        Func<Task> act = () => parser.ParseAsync("00044", _caller.Token);
        await act.Should().ThrowAsync<ObservationDatasetRejectedException>();

        var om = Omissions();
        om.Should().HaveCount(2);
        om.Where(o => o.Message.Contains("WindDirection")).Should().ContainSingle();
        om.Where(o => o.Message.Contains("RelativeHumidity")).Should().ContainSingle();
        om.Where(o => o.Message.Contains("ZeroDirection")).Should().ContainSingle();
        om.Where(o => o.Message.Contains("Missing")).Should().ContainSingle();
    }

    [TestMethod]
    public async Task ParseAsync_LogsListDiagnosticsNotYetLogged()
    {
        var lists = new[] { new CdcProductList("tu", "AirTemperatureStationList", new CdcStationCatalog(Array.Empty<CdcStationRecord>(), new[] { new CatalogDiagnostic(CatalogDiagnostic.RecordSkippedId, "AirTemperatureStationList", 7, "00091", "Bad record."), new CatalogDiagnostic(CatalogDiagnostic.RecordSkippedId, "AirTemperatureStationList", 8, "00092", "Logged.") { AlreadyLogged = true } })) };
        var station = Station("tu");
        _resolver.Setup(r => r.ValidateId("00044")).Returns("00044");
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(lists);
        _resolver.Setup(r => r.Resolve("00044", lists)).Returns(station);
        _source.Setup(s => s.ReadArchivesAsync("00044", station.ProductCodes, It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") });
        ArrangeMap(new DwdMappingResult(new[] { Temperature(12.3, Retrieved) }, Array.Empty<DwdMappingOmission>()));
        ArrangeBuild();

        await _subject.ParseAsync("00044", _caller.Token);

        _logger.Count(CatalogDiagnostic.RecordSkippedId).Should().Be(1);
    }

    [TestMethod]
    public async Task ParseAsync_ConflictingStation_LogsConflictDiagnosticBeforeFailure()
    {
        _resolver.Setup(r => r.ValidateId("00044")).Returns("00044");
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(Lists);
        _resolver.Setup(r => r.Resolve("00044", Lists)).Throws(new DwdStationException(DwdStationException.StationMetadataConflictErrorId, "Conflict.", new CatalogDiagnostic(CatalogDiagnostic.StationConflictId, "AirTemperatureStationList", 3, "00044", "Lists 'AirTemperatureStationList' and 'WindStationList' differ.")));

        Func<Task> act = () => _subject.ParseAsync("00044", _caller.Token);
        await act.Should().ThrowAsync<DwdStationException>().Where(ex => ex.ErrorId == DwdStationException.StationMetadataConflictErrorId);

        _logger.Count(CatalogDiagnostic.StationConflictId).Should().Be(1);
        _logger.Entries.Single(e => e.EventName == CatalogDiagnostic.StationConflictId).Message.Should().Contain("WindStationList");
    }

    [TestMethod]
    public async Task ParseAsync_LogEntriesCarryStationIdAndUtc()
    {
        ArrangeUntilArchives(Station("tu"), new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") });
        ArrangeMap(new DwdMappingResult(new[] { Temperature(12.3, Retrieved) }, new[] { new DwdMappingOmission(QuantityKind.WindDirection, DwdMappingOmissionReason.CalmWind) }));
        ArrangeBuild();

        await _subject.ParseAsync("00044", _caller.Token);

        var om = Omissions().Should().ContainSingle();
        om.Which.Message.Should().Contain("00044").And.Contain("2026-10-24T12:20:30");
    }

    [TestMethod]
    public async Task ParseAsync_SuccessLogsNoValues()
    {
        ArrangeUntilArchives(Station("tu"), new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") });
        ArrangeMap(new DwdMappingResult(new[] { Temperature(12.3, Retrieved) }, Array.Empty<DwdMappingOmission>()));
        ArrangeBuild(new OmittedReading(QuantityKind.RelativeHumidity, OmissionReason.Missing));

        await _subject.ParseAsync("00044", _caller.Token);

        _logger.Entries.Should().NotContain(e => e.Message.Contains("12.3"));
    }
}
