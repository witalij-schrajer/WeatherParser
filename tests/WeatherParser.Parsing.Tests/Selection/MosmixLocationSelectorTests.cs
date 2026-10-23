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
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Selection;

[TestClass]
public sealed class MosmixLocationSelectorTests
{
    private MockRepository _mocks = null!;
    private Mock<IMosmixCatalogSource> _source = null!;
    private Mock<IMosmixEvidenceCache> _cache = null!;
    private Mock<IMosmixAvailabilityResolver> _resolver = null!;
    private Mock<ISearchAreaFilter> _filter = null!;
    private Mock<IListEntryOrdering> _ordering = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<MosmixLocationSelector> _logger = null!;
    private MosmixCatalogEndpoints _options = null!;
    private MosmixLocationSelector _subject = null!;

    private static readonly MosmixLocationRecord L1 = MosmixSelectionTestData.Record("X123A", 250m, 50.1m);
    private static readonly MosmixLocationRecord L2 = MosmixSelectionTestData.Record("01234", 400m, 50.2m);
    private static readonly MosmixLocationRecord L3 = MosmixSelectionTestData.Record("B456", 300m, 50.3m);
    private static readonly MosmixLocationRecord L4 = MosmixSelectionTestData.Record("K999", 100m, 50.4m);
    private static readonly MosmixLocationRecord L5 = MosmixSelectionTestData.Record("N000", null, 50.5m);

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _source = _mocks.Create<IMosmixCatalogSource>();
        _cache = _mocks.Create<IMosmixEvidenceCache>();
        _resolver = _mocks.Create<IMosmixAvailabilityResolver>();
        _filter = _mocks.Create<ISearchAreaFilter>();
        _ordering = _mocks.Create<IListEntryOrdering>();
        _clock = new ManualTimeProvider(MosmixSelectionTestData.Reference);
        _logger = new CapturingLogger<MosmixLocationSelector>();
        _options = MosmixSelectionTestData.Options();
        _subject = MosmixSelectionTestData.Create(_options, _source.Object, _cache.Object, _resolver.Object, _filter.Object, _ordering.Object, _clock, _logger);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private MosmixSEvidenceIndex SetupIndex(MosmixSEvidenceIndex? index = null)
    {
        var value = index ?? MosmixSelectionTestData.FreshIndex();
        _cache.Setup(c => c.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(value);
        return value;
    }

    private void SetupCatalog(IReadOnlyList<MosmixLocationRecord> records, params CatalogDiagnostic[] diagnostics)
    {
        _source.Setup(s => s.ReadCatalogAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ReturnsAsync(new MosmixLocationCatalog(records, diagnostics));
    }

    private void SetupArea(MosmixLocationRecord record, SearchAreaMatch match, decimal? distanceKm, decimal? differenceM)
    {
        _filter.Setup(f => f.Evaluate(It.IsAny<SearchParameters>(), record.Latitude, record.Longitude, record.AltitudeM)).Returns(new SearchAreaEvaluation(match, distanceKm, differenceM));
    }

    private List<IReadOnlyCollection<string>> SetupMosmixL(IReadOnlyDictionary<string, MosmixLRetrieval?> retrievals)
    {
        var captured = new List<IReadOnlyCollection<string>>();
        _source.Setup(s => s.ReadMosmixLEvidenceAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<string>, CancellationToken, CancellationToken>((keys, _, _) => captured.Add(keys.ToList()))
            .ReturnsAsync(retrievals);
        return captured;
    }

    private List<MosmixProductAvailability> SetupResolve(string id, MosmixSEvidenceIndex index, MosmixLRetrieval? retrieval)
    {
        var items = MosmixSelectionTestData.Items(id);
        _resolver.Setup(r => r.Resolve(id, index, retrieval)).Returns(items);
        return items;
    }

    private void SetupOrderingUnchanged()
    {
        _ordering.Setup(o => o.Order(It.IsAny<IEnumerable<MosmixLocationListEntry>>())).Returns((IEnumerable<MosmixLocationListEntry> entries) => entries.ToList());
    }

    private static MosmixLRetrieval Retrieval(string id) => new(new DateTime(2026, 10, 21, 12, 35, 1, DateTimeKind.Utc), new MosmixLEvidence(id, new DateTime(2026, 10, 21, 9, 0, 0, DateTimeKind.Utc), new[] { "TTT" }));

    [TestMethod]
    public async Task SelectAsync_InsideLocations_BecomeEntriesWithAllFieldsAndResolvedProducts()
    {
        // Arrange
        var index = SetupIndex();
        SetupCatalog(new[] { L1, L2, L3, L4, L5 });
        SetupArea(L1, SearchAreaMatch.Inside, 12.5m, -50m);
        SetupArea(L2, SearchAreaMatch.Inside, 20.25m, 100m);
        SetupArea(L3, SearchAreaMatch.Inside, 0m, 0m);
        SetupArea(L4, SearchAreaMatch.Outside, 90m, 200m);
        var rX = Retrieval("X123A");
        var rB = Retrieval("B456");
        var capturedMosmixL = SetupMosmixL(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = rX, ["01234"] = null, ["B456"] = rB });
        var iX = SetupResolve("X123A", index, rX);
        var i0 = SetupResolve("01234", index, null);
        var iB = SetupResolve("B456", index, rB);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        capturedMosmixL.Should().ContainSingle();
        capturedMosmixL[0].Should().BeEquivalentTo(new[] { "X123A", "01234", "B456" });
        result.Locations.Should().HaveCount(3);
        result.Locations[0].MosmixId.Should().Be("X123A");
        result.Locations[0].Name.Should().Be("Name X123A");
        result.Locations[0].Latitude.Should().Be(50.1m);
        result.Locations[0].Longitude.Should().Be(9.25m);
        result.Locations[0].AltitudeM.Should().Be(250m);
        result.Locations[0].DistanceKm.Should().Be(12.5m);
        result.Locations[0].AltitudeDifferenceM.Should().Be(-50m);
        result.Locations[0].Products.Should().Equal(iX);
        result.Locations[1].MosmixId.Should().Be("01234");
        result.Locations[1].Name.Should().Be("Name 01234");
        result.Locations[1].Latitude.Should().Be(50.2m);
        result.Locations[1].Longitude.Should().Be(9.25m);
        result.Locations[1].AltitudeM.Should().Be(400m);
        result.Locations[1].DistanceKm.Should().Be(20.25m);
        result.Locations[1].AltitudeDifferenceM.Should().Be(100m);
        result.Locations[1].Products.Should().Equal(i0);
        result.Locations[2].MosmixId.Should().Be("B456");
        result.Locations[2].Name.Should().Be("Name B456");
        result.Locations[2].Latitude.Should().Be(50.3m);
        result.Locations[2].Longitude.Should().Be(9.25m);
        result.Locations[2].AltitudeM.Should().Be(300m);
        result.Locations[2].DistanceKm.Should().Be(0m);
        result.Locations[2].AltitudeDifferenceM.Should().Be(0m);
        result.Locations[2].Products.Should().Equal(iB);
    }

    [TestMethod]
    public async Task SelectAsync_SetsMetadata()
    {
        // Arrange
        var index = SetupIndex();
        SetupCatalog(new[] { L1 });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 2m);
        var capturedMosmixL = SetupMosmixL(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null });
        SetupResolve("X123A", index, null);
        SetupOrderingUnchanged();

        var input = MosmixSelectionTestData.Parameters();
        // Act
        var result = await _subject.SelectAsync(input, CancellationToken.None);

        // Assert
        result.Source.Should().Be("dwd:mosmix-stationskatalog");
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
    public async Task SelectAsync_MissingIndex_FailsBeforeFurtherRequests()
    {
        // Arrange
        _cache.Setup(c => c.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync((MosmixSEvidenceIndex?)null);

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<EvidenceCacheException>()).Which.ErrorId.Should().Be(EvidenceCacheException.EvidenceMissingErrorId);
    }

    [TestMethod]
    public async Task SelectAsync_StaleIndex_NoticeOnceAndIndexUsed()
    {
        // Arrange
        var index = SetupIndex(MosmixSelectionTestData.Index(new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc)));
        SetupCatalog(new[] { L1 });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 2m);
        var capturedMosmixL = SetupMosmixL(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null });
        SetupResolve("X123A", index, null);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        result.Locations.Should().ContainSingle();
        _logger.Count("MOSMIX-S-EVIDENCE-STALE").Should().Be(1);
        var staleEntry = _logger.Entries.Single(entry => entry.EventName == "MOSMIX-S-EVIDENCE-STALE");
        staleEntry.Level.Should().Be(LogLevel.Warning);
        staleEntry.Message.Should().Contain("12:34:56");
        staleEntry.Message.Should().Contain("06:00:00");
    }

    [TestMethod]
    public async Task SelectAsync_FreshIndex_NoStaleNotice()
    {
        // Arrange
        var index = SetupIndex();
        SetupCatalog(new[] { L1 });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 2m);
        var capturedMosmixL = SetupMosmixL(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null });
        SetupResolve("X123A", index, null);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        _logger.Count("MOSMIX-S-EVIDENCE-STALE").Should().Be(0);
    }

    [TestMethod]
    public async Task SelectAsync_MosmixL404_ResolverReceivesNoEvidence()
    {
        // Arrange
        var index = SetupIndex();
        SetupCatalog(new[] { L1 });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 2m);
        var capturedMosmixL = SetupMosmixL(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null });
        var items = SetupResolve("X123A", index, null);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        result.Locations[0].Products.Should().Equal(items);
    }

    [TestMethod]
    public async Task SelectAsync_LogsSourceDiagnosticsOnce_ExceptAlreadyLogged()
    {
        // Arrange
        var skipped = new CatalogDiagnostic("CATALOG-RECORD-SKIPPED", "CatalogUri", 9, "Z999", "Synthetic reason.");
        var noAltitude = new CatalogDiagnostic("MOSMIX-LOCATION-NO-ALTITUDE", "CatalogUri", 8, "N000", "Synthetic reason.") { AlreadyLogged = true };
        SetupCatalog(new[] { L1, L5 }, skipped, noAltitude);
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 2m);
        var capturedMosmixL = SetupMosmixL(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null });
        var index = SetupIndex();
        SetupResolve("X123A", index, null);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        _logger.Count("CATALOG-RECORD-SKIPPED").Should().Be(1);
        var skippedEntry = _logger.Entries.Single(entry => entry.EventName == "CATALOG-RECORD-SKIPPED");
        skippedEntry.Level.Should().Be(LogLevel.Warning);
        skippedEntry.Message.Should().Contain("CatalogUri");
        skippedEntry.Message.Should().Contain("9");
        skippedEntry.Message.Should().Contain("Z999");
        _logger.Count("MOSMIX-LOCATION-NO-ALTITUDE").Should().Be(0);
    }

    [TestMethod]
    public async Task SelectAsync_AllLocationsNotFilterable_ReturnsEmptyListWithOneEmptyResultNotice()
    {
        // Arrange
        SetupIndex();
        var noAltitude = new CatalogDiagnostic("MOSMIX-LOCATION-NO-ALTITUDE", "CatalogUri", 8, "N000", "Synthetic reason.") { AlreadyLogged = true };
        SetupCatalog(new[] { L5 }, noAltitude);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        result.Locations.Should().BeEmpty();
        _logger.Entries.Should().ContainSingle();
        _logger.Entries[0].EventName.Should().Be("SELECTION-EMPTY");
        _logger.Entries[0].Level.Should().Be(LogLevel.Information);
    }

    [TestMethod]
    public async Task SelectAsync_NotFilterableLocations_Excluded()
    {
        // Arrange
        SetupCatalog(new[] { L1, L3 });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 1m);
        SetupArea(L3, SearchAreaMatch.NotFilterable, null, null);
        var capturedMosmixL = SetupMosmixL(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null });
        var index = SetupIndex();
        SetupResolve("X123A", index, null);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        capturedMosmixL.Should().ContainSingle();
        capturedMosmixL[0].Should().BeEquivalentTo(new[] { "X123A" });
        result.Locations.Should().ContainSingle();
        result.Locations[0].MosmixId.Should().Be("X123A");
    }

    [TestMethod]
    public async Task SelectAsync_OutsideLocations_Omitted()
    {
        // Arrange
        SetupIndex();
        SetupCatalog(new[] { L4 });
        SetupArea(L4, SearchAreaMatch.Outside, 90m, 200m);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        result.Locations.Should().BeEmpty();
        _logger.Count("SELECTION-EMPTY").Should().Be(1);
    }

    [TestMethod]
    public async Task SelectAsync_OrdersThroughListEntryOrdering()
    {
        // Arrange
        SetupCatalog(new[] { L1, L2 });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 1m);
        SetupArea(L2, SearchAreaMatch.Inside, 2m, 2m);
        var capturedMosmixL = SetupMosmixL(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null, ["01234"] = null });
        var index = SetupIndex();
        SetupResolve("X123A", index, null);
        SetupResolve("01234", index, null);
        var orderingInput = new List<MosmixLocationListEntry>();
        _ordering.Setup(o => o.Order(It.IsAny<IEnumerable<MosmixLocationListEntry>>()))
            .Callback<IEnumerable<MosmixLocationListEntry>>(entries => orderingInput.AddRange(entries))
            .Returns((IEnumerable<MosmixLocationListEntry> entries) => entries.Reverse().ToList());

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        orderingInput.Select(e => e.MosmixId).Should().Equal("X123A", "01234");
        result.Locations.Select(s => s.MosmixId).Should().Equal("01234", "X123A");
        _ordering.Verify(o => o.Order(It.IsAny<IEnumerable<MosmixLocationListEntry>>()), Times.Once());
    }

    [TestMethod]
    public async Task SelectAsync_CatalogFailure_PropagatesWithoutList()
    {
        // Arrange
        SetupIndex();
        var failure = new CatalogRetrievalException(CatalogRetrievalException.RetrievalFailedErrorId, "CatalogUri", "Synthetic failure.");
        _source.Setup(s => s.ReadCatalogAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<CatalogRetrievalException>()).Which.Should().BeSameAs(failure);
        _logger.Entries.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SelectAsync_MosmixLFailure_PropagatesWithoutList()
    {
        // Arrange
        var index = SetupIndex();
        SetupCatalog(new[] { L1 });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 2m);
        var failure = new CatalogFormatException(CatalogFormatException.MosmixEvidenceInvalidErrorId, "Synthetic failure.");
        _source.Setup(s => s.ReadMosmixLEvidenceAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<CatalogFormatException>()).Which.Should().BeSameAs(failure);
        _logger.Count("SELECTION-EMPTY").Should().Be(0);
    }

    [TestMethod]
    public async Task SelectAsync_IndexReadBeforeCatalogDownloadBeforeMosmixL()
    {
        // Arrange
        var calls = new List<string>();
        var index = MosmixSelectionTestData.FreshIndex();
        _cache.Setup(c => c.ReadAsync(It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("index"))
            .ReturnsAsync(index);
        _source.Setup(s => s.ReadCatalogAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("catalog"))
            .ReturnsAsync(new MosmixLocationCatalog(new[] { L1 }, Array.Empty<CatalogDiagnostic>()));
        _source.Setup(s => s.ReadMosmixLEvidenceAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("mosmixL"))
            .ReturnsAsync(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 2m);
        SetupResolve("X123A", index, null);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        calls.Should().Equal("index", "catalog", "mosmixL");
    }

    [TestMethod]
    public async Task SelectAsync_ReferenceInstantReadOnce()
    {
        // Arrange
        var index = SetupIndex();
        _source.Setup(s => s.ReadCatalogAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(30)))
            .ReturnsAsync(new MosmixLocationCatalog(new[] { L1 }, Array.Empty<CatalogDiagnostic>()));
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 2m);
        var capturedMosmixL = SetupMosmixL(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null });
        SetupResolve("X123A", index, null);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        _clock.GetUtcNowCalls.Should().Be(1);
        result.RetrievedAtUtc.Should().Be(new DateTime(2026, 10, 21, 12, 34, 56, DateTimeKind.Utc));
    }

    [TestMethod]
    public async Task SelectAsync_ResultSerializesWithAllRequiredMembers()
    {
        // Arrange
        var index = SetupIndex();
        SetupCatalog(new[] { L1 });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 2m);
        var capturedMosmixL = SetupMosmixL(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null });
        SetupResolve("X123A", index, null);
        SetupOrderingUnchanged();

        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);
        using var stream = new MemoryStream();

        // Act
        await new JsonExportSerializer().SerializeAsync(result, stream, CancellationToken.None);

        // Assert
        var json = Encoding.UTF8.GetString(stream.ToArray());
        json.Should().Contain("\"mosmix_id\":\"X123A\"");
        json.Should().Contain("\"source\":\"dwd:mosmix-stationskatalog\"");
        json.Should().Contain("\"retrieved_at\":\"2026-10-21T12:34:56\"");
    }

    [TestMethod]
    public void SelectAsync_NeverUsesCdcSource()
    {
        // Arrange
        var constructor = typeof(MosmixLocationSelector).GetConstructors();
        constructor.Should().ContainSingle();

        var parameters = constructor[0].GetParameters();
        parameters.Select(p => p.ParameterType).Should().NotContain(typeof(ICdcCatalogSource));
        parameters.Select(p => p.ParameterType).Should().NotContain(typeof(CdcCatalogEndpoints));
        parameters.Select(p => p.ParameterType).Should().NotContain(typeof(ICdcAvailabilityDeriver));
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesMosmixLocationSelector()
    {
        // Arrange
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        // Act & Assert
        provider.GetRequiredService<IMosmixLocationSelector>().Should().BeOfType<MosmixLocationSelector>();
    }
}
