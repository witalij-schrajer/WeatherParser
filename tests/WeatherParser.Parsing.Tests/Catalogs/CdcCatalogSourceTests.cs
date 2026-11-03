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
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class CdcCatalogSourceTests
{
    private MockRepository _mocks = null!;
    private Mock<ICatalogDownloader> _downloader = null!;
    private Mock<ICdcStationCatalogReader> _lists = null!;
    private Mock<ICdcNowEvidenceReader> _evidence = null!;
    private CancellationTokenSource _deadline = null!;
    private CancellationTokenSource _caller = null!;
    private ICdcCatalogSource _subject = null!;

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
        _subject = new CdcCatalogSource(
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

    [TestMethod]
    public async Task CdcCatalogSource_DownloadsAndReadsTheFourStationLists()
    {
        var nameToUri = new Dictionary<string, string>
        {
            ["AirTemperatureStationList"] = "https://example.org/cdc/air_temperature/now/list.txt?version=2",
            ["WindStationList"] = "https://example.org/cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt",
            ["PrecipitationStationList"] = "https://example.org/cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt",
            ["SolarStationList"] = "https://example.org/cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt"
        };

        var catalog = Catalog("00044");
        foreach (var (name, uri) in nameToUri)
        {
            _downloader.Setup(d => d.DownloadAsync(
                    It.Is<CatalogRequest>(r => r.EndpointName == name && r.Uri.OriginalString == uri && r.MaxBytes == CatalogRequest.ListOrCatalogMaxBytes && !r.NotFoundIsAbsent && r.DeadlineKind == CatalogDeadlineKind.Selection && r.DeadlineToken == _deadline.Token),
                    It.IsAny<Func<Stream, CancellationToken, Task<CdcStationCatalog>>>(),
                    _caller.Token))
                .Returns((CatalogRequest r, Func<Stream, CancellationToken, Task<CdcStationCatalog>> read, CancellationToken token) => read(new MemoryStream(new byte[] { 1 }), token)!);
            _lists.Setup(r => r.ReadAsync(It.IsAny<Stream>(), name, _caller.Token)).ReturnsAsync(catalog);
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
    public async Task Sources_PreserveAllReaderDiagnostics()
    {
        var diagnostics = new[] { new CatalogDiagnostic(CatalogDiagnostic.RecordSkippedId, "AirTemperatureStationList", 4, "00044", "bad record") };
        var catalog = new CdcStationCatalog(
            new[] { new CdcStationRecord("00044", new DateOnly(2000, 1, 1), new DateOnly(2026, 10, 22), 100m, 50.1m, 9.1m, "MUSTERHAUSEN", "Hessen", 3) },
            diagnostics);
        _lists.Setup(r => r.ReadAsync(It.IsAny<Stream>(), "AirTemperatureStationList", _caller.Token)).ReturnsAsync(catalog);

        ExpectList("AirTemperatureStationList", "https://example.org/cdc/air_temperature/now/list.txt?version=2");
        ExpectList("WindStationList", "https://example.org/cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt");
        ExpectList("PrecipitationStationList", "https://example.org/cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt");
        ExpectList("SolarStationList", "https://example.org/cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt");

        _lists.Setup(r => r.ReadAsync(It.IsAny<Stream>(), "SolarStationList", _caller.Token)).ReturnsAsync(Catalog("00045"));
        _lists.Setup(r => r.ReadAsync(It.IsAny<Stream>(), "WindStationList", _caller.Token)).ReturnsAsync(Catalog("00046"));
        _lists.Setup(r => r.ReadAsync(It.IsAny<Stream>(), "PrecipitationStationList", _caller.Token)).ReturnsAsync(Catalog("00047"));

        var result = await _subject.ReadStationListsAsync(_deadline.Token, _caller.Token);

        result[0].Catalog.Diagnostics.Should().Equal(diagnostics);
    }

    [TestMethod]
    public async Task Sources_PropagateReaderFormatFailure()
    {
        ExpectList("AirTemperatureStationList", "https://example.org/cdc/air_temperature/now/list.txt?version=2");
        ExpectList("WindStationList", "https://example.org/cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt");

        _lists.Setup(r => r.ReadAsync(It.IsAny<Stream>(), "AirTemperatureStationList", _caller.Token)).ReturnsAsync(Catalog("00044"));
        _lists.Setup(r => r.ReadAsync(It.IsAny<Stream>(), "WindStationList", _caller.Token)).ThrowsAsync(new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "bad header"));

        Func<Task> act = () => _subject.ReadStationListsAsync(_deadline.Token, _caller.Token);
        await act.Should().ThrowAsync<CatalogFormatException>().WithMessage("*bad header*");
    }

    [TestMethod]
    public async Task Sources_PropagateRetrievalFailureWithoutPartialResult()
    {
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == "AirTemperatureStationList"),
                It.IsAny<Func<Stream, CancellationToken, Task<CdcStationCatalog>>>(),
                _caller.Token))
            .ThrowsAsync(new CatalogRetrievalException(CatalogRetrievalException.RetrievalFailedErrorId, "AirTemperatureStationList", "failed"));

        Func<Task> act = () => _subject.ReadStationListsAsync(_deadline.Token, _caller.Token);
        await act.Should().ThrowAsync<CatalogRetrievalException>().WithMessage("*failed*");
    }

    [TestMethod]
    public async Task CdcCatalogSource_DownloadsPerStationFilesOnDemand()
    {
        var keys = new[]
        {
            new CdcEvidenceKey("00044", "tu"),
            new CdcEvidenceKey("00044", "ff"),
            new CdcEvidenceKey("01234", "rr"),
            new CdcEvidenceKey("01234", "sd")
        };

        var nameToUri = new Dictionary<string, string>
        {
            ["tu"] = "https://example.org/cdc/air_temperature/now/10minutenwerte_TU_00044_now.zip",
            ["ff"] = "https://example.org/cdc/wind/now/10minutenwerte_wind_00044_now.zip",
            ["rr"] = "https://example.org/cdc/precipitation/now/10minutenwerte_nieder_01234_now.zip",
            ["sd"] = "https://example.org/cdc/solar/now/10minutenwerte_SOLAR_01234_now.zip"
        };

        foreach (var key in keys)
        {
            var name = key.ProductCode switch { "tu" => "AirTemperatureNowDirectory", "ff" => "WindNowDirectory", "rr" => "PrecipitationNowDirectory", _ => "SolarNowDirectory" };
            var uri = nameToUri[key.ProductCode];
            _downloader.Setup(d => d.DownloadAsync(
                    It.Is<CatalogRequest>(r => r.EndpointName == name && r.Uri.OriginalString == uri && r.MaxBytes == CatalogRequest.PerStationFileMaxBytes && r.NotFoundIsAbsent && r.DeadlineKind == CatalogDeadlineKind.Selection && r.DeadlineToken == _deadline.Token),
                    It.IsAny<Func<Stream, CancellationToken, Task<CdcNowEvidence>>>() ,
                    _caller.Token))
                .Returns(async (CatalogRequest r, Func<Stream, CancellationToken, Task<CdcNowEvidence>> read, CancellationToken token) => key.ProductCode is "tu" or "sd" ? await read(new MemoryStream(new byte[] { 1 }), token) : (CdcNowEvidence?)null);
        }

        _evidence.Setup(r => r.ReadAsync(It.IsAny<Stream>(), "00044", "tu", It.IsAny<CancellationToken>())).ReturnsAsync(new CdcNowEvidence("00044", "tu", Array.Empty<CdcNowEvidenceRow>()));
        _evidence.Setup(r => r.ReadAsync(It.IsAny<Stream>(), "01234", "sd", It.IsAny<CancellationToken>())).ReturnsAsync(new CdcNowEvidence("01234", "sd", Array.Empty<CdcNowEvidenceRow>()));

        var result = await _subject.ReadEvidenceAsync(keys, _deadline.Token, _caller.Token);

        result.Should().HaveCount(4);
        result[keys[0]].Should().NotBeNull();
        result[keys[1]].Should().BeNull();
        result[keys[2]].Should().BeNull();
        result[keys[3]].Should().NotBeNull();
    }

    [TestMethod]
    public async Task CdcCatalogSource_PerStationRequests_AtMostFourInParallel()
    {
        var keys = Enumerable.Range(0, 10).Select(i => new CdcEvidenceKey("0000" + i, "tu")).ToArray();
        var current = 0;
        var max = 0;
        var gate = new object();

        _downloader.Setup(d => d.DownloadAsync(
                It.IsAny<CatalogRequest>(),
                It.IsAny<Func<Stream, CancellationToken, Task<CdcNowEvidence>>>() ,
                _caller.Token))
            .Returns((CatalogRequest r, Func<Stream, CancellationToken, Task<CdcNowEvidence>> read, CancellationToken token) => Track());

        async Task<CdcNowEvidence?> Track()
        {
            var now = Interlocked.Increment(ref current);
            lock (gate)
            {
                max = Math.Max(max, now);
            }
            await Task.Delay(30);
            Interlocked.Decrement(ref current);
            return null;
        }

        var result = await _subject.ReadEvidenceAsync(keys, _deadline.Token, _caller.Token);

        max.Should().BeLessThanOrEqualTo(4);
        max.Should().BeGreaterThan(1);
        result.Should().HaveCount(10);
    }

    [TestMethod]
    public async Task CdcCatalogSource_UnknownProductCode_ThrowsArgumentException()
    {
        var key = new CdcEvidenceKey("00044", "tx");

        Func<Task> act = () => _subject.ReadEvidenceAsync(new[] { key }, _deadline.Token, _caller.Token);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    private void ExpectList(string name, string uri) =>
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == name && r.Uri.OriginalString == uri && r.MaxBytes == CatalogRequest.ListOrCatalogMaxBytes && !r.NotFoundIsAbsent && r.DeadlineKind == CatalogDeadlineKind.Selection && r.DeadlineToken == _deadline.Token),
                It.IsAny<Func<Stream, CancellationToken, Task<CdcStationCatalog>>>(),
                _caller.Token))
            .Returns((CatalogRequest r, Func<Stream, CancellationToken, Task<CdcStationCatalog>> read, CancellationToken token) => read(new MemoryStream(new byte[] { 1 }), token)!);

    private static CdcStationCatalog Catalog(string id) => new(
        new[] { new CdcStationRecord(id, new DateOnly(2000, 1, 1), new DateOnly(2026, 10, 22), 100m, 50.1m, 9.1m, "MUSTERHAUSEN", "Hessen", 3) },
        Array.Empty<CatalogDiagnostic>());
}
