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
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Evidence.MosmixL;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public sealed class MosmixEvidenceCrcCharacterizationTests
{
    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    found = false;
                    break;
                }
            }
            if (found)
                return i;
        }
        return -1;
    }

    [TestMethod]
    public async Task MosmixLEvidenceReader_CrcMismatchValidXml_BehaviorUnchanged()
    {
        // Arrange
        var kmz = MosmixKmzBuilder.Kmz(MosmixKmzBuilder.Kml(), CompressionLevel.NoCompression);
        var memberNameBytes = Encoding.ASCII.GetBytes(MosmixKmzBuilder.MemberName);
        var indexOfMember = IndexOf(kmz, memberNameBytes);
        if (indexOfMember < 0)
            throw new InvalidOperationException("Could not find member name in KMZ");

        // Find the "281.05" string and change '0' to '1'
        var searchFor = Encoding.ASCII.GetBytes("281.05");
        var indexOfValue = IndexOf(kmz, searchFor);
        if (indexOfValue < 0)
            throw new InvalidOperationException("Could not find '281.05' in KMZ");

        kmz[indexOfValue + 4] = (byte)'1'; // Change '0' to '1'

        // Act
        var act = async () => await new MosmixLEvidenceReader().ReadAsync(new MemoryStream(kmz), "X123A", CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task MosmixSEvidenceRefresher_CrcMismatchValidXml_BehaviorUnchanged()
    {
        // Arrange
        var mock = new MockRepository(MockBehavior.Strict);
        var downloader = mock.Create<ICatalogDownloader>();
        var cache = mock.Create<IMosmixEvidenceCache>();
        var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 23, 4, 12, 7, 500, TimeSpan.Zero));
        var options = new MosmixCatalogEndpoints()
        {
            CatalogUri = new("https://example.org/mosmix/stations.cfg"),
            MosmixLSingleStationsDirectory = new("https://example.org/mosmix/single_stations/"),
            MosmixSAllStationsUri = new("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=1"),
            EvidenceCacheDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-evidence-test"),
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
            MosmixSRefreshDeadline = TimeSpan.FromMinutes(10)
        };
        var validator = new MosmixCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
        var subject = new MosmixSEvidenceRefresher(options, validator, downloader.Object, cache.Object, time);

        var kml = MosmixSKmzBuilder.Kml(placemarks: new[] { MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "1.25 - -")) });
        var kmz = MosmixKmzBuilder.Zip(CompressionLevel.NoCompression, ("MOSMIX_S_2026102303_240.kml", Encoding.Latin1.GetBytes(kml)));

        // Find the "1.25" string and change '2' to '3'
        var searchFor = Encoding.ASCII.GetBytes("1.25");
        var indexOfValue = IndexOf(kmz, searchFor);
        if (indexOfValue < 0)
            throw new InvalidOperationException("Could not find '1.25' in KMZ");

        kmz[indexOfValue + 2] = (byte)'3'; // Change '2' to '3'

        downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == "MosmixSAllStationsUri" && r.Uri.OriginalString == "https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=1" && r.MaxBytes == CatalogRequest.MosmixSAllStationsMaxBytes && !r.NotFoundIsAbsent && r.DeadlineKind == CatalogDeadlineKind.Refresh),
                It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (CatalogRequest r, Func<Stream, CancellationToken, Task<byte[]>> read, CancellationToken token) =>
            {
                var bytes = await read(new MemoryStream(kmz), token);
                return bytes;
            });

        cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var act = async () => await subject.RefreshAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        mock.VerifyAll();
    }
}
