using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Observations.Dwd;

[TestClass]
public sealed class DwdObservationSourceTests
{
    private MockRepository _mocks = null!;
    private Mock<ICatalogDownloader> _downloader = null!;
    private Mock<ICdcStationCatalogReader> _lists = null!;
    private Mock<ICdcNowEvidenceReader> _evidence = null!;
    private CancellationTokenSource _deadline = null!;
    private CancellationTokenSource _caller = null!;
    private IDwdObservationSource _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _downloader = _mocks.Create<ICatalogDownloader>();
        _lists = _mocks.Create<ICdcStationCatalogReader>();
        _evidence = _mocks.Create<ICdcNowEvidenceReader>();
        _deadline = new();
        _caller = new();

        var options = new CdcCatalogEndpoints
        {
            AirTemperatureStationList = new Uri("https://example.org/cdc/air_temperature/now/list.txt?version=2"),
            AirTemperatureNowDirectory = new Uri("https://example.org/cdc/air_temperature/now/"),
            WindStationList = new Uri("https://example.org/cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt"),
            WindNowDirectory = new Uri("https://example.org/cdc/wind/now/"),
            PrecipitationStationList = new Uri("https://example.org/cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt"),
            PrecipitationNowDirectory = new Uri("https://example.org/cdc/precipitation/now/"),
            SolarStationList = new Uri("https://example.org/cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt"),
            SolarNowDirectory = new Uri("https://example.org/cdc/solar/now/"),
        };
        _subject = new DwdObservationSource(
            new DwdObservationOptions { ObservationDeadline = TimeSpan.FromMinutes(30) },
            options,
            new CdcCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator())),
            _downloader.Object,
            _lists.Object,
            _evidence.Object,
            new CdcRequestComposer());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _deadline.Dispose();
        _caller.Dispose();
        _mocks.VerifyAll();
    }

    private static readonly (string Name, string Uri)[] Lists = {
        ("AirTemperatureStationList", "https://example.org/cdc/air_temperature/now/list.txt?version=2"),
        ("WindStationList", "https://example.org/cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt"),
        ("PrecipitationStationList", "https://example.org/cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt"),
        ("SolarStationList", "https://example.org/cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt")
    };

    private static readonly (string Code, string Name, string Uri)[] Archives = {
        ("tu", "AirTemperatureNowDirectory", "https://example.org/cdc/air_temperature/now/10minutenwerte_TU_00044_now.zip"),
        ("ff", "WindNowDirectory", "https://example.org/cdc/wind/now/10minutenwerte_wind_00044_now.zip"),
        ("rr", "PrecipitationNowDirectory", "https://example.org/cdc/precipitation/now/10minutenwerte_nieder_00044_now.zip"),
        ("sd", "SolarNowDirectory", "https://example.org/cdc/solar/now/10minutenwerte_SOLAR_00044_now.zip")
    };

    private void ExpectList(string name, string uri, CdcStationCatalog catalog)
    {
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == name && r.Uri.OriginalString == uri && r.MaxBytes == CatalogRequest.ListOrCatalogMaxBytes && !r.NotFoundIsAbsent && r.DeadlineKind == CatalogDeadlineKind.Observation && r.DeadlineToken == _deadline.Token),
                It.IsAny<Func<Stream, CancellationToken, Task<CdcStationCatalog>>>(),
                _caller.Token))
            .Returns((CatalogRequest r, Func<Stream, CancellationToken, Task<CdcStationCatalog>> read, CancellationToken token) => read(new MemoryStream(new byte[] { 1 }), token)!);
        _lists.Setup(l => l.ReadAsync(It.IsAny<Stream>(), name, _caller.Token)).ReturnsAsync(catalog);
    }

    private void FailFirstList(Exception exception)
    {
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == "AirTemperatureStationList"),
                It.IsAny<Func<Stream, CancellationToken, Task<CdcStationCatalog>>>(),
                _caller.Token))
            .ThrowsAsync(exception);
    }

    [TestMethod]
    public async Task ReadStationListsAsync_RequestsFourListsWithObservationKind()
    {
        foreach (var (name, uri) in Lists)
        {
            ExpectList(name, uri, Catalog("00044"));
        }

        var result = await _subject.ReadStationListsAsync(_deadline.Token, _caller.Token);

        result.Should().HaveCount(4);
    }

    [TestMethod]
    public async Task ReadStationListsAsync_ReturnsProductListsInProductOrder()
    {
        foreach (var (name, uri) in Lists)
        {
            ExpectList(name, uri, Catalog("00044"));
        }

        var result = await _subject.ReadStationListsAsync(_deadline.Token, _caller.Token);

        result.Should().HaveCount(4);
        result[0].ProductCode.Should().Be("tu");
        result[1].ProductCode.Should().Be("ff");
        result[2].ProductCode.Should().Be("rr");
        result[3].ProductCode.Should().Be("sd");
        result.Select(r => r.EndpointName).Should().ContainInOrder(
            "AirTemperatureStationList",
            "WindStationList",
            "PrecipitationStationList",
            "SolarStationList");
        var catalog0 = result[0].Catalog;
        var catalog1 = result[1].Catalog;
        var catalog2 = result[2].Catalog;
        var catalog3 = result[3].Catalog;
        result[0].Catalog.Should().BeSameAs(catalog0);
        result[1].Catalog.Should().BeSameAs(catalog1);
        result[2].Catalog.Should().BeSameAs(catalog2);
        result[3].Catalog.Should().BeSameAs(catalog3);
    }

    [TestMethod]
    public async Task ReadStationListsAsync_NotFound_IsRetrievalFailure()
    {
        FailFirstList(new CatalogRetrievalException(CatalogRetrievalException.RetrievalFailedErrorId, "AirTemperatureStationList", "HTTP status 404"));

        Func<Task> act = () => _subject.ReadStationListsAsync(_deadline.Token, _caller.Token);
        await act.Should().ThrowAsync<CatalogRetrievalException>()
            .WithMessage("*HTTP status 404*")
            .Where(ex => ex.ErrorId == CatalogRetrievalException.RetrievalFailedErrorId);
    }

    [TestMethod]
    public async Task ReadStationListsAsync_FormatFailure_Propagates()
    {
        var (name, uri) = Lists[0];
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == name),
                It.IsAny<Func<Stream, CancellationToken, Task<CdcStationCatalog>>>(),
                _caller.Token))
            .Returns((CatalogRequest r, Func<Stream, CancellationToken, Task<CdcStationCatalog>> read, CancellationToken token) => read(new MemoryStream(new byte[] { 1 }), token)!);
        _lists.Setup(l => l.ReadAsync(It.IsAny<Stream>(), name, _caller.Token)).ThrowsAsync(new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "Bad list."));

        Func<Task> act = () => _subject.ReadStationListsAsync(_deadline.Token, _caller.Token);
        await act.Should().ThrowAsync<CatalogFormatException>()
            .WithMessage("*Bad list.*")
            .Where(ex => ex.ErrorId == CatalogFormatException.CatalogFormatInvalidErrorId);
    }

    [TestMethod]
    public async Task ReadStationListsAsync_RetrievalFailure_Propagates()
    {
        FailFirstList(new CatalogRetrievalException(CatalogRetrievalException.TimeoutErrorId, "AirTemperatureStationList", "Timeout."));

        Func<Task> act = () => _subject.ReadStationListsAsync(_deadline.Token, _caller.Token);
        await act.Should().ThrowAsync<CatalogRetrievalException>()
            .WithMessage("*Timeout.*")
            .Where(ex => ex.ErrorId == CatalogRetrievalException.TimeoutErrorId);
    }

    [TestMethod]
    public async Task ReadStationListsAsync_DeadlineExpired_ThrowsObservationDeadlineExceeded()
    {
        FailFirstList(new ObservationLimitException(ObservationLimitException.DeadlineExceededErrorId, "Deadline."));

        Func<Task> act = () => _subject.ReadStationListsAsync(_deadline.Token, _caller.Token);
        await act.Should().ThrowAsync<ObservationLimitException>()
            .WithMessage("*Deadline.*")
            .Where(ex => ex.ErrorId == ObservationLimitException.DeadlineExceededErrorId);
    }

    [TestMethod]
    public async Task ReadStationListsAsync_CallerCancelled_ThrowsOperationCanceled()
    {
        FailFirstList(new OperationCanceledException(_caller.Token));

        Func<Task> act = () => _subject.ReadStationListsAsync(_deadline.Token, _caller.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static CdcStationCatalog Catalog(string id) => new(
        new[] { new CdcStationRecord(id, new DateOnly(2000, 1, 1), new DateOnly(2026, 10, 22), 100m, 50.1m, 9.1m, "MUSTERHAUSEN", "Hessen", 3) },
        Array.Empty<CatalogDiagnostic>());

    private static CdcNowEvidence Evidence(string code) => new("00044", code, Array.Empty<CdcNowEvidenceRow>());

    private void ExpectArchive(string code, string name, string uri, CdcNowEvidence evidence, string stationId = "00044")
    {
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == name && r.Uri.OriginalString == uri && r.MaxBytes == CatalogRequest.PerStationFileMaxBytes && r.NotFoundIsAbsent && r.DeadlineKind == CatalogDeadlineKind.Observation && r.DeadlineToken == _deadline.Token),
                It.IsAny<Func<Stream, CancellationToken, Task<CdcNowEvidence>>>(),
                _caller.Token))
            .Returns((CatalogRequest r, Func<Stream, CancellationToken, Task<CdcNowEvidence>> read, CancellationToken token) => read(new MemoryStream(new byte[] { 1 }), token)!);
        _evidence.Setup(e => e.ReadAsync(It.IsAny<Stream>(), stationId, code, _caller.Token)).ReturnsAsync(evidence);
    }

    private void ExpectNotFound(string name)
    {
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == name && r.NotFoundIsAbsent),
                It.IsAny<Func<Stream, CancellationToken, Task<CdcNowEvidence>>>(),
                _caller.Token))
            .ReturnsAsync((CdcNowEvidence?)null);
    }

    private void FailArchive(string name, Exception exception)
    {
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == name),
                It.IsAny<Func<Stream, CancellationToken, Task<CdcNowEvidence>>>(),
                _caller.Token))
            .ThrowsAsync(exception);
    }

    private Task<IReadOnlyDictionary<string, CdcNowEvidence?>> ReadArchives(params string[] codes) => _subject.ReadArchivesAsync("00044", codes, _deadline.Token, _caller.Token);

    [TestMethod]
    public async Task ReadArchivesAsync_ComposesArchiveUriPerProduct()
    {
        var expected = new Dictionary<string, CdcNowEvidence>(StringComparer.Ordinal);
        foreach (var (code, name, uri) in Archives)
        {
            expected[code] = Evidence(code);
            ExpectArchive(code, name, uri, expected[code]);
        }

        var result = await ReadArchives("tu", "ff", "rr", "sd");

        result.Keys.Should().BeEquivalentTo("tu", "ff", "rr", "sd");
        foreach (var code in expected.Keys)
        {
            result[code].Should().BeSameAs(expected[code]);
        }
    }

    [TestMethod]
    public async Task ReadArchivesAsync_UsesPerStationBoundAndObservationKind()
    {
        ExpectArchive("tu", "AirTemperatureNowDirectory", "https://example.org/cdc/air_temperature/now/10minutenwerte_TU_00044_now.zip", Evidence("tu"));

        var result = await ReadArchives("tu");

        result["tu"].Should().NotBeNull();
    }

    [TestMethod]
    public async Task ReadArchivesAsync_NotFound_ReturnsNullForThatProduct()
    {
        ExpectArchive("tu", "AirTemperatureNowDirectory", "https://example.org/cdc/air_temperature/now/10minutenwerte_TU_00044_now.zip", Evidence("tu"));
        ExpectNotFound("WindNowDirectory");

        var result = await ReadArchives("tu", "ff");

        result["ff"].Should().BeNull();
        result["tu"].Should().NotBeNull();
    }

    [DataRow("CATALOG-RETRIEVAL-FAILED")]
    [DataRow("CATALOG-TOO-LARGE")]
    [TestMethod]
    public async Task ReadArchivesAsync_OtherFailure_Propagates(string id)
    {
        FailArchive("AirTemperatureNowDirectory", new CatalogRetrievalException(id, "AirTemperatureNowDirectory", "Failure."));

        Func<Task> act = () => ReadArchives("tu");
        await act.Should().ThrowAsync<CatalogRetrievalException>()
            .Where(ex => ex.ErrorId == id);
    }

    [TestMethod]
    public async Task ReadArchivesAsync_InvalidArchive_Propagates()
    {
        ExpectArchive("tu", "AirTemperatureNowDirectory", "https://example.org/cdc/air_temperature/now/10minutenwerte_TU_00044_now.zip", Evidence("tu"));

        _evidence.Setup(e => e.ReadAsync(It.IsAny<Stream>(), "00044", "tu", _caller.Token)).ThrowsAsync(new CatalogFormatException(CatalogFormatException.CdcEvidenceInvalidErrorId, "Bad archive."));

        Func<Task> act = () => ReadArchives("tu");
        await act.Should().ThrowAsync<CatalogFormatException>()
            .Where(ex => ex.ErrorId == CatalogFormatException.CdcEvidenceInvalidErrorId);
    }

    [Timeout(10000)]
    [TestMethod]
    public async Task ReadArchivesAsync_AllFourProductsMayRunInParallel()
    {
        var arrived = 0;
        var peak = 0;
        var allArrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        foreach (var (_, name, _) in Archives)
        {
            _downloader.Setup(d => d.DownloadAsync(
                    It.Is<CatalogRequest>(r => r.EndpointName == name && r.NotFoundIsAbsent),
                    It.IsAny<Func<Stream, CancellationToken, Task<CdcNowEvidence>>>(),
                    _caller.Token))
                .Returns(async (CatalogRequest r, Func<Stream, CancellationToken, Task<CdcNowEvidence>> read, CancellationToken token) =>
                {
                    var now = Interlocked.Increment(ref arrived);
                    InterlockedMax(ref peak, now);
                    if (now == 4)
                    {
                        allArrived.SetResult();
                    }
                    await allArrived.Task;
                    Interlocked.Decrement(ref arrived);
                    return Evidence(r.Uri.OriginalString.Contains("_TU_") ? "tu" : r.Uri.OriginalString.Contains("_wind_") ? "ff" : r.Uri.OriginalString.Contains("_nieder_") ? "rr" : "sd");
                });
        }

        var result = await ReadArchives("tu", "ff", "rr", "sd");

        peak.Should().Be(4);
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while ((current = target) < value && Interlocked.CompareExchange(ref target, value, current) != current)
        {
        }
    }

    [TestMethod]
    public async Task ReadArchivesAsync_DuplicateProductCode_ThrowsArgumentExceptionBeforeAnyRequest()
    {
        Func<Task> act = () => ReadArchives("tu", "tu");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task ReadArchivesAsync_UnknownProductCode_ThrowsArgumentException()
    {
        Func<Task> act = () => ReadArchives("tu", "tx");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task ReadArchivesAsync_RequestsOnlyGivenProducts()
    {
        ExpectArchive("tu", "AirTemperatureNowDirectory", "https://example.org/cdc/air_temperature/now/10minutenwerte_TU_00044_now.zip", Evidence("tu"));
        ExpectArchive("sd", "SolarNowDirectory", "https://example.org/cdc/solar/now/10minutenwerte_SOLAR_00044_now.zip", Evidence("sd"));

        var result = await ReadArchives("tu", "sd");

        result.Should().ContainKeys("tu", "sd");
        result.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task ReadArchivesAsync_DeadlineExpiredDuringArchive_ThrowsObservationDeadlineExceeded()
    {
        FailArchive("AirTemperatureNowDirectory", new ObservationLimitException(ObservationLimitException.DeadlineExceededErrorId, "Deadline."));

        Func<Task> act = () => ReadArchives("tu");
        await act.Should().ThrowAsync<ObservationLimitException>()
            .Where(ex => ex.ErrorId == ObservationLimitException.DeadlineExceededErrorId);
    }

    [TestMethod]
    public async Task ReadArchivesAsync_CallerCancelledDuringArchive_ThrowsOperationCanceled()
    {
        FailArchive("AirTemperatureNowDirectory", new OperationCanceledException(_caller.Token));

        Func<Task> act = () => ReadArchives("tu");
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task ReadArchivesAsync_PassesRequestedStationIdToReader()
    {
        ExpectArchive("tu", "AirTemperatureNowDirectory", "https://example.org/cdc/air_temperature/now/10minutenwerte_TU_00183_now.zip", Evidence("tu"), "00183");

        var result = await _subject.ReadArchivesAsync("00183", new[] { "tu" }, _deadline.Token, _caller.Token);

        result["tu"].Should().NotBeNull();
    }

    [TestMethod]
    public async Task ReadArchivesAsync_NullStationId_ThrowsArgumentNullException()
    {
        Func<Task> act = () => _subject.ReadArchivesAsync(null!, new[] { "tu" }, _deadline.Token, _caller.Token);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task ReadArchivesAsync_NullProductCodes_ThrowsArgumentNullException()
    {
        Func<Task> act = () => _subject.ReadArchivesAsync("00044", null!, _deadline.Token, _caller.Token);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task ReadArchivesAsync_FailureStopsFurtherDispatch()
    {
        FailArchive("AirTemperatureNowDirectory", new CatalogRetrievalException(CatalogRetrievalException.RetrievalFailedErrorId, "AirTemperatureNowDirectory", "HTTP status 500"));

        await FluentActions.Awaiting(() => ReadArchives("tu", "ff", "rr", "sd")).Should().ThrowAsync<CatalogRetrievalException>().Where(ex => ex.ErrorId == CatalogRetrievalException.RetrievalFailedErrorId);

        _downloader.Invocations.Should().ContainSingle();
    }
}
