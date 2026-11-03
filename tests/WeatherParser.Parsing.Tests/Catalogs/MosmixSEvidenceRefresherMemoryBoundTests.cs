using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
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

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixSEvidenceRefresherMemoryBoundTests
{
    private const long AllocationBound = 64L * 1024 * 1024;

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

    private async Task AssertRejectedWithBoundedAllocation(byte[] kmz)
    {
        ExpectDownload(kmz);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalAllocatedBytes(precise: true);
        (await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None)).Should().ThrowAsync<CatalogFormatException>()).Which.ErrorId.Should().Be(CatalogFormatException.MosmixEvidenceInvalidErrorId);
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
        allocated.Should().BeLessThan(AllocationBound);
    }

    private static byte[] KmzFromBytes(byte[] member)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = archive.CreateEntry("MOSMIX_S_2026102303_240.kml", CompressionLevel.Optimal).Open();
            entry.Write(member, 0, member.Length);
        }

        return stream.ToArray();
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task RefreshAsync_OversizedForecastValue_RejectedWithBoundedAllocation()
    {
        var values = string.Join(" ", Enumerable.Repeat("1.00", 10_000_000));
        var kmz = MosmixSKmzBuilder.Kmz(MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", values)) }));
        await AssertRejectedWithBoundedAllocation(kmz);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task RefreshAsync_OversizedPlacemarkName_RejectedWithBoundedAllocation()
    {
        var kmz = MosmixSKmzBuilder.Kmz(MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark(new string('A', 50_000_000), MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) }));
        await AssertRejectedWithBoundedAllocation(kmz);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task RefreshAsync_OversizedIssueTime_RejectedWithBoundedAllocation()
    {
        var kmz = MosmixSKmzBuilder.Kmz(MosmixSKmzBuilder.Kml(issueTime: new string('9', 50_000_000), placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) }));
        await AssertRejectedWithBoundedAllocation(kmz);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task RefreshAsync_OversizedCdataForecastValue_RejectedWithBoundedAllocation()
    {
        var values = "<![CDATA[" + string.Join(" ", Enumerable.Repeat("1.00", 10_000_000)) + "]]>";
        var kmz = MosmixSKmzBuilder.Kmz(MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", values)) }));
        await AssertRejectedWithBoundedAllocation(kmz);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task RefreshAsync_OversizedCdataName_RejectedWithBoundedAllocation()
    {
        var kmz = MosmixSKmzBuilder.Kmz(MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("<![CDATA[" + new string('A', 50_000_000) + "]]>", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) }));
        await AssertRejectedWithBoundedAllocation(kmz);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task RefreshAsync_OversizedAttributeValue_RejectedWithBoundedAllocation()
    {
        var kmz = MosmixSKmzBuilder.Kmz(MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast(new string('T', 50_000_000), "1.00 - -")) }));
        await AssertRejectedWithBoundedAllocation(kmz);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task RefreshAsync_OversizedElementName_RejectedWithBoundedAllocation()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });
        var hugeElementName = "<kml:" + new string('d', 50_000_000) + "/>";
        kml = kml.Replace("<kml:description>ORT</kml:description>", hugeElementName);
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        await AssertRejectedWithBoundedAllocation(kmz);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task RefreshAsync_OversizedIgnoredPlacemarkChild_RejectedOnlyByStructureWithBoundedAllocation()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });
        var hugeDescription = "<kml:description>" + new string('x', 50_000_000) + "</kml:description>";
        kml = kml.Replace("<kml:description>ORT</kml:description>", hugeDescription);
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        ExpectDownload(kmz);
        _cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalAllocatedBytes(precise: true);
        await _subject.RefreshAsync(CancellationToken.None);
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
        allocated.Should().BeLessThan(AllocationBound);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task RefreshAsync_OversizedTextBetweenPlacemarkChildren_AcceptedWithBoundedAllocation()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });
        kml = kml.Replace("<kml:description>ORT</kml:description>", "<kml:description>ORT</kml:description>" + new string('x', 50_000_000));
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        ExpectDownload(kmz);
        _cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalAllocatedBytes(precise: true);
        await _subject.RefreshAsync(CancellationToken.None);
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
        allocated.Should().BeLessThan(AllocationBound);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task RefreshAsync_Utf16LittleEndianCdata_RejectedWithBoundedAllocation()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "<![CDATA[>" + new string('A', 50_000_000) + "]]>")) }).Replace("encoding=\"ISO-8859-1\"", "encoding=\"utf-16\"");
        var member = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(kml)).ToArray();
        var kmz = KmzFromBytes(member);
        await AssertRejectedWithBoundedAllocation(kmz);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task RefreshAsync_Utf16BigEndianCdata_RejectedWithBoundedAllocation()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "<![CDATA[>" + new string('A', 50_000_000) + "]]>")) }).Replace("encoding=\"ISO-8859-1\"", "encoding=\"utf-16\"");
        var member = Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes(kml)).ToArray();
        var kmz = KmzFromBytes(member);
        await AssertRejectedWithBoundedAllocation(kmz);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task RefreshAsync_ManyDistinctLargeNames_RejectedWithBoundedAllocation()
    {
        var names = string.Concat(Enumerable.Range(0, 100).Select(i => "<kml:n" + i.ToString() + new string('a', 500_000) + "/>"));
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });
        kml = kml.Replace("<kml:description>ORT</kml:description>", names);
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        await AssertRejectedWithBoundedAllocation(kmz);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task RefreshAsync_DeeplyNestedIgnoredSubtree_RejectedWithBoundedAllocation()
    {
        var nested = string.Concat(Enumerable.Repeat("<x>", 1_000_000)) + string.Concat(Enumerable.Repeat("</x>", 1_000_000));
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });
        kml = kml.Replace("<kml:description>ORT</kml:description>", "<kml:description>" + nested + "</kml:description>");
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        await AssertRejectedWithBoundedAllocation(kmz);
    }
}
