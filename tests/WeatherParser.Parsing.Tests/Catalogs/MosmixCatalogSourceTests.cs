using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixCatalogSourceTests
{
    private MockRepository _mocks = null!;
    private Mock<ICatalogDownloader> _downloader = null!;
    private Mock<IMosmixLocationCatalogReader> _catalog = null!;
    private Mock<IMosmixLEvidenceReader> _evidence = null!;
    private CancellationTokenSource _deadline = null!;
    private CancellationTokenSource _caller = null!;
    private TimeProvider _time = null!;
    private IMosmixCatalogSource _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _downloader = _mocks.Create<ICatalogDownloader>();
        _catalog = _mocks.Create<IMosmixLocationCatalogReader>();
        _evidence = _mocks.Create<IMosmixLEvidenceReader>();
        _deadline = new();
        _caller = new();
        _time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 23, 4, 30, 15, 750, TimeSpan.Zero));

        var options = new MosmixCatalogEndpoints
        {
            CatalogUri = new Uri("https://example.org/mosmix/stations.cfg?version=2"),
            MosmixLSingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/"),
            MosmixSAllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz"),
            EvidenceCacheDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-evidence-test"),
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
        };
        _subject = new MosmixCatalogSource(
            options,
            new MosmixCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator())),
            _downloader.Object,
            _catalog.Object,
            _evidence.Object,
            _time);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _deadline.Dispose();
        _caller.Dispose();
        _mocks.VerifyAll();
    }

    [TestMethod]
    public async Task MosmixCatalogSource_DownloadsAndReadsCatalog()
    {
        var catalog = new MosmixLocationCatalog(
            new[] { new MosmixLocationRecord("X123A", "MUSTERHAUSEN", 50.5m, 9.25m, 250m, 3) },
            Array.Empty<CatalogDiagnostic>());
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == "CatalogUri" && r.Uri.OriginalString == "https://example.org/mosmix/stations.cfg?version=2" && r.MaxBytes == CatalogRequest.ListOrCatalogMaxBytes && !r.NotFoundIsAbsent && r.DeadlineKind == CatalogDeadlineKind.Selection && r.DeadlineToken == _deadline.Token),
                It.IsAny<Func<Stream, CancellationToken, Task<MosmixLocationCatalog>>>(),
                _caller.Token))
            .Returns((CatalogRequest r, Func<Stream, CancellationToken, Task<MosmixLocationCatalog>> read, CancellationToken token) => read(new MemoryStream(new byte[] { 1 }), token)!);
        _catalog.Setup(r => r.ReadAsync(It.IsAny<Stream>(), "CatalogUri", _caller.Token)).ReturnsAsync(catalog);

        var result = await _subject.ReadCatalogAsync(_deadline.Token, _caller.Token);

        result.Should().BeSameAs(catalog);
    }

    [TestMethod]
    public async Task MosmixL_CatalogSource_DownloadsPerLocationKmlOnDemand()
    {
        var ids = new[] { "X123A", "01234" };
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == "MosmixLSingleStationsDirectory" && r.Uri.OriginalString == "https://example.org/mosmix/single_stations/X123A/kml/MOSMIX_L_LATEST_X123A.kmz" && r.MaxBytes == CatalogRequest.PerStationFileMaxBytes && r.NotFoundIsAbsent && r.DeadlineKind == CatalogDeadlineKind.Selection && r.DeadlineToken == _deadline.Token),
                It.IsAny<Func<Stream, CancellationToken, Task<MosmixLEvidence>>>() ,
                _caller.Token))
            .Returns((CatalogRequest r, Func<Stream, CancellationToken, Task<MosmixLEvidence>> read, CancellationToken token) => read(new MemoryStream(new byte[] { 1 }), token)!);
        _evidence.Setup(r => r.ReadAsync(It.IsAny<Stream>(), "X123A", It.IsAny<CancellationToken>())).ReturnsAsync(new MosmixLEvidence("X123A", new DateTime(2026, 10, 23, 3, 0, 0, DateTimeKind.Utc), new[] { "TTT" }));
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == "MosmixLSingleStationsDirectory" && r.Uri.OriginalString == "https://example.org/mosmix/single_stations/01234/kml/MOSMIX_L_LATEST_01234.kmz" && r.MaxBytes == CatalogRequest.PerStationFileMaxBytes && r.NotFoundIsAbsent && r.DeadlineKind == CatalogDeadlineKind.Selection && r.DeadlineToken == _deadline.Token),
                It.IsAny<Func<Stream, CancellationToken, Task<MosmixLEvidence>>>() ,
                _caller.Token))
            .Returns((CatalogRequest r, Func<Stream, CancellationToken, Task<MosmixLEvidence>> read, CancellationToken token) => Task.FromResult<MosmixLEvidence?>(null)!);

        var result = await _subject.ReadMosmixLEvidenceAsync(ids, _deadline.Token, _caller.Token);

        result.Should().HaveCount(2);
        result["X123A"].Should().NotBeNull();
        result["X123A"]!.Evidence.MosmixId.Should().Be("X123A");
        result["X123A"]!.RetrievedAtUtc.Should().Be(new DateTime(2026, 10, 23, 4, 30, 15, DateTimeKind.Utc));
        result["01234"].Should().BeNull();
    }

    [TestMethod]
    public async Task MosmixCatalogSource_PropagatesRetrievalFailure()
    {
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == "CatalogUri"),
                It.IsAny<Func<Stream, CancellationToken, Task<MosmixLocationCatalog>>>(),
                _caller.Token))
            .ThrowsAsync(new CatalogRetrievalException("CATALOG-TIMEOUT", "CatalogUri", "timeout"));

        Func<Task> act = () => _subject.ReadCatalogAsync(_deadline.Token, _caller.Token);
        await act.Should().ThrowAsync<CatalogRetrievalException>().WithMessage("*timeout*");
    }

    [TestMethod]
    public async Task Sources_NeverUseOtherCatalogEndpoints()
    {
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == "CatalogUri" && r.Uri.OriginalString == "https://example.org/mosmix/stations.cfg?version=2"),
                It.IsAny<Func<Stream, CancellationToken, Task<MosmixLocationCatalog>>>(),
                _caller.Token))
            .Returns((CatalogRequest r, Func<Stream, CancellationToken, Task<MosmixLocationCatalog>> read, CancellationToken token) => read(new MemoryStream(new byte[] { 1 }), token)!);
        _catalog.Setup(r => r.ReadAsync(It.IsAny<Stream>(), "CatalogUri", _caller.Token)).ReturnsAsync(new MosmixLocationCatalog(Array.Empty<MosmixLocationRecord>(), Array.Empty<CatalogDiagnostic>()));

        await _subject.ReadCatalogAsync(_deadline.Token, _caller.Token);

        // Strict mock ensures no other calls
    }

    [TestMethod]
    public async Task MosmixL_DuplicateIds_ThrowArgumentException()
    {
        var ids = new[] { "X123A", "X123A" };

        Func<Task> act = () => _subject.ReadMosmixLEvidenceAsync(ids, _deadline.Token, _caller.Token);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
