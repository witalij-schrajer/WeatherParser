using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Selection;

namespace WeatherParser.Parsing.Tests.Selection;

[TestClass]
public sealed class MosmixLocationSelectorLimitTests
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
    private static readonly MosmixLocationRecord L6 = MosmixSelectionTestData.Record("Y777", 260m, 50.6m);

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

    private static decimal Dec(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static async Task ShouldBeSearchParameterInvalid(Func<Task> act)
    {
        (await act.Should().ThrowAsync<SearchParameterException>()).Which.ErrorId.Should().Be(SearchParameterException.SearchParameterInvalidErrorId);
    }

    private void ArrangeEmptySelection()
    {
        SetupIndex();
        SetupCatalog(new[] { L4 });
        SetupArea(L4, SearchAreaMatch.Outside, 90m, 200m);
        SetupOrderingUnchanged();
    }

    [TestMethod]
    public async Task SelectAsync_NullParameters_ThrowsSearchParameterInvalidWithoutRequests()
    {
        // Act
        Func<Task> act = () => _subject.SelectAsync(null!, CancellationToken.None);

        // Assert
        await ShouldBeSearchParameterInvalid(act);
        _clock.GetUtcNowCalls.Should().Be(0);
    }

    [DataRow("latitude")]
    [DataRow("longitude")]
    [DataRow("altitude_m")]
    [DataRow("max_distance_km")]
    [DataRow("max_altitude_difference_m")]
    [TestMethod]
    public async Task SelectAsync_UnassignedMember_ThrowsSearchParameterInvalidWithoutRequests(string missing)
    {
        // Arrange
        var parameters = new SearchParameters();
        if (missing != "latitude") { parameters.Latitude = 50.5m; }
        if (missing != "longitude") { parameters.Longitude = 9.5m; }
        if (missing != "altitude_m") { parameters.AltitudeM = 300m; }
        if (missing != "max_distance_km") { parameters.MaxDistanceKm = 25m; }
        if (missing != "max_altitude_difference_m") { parameters.MaxAltitudeDifferenceM = 200m; }

        // Act
        Func<Task> act = () => _subject.SelectAsync(parameters, CancellationToken.None);

        // Assert
        await ShouldBeSearchParameterInvalid(act);
        _clock.GetUtcNowCalls.Should().Be(0);
    }

    [DataRow("90.0001", "0", "0", "1", "1")]
    [DataRow("0", "-180.0001", "0", "1", "1")]
    [DataRow("0", "0", "0", "-0.0001", "1")]
    [DataRow("0", "0", "0", "1", "-0.0001")]
    [DataRow("0", "0", "-79228162514264337593543950335", "1", "1")]
    [TestMethod]
    public async Task SelectAsync_InvalidValues_ThrowsSearchParameterInvalidWithoutRequests(string latitude, string longitude, string altitude, string distance, string difference)
    {
        // Arrange
        var parameters = new SearchParameters()
        {
            Latitude = Dec(latitude),
            Longitude = Dec(longitude),
            AltitudeM = Dec(altitude),
            MaxDistanceKm = Dec(distance),
            MaxAltitudeDifferenceM = Dec(difference)
        };

        // Act
        Func<Task> act = () => _subject.SelectAsync(parameters, CancellationToken.None);

        // Assert
        await ShouldBeSearchParameterInvalid(act);
        _clock.GetUtcNowCalls.Should().Be(0);
    }

    [DataRow("-90", "180", "9000", "0", "0")]
    [DataRow("90", "-180", "-500", "0", "0")]
    [TestMethod]
    public async Task SelectAsync_BoundaryValues_AcceptedAndBecomeResultParameters(string latitude, string longitude, string altitude, string distance, string difference)
    {
        // Arrange
        ArrangeEmptySelection();
        var parameters = new SearchParameters()
        {
            Latitude = Dec(latitude),
            Longitude = Dec(longitude),
            AltitudeM = Dec(altitude),
            MaxDistanceKm = Dec(distance),
            MaxAltitudeDifferenceM = Dec(difference)
        };

        // Act
        var result = await _subject.SelectAsync(parameters, CancellationToken.None);

        // Assert
        result.SearchParameters.Latitude.Should().Be(Dec(latitude));
        result.SearchParameters.Longitude.Should().Be(Dec(longitude));
        result.SearchParameters.AltitudeM.Should().Be(Dec(altitude));
        result.SearchParameters.MaxDistanceKm.Should().Be(Dec(distance));
        result.SearchParameters.MaxAltitudeDifferenceM.Should().Be(Dec(difference));
    }

    [TestMethod]
    public async Task SelectAsync_SnapshotIndependence_CallerChangesDoNotAffectSelection()
    {
        // Arrange
        var input = MosmixSelectionTestData.Parameters();
        var index = MosmixSelectionTestData.FreshIndex();
        _cache.Setup(c => c.ReadAsync(It.IsAny<CancellationToken>()))
            .Callback(() => { input.Latitude = 1m; input.MaxDistanceKm = 999m; })
            .ReturnsAsync(index);
        SetupCatalog(new[] { L1 });
        _filter.Setup(f => f.Evaluate(It.Is<SearchParameters>(p => p.Latitude == 50.5m && p.MaxDistanceKm == 25m), L1.Latitude, L1.Longitude, L1.AltitudeM))
            .Returns(new SearchAreaEvaluation(SearchAreaMatch.Inside, 5m, 1m));
        var capturedMosmixL = SetupMosmixL(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null });
        SetupResolve("X123A", index, null);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(input, CancellationToken.None);

        // Assert
        result.SearchParameters.Latitude.Should().Be(50.5m);
        result.SearchParameters.MaxDistanceKm.Should().Be(25m);
        result.SearchParameters.Should().NotBeSameAs(input);
    }

    [TestMethod]
    public async Task SelectAsync_StaleIndexAtMaxAge_ReportsNoNotice()
    {
        // Arrange
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero));
        var subject = MosmixSelectionTestData.Create(_options, _source.Object, _cache.Object, _resolver.Object, _filter.Object, _ordering.Object, clock, _logger);
        SetupIndex(MosmixSelectionTestData.Index(new DateTime(2026, 10, 21, 6, 0, 0, DateTimeKind.Utc)));
        SetupCatalog(new[] { L4 });
        SetupArea(L4, SearchAreaMatch.Outside, 90m, 200m);
        SetupOrderingUnchanged();

        // Act
        var result = await subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        _logger.Count("MOSMIX-S-EVIDENCE-STALE").Should().Be(0);
    }

    [TestMethod]
    public async Task SelectAsync_StaleIndexJustBeyondMaxAge_ReportsNotice()
    {
        // Arrange
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero));
        var subject = MosmixSelectionTestData.Create(_options, _source.Object, _cache.Object, _resolver.Object, _filter.Object, _ordering.Object, clock, _logger);
        SetupIndex(MosmixSelectionTestData.Index(new DateTime(2026, 10, 21, 5, 59, 59, DateTimeKind.Utc)));
        SetupCatalog(new[] { L4 });
        SetupArea(L4, SearchAreaMatch.Outside, 90m, 200m);
        SetupOrderingUnchanged();

        // Act
        var result = await subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        _logger.Count("MOSMIX-S-EVIDENCE-STALE").Should().Be(1);
    }

    [TestMethod]
    public async Task SelectAsync_CandidateCap_EqualsLimit_Proceeds()
    {
        // Arrange
        var index = SetupIndex();
        SetupCatalog(new[] { L1, L2, L3, L4 });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 1m);
        SetupArea(L2, SearchAreaMatch.Inside, 2m, 2m);
        SetupArea(L3, SearchAreaMatch.Inside, 3m, 3m);
        SetupArea(L4, SearchAreaMatch.Outside, 90m, 200m);
        var capturedMosmixL = SetupMosmixL(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null, ["01234"] = null, ["B456"] = null });
        SetupResolve("X123A", index, null);
        SetupResolve("01234", index, null);
        SetupResolve("B456", index, null);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        result.Locations.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task SelectAsync_CandidateCap_ExceedsLimit_FailsBeforeAnyMosmixLRequest()
    {
        // Arrange
        var index = SetupIndex();
        SetupCatalog(new[] { L1, L2, L3, L6, L4 });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 1m);
        SetupArea(L2, SearchAreaMatch.Inside, 2m, 2m);
        SetupArea(L3, SearchAreaMatch.Inside, 3m, 3m);
        SetupArea(L6, SearchAreaMatch.Inside, 4m, 4m);
        SetupArea(L4, SearchAreaMatch.Outside, 90m, 200m);
        // No mosmixL setup - strict mock should not be called
        // No resolver setup - strict mock should not be called
        // No ordering setup - strict mock should not be called

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        var failure = (await act.Should().ThrowAsync<SelectionLimitException>()).Which;
        failure.ErrorId.Should().Be(SelectionLimitException.TooManyCandidatesErrorId);
        failure.Message.Should().Contain("4").And.Contain("3");
    }

    [TestMethod]
    public async Task SelectAsync_PassesCallerTokenAndSelectionDeadlineToSource()
    {
        // Arrange
        using var caller = new CancellationTokenSource();
        CancellationToken cacheToken = default;
        CancellationToken catalogDeadline = default;
        CancellationToken catalogToken = default;
        CancellationToken mosmixLDeadline = default;
        CancellationToken mosmixLToken = default;
        var index = MosmixSelectionTestData.FreshIndex();
        _cache.Setup(c => c.ReadAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => cacheToken = token)
            .ReturnsAsync(index);
        _source.Setup(s => s.ReadCatalogAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback<CancellationToken, CancellationToken>((deadline, token) =>
            {
                catalogDeadline = deadline;
                catalogToken = token;
            })
            .ReturnsAsync(new MosmixLocationCatalog(new[] { L1 }, Array.Empty<CatalogDiagnostic>()));
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 2m);
        _source.Setup(s => s.ReadMosmixLEvidenceAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<string>, CancellationToken, CancellationToken>((_, deadline, token) =>
            {
                mosmixLDeadline = deadline;
                mosmixLToken = token;
            })
            .ReturnsAsync(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null });
        SetupResolve("X123A", index, null);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), caller.Token);

        // Assert
        cacheToken.Should().NotBe(caller.Token);
        cacheToken.CanBeCanceled.Should().BeTrue();
        catalogToken.Should().Be(caller.Token);
        mosmixLToken.Should().Be(caller.Token);
        mosmixLDeadline.Should().Be(catalogDeadline);
        catalogDeadline.Should().NotBe(caller.Token);
        catalogDeadline.CanBeCanceled.Should().BeTrue();
        catalogDeadline.IsCancellationRequested.Should().BeFalse();
    }

    [TestMethod]
    public async Task SelectAsync_SelectionDeadlineOption_NotExceededJustBelowLimit()
    {
        // Arrange
        var index = SetupIndex();
        _source.Setup(s => s.ReadCatalogAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(119)))
            .ReturnsAsync(new MosmixLocationCatalog(new[] { L4 }, Array.Empty<CatalogDiagnostic>()));
        SetupArea(L4, SearchAreaMatch.Outside, 90m, 200m);
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        result.Locations.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SelectAsync_DeadlineExceededDuringIndexRead_FailsWithSelectionDeadlineExceeded()
    {
        // Arrange
        _cache.Setup(c => c.ReadAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(120)))
            .ReturnsAsync(MosmixSelectionTestData.FreshIndex());

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.ErrorId.Should().Be(SelectionLimitException.DeadlineExceededErrorId);
    }

    [TestMethod]
    public async Task SelectAsync_DeadlineExceededDuringCatalogDownload_FailsWithSelectionDeadlineExceeded()
    {
        // Arrange
        var index = SetupIndex();
        _source.Setup(s => s.ReadCatalogAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(120)))
            .ReturnsAsync(new MosmixLocationCatalog(new[] { L1 }, Array.Empty<CatalogDiagnostic>()));

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.ErrorId.Should().Be(SelectionLimitException.DeadlineExceededErrorId);
    }

    [TestMethod]
    public async Task SelectAsync_DeadlineExceededDuringMosmixLRequests_FailsWithSelectionDeadlineExceeded()
    {
        // Arrange
        var index = SetupIndex();
        SetupCatalog(new[] { L1 });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 2m);
        _source.Setup(s => s.ReadMosmixLEvidenceAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(120)))
            .ReturnsAsync(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null });
        // No resolver setup - strict mock should not be called
        // No ordering setup - strict mock should not be called

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.ErrorId.Should().Be(SelectionLimitException.DeadlineExceededErrorId);
    }

    [TestMethod]
    public async Task SelectAsync_OperationCanceledForDeadline_MapsToSelectionDeadlineExceeded()
    {
        // Arrange
        var index = SetupIndex();
        _source.Setup(s => s.ReadCatalogAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Returns((CancellationToken deadline, CancellationToken token) =>
            {
                _clock.Advance(TimeSpan.FromSeconds(120));
                return Task.FromException<MosmixLocationCatalog>(new OperationCanceledException(deadline));
            });

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.ErrorId.Should().Be(SelectionLimitException.DeadlineExceededErrorId);
    }

    [TestMethod]
    public async Task SelectAsync_SelectionDeadlineAndCancellation_ThrowsOperationCanceledExceptionWithCallerToken()
    {
        // Arrange
        using var caller = new CancellationTokenSource();
        var index = SetupIndex();
        _source.Setup(s => s.ReadCatalogAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                _clock.Advance(TimeSpan.FromSeconds(120));
                caller.Cancel();
            })
            .ReturnsAsync(new MosmixLocationCatalog(new[] { L1 }, Array.Empty<CatalogDiagnostic>()));

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), caller.Token);

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should().Be(caller.Token);
    }

    [TestMethod]
    public async Task SelectAsync_SourceDeadlineFailure_PropagatesUnchanged()
    {
        // Arrange
        var index = SetupIndex();
        SetupCatalog(new[] { L1 });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 2m);
        var failure = new SelectionLimitException(SelectionLimitException.DeadlineExceededErrorId, "Synthetic deadline.");
        _source.Setup(s => s.ReadMosmixLEvidenceAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.Should().BeSameAs(failure);
    }

    [TestMethod]
    public async Task SelectAsync_DeadlineExpiresDuringFiltering_FailsWithSelectionDeadlineExceededWithoutNotice()
    {
        // Arrange
        var index = SetupIndex();
        SetupCatalog(new[] { L4 });
        _filter.Setup(f => f.Evaluate(It.IsAny<SearchParameters>(), L4.Latitude, L4.Longitude, L4.AltitudeM))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(120)))
            .Returns(new SearchAreaEvaluation(SearchAreaMatch.Outside, 80m, 0m));

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.ErrorId.Should().Be(SelectionLimitException.DeadlineExceededErrorId);
        _logger.Count("SELECTION-EMPTY").Should().Be(0);
    }

    [TestMethod]
    public async Task SelectAsync_DeadlineExpiresDuringOrdering_FailsWithSelectionDeadlineExceededWithoutNotice()
    {
        // Arrange
        SetupIndex();
        SetupCatalog(new[] { L4 });
        SetupArea(L4, SearchAreaMatch.Outside, 90m, 200m);
        _ordering.Setup(o => o.Order(It.IsAny<IEnumerable<MosmixLocationListEntry>>()))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(120)))
            .Returns(new List<MosmixLocationListEntry>());

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.ErrorId.Should().Be(SelectionLimitException.DeadlineExceededErrorId);
        _logger.Count("SELECTION-EMPTY").Should().Be(0);
    }

    [TestMethod]
    public async Task SelectAsync_CallerCancelledDuringOrdering_ThrowsOperationCanceledExceptionWithCallerToken()
    {
        // Arrange
        using var caller = new CancellationTokenSource();
        SetupIndex();
        SetupCatalog(new[] { L4 });
        SetupArea(L4, SearchAreaMatch.Outside, 90m, 200m);
        _ordering.Setup(o => o.Order(It.IsAny<IEnumerable<MosmixLocationListEntry>>()))
            .Callback(() =>
            {
                caller.Cancel();
                _clock.Advance(TimeSpan.FromSeconds(120));
            })
            .Returns(new List<MosmixLocationListEntry>());

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), caller.Token);

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should().Be(caller.Token);
        _logger.Count("SELECTION-EMPTY").Should().Be(0);
    }

    [TestMethod]
    public async Task SelectAsync_DeadlineExpiresDuringAvailabilityResolution_FailsWithSelectionDeadlineExceeded()
    {
        // Arrange
        var index = SetupIndex();
        SetupCatalog(new[] { L1 });
        SetupArea(L1, SearchAreaMatch.Inside, 1m, 1m);
        SetupMosmixL(new Dictionary<string, MosmixLRetrieval?> { ["X123A"] = null });
        _resolver.Setup(r => r.Resolve("X123A", index, null))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(120)))
            .Returns(MosmixSelectionTestData.Items("X123A"));
        SetupOrderingUnchanged();

        // Act
        Func<Task> act = () => _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.ErrorId.Should().Be(SelectionLimitException.DeadlineExceededErrorId);
        _logger.Count("SELECTION-EMPTY").Should().Be(0);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task SelectAsync_DeadlineExpiresWhileIndexReadWaits_FailsWithSelectionDeadlineExceeded()
    {
        _cache.Setup(c => c.ReadAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return (MosmixSEvidenceIndex?)null;
            });

        var selection = _subject.SelectAsync(MosmixSelectionTestData.Parameters(), CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(120));

        (await FluentActions.Awaiting(() => selection).Should().ThrowAsync<SelectionLimitException>())
            .Which.ErrorId.Should().Be(SelectionLimitException.DeadlineExceededErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task SelectAsync_CallerCancelsWhileIndexReadWaits_ThrowsOperationCanceledExceptionWithCallerToken()
    {
        using var caller = new CancellationTokenSource();
        _cache.Setup(c => c.ReadAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return (MosmixSEvidenceIndex?)null;
            });

        var selection = _subject.SelectAsync(MosmixSelectionTestData.Parameters(), caller.Token);
        caller.Cancel();

        (await FluentActions.Awaiting(() => selection).Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should().Be(caller.Token);
    }
}
