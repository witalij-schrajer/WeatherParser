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

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixSEvidenceRefresherStructureTests
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

    private static string Valid() => MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });

    private async Task ShouldBeInvalid(byte[] kmz)
    {
        ExpectDownload(kmz);
        await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>()
            .Where(e => e.ErrorId == CatalogFormatException.MosmixEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task Refresh_RootIsNotKml_IsInvalid()
    {
        var text = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>\n<other/>\n";
        await ShouldBeInvalid(MosmixSKmzBuilder.Kmz(text));
    }

    [TestMethod]
    public async Task Refresh_TwoDocuments_IsInvalid()
    {
        var text = Valid().Replace("</kml:kml>", "<kml:Document></kml:Document></kml:kml>");
        await ShouldBeInvalid(MosmixSKmzBuilder.Kmz(text));
    }

    [TestMethod]
    public async Task Refresh_EmptyDocument_IsInvalid()
    {
        var text = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>\n<kml:kml xmlns:kml=\"http://www.opengis.net/kml/2.2\"><kml:Document/></kml:kml>\n";
        await ShouldBeInvalid(MosmixSKmzBuilder.Kmz(text));
    }

    [TestMethod]
    public async Task Refresh_NoDocument_IsInvalid()
    {
        var text = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>\n<kml:kml xmlns:kml=\"http://www.opengis.net/kml/2.2\"></kml:kml>\n";
        await ShouldBeInvalid(MosmixSKmzBuilder.Kmz(text));
    }

    [TestMethod]
    public async Task Refresh_TwoProductDefinitions_IsInvalid()
    {
        var text = Valid().Replace("</dwd:ProductDefinition>", "</dwd:ProductDefinition><dwd:ProductDefinition><dwd:ProductID>MOSMIX</dwd:ProductID></dwd:ProductDefinition>");
        await ShouldBeInvalid(MosmixSKmzBuilder.Kmz(text));
    }

    [TestMethod]
    public async Task Refresh_PlacemarkBeforeProductDefinition_IsInvalid()
    {
        var text = Valid().Replace("<kml:Document>\n", "<kml:Document>\n" + MosmixSKmzBuilder.Placemark("B456", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) + "\n");
        await ShouldBeInvalid(MosmixSKmzBuilder.Kmz(text));
    }

    [TestMethod]
    public async Task Refresh_ProductIdIsNotMosmix_IsInvalid()
    {
        var text = Valid().Replace("<dwd:ProductID>MOSMIX</dwd:ProductID>", "<dwd:ProductID>OTHER</dwd:ProductID>");
        await ShouldBeInvalid(MosmixSKmzBuilder.Kmz(text));
    }

    [TestMethod]
    public async Task Refresh_ImpossibleIssueTime_IsInvalid()
    {
        var kml = MosmixSKmzBuilder.Kml(issueTime: "2026-02-30T03:00:00Z", placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")) });
        await ShouldBeInvalid(MosmixSKmzBuilder.Kmz(kml));
    }

    [TestMethod]
    public async Task Refresh_EmptyForecastTimeSteps_IsInvalid()
    {
        var kml = MosmixSKmzBuilder.Kml(timeSteps: 0, placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A") });
        await ShouldBeInvalid(MosmixSKmzBuilder.Kmz(kml));
    }

    [TestMethod]
    public async Task Refresh_TwoPlacemarkExtendedData_IsInvalid()
    {
        var text = Valid().Replace("<kml:description>ORT</kml:description>", "<kml:description>ORT</kml:description><kml:ExtendedData></kml:ExtendedData>");
        await ShouldBeInvalid(MosmixSKmzBuilder.Kmz(text));
    }

    [TestMethod]
    public async Task Refresh_CorruptCentralDirectory_IsInvalid()
    {
        var kmz = MosmixSKmzBuilder.Kmz(Valid());
        // Find last index of 0x50, 0x4B, 0x01, 0x02
        int i = -1;
        for (int j = 0; j <= kmz.Length - 4; j++)
        {
            if (kmz[j] == 0x50 && kmz[j + 1] == 0x4B && kmz[j + 2] == 0x01 && kmz[j + 3] == 0x02)
            {
                i = j;
            }
        }

        if (i >= 0)
        {
            kmz[i + 2] = 0x07; // Corrupt version made too high
        }

        await ShouldBeInvalid(kmz);
    }

    [TestMethod]
    public async Task Refresh_UnknownElementsAreSkipped_AndPlacemarkWithoutExtendedDataHasNoElements_Succeeds()
    {
        var text = Valid().Replace("<kml:Document>\n", "<kml:Document>\n<kml:name>Example</kml:name>\n")
            .Replace("</kml:Document>", "</kml:Document><kml:Folder/>")
            .Replace("</kml:Document>", "<kml:Placemark><kml:name>B456</kml:name></kml:Placemark></kml:Document>");

        ExpectDownload(MosmixSKmzBuilder.Kmz(text));
        MosmixSEvidenceIndex? captured = null;
        _cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>()))
            .Returns(async (MosmixSEvidenceIndex index, CancellationToken token) =>
            {
                captured = index;
                await Task.CompletedTask;
            });

        await _subject.RefreshAsync(CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Locations.Should().ContainKey("X123A");
        captured.Locations["X123A"].Should().Contain("TTT");
        captured.Locations.Should().ContainKey("B456");
        captured.Locations["B456"].Should().BeEmpty();
    }

    [TestMethod]
    public async Task Refresh_NonDefaultPort_IsKeptInSourceUri()
    {
        var options = new MosmixCatalogEndpoints
        {
            CatalogUri = new("https://example.org/mosmix/stations.cfg"),
            MosmixLSingleStationsDirectory = new("https://example.org/mosmix/single_stations/"),
            MosmixSAllStationsUri = new("https://example.org:8443/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=1"),
            EvidenceCacheDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-evidence-test"),
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
            MosmixSRefreshDeadline = TimeSpan.FromMinutes(10)
        };
        var validator = new MosmixCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
        var subject = new MosmixSEvidenceRefresher(options, validator, _downloader.Object, _cache.Object, _time);

        var kmz = MosmixSKmzBuilder.Kmz(Valid());
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.Uri.OriginalString == "https://example.org:8443/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=1"),
                It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (CatalogRequest r, Func<Stream, CancellationToken, Task<byte[]>> read, CancellationToken token) => (byte[]?)await read(new MemoryStream(kmz), token));
        MosmixSEvidenceIndex? captured = null;
        _cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>()))
            .Returns(async (MosmixSEvidenceIndex index, CancellationToken token) =>
            {
                captured = index;
                await Task.CompletedTask;
            });

        await subject.RefreshAsync(CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.SourceUri.Should().Be("https://example.org:8443/mosmix/MOSMIX_S_LATEST_240.kmz");
    }
}
