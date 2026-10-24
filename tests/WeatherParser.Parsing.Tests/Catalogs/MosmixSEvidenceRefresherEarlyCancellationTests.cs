using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixSEvidenceRefresherEarlyCancellationTests
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

    [TestMethod]
    [Timeout(10000)]
    public async Task CallerCancelledBeforeParsing_InvalidZip_ThrowsOperationCanceledWithCallerToken()
    {
        using var caller = new CancellationTokenSource();
        ExpectDownload(Encoding.ASCII.GetBytes("not a zip archive"), () => caller.Cancel());
        await FluentActions.Awaiting(() => _subject.RefreshAsync(caller.Token))
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DeadlineExpiredBeforeParsing_TwoKmlMembers_ThrowsRefreshDeadlineExceeded()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry1 = archive.CreateEntry("MOSMIX_S_2026102303_240.kml", CompressionLevel.Optimal);
            using (var entryStream = entry1.Open())
            {
                var bytes = Encoding.Latin1.GetBytes(MosmixSKmzBuilder.Kml());
                entryStream.Write(bytes, 0, bytes.Length);
            }
            var entry2 = archive.CreateEntry("MOSMIX_S_2026102304_240.kml", CompressionLevel.Optimal);
            using (var entryStream = entry2.Open())
            {
                var bytes = Encoding.Latin1.GetBytes(MosmixSKmzBuilder.Kml());
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }

        var zip = stream.ToArray();
        ExpectDownload(zip, () => _time.Advance(TimeSpan.FromMinutes(11)));
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<EvidenceCacheException>()
            .Where(ex => ex.ErrorId == "MOSMIX-S-REFRESH-DEADLINE-EXCEEDED");
    }
}
