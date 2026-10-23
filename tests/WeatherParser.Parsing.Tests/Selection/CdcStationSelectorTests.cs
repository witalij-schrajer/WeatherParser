using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Selection;

namespace WeatherParser.Parsing.Tests.Selection;

[TestClass]
public sealed class CdcStationSelectorTests
{
    private MockRepository _mocks = null!;
    private Mock<ICdcCatalogSource> _source = null!;
    private Mock<ICdcAvailabilityDeriver> _deriver = null!;
    private Mock<ISearchAreaFilter> _filter = null!;
    private Mock<IListEntryOrdering> _ordering = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<CdcStationSelector> _logger = null!;
    private CdcCatalogEndpoints _options = null!;
    private CdcStationSelector _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _source = _mocks.Create<ICdcCatalogSource>();
        _deriver = _mocks.Create<ICdcAvailabilityDeriver>();
        _filter = _mocks.Create<ISearchAreaFilter>();
        _ordering = _mocks.Create<IListEntryOrdering>();
        _clock = new ManualTimeProvider(CdcSelectionTestData.Reference);
        _logger = new CapturingLogger<CdcStationSelector>();
        _options = new CdcCatalogEndpoints { MaxEvidenceCandidates = 3, SelectionDeadline = TimeSpan.FromSeconds(120) };
        _subject = CdcSelectionTestData.Create(_options, _source.Object, _deriver.Object, _filter.Object, _ordering.Object, _clock, _logger);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private IReadOnlyList<CdcProductList> SetupLists(params CatalogDiagnostic[] diagnostics)
    {
        var lists = CdcSelectionTestData.Lists(diagnostics);
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ReturnsAsync(lists);
        return lists;
    }

    private void SetupCandidates(IReadOnlyList<CdcProductList> lists, IReadOnlyList<CdcCandidateStation> stations, params CatalogDiagnostic[] diagnostics)
    {
        _deriver.Setup(d => d.SelectCandidates(lists, CdcSelectionTestData.Reference)).Returns(new CdcCandidates(stations, diagnostics));
    }

    private void SetupArea(CdcCandidateStation station, SearchAreaMatch match, decimal? distanceKm, decimal? differenceM)
    {
        _filter.Setup(f => f.Evaluate(It.IsAny<SearchParameters>(), station.Latitude, station.Longitude, station.AltitudeM)).Returns(new SearchAreaEvaluation(match, distanceKm, differenceM));
    }

