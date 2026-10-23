using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixSEvidenceRefresherTests
{
    private MockRepository _mocks = null!;
    private Mock<ICatalogDownloader> _downloader = null!;
    private Mock<IMosmixEvidenceCache> _cache = null!;
    private ManualTimeProvider _time = null!;
    private MosmixCatalogEndpoints _options = null!;
    private MosmixSEvidenceRefresher _subject = null!;
    private MosmixCatalogEndpointsValidator _validator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _downloader = _mocks.Create<ICatalogDownloader>();
        _cache = _mocks.Create<IMosmixEvidenceCache>();
        _time = new(new DateTimeOffset(2026, 10, 23, 4, 12, 7, 500, TimeSpan.Zero));
        _options = new()
        {
            CatalogUri = new Uri("https://example.org/mosmix/stations.cfg"),
            MosmixLSingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/"),
            MosmixSAllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=1"),
            EvidenceCacheDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-evidence-test"),
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
            MosmixSRefreshDeadline = TimeSpan.FromMinutes(10)
        };
        _validator = new(new ParserEndpointValidator(new EndpointUriValidator()));
        _subject = new(_options, _validator, _downloader.Object, _cache.Object, _time);
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

    private async Task ShouldBeInvalid(byte[] kmz)
    {
        ExpectDownload(kmz);
        (await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>())
            .Which.ErrorId.Should().Be(CatalogFormatException.MosmixEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_Success()
    {
        var kmz = MosmixSKmzBuilder.Kmz(MosmixSKmzBuilder.Kml(placemarks: new[] {
            MosmixSKmzBuilder.Placemark("X123A",
                MosmixSKmzBuilder.Forecast("TTT", "281.15 - 280.95"),
                MosmixSKmzBuilder.Forecast("FF", "3.10 - -"),
                MosmixSKmzBuilder.Forecast("wwM", "- - -")),
            MosmixSKmzBuilder.Placemark("01234",
                MosmixSKmzBuilder.Forecast("TTT", "- - -"))
        }));
        ExpectDownload(kmz, () => _time.Advance(TimeSpan.FromSeconds(3)));
        MosmixSEvidenceIndex? written = null;
        _cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>()))
            .Callback((MosmixSEvidenceIndex i, CancellationToken _) => written = i)
            .Returns(Task.CompletedTask);
        await _subject.RefreshAsync(CancellationToken.None);
        written.Should().NotBeNull();
        written.SourceUri.Should().Be("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz");
        written.DownloadedAtUtc.Should().Be(new DateTime(2026, 10, 23, 4, 12, 10, DateTimeKind.Utc));
        written.IssuedAtUtc.Should().Be(new DateTime(2026, 10, 23, 3, 0, 0, DateTimeKind.Utc));
        written.FileSha256.Should().Be(Convert.ToHexStringLower(SHA256.HashData(kmz)));
        written.FileSize.Should().Be(kmz.Length);
        written.Locations["X123A"].Should().Equal("FF", "TTT");
        written.Locations["01234"].Should().BeEmpty();
        written.Locations.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_NoPlacemarks_ThrowsCatalogFormatException()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: Array.Empty<string>());
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        await ShouldBeInvalid(kmz);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_MaxPlacemarks_Success()
    {
        var placemarks = MosmixSKmzBuilder.Placemarks(20000, i => MosmixSKmzBuilder.Forecast("TTT", "1.00 - -"));
        var kml = MosmixSKmzBuilder.Kml(placemarks: placemarks);
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        ExpectDownload(kmz, () => _time.Advance(TimeSpan.FromSeconds(3)));
        MosmixSEvidenceIndex? written = null;
        _cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>()))
            .Callback((MosmixSEvidenceIndex i, CancellationToken _) => written = i)
            .Returns(Task.CompletedTask);
        await _subject.RefreshAsync(CancellationToken.None);
        written.Should().NotBeNull();
        written.Locations.Should().HaveCount(20000);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_TooManyPlacemarks_ThrowsCatalogFormatException()
    {
        var placemarks = MosmixSKmzBuilder.Placemarks(20001, i => MosmixSKmzBuilder.Forecast("TTT", "1.00 - -"));
        var kml = MosmixSKmzBuilder.Kml(placemarks: placemarks);
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        await ShouldBeInvalid(kmz);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_MaxForecastsPerPlacemark_Success()
    {
        var forecasts = new StringBuilder();
        for (int j = 0; j < 200; j++)
        {
            forecasts.Append(MosmixSKmzBuilder.Forecast("E" + j.ToString("D3"), "1.00 - -"));
        }
        var placemark = MosmixSKmzBuilder.Placemark("X123A", forecasts.ToString());
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { placemark });
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        ExpectDownload(kmz, () => _time.Advance(TimeSpan.FromSeconds(3)));
        MosmixSEvidenceIndex? written = null;
        _cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>()))
            .Callback((MosmixSEvidenceIndex i, CancellationToken _) => written = i)
            .Returns(Task.CompletedTask);
        await _subject.RefreshAsync(CancellationToken.None);
        written.Should().NotBeNull();
        written.Locations["X123A"].Should().HaveCount(200);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_TooManyForecastsPerPlacemark_ThrowsCatalogFormatException()
    {
        var forecasts = new StringBuilder();
        for (int j = 0; j < 201; j++)
        {
            forecasts.Append(MosmixSKmzBuilder.Forecast("E" + j.ToString("D3"), "1.00 - -"));
        }
        var placemark = MosmixSKmzBuilder.Placemark("X123A", forecasts.ToString());
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { placemark });
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        await ShouldBeInvalid(kmz);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_DuplicatePlacemarkId_FirstOrder_ThrowsCatalogFormatException()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] {
            MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")),
            MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("FF", "1.00 - -")),
            MosmixSKmzBuilder.Placemark("B456", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -"))
        });
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        await ShouldBeInvalid(kmz);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_DuplicatePlacemarkId_SecondOrder_ThrowsCatalogFormatException()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] {
            MosmixSKmzBuilder.Placemark("B456", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")),
            MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("FF", "1.00 - -")),
            MosmixSKmzBuilder.Placemark("C789", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -")),
            MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("FF", "1.00 - -"))
        });
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        await ShouldBeInvalid(kmz);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_InvalidPlacemarkId_ThrowsCatalogFormatException()
    {
        // Test lowercase
        {
            var kml = MosmixSKmzBuilder.Kml(placemarks: new[] {
                MosmixSKmzBuilder.Placemark("x123a", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -"))
            });
            var kmz = MosmixSKmzBuilder.Kmz(kml);
            await ShouldBeInvalid(kmz);
        }

        // Test too short
        {
            var kml = MosmixSKmzBuilder.Kml(placemarks: new[] {
                MosmixSKmzBuilder.Placemark("ABC", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -"))
            });
            var kmz = MosmixSKmzBuilder.Kmz(kml);
            await ShouldBeInvalid(kmz);
        }

        // Test too long
        {
            var kml = MosmixSKmzBuilder.Kml(placemarks: new[] {
                MosmixSKmzBuilder.Placemark("ABCDEF", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -"))
            });
            var kmz = MosmixSKmzBuilder.Kmz(kml);
            await ShouldBeInvalid(kmz);
        }
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_InvalidForecastElementName_ThrowsCatalogFormatException()
    {
        // Test 17 characters
        {
            var kml = MosmixSKmzBuilder.Kml(placemarks: new[] {
                MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("ABCDEFGHIJKLMNOPQ", "1.00 - -"))
            });
            var kmz = MosmixSKmzBuilder.Kmz(kml);
            await ShouldBeInvalid(kmz);
        }

        // Test invalid character
        {
            var kml = MosmixSKmzBuilder.Kml(placemarks: new[] {
                MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("T-T", "1.00 - -"))
            });
            var kmz = MosmixSKmzBuilder.Kmz(kml);
            await ShouldBeInvalid(kmz);
        }
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_ElementNameOf16Characters_Success()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] {
            MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("ABCDEFGHIJKLMNOP", "1.00 - -"))
        });
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        ExpectDownload(kmz, () => _time.Advance(TimeSpan.FromSeconds(3)));
        MosmixSEvidenceIndex? written = null;
        _cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>()))
            .Callback((MosmixSEvidenceIndex i, CancellationToken _) => written = i)
            .Returns(Task.CompletedTask);
        await _subject.RefreshAsync(CancellationToken.None);
        written.Should().NotBeNull();
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_OversizedMember_ThrowsCatalogFormatException()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] {
            MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -"))
        });
        var length = Encoding.Latin1.GetByteCount(kml);
        var subject = new MosmixSEvidenceRefresher(_options, _validator, _downloader.Object, _cache.Object, _time, length - 1);
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        ExpectDownload(kmz);
        (await FluentActions.Awaiting(() => subject.RefreshAsync(CancellationToken.None)).Should().ThrowAsync<CatalogFormatException>())
            .Which.ErrorId.Should().Be(CatalogFormatException.MosmixEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_RefreshAsync_MaxMemberSize_Success()
    {
        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] {
            MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.00 - -"))
        });
        var length = Encoding.Latin1.GetByteCount(kml);
        var subject = new MosmixSEvidenceRefresher(_options, _validator, _downloader.Object, _cache.Object, _time, length);
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        ExpectDownload(kmz, () => _time.Advance(TimeSpan.FromSeconds(3)));
        MosmixSEvidenceIndex? written = null;
        _cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>()))
            .Callback((MosmixSEvidenceIndex i, CancellationToken _) => written = i)
            .Returns(Task.CompletedTask);
        await subject.RefreshAsync(CancellationToken.None);
        written.Should().NotBeNull();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesMosmixSEvidenceRefresher()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var refresher = provider.GetRequiredService<IMosmixSEvidenceRefresher>();
        refresher.Should().BeOfType<MosmixSEvidenceRefresher>();
    }

    [TestMethod]
    public void Constructor_InvalidOptions_Throws()
    {
        _options.MosmixSEvidenceMaxAge = null;
        FluentActions.Invoking(() => new MosmixSEvidenceRefresher(_options, _validator, _downloader.Object, _cache.Object, _time))
            .Should().Throw<EvidenceCacheException>()
            .Which.ErrorId.Should().Be("EVIDENCE-CACHE-SETTINGS-INVALID");
    }
}
