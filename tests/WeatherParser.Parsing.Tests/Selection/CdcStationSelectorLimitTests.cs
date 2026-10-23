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
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Selection;

namespace WeatherParser.Parsing.Tests.Selection;

[TestClass]
public sealed class CdcStationSelectorLimitTests
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
    private static readonly CdcCandidateStation D = CdcSelectionTestData.Station("07777", 280m, 50.4m, "sd");
    private static readonly CdcCandidateStation E = CdcSelectionTestData.Station("09001", 250m, 50.5m, "tu");
    private static readonly CdcCandidateStation F = CdcSelectionTestData.Station("09002", 260m, 50.6m, "tu");

    private void ArrangeEmptySelection()
    {
        var lists = SetupLists();
        SetupCandidates(lists, Array.Empty<CdcCandidateStation>());
        SetupDerive(Array.Empty<CdcStationAvailability>());
        SetupOrderingUnchanged();
    }

    private static decimal Dec(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static async Task ShouldBeSearchParameterInvalid(Func<Task> act)
    {
        (await act.Should().ThrowAsync<SearchParameterException>()).Which.ErrorId.Should().Be(SearchParameterException.SearchParameterInvalidErrorId);
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
    [DataRow("-90.0001", "0", "0", "1", "1")]
    [DataRow("0", "180.0001", "0", "1", "1")]
    [DataRow("0", "-180.0001", "0", "1", "1")]
    [DataRow("0", "0", "0", "-0.0001", "1")]
    [DataRow("0", "0", "0", "1", "-0.0001")]
    [DataRow("0", "0", "79228162514264337593543950335", "1", "1")]
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

    [DataRow("90", "180", "0", "0", "0")]
    [DataRow("-90", "-180", "-1000", "0", "0")]
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
        var input = CdcSelectionTestData.Parameters();
        var lists = CdcSelectionTestData.Lists();
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback(() => { input.Latitude = 1m; input.MaxDistanceKm = 999m; })
            .ReturnsAsync(lists);
        SetupCandidates(lists, new[] { A });
        _filter.Setup(f => f.Evaluate(It.Is<SearchParameters>(p => p.Latitude == 50.5m && p.MaxDistanceKm == 25m), A.Latitude, A.Longitude, A.AltitudeM))
            .Returns(new SearchAreaEvaluation(SearchAreaMatch.Inside, 5m, 1m));
        var evidence = NoEvidence(new CdcEvidenceKey("00044", "ff"), new CdcEvidenceKey("00044", "tu"));
        _source.Setup(s => s.ReadEvidenceAsync(It.IsAny<IReadOnlyCollection<CdcEvidenceKey>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ReturnsAsync(evidence);
        SetupDerive(
            new[] { new CdcStationAvailability(A, new[] { "temperature_c" }) });
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(input, CancellationToken.None);

        // Assert
        result.SearchParameters.Latitude.Should().Be(50.5m);
        result.SearchParameters.MaxDistanceKm.Should().Be(25m);
        result.SearchParameters.Should().NotBeSameAs(input);
    }

    [TestMethod]
    public async Task SelectAsync_CandidatesExactlyAtCap_Proceeds()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, new[] { A, D, E, B }, CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupArea(A, SearchAreaMatch.Inside, 5m, 1m);
        SetupArea(D, SearchAreaMatch.Inside, 6m, 2m);
        SetupArea(E, SearchAreaMatch.Inside, 7m, 3m);
        SetupArea(B, SearchAreaMatch.Outside, 80m, 0m);
        var evidence = NoEvidence(new CdcEvidenceKey("00044", "ff"), new CdcEvidenceKey("00044", "tu"), new CdcEvidenceKey("07777", "sd"), new CdcEvidenceKey("09001", "tu"));
        _source.Setup(s => s.ReadEvidenceAsync(It.IsAny<IReadOnlyCollection<CdcEvidenceKey>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ReturnsAsync(evidence);
        SetupDerive(
            new[] {
                new CdcStationAvailability(A, new[] { "temperature_c" }),
                new CdcStationAvailability(D, new[] { "temperature_c" }),
                new CdcStationAvailability(E, new[] { "temperature_c" })
            });
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        result.Stations.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task SelectAsync_CandidatesOneAboveCap_FailsBeforeAnyPerStationRequest()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, new[] { A, D, E, F, B }, CdcSelectionTestData.Diagnostic("CDC-STATION-CONFLICT", "08888"));
        SetupArea(A, SearchAreaMatch.Inside, 5m, 1m);
        SetupArea(D, SearchAreaMatch.Inside, 6m, 2m);
        SetupArea(E, SearchAreaMatch.Inside, 7m, 3m);
        SetupArea(F, SearchAreaMatch.Inside, 8m, 4m);
        SetupArea(B, SearchAreaMatch.Outside, 80m, 0m);
        // No evidence setup - strict mock should not be called
        // No derive setup - strict mock should not be called
        // No ordering setup - strict mock should not be called

        // Act
        Func<Task> act = () => _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

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
        CancellationToken listDeadline = default;
        CancellationToken listToken = default;
        CancellationToken evidenceDeadline = default;
        CancellationToken evidenceToken = default;
        var lists = CdcSelectionTestData.Lists();
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback<CancellationToken, CancellationToken>((deadline, token) =>
            {
                listDeadline = deadline;
                listToken = token;
            })
            .ReturnsAsync(lists);
        SetupCandidates(lists, new[] { A });
        _filter.Setup(f => f.Evaluate(It.IsAny<SearchParameters>(), A.Latitude, A.Longitude, A.AltitudeM))
            .Returns(new SearchAreaEvaluation(SearchAreaMatch.Inside, 5m, 1m));
        var evidence = NoEvidence(new CdcEvidenceKey("00044", "ff"), new CdcEvidenceKey("00044", "tu"));
        _source.Setup(s => s.ReadEvidenceAsync(It.IsAny<IReadOnlyCollection<CdcEvidenceKey>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<CdcEvidenceKey>, CancellationToken, CancellationToken>((_, deadline, token) =>
            {
                evidenceDeadline = deadline;
                evidenceToken = token;
            })
            .ReturnsAsync(evidence);
        SetupDerive(
            new[] { new CdcStationAvailability(A, new[] { "temperature_c" }) });
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), caller.Token);

        // Assert
        listToken.Should().Be(caller.Token);
        evidenceToken.Should().Be(caller.Token);
        evidenceDeadline.Should().Be(listDeadline);
        listDeadline.Should().NotBe(caller.Token);
        listDeadline.CanBeCanceled.Should().BeTrue();
        listDeadline.IsCancellationRequested.Should().BeFalse();
    }

    [TestMethod]
    public async Task SelectAsync_SelectionDeadlineOption_NotExceededJustBelowLimit()
    {
        // Arrange
        var lists = CdcSelectionTestData.Lists();
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(119)))
            .ReturnsAsync(lists);
        SetupCandidates(lists, Array.Empty<CdcCandidateStation>());
        SetupDerive(Array.Empty<CdcStationAvailability>());
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        result.Stations.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SelectAsync_DeadlineExceededDuringInitialRetrieval_FailsWithSelectionDeadlineExceeded()
    {
        // Arrange
        var lists = CdcSelectionTestData.Lists();
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(120)))
            .ReturnsAsync(lists);
        // No deriver setup - strict mock should not be called

        // Act
        Func<Task> act = () => _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.ErrorId.Should().Be(SelectionLimitException.DeadlineExceededErrorId);
    }

    [TestMethod]
    public async Task SelectAsync_DeadlineExceededDuringPerStationRequests_FailsWithSelectionDeadlineExceeded()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, new[] { A });
        _filter.Setup(f => f.Evaluate(It.IsAny<SearchParameters>(), A.Latitude, A.Longitude, A.AltitudeM))
            .Returns(new SearchAreaEvaluation(SearchAreaMatch.Inside, 5m, 1m));
        _source.Setup(s => s.ReadEvidenceAsync(It.IsAny<IReadOnlyCollection<CdcEvidenceKey>>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(120)))
            .ReturnsAsync(NoEvidence(new CdcEvidenceKey("00044", "ff"), new CdcEvidenceKey("00044", "tu")));
        // No derive setup - strict mock should not be called
        // No ordering setup - strict mock should not be called

        // Act
        Func<Task> act = () => _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.ErrorId.Should().Be(SelectionLimitException.DeadlineExceededErrorId);
    }

    [TestMethod]
    public async Task SelectAsync_SourceDeadlineFailure_PropagatesUnchanged()
    {
        // Arrange
        var failure = new SelectionLimitException(SelectionLimitException.DeadlineExceededErrorId, "Synthetic deadline.");
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);

        // Act
        Func<Task> act = () => _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.Should().BeSameAs(failure);
    }

    [TestMethod]
    public async Task SelectAsync_OperationCanceledForDeadline_MapsToSelectionDeadlineExceeded()
    {
        // Arrange
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Returns((CancellationToken deadline, CancellationToken token) =>
            {
                _clock.Advance(TimeSpan.FromSeconds(120));
                return Task.FromException<IReadOnlyList<CdcProductList>>(new OperationCanceledException(deadline));
            });

        // Act
        Func<Task> act = () => _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.ErrorId.Should().Be(SelectionLimitException.DeadlineExceededErrorId);
    }

    [TestMethod]
    public async Task SelectAsync_CallerCancellationAndDeadlineExpiry_ThrowsOperationCanceledExceptionWithCallerToken()
    {
        // Arrange
        using var caller = new CancellationTokenSource();
        var lists = CdcSelectionTestData.Lists();
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                _clock.Advance(TimeSpan.FromSeconds(120));
                caller.Cancel();
            })
            .ReturnsAsync(lists);

        // Act
        Func<Task> act = () => _subject.SelectAsync(CdcSelectionTestData.Parameters(), caller.Token);

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should().Be(caller.Token);
    }

    [TestMethod]
    public async Task SelectAsync_FractionalSecondsBeforeMidnight_TruncatedToWholeSeconds()
    {
        // Arrange
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero).AddTicks(9999999));
        _subject = CdcSelectionTestData.Create(_options, _source.Object, _deriver.Object, _filter.Object, _ordering.Object, clock, _logger);
        var lists = SetupLists();
        _deriver.Setup(d => d.SelectCandidates(lists, It.IsAny<DateTimeOffset>()))
            .Returns(new CdcCandidates(Array.Empty<CdcCandidateStation>(), Array.Empty<CatalogDiagnostic>()));
        _deriver.Setup(d => d.Derive(It.IsAny<CdcCandidates>(), It.IsAny<IReadOnlyDictionary<CdcEvidenceKey, CdcNowEvidence?>>(), It.IsAny<DateTimeOffset>()))
            .Returns(new CdcAvailabilityResult(Array.Empty<CdcStationAvailability>(), Array.Empty<CatalogDiagnostic>()));
        SetupOrderingUnchanged();

        // Act
        var result = await _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        result.RetrievedAtUtc.Should().Be(new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc));
        result.RetrievedAtWire.Should().Be("2026-12-31T23:59:59");
    }

    [TestMethod]
    public async Task SelectAsync_DeadlineExpiresDuringFiltering_FailsWithSelectionDeadlineExceededWithoutNotice()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, new[] { B });
        _filter.Setup(f => f.Evaluate(It.IsAny<SearchParameters>(), B.Latitude, B.Longitude, B.AltitudeM))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(120)))
            .Returns(new SearchAreaEvaluation(SearchAreaMatch.Outside, 80m, 0m));

        // Act
        Func<Task> act = () => _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.ErrorId.Should().Be(SelectionLimitException.DeadlineExceededErrorId);
        _logger.Count("SELECTION-EMPTY").Should().Be(0);
    }

    [TestMethod]
    public async Task SelectAsync_DeadlineExpiresDuringOrdering_FailsWithSelectionDeadlineExceededWithoutNotice()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, Array.Empty<CdcCandidateStation>());
        SetupDerive(Array.Empty<CdcStationAvailability>());
        _ordering.Setup(o => o.Order(It.IsAny<IEnumerable<CdcStationListEntry>>()))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(120)))
            .Returns(new List<CdcStationListEntry>());

        // Act
        Func<Task> act = () => _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

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
        var lists = SetupLists();
        SetupCandidates(lists, Array.Empty<CdcCandidateStation>());
        SetupDerive(Array.Empty<CdcStationAvailability>());
        _ordering.Setup(o => o.Order(It.IsAny<IEnumerable<CdcStationListEntry>>()))
            .Callback(() =>
            {
                caller.Cancel();
                _clock.Advance(TimeSpan.FromSeconds(120));
            })
            .Returns(new List<CdcStationListEntry>());

        // Act
        Func<Task> act = () => _subject.SelectAsync(CdcSelectionTestData.Parameters(), caller.Token);

        // Assert
        (await act.Should().ThrowAsync<OperationCanceledException>())
            .Which.CancellationToken.Should().Be(caller.Token);
        _logger.Count("SELECTION-EMPTY").Should().Be(0);
    }

    [TestMethod]
    public async Task SelectAsync_DeadlineExpiresDuringDerivation_FailsWithSelectionDeadlineExceeded()
    {
        // Arrange
        var lists = SetupLists();
        SetupCandidates(lists, Array.Empty<CdcCandidateStation>());
        _deriver.Setup(d => d.Derive(It.IsAny<CdcCandidates>(), It.IsAny<IReadOnlyDictionary<CdcEvidenceKey, CdcNowEvidence?>>(), CdcSelectionTestData.Reference))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(120)))
            .Returns(new CdcAvailabilityResult(Array.Empty<CdcStationAvailability>(), Array.Empty<CatalogDiagnostic>()));
        SetupOrderingUnchanged();

        // Act
        Func<Task> act = () => _subject.SelectAsync(CdcSelectionTestData.Parameters(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<SelectionLimitException>())
            .Which.ErrorId.Should().Be(SelectionLimitException.DeadlineExceededErrorId);
        _logger.Count("SELECTION-EMPTY").Should().Be(0);
    }
}
