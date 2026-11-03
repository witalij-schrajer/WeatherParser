using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixSEvidenceRefresherFailureTests
{
    private MockRepository _mocks = null!;
    private Mock<ICatalogDownloader> _downloader = null!;
    private Mock<IMosmixEvidenceCache> _cache = null!;
    private ManualTimeProvider _time = null!;
    private MosmixCatalogEndpoints _options = null!;
    private MosmixSEvidenceRefresher _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _downloader = _mocks.Create<ICatalogDownloader>();
        _cache = _mocks.Create<IMosmixEvidenceCache>();
        _time = new(new DateTimeOffset(2026, 10, 23, 4, 12, 7, 500, TimeSpan.Zero));
        _options = new()
        {
            CatalogUri = new("https://example.org/mosmix/stations.cfg"),
            MosmixLSingleStationsDirectory = new("https://example.org/mosmix/single_stations/"),
            MosmixSAllStationsUri = new("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=1"),
            EvidenceCacheDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-evidence-test"),
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
            MosmixSRefreshDeadline = TimeSpan.FromMinutes(10)
        };
        var validator = new MosmixCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
        _subject = new(_options, validator, _downloader.Object, _cache.Object, _time);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private void ExpectDownload(byte[] kmz, Action? onDownload = null) =>
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == "MosmixSAllStationsUri" && r.Uri.OriginalString == "https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=1" && r.MaxBytes == CatalogRequest.MosmixSAllStationsMaxBytes && !r.NotFoundIsAbsent && r.DeadlineKind == CatalogDeadlineKind.Refresh),
                It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (CatalogRequest r, Func<Stream, CancellationToken, Task<byte[]>> read, CancellationToken token) =>
            {
                var bytes = await read(new MemoryStream(kmz), token);
                onDownload?.Invoke();
                return bytes;
            });

    private static byte[] ValidKmz() => MosmixSKmzBuilder.Kmz(MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) }));

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_InvalidIssueTime_ThrowsCatalogFormatException()
    {
        var kml = MosmixSKmzBuilder.Kml(issueTime: "2026-10-23T03:00:00.500Z", placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        ExpectDownload(kmz);
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>()
            .Where(e => e.ErrorId == CatalogFormatException.MosmixEvidenceInvalidErrorId);

        kml = MosmixSKmzBuilder.Kml(issueTime: "2026-10-23 03:00:00Z", placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });
        kmz = MosmixSKmzBuilder.Kmz(kml);
        ExpectDownload(kmz);
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>()
            .Where(e => e.ErrorId == CatalogFormatException.MosmixEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_MalformedXml_ThrowsCatalogFormatException()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        var kmlBytes = Encoding.Latin1.GetBytes(kml);
        var malformedKml = Encoding.Latin1.GetString(kmlBytes, 0, kmlBytes.Length - 1); // Remove last character
        var malformedKmz = MosmixSKmzBuilder.Kmz(malformedKml);
        ExpectDownload(malformedKmz);
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>()
            .Where(e => e.ErrorId == CatalogFormatException.MosmixEvidenceInvalidErrorId);

        kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });
        kmz = MosmixSKmzBuilder.Kmz(kml);
        var dtdKml = kml.Insert(1, "<!DOCTYPE kml:kml [<!ENTITY x \"y\">]>\n");
        var dtdKmz = MosmixSKmzBuilder.Kmz(dtdKml);
        ExpectDownload(dtdKmz);
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>()
            .Where(e => e.ErrorId == CatalogFormatException.MosmixEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_MissingRequiredNode_ThrowsCatalogFormatException()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        var missingProductIdKml = kml.Replace("<dwd:ProductID>MOSMIX</dwd:ProductID>", "");
        var missingProductIdKmz = MosmixSKmzBuilder.Kmz(missingProductIdKml);
        ExpectDownload(missingProductIdKmz);
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>()
            .Where(e => e.ErrorId == CatalogFormatException.MosmixEvidenceInvalidErrorId);

        var missingForecastTimeStepsKml = Regex.Replace(kml, "<dwd:ForecastTimeSteps>.*?</dwd:ForecastTimeSteps>", "", RegexOptions.Singleline);
        var missingForecastTimeStepsKmz = MosmixSKmzBuilder.Kmz(missingForecastTimeStepsKml);
        ExpectDownload(missingForecastTimeStepsKmz);
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>()
            .Where(e => e.ErrorId == CatalogFormatException.MosmixEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_InvalidNamespace_ThrowsCatalogFormatException()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        var invalidNamespaceKml = kml.Replace("https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd", "https://example.org/other.xsd");
        var invalidNamespaceKmz = MosmixSKmzBuilder.Kmz(invalidNamespaceKml);
        ExpectDownload(invalidNamespaceKmz);
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>()
            .Where(e => e.ErrorId == CatalogFormatException.MosmixEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_InvalidForecastValue_ThrowsCatalogFormatException()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.2.3 - -")) });
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        ExpectDownload(kmz);
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>()
            .Where(e => e.ErrorId == CatalogFormatException.MosmixEvidenceInvalidErrorId);

        kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 -")) });
        kmz = MosmixSKmzBuilder.Kmz(kml);
        ExpectDownload(kmz);
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>()
            .Where(e => e.ErrorId == CatalogFormatException.MosmixEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_TwoKmlMembers_ThrowsCatalogFormatException()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry1 = archive.CreateEntry("MOSMIX_S_2026102303_240.kml", CompressionLevel.Optimal);
            using (var entryStream = entry1.Open())
            {
                var bytes = Encoding.Latin1.GetBytes(kml);
                entryStream.Write(bytes, 0, bytes.Length);
            }
            var entry2 = archive.CreateEntry("MOSMIX_S_2026102304_240.kml", CompressionLevel.Optimal);
            using (var entryStream = entry2.Open())
            {
                var bytes = Encoding.Latin1.GetBytes(kml);
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }
        var kmz = stream.ToArray();
        ExpectDownload(kmz);
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>()
            .Where(e => e.ErrorId == CatalogFormatException.MosmixEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_OversizedDownload_ThrowsCatalogRetrievalException()
    {
        _downloader.Setup(d => d.DownloadAsync(
                It.IsAny<CatalogRequest>(),
                It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CatalogRetrievalException(CatalogRetrievalException.TooLargeErrorId, "MosmixSAllStationsUri", "too large"));
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogRetrievalException>()
            .Where(e => e.ErrorId == "CATALOG-TOO-LARGE");
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_RequestTimeout_ThrowsCatalogRetrievalException()
    {
        _downloader.Setup(d => d.DownloadAsync(
                It.IsAny<CatalogRequest>(),
                It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CatalogRetrievalException(CatalogRetrievalException.TimeoutErrorId, "MosmixSAllStationsUri", "timeout"));
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogRetrievalException>()
            .Where(e => e.ErrorId == "CATALOG-TIMEOUT");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RefreshAsync_DeadlineExceeded_ThrowsRefreshDeadlineExceeded()
    {
        ExpectDownload(ValidKmz());
        _cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>()))
            .Returns(async (MosmixSEvidenceIndex index, CancellationToken token) =>
            {
                _time.Advance(TimeSpan.FromMinutes(11));
                await Task.Delay(Timeout.Infinite, token);
            });
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<EvidenceCacheException>()
            .Where(e => e.ErrorId == "MOSMIX-S-REFRESH-DEADLINE-EXCEEDED");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_CallerTokenCancellation_ThrowsOperationCanceledException()
    {
        using var caller = new CancellationTokenSource();
        ExpectDownload(ValidKmz());
        _cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>()))
            .Returns(async (MosmixSEvidenceIndex index, CancellationToken token) =>
            {
                caller.Cancel();
                await Task.Delay(Timeout.Infinite, token);
            });
        await FluentActions.Awaiting(() => _subject.RefreshAsync(caller.Token))
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(e => e.CancellationToken == caller.Token);
    }

    [TestMethod]
    public async Task RefreshAsync_CacheDirectoryNotWritable_ThrowsEvidenceCacheFailed()
    {
        _cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>()))
            .Throws(new EvidenceCacheException(EvidenceCacheException.CacheFailedErrorId, "not writable"));
        ExpectDownload(ValidKmz());
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<EvidenceCacheException>()
            .Where(e => e.ErrorId == EvidenceCacheException.CacheFailedErrorId);
    }

    [TestMethod]
    public async Task RefreshAsync_AnyFailure_KeepsPreviousIndex()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "wp-refresh-" + Guid.NewGuid().ToString("N"));
        try
        {
            _options.EvidenceCacheDirectory = tempDir;
            Directory.CreateDirectory(tempDir);
            var validator = new MosmixCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
            var cache = new FileSystemMosmixEvidenceCache(_options, validator);
            var subject = new MosmixSEvidenceRefresher(_options, validator, _downloader.Object, cache, _time);

            // First refresh - valid data
            ExpectDownload(ValidKmz());
            await subject.RefreshAsync(CancellationToken.None);

            // Second refresh - invalid data
            var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")), MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "2.00 - -")) });
            var kmz = MosmixSKmzBuilder.Kmz(kml);
            ExpectDownload(kmz);
            await FluentActions.Awaiting(() => subject.RefreshAsync(CancellationToken.None))
                .Should().ThrowAsync<CatalogFormatException>()
                .Where(e => e.ErrorId == CatalogFormatException.MosmixEvidenceInvalidErrorId);

            // Check that previous index is still there
            var index = await cache.ReadAsync(CancellationToken.None);
            index.Should().NotBeNull();
            index!.Locations.Should().ContainKey("X123A");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
