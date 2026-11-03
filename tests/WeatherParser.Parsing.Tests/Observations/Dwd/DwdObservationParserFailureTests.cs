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
public sealed class DwdObservationParserFailureTests
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

    [TestMethod]
    public async Task ParseAsync_InvalidId_ThrowsBeforeAnySourceCall()
    {
        _resolver.Setup(r => r.ValidateId("A1234")).Throws(new DwdStationException(DwdStationException.StationIdInvalidErrorId, "Invalid."));
        
        Func<Task> act = () => _subject.ParseAsync("A1234", _caller.Token);
        await act.Should().ThrowExactlyAsync<DwdStationException>()
            .WithMessage("*Invalid.*")
            .Where(ex => ex.ErrorId == DwdStationException.StationIdInvalidErrorId);

        _logger.Entries.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ParseAsync_UnknownStation_ThrowsBeforeAnyArchiveRequest()
    {
        _resolver.Setup(r => r.ValidateId("00044")).Returns("00044");
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(Lists);
        _resolver.Setup(r => r.Resolve("00044", Lists)).Throws(new DwdStationException(DwdStationException.StationUnknownErrorId, "Unknown."));
        
        Func<Task> act = () => _subject.ParseAsync("00044", _caller.Token);
        await act.Should().ThrowExactlyAsync<DwdStationException>()
            .WithMessage("*Unknown.*")
            .Where(ex => ex.ErrorId == DwdStationException.StationUnknownErrorId);

        _source.Verify(s => s.ReadArchivesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ParseAsync_ConflictingStation_ThrowsMetadataConflictBeforeAnyArchiveRequest()
    {
        _resolver.Setup(r => r.ValidateId("00044")).Returns("00044");
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(Lists);
        _resolver.Setup(r => r.Resolve("00044", Lists)).Throws(new DwdStationException(DwdStationException.StationMetadataConflictErrorId, "Conflict.", new CatalogDiagnostic(CatalogDiagnostic.StationConflictId, "AirTemperatureStationList", 3, "00044", "Lists 'AirTemperatureStationList' and 'WindStationList' differ.")));
        
        var exception = await FluentActions.Awaiting(() => _subject.ParseAsync("00044", _caller.Token)).Should().ThrowAsync<DwdStationException>();
        exception.Which.ErrorId.Should().Be(DwdStationException.StationMetadataConflictErrorId);
        exception.Which.Diagnostic.Should().NotBeNull();
        exception.Which.Diagnostic!.Id.Should().Be(CatalogDiagnostic.StationConflictId);
        
        _source.Verify(s => s.ReadArchivesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ParseAsync_MissingProduct_LogsNoticeAndContinues()
    {
        ArrangeUntilArchives(Station("tu", "ff"), new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu"), ["ff"] = null });
        
        _mapper.Setup(m => m.Map(It.Is<IReadOnlyList<CdcNowEvidence>>(e => e.Count == 1 && e[0].ProductCode == "tu")))
            .Returns(new DwdMappingResult(new[] { Temperature(12.3, Retrieved) }, Array.Empty<DwdMappingOmission>()));
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Returns(new ObservationBuildResult(new ObservationDataset(), Array.Empty<OmittedReading>()));

        await _subject.ParseAsync("00044", _caller.Token);

        _logger.Count(DwdObservationNoticeIds.ProductMissing).Should().Be(1);
        var entry = _logger.Entries.Should().ContainSingle().Which;
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain("ff").And.Contain("00044");
    }

    [TestMethod]
    public async Task ParseAsync_AllProductsMissing_ThrowsObservationDatasetRejected()
    {
        ArrangeUntilArchives(Station("tu"), new Dictionary<string, CdcNowEvidence?> { ["tu"] = null });
        
        var parser = RealBuilderParser(_resolver, _source, _mapper, _clock, _logger);
        _mapper.Setup(m => m.Map(It.Is<IReadOnlyList<CdcNowEvidence>>(e => e.Count == 0)))
            .Returns(new DwdMappingResult(Array.Empty<ObservationReading>(), Array.Empty<DwdMappingOmission>()));
        
        Func<Task> act = () => parser.ParseAsync("00044", _caller.Token);
        await act.Should().ThrowExactlyAsync<ObservationDatasetRejectedException>();
    }

    [TestMethod]
    public async Task ParseAsync_RetrievalFailure_Propagates()
    {
        _resolver.Setup(r => r.ValidateId("00044")).Returns("00044");
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).Throws(new CatalogRetrievalException(CatalogRetrievalException.RetrievalFailedErrorId, "AirTemperatureStationList", "Failed."));
        
        Func<Task> act = () => _subject.ParseAsync("00044", _caller.Token);
        await act.Should().ThrowExactlyAsync<CatalogRetrievalException>()
            .WithMessage("*Failed.*")
            .Where(ex => ex.ErrorId == CatalogRetrievalException.RetrievalFailedErrorId);
    }

    [TestMethod]
    public async Task ParseAsync_FormatFailure_Propagates()
    {
        _resolver.Setup(r => r.ValidateId("00044")).Returns("00044");
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(Lists);
        _resolver.Setup(r => r.Resolve("00044", Lists)).Returns(Station("tu"));
        _source.Setup(s => s.ReadArchivesAsync("00044", new[] { "tu" }, It.IsAny<CancellationToken>(), _caller.Token)).Throws(new CatalogFormatException(CatalogFormatException.CdcEvidenceInvalidErrorId, "Bad archive."));
        
        Func<Task> act = () => _subject.ParseAsync("00044", _caller.Token);
        await act.Should().ThrowExactlyAsync<CatalogFormatException>()
            .WithMessage("*Bad archive.*")
            .Where(ex => ex.ErrorId == CatalogFormatException.CdcEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task ParseAsync_NoUsableReading_ThrowsObservationDatasetRejected()
    {
        ArrangeUntilArchives(Station("tu"), new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") });
        
        _mapper.Setup(m => m.Map(It.Is<IReadOnlyList<CdcNowEvidence>>(e => e.Count == 1 && e[0].ProductCode == "tu")))
            .Returns(new DwdMappingResult(Array.Empty<ObservationReading>(), Array.Empty<DwdMappingOmission>()));
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Throws(new ObservationDatasetRejectedException("No reading."));
        
        Func<Task> act = () => _subject.ParseAsync("00044", _caller.Token);
        await act.Should().ThrowExactlyAsync<ObservationDatasetRejectedException>()
            .WithMessage("*No reading.*");
    }

    [TestMethod]
    public async Task ParseAsync_NeverUsesAnotherStationsData()
    {
        ArrangeUntilArchives(Station("tu"), new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") });
        
        _mapper.Setup(m => m.Map(It.Is<IReadOnlyList<CdcNowEvidence>>(e => e.Count == 1 && e[0].ProductCode == "tu")))
            .Returns(new DwdMappingResult(Array.Empty<ObservationReading>(), Array.Empty<DwdMappingOmission>()));
        string capturedStationId = null!;
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>()))
            .Callback<ObservationInput>(input => capturedStationId = input.Location!.StationId)
            .Returns(new ObservationBuildResult(new ObservationDataset(), Array.Empty<OmittedReading>()));

        await _subject.ParseAsync("00044", _caller.Token);

        capturedStationId.Should().Be("00044");
    }
}