    private List<IReadOnlyCollection<CdcEvidenceKey>> SetupEvidence(IReadOnlyDictionary<CdcEvidenceKey, CdcNowEvidence?> evidence)
    {
        var captured = new List<IReadOnlyCollection<CdcEvidenceKey>>();
        _source.Setup(s => s.ReadEvidenceAsync(It.IsAny<IReadOnlyCollection<CdcEvidenceKey>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<CdcEvidenceKey>, CancellationToken, CancellationToken>((keys, _, _) => captured.Add(keys.ToList()))
            .ReturnsAsync(evidence);
        return captured;
    }

    private List<CdcCandidates> SetupDerive(IReadOnlyList<CdcStationAvailability> stations, params CatalogDiagnostic[] diagnostics)
    {
        var captured = new List<CdcCandidates>();
        _deriver.Setup(d => d.Derive(It.IsAny<CdcCandidates>(), It.IsAny<IReadOnlyDictionary<CdcEvidenceKey, CdcNowEvidence?>>(), CdcSelectionTestData.Reference))
            .Callback<CdcCandidates, IReadOnlyDictionary<CdcEvidenceKey, CdcNowEvidence?>, DateTimeOffset>((candidates, _, _) => captured.Add(candidates))
            .Returns(new CdcAvailabilityResult(stations, diagnostics));
        return captured;
    }

    private void SetupOrderingUnchanged()
    {
        _ordering.Setup(o => o.Order(It.IsAny<IEnumerable<CdcStationListEntry>>())).Returns((IEnumerable<CdcStationListEntry> entries) => entries.ToList());
    }

    private static Dictionary<CdcEvidenceKey, CdcNowEvidence?> NoEvidence(params CdcEvidenceKey[] keys) => keys.ToDictionary(key => key, _ => (CdcNowEvidence?)null);

    private static readonly CdcCandidateStation A = CdcSelectionTestData.Station("00044", 120.5m, 50.1m, "ff", "tu");
    private static readonly CdcCandidateStation B = CdcSelectionTestData.Station("01234", 300m, 50.2m, "tu");
    private static readonly CdcCandidateStation C = CdcSelectionTestData.Station("05000", null, 50.3m, "rr");
    private static readonly CdcCandidateStation D = CdcSelectionTestData.Station("07777", 280m, 50.4m, "sd");

    [TestMethod]
    public async Task SelectAsync_InsideCandidates_BecomeEntriesWithAllFields()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, new[] { A, B, C }, CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupArea(A, SearchAreaMatch.Inside, 12.3456m, -179.5m);
        SetupArea(B, SearchAreaMatch.Outside, 80m, 0m);
        SetupArea(C, SearchAreaMatch.NotFilterable, null, null);
        var capturedEvidence = SetupEvidence(NoEvidence(new CdcEvidenceKey("00044", "ff"), new CdcEvidenceKey("00044", "tu")));
        var capturedDerive = SetupDerive(
            new[] { new CdcStationAvailability(A, new[] { "temperature_c", "wind_speed_kmh" }) },
            CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        capturedEvidence.Should().ContainSingle();
        capturedEvidence[0].Should().BeEquivalentTo(new[] { new CdcEvidenceKey("00044", "ff"), new CdcEvidenceKey("00044", "tu") });
        capturedDerive.Should().ContainSingle();
        capturedDerive[0].Stations.Should().Equal(new[] { A });
        capturedDerive[0].Diagnostics.Should().Equal(new[] { CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888") });
        result.Stations.Should().ContainSingle();
        var station = result.Stations[0];
        station.StationId.Should().Be("00044");
        station.Name.Should().Be("Name 00044");
        station.Latitude.Should().Be(50.1m);
        station.Longitude.Should().Be(9.25m);
        station.AltitudeM.Should().Be(120.5m);
        station.DistanceKm.Should().Be(12.3456m);
        station.AltitudeDifferenceM.Should().Be(-179.5m);
        station.AvailableMeasurements.Should().Equal("temperature_c", "wind_speed_kmh");
    }

    [TestMethod]
    public async Task SelectAsync_SetsMetadata()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, new[] { A }, CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupArea(A, SearchAreaMatch.Inside, 1m, 2m);
        var evidence = NoEvidence(new CdcEvidenceKey("00044", "ff"), new CdcEvidenceKey("00044", "tu"));
        _source.Setup(s => s.ReadEvidenceAsync(It.IsAny<IReadOnlyCollection<CdcEvidenceKey>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ReturnsAsync(evidence);
        SetupDerive(
            new[] { new CdcStationAvailability(A, new[] { "temperature_c" }) },
            CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupOrderingUnchanged();

        var input = CdcSelectionTestData.Parameters();
        // Act
        var result = await _subject.SelectAsync(input, CancellationToken.None);

        // Assert
        result.Source.Should().Be("dwd:cdc-10min-now");
        result.RetrievedAtUtc.Should().Be(new DateTime(2026, 10, 21, 12, 34, 56, DateTimeKind.Utc));
        result.RetrievedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        result.RetrievedAtWire.Should().Be("2026-10-21T12:34:56");
        result.SearchParameters.Should().NotBeSameAs(input);
        result.SearchParameters.Latitude.Should().Be(50.5m);
        result.SearchParameters.Longitude.Should().Be(9.5m);
        result.SearchParameters.AltitudeM.Should().Be(300m);
        result.SearchParameters.MaxDistanceKm.Should().Be(25m);
        result.SearchParameters.MaxAltitudeDifferenceM.Should().Be(200m);
    }

    [TestMethod]
    public async Task SelectAsync_OrdersThroughListEntryOrdering()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, new[] { A, D }, CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupArea(A, SearchAreaMatch.Inside, 5m, 1m);
        SetupArea(D, SearchAreaMatch.Inside, 6m, -20m);
        var evidence = NoEvidence(new CdcEvidenceKey("00044", "ff"), new CdcEvidenceKey("00044", "tu"), new CdcEvidenceKey("07777", "sd"));
        _source.Setup(s => s.ReadEvidenceAsync(It.IsAny<IReadOnlyCollection<CdcEvidenceKey>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ReturnsAsync(evidence);
        SetupDerive(
            new[] { 
                new CdcStationAvailability(A, new[] { "temperature_c" }),
                new CdcStationAvailability(D, new[] { "solar_irradiance_w_m2" })
            },
            CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        var orderingInput = new List<CdcStationListEntry>();
        _ordering.Setup(o => o.Order(It.IsAny<IEnumerable<CdcStationListEntry>>()))
            .Callback<IEnumerable<CdcStationListEntry>>(entries => orderingInput.AddRange(entries))
            .Returns((IEnumerable<CdcStationListEntry> entries) => entries.Reverse().ToList());

        // Act
        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        orderingInput.Select(e => e.StationId).Should().Equal("00044", "07777");
        result.Stations.Select(s => s.StationId).Should().Equal("07777", "00044");
        _ordering.Verify(o => o.Order(It.IsAny<IEnumerable<CdcStationListEntry>>()), Times.Once());
    }

    [TestMethod]
    public async Task SelectAsync_InactiveStations_NeverEntries()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, new[] { A, D }, CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupArea(A, SearchAreaMatch.Inside, 5m, 1m);
        SetupArea(D, SearchAreaMatch.Inside, 6m, -20m);
        var evidence = NoEvidence(new CdcEvidenceKey("00044", "ff"), new CdcEvidenceKey("00044", "tu"), new CdcEvidenceKey("07777", "sd"));
        _source.Setup(s => s.ReadEvidenceAsync(It.IsAny<IReadOnlyCollection<CdcEvidenceKey>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ReturnsAsync(evidence);
        SetupDerive(
            new[] { new CdcStationAvailability(A, new[] { "temperature_c" }) },
            CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        result.Stations.Should().ContainSingle();
        result.Stations[0].StationId.Should().Be("00044");
    }

    [TestMethod]
    public async Task SelectAsync_NotFilterableCandidates_ExcludedWithNoticeAsDecided()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, new[] { C }, CdcSelectionTestData.Diagnostic("CDC-STATION-NO-ALTITUDE", "05000"));
        SetupArea(C, SearchAreaMatch.NotFilterable, null, null);
        var capturedDerive = SetupDerive(
            Array.Empty<CdcStationAvailability>(),
            CdcSelectionTestData.Diagnostic("CDC-STATION-NO-ALTITUDE", "05000"));
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        result.Stations.Should().BeEmpty();
        capturedDerive[0].Stations.Should().BeEmpty();
        capturedDerive[0].Diagnostics.Should().Equal(new[] { CdcSelectionTestData.Diagnostic("CDC-STATION-NO-ALTITUDE", "05000") });
        _logger.Count("CDC-STATION-NO-ALTITUDE").Should().Be(1);
        _logger.Entries.Single(entry => entry.EventName == "CDC-STATION-NO-ALTITUDE").Level.Should().Be(LogLevel.Warning);
        _logger.Count("SELECTION-EMPTY").Should().Be(1);
    }

    [TestMethod]
    public async Task SelectAsync_OutsideCandidates_Omitted()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, new[] { B }, CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupArea(B, SearchAreaMatch.Outside, 80m, 0m);
        var capturedDerive = SetupDerive(
            Array.Empty<CdcStationAvailability>(),
            CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        result.Stations.Should().BeEmpty();
        capturedDerive[0].Stations.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SelectAsync_LogsSourceDiagnosticsOnce()
    {
        // Arrange
        var skipped = new CatalogDiagnostic("CATALOG-RECORD-SKIPPED", "AirTemperatureStationList", 7, "00099", "Synthetic reason.");
        var logged = CdcSelectionTestData.Diagnostic("CDC-STATION-NO-ALTITUDE", "00100") with { AlreadyLogged = true };
        var lists = SetupLists(skipped, logged);
        SetupCandidates(lists, new[] { A }, CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupArea(A, SearchAreaMatch.Inside, 5m, 1m);
        var evidence = NoEvidence(new CdcEvidenceKey("00044", "ff"), new CdcEvidenceKey("00044", "tu"));
        _source.Setup(s => s.ReadEvidenceAsync(It.IsAny<IReadOnlyCollection<CdcEvidenceKey>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ReturnsAsync(evidence);
        SetupDerive(
            new[] { new CdcStationAvailability(A, new[] { "temperature_c" }) },
            CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        _logger.Count("CATALOG-RECORD-SKIPPED").Should().Be(1);
        var skippedEntry = _logger.Entries.Single(entry => entry.EventName == "CATALOG-RECORD-SKIPPED");
        skippedEntry.Level.Should().Be(LogLevel.Warning);
        skippedEntry.Message.Should().Contain("AirTemperatureStationList");
        skippedEntry.Message.Should().Contain("7");
        skippedEntry.Message.Should().Contain("00099");
        _logger.Count("CDC-STATION-NO-ALTITUDE").Should().Be(0);
        result.Stations.Should().ContainSingle();
    }

    [TestMethod]
    public async Task SelectAsync_LogsDerivationDiagnosticsOnce()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, new[] { A }, CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupArea(A, SearchAreaMatch.Inside, 5m, 1m);
        var evidence = NoEvidence(new CdcEvidenceKey("00044", "ff"), new CdcEvidenceKey("00044", "tu"));
        _source.Setup(s => s.ReadEvidenceAsync(It.IsAny<IReadOnlyCollection<CdcEvidenceKey>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ReturnsAsync(evidence);
        SetupDerive(
            new[] { new CdcStationAvailability(A, new[] { "temperature_c" }) },
            CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        _logger.Count("CDC-STATION-CONFLICT").Should().Be(1);
        var conflictEntry = _logger.Entries.Single(entry => entry.EventName == "CDC-STATION-CONFLICT");
        conflictEntry.Message.Should().Contain("WindStationList");
        conflictEntry.Message.Should().Contain("08888");
        result.Stations.Should().ContainSingle();
    }

    [TestMethod]
    public async Task SelectAsync_NoMatches_EmptyListAndOneNotice()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, Array.Empty<CdcCandidateStation>());
        SetupDerive(Array.Empty<CdcStationAvailability>());
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        result.Stations.Should().BeEmpty();
        result.Source.Should().Be("dwd:cdc-10min-now");
        _logger.Entries.Should().ContainSingle();
        _logger.Entries[0].EventName.Should().Be("SELECTION-EMPTY");
        _logger.Entries[0].Level.Should().Be(LogLevel.Information);
    }

    [TestMethod]
    public async Task SelectAsync_ListRetrievalFailure_PropagatesWithoutList()
    {
        // Arrange
        var failure = new CatalogRetrievalException(CatalogRetrievalException.RetrievalFailedErrorId, "WindStationList", "Synthetic failure.");
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);

        // Act
        Func<Task> act = () => _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<CatalogRetrievalException>()).Which.Should().BeSameAs(failure);
        _logger.Entries.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SelectAsync_EvidenceFormatFailure_PropagatesWithoutList()
    {
        // Arrange
        var failure = new CatalogFormatException(CatalogFormatException.CdcEvidenceInvalidErrorId, "Synthetic failure.");
        var lists = SetupLists();
        SetupCandidates(lists, new[] { A }, CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupArea(A, SearchAreaMatch.Inside, 5m, 1m);
        _source.Setup(s => s.ReadEvidenceAsync(It.IsAny<IReadOnlyCollection<CdcEvidenceKey>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);

        // Act
        Func<Task> act = () => _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<CatalogFormatException>()).Which.Should().BeSameAs(failure);
        _logger.Count("SELECTION-EMPTY").Should().Be(0);
    }

    [TestMethod]
    public async Task SelectAsync_CandidateFormatFailure_PropagatesWithoutList()
    {
        // Arrange
        var failure = new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "Synthetic failure.");
        var lists = SetupLists();
        _deriver.Setup(d => d.SelectCandidates(lists, CdcSelectionTestData.Reference)).Throws(failure);

        // Act
        Func<Task> act = () => _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<CatalogFormatException>()).Which.Should().BeSameAs(failure);
        _logger.Count("SELECTION-EMPTY").Should().Be(0);
    }

    [TestMethod]
    public async Task SelectAsync_ResultSerializesWithAllRequiredMembers()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, new[] { A }, CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupArea(A, SearchAreaMatch.Inside, 5m, 1m);
        var evidence = NoEvidence(new CdcEvidenceKey("00044", "ff"), new CdcEvidenceKey("00044", "tu"));
        _source.Setup(s => s.ReadEvidenceAsync(It.IsAny<IReadOnlyCollection<CdcEvidenceKey>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ReturnsAsync(evidence);
        SetupDerive(
            new[] { new CdcStationAvailability(A, new[] { "temperature_c" }) },
            CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupOrderingUnchanged();

        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);
        using var stream = new MemoryStream();

        // Act
        await new JsonExportSerializer().SerializeAsync(result, stream, CancellationToken.None);

        // Assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        json.Should().Contain("\"station_id\":\"00044\"");
        json.Should().Contain("\"retrieved_at\":\"2026-10-21T12:34:56\"");
        json.Should().Contain("\"source\":\"dwd:cdc-10min-now\"");
    }

    [TestMethod]
    public async Task SelectAsync_ReferenceInstantReadOnce()
    {
        // Arrange
        var lists = CdcSelectionTestData.Lists();
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(30)))
            .ReturnsAsync(lists);
        SetupCandidates(lists, new[] { A }, CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupArea(A, SearchAreaMatch.Inside, 5m, 1m);
        var evidence = NoEvidence(new CdcEvidenceKey("00044", "ff"), new CdcEvidenceKey("00044", "tu"));
        _source.Setup(s => s.ReadEvidenceAsync(It.IsAny<IReadOnlyCollection<CdcEvidenceKey>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ReturnsAsync(evidence);
        SetupDerive(
            new[] { new CdcStationAvailability(A, new[] { "temperature_c" }) },
            CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        _clock.GetUtcNowCalls.Should().Be(1);
        result.RetrievedAtUtc.Should().Be(new DateTime(2026, 10, 21, 12, 34, 56, DateTimeKind.Utc));
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesCdcStationSelector()
    {
        // Arrange
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        // Act & Assert
        provider.GetRequiredService<ICdcStationSelector>().Should().BeOfType<CdcStationSelector>();
    }
}
