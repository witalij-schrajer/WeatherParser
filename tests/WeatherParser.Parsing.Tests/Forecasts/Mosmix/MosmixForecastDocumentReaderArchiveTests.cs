using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using WeatherParser.Parsing.Evidence.Kmz;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Forecasts.Mosmix;
using WeatherParser.Parsing.Tests;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public class MosmixForecastDocumentReaderArchiveTests
{
    private IMosmixForecastDocumentReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new MosmixForecastDocumentReader();
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_ValidFile_Succeeds()
    {
        var kmz = MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single());
        var result = await Single(kmz);
        result.MosmixId.Should().Be("10637");
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_NoKmlMember_FailsFormatInvalid()
    {
        var kmz = MosmixForecastKmzBuilder.Zip(CompressionLevel.Optimal, ("readme.txt", Encoding.ASCII.GetBytes("x")));
        await ShouldFail(() => Single(kmz), MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_TwoKmlMembers_FailsFormatInvalid()
    {
        var kmz = MosmixForecastKmzBuilder.Zip(CompressionLevel.Optimal, ("a.kml", Encoding.Latin1.GetBytes(MosmixForecastKmzBuilder.Single())), ("b.kml", Encoding.Latin1.GetBytes(MosmixForecastKmzBuilder.Single())));
        await ShouldFail(() => Single(kmz), MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_NotAZip_FailsFormatInvalid()
    {
        var kmz = Encoding.ASCII.GetBytes("not a zip archive");
        await ShouldFail(() => Single(kmz), MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_MemberNameWithPathSegments_OnlyCompared()
    {
        var kml = MosmixForecastKmzBuilder.Single();
        var kmz = MosmixForecastKmzBuilder.Zip(CompressionLevel.Optimal, ("../outside/MOSMIX_L_2026102615_10637.kml", Encoding.Latin1.GetBytes(kml)));
        var result = await Single(kmz);
        result.MosmixId.Should().Be("10637");

        // Verify that the file was not actually written to disk
        var tempPath = Path.GetTempPath();
        var outsidePath = Path.Combine(tempPath, "outside");
        var filePath = Path.Combine(outsidePath, "MOSMIX_L_2026102615_10637.kml");
        File.Exists(filePath).Should().BeFalse();
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_CompressedAtBound_Accepted()
    {
        var kmz = MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single());
        var limits = MosmixForecastReaderLimits.Contract with { SingleCompressedBytes = kmz.Length };
        var subject = new MosmixForecastDocumentReader(limits);
        var result = await subject.ReadSingleLocationAsync(new MemoryStream(kmz), "10637", CancellationToken.None);
        result.MosmixId.Should().Be("10637");
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_CompressedOneByteAbove_FailsTooLarge()
    {
        var kmz = MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single());
        var limits = MosmixForecastReaderLimits.Contract with { SingleCompressedBytes = kmz.Length - 1 };
        var subject = new MosmixForecastDocumentReader(limits);
        await ShouldFail(() => subject.ReadSingleLocationAsync(new MemoryStream(kmz), "10637", CancellationToken.None), MosmixForecastErrorIds.TooLarge);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_UnpackedAtBound_Accepted()
    {
        var kml = MosmixForecastKmzBuilder.Single();
        var kmz = MosmixForecastKmzBuilder.Kmz(kml);
        var length = Encoding.Latin1.GetByteCount(kml);
        var limits = MosmixForecastReaderLimits.Contract with { SingleUnpackedBytes = length };
        var subject = new MosmixForecastDocumentReader(limits);
        var result = await subject.ReadSingleLocationAsync(new MemoryStream(kmz), "10637", CancellationToken.None);
        result.MosmixId.Should().Be("10637");
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_UnpackedOneByteAbove_FailsTooLarge()
    {
        var kml = MosmixForecastKmzBuilder.Single();
        var kmz = MosmixForecastKmzBuilder.Kmz(kml);
        var length = Encoding.Latin1.GetByteCount(kml);
        var limits = MosmixForecastReaderLimits.Contract with { SingleUnpackedBytes = length - 1 };
        var subject = new MosmixForecastDocumentReader(limits);
        await ShouldFail(() => subject.ReadSingleLocationAsync(new MemoryStream(kmz), "10637", CancellationToken.None), MosmixForecastErrorIds.TooLarge);
    }

    [TestMethod]
    public async Task ReadAllStationsAsync_CompressedAndUnpackedBounds_EnforcedOnBothSides()
    {
        var kml = AllStationsKml();
        var kmz = MosmixForecastKmzBuilder.Kmz(kml, CompressionLevel.Optimal, MosmixForecastKmzBuilder.AllStationsMemberName);
        var length = Encoding.Latin1.GetByteCount(kml);

        // Test compressed bound
        {
            var limits = MosmixForecastReaderLimits.Contract with { AllStationsCompressedBytes = kmz.Length };
            var subject = new MosmixForecastDocumentReader(limits);
            await subject.ReadAllStationsAsync(new MemoryStream(kmz), "10637", CancellationToken.None);
        }

        // Test uncompressed bound
        {
            var limits = MosmixForecastReaderLimits.Contract with { AllStationsUnpackedBytes = length };
            var subject = new MosmixForecastDocumentReader(limits);
            await subject.ReadAllStationsAsync(new MemoryStream(kmz), "10637", CancellationToken.None);
        }

        // Test compressed below bound
        {
            var limits = MosmixForecastReaderLimits.Contract with { AllStationsCompressedBytes = kmz.Length - 1 };
            var subject = new MosmixForecastDocumentReader(limits);
            await ShouldFail(() => subject.ReadAllStationsAsync(new MemoryStream(kmz), "10637", CancellationToken.None), MosmixForecastErrorIds.TooLarge);
        }

        // Test uncompressed below bound
        {
            var limits = MosmixForecastReaderLimits.Contract with { AllStationsUnpackedBytes = length - 1 };
            var subject = new MosmixForecastDocumentReader(limits);
            await ShouldFail(() => subject.ReadAllStationsAsync(new MemoryStream(kmz), "10637", CancellationToken.None), MosmixForecastErrorIds.TooLarge);
        }
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_Dtd_FailsFormatInvalid()
    {
        var kml = MosmixForecastKmzBuilder.Single();
        var dtdLine = "<!DOCTYPE kml:kml [<!ENTITY e \"x\">]>";
        var end = kml.IndexOf('\n') + 1;
        kml = kml.Substring(0, end) + dtdLine + "\n" + kml.Substring(end);
        var kmz = MosmixForecastKmzBuilder.Kmz(kml);
        await ShouldFail(() => Single(kmz), MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_ExternalEntity_FailsFormatInvalid()
    {
        var kml = MosmixForecastKmzBuilder.Single();
        var dtdLine = "<!DOCTYPE kml:kml [<!ENTITY e SYSTEM \"http://example.invalid/e.txt\">]>";
        var end = kml.IndexOf('\n') + 1;
        kml = kml.Substring(0, end) + dtdLine + "\n" + kml.Substring(end);
        kml = kml.Replace("<kml:description>BEISPIELSTADT</kml:description>", "<kml:description>&e;</kml:description>");
        var kmz = MosmixForecastKmzBuilder.Kmz(kml);
        await ShouldFail(() => Single(kmz), MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_ExternalResourceReference_NeverFollowed()
    {
        var kml = MosmixForecastKmzBuilder.Single();
        var pos = kml.IndexOf("<kml:Document>");
        pos.Should().BeGreaterThanOrEqualTo(0);
        kml = kml.Substring(0, pos + 14) +
              "<kml:NetworkLink><kml:Link><kml:href>http://example.invalid/other.kml</kml:href></kml:Link></kml:NetworkLink>" +
              kml.Substring(pos + 14);
        kml = kml.Replace("<kml:kml ", "<kml:kml xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://example.invalid/kml.xsd\" ");
        var kmz = MosmixForecastKmzBuilder.Kmz(kml);
        var result = await Single(kmz);
        result.MosmixId.Should().Be("10637");
    }

    [TestMethod]
    public async Task ReadAllStationsAsync_ExternalResourceReference_NeverFollowed()
    {
        var kml = AllStationsKml();
        var pos = kml.IndexOf("<kml:Document>");
        pos.Should().BeGreaterThanOrEqualTo(0);
        kml = kml.Substring(0, pos + 14) +
              "<kml:NetworkLink><kml:Link><kml:href>http://example.invalid/other.kml</kml:href></kml:Link></kml:NetworkLink>" +
              kml.Substring(pos + 14);
        kml = kml.Replace("<kml:kml ", "<kml:kml xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://example.invalid/kml.xsd\" ");
        var kmz = MosmixForecastKmzBuilder.Kmz(kml, CompressionLevel.Optimal, MosmixForecastKmzBuilder.AllStationsMemberName);
        var result = await All(kmz);
        result.MosmixId.Should().Be("10637");
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_CrcMismatchValidXml_FailsFormatInvalid()
    {
        var kml = MosmixForecastKmzBuilder.Single();
        var kmz = MosmixForecastKmzBuilder.Kmz(kml, CompressionLevel.NoCompression);
        var bytes = kmz.ToArray();

        // Find the first "281.05" and change it to "281.15"
        var index = IndexOf(bytes, Encoding.Latin1.GetBytes("281.05"));
        index.Should().BeGreaterThanOrEqualTo(0);
        bytes[index + 4] = (byte)'1';

        await ShouldFail(() => Single(bytes), MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadAllStationsAsync_CrcMismatchValidXml_FailsFormatInvalid()
    {
        var kml = AllStationsKml();
        var kmz = MosmixForecastKmzBuilder.Kmz(kml, CompressionLevel.NoCompression, MosmixForecastKmzBuilder.AllStationsMemberName);
        var bytes = kmz.ToArray();

        // Find the last "281.05" and change it to "281.15"
        var index = IndexOf(bytes, Encoding.Latin1.GetBytes("281.05"));
        int lastIndex = -1;
        while (index >= 0)
        {
            lastIndex = index;
            index = IndexOf(bytes, Encoding.Latin1.GetBytes("281.05"), index + 1);
        }

        lastIndex.Should().BeGreaterThanOrEqualTo(0);
        bytes[lastIndex + 4] = (byte)'1';

        await ShouldFail(() => All(bytes), MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_CrcMismatchDeflated_FailsFormatInvalid()
    {
        var kml = MosmixForecastKmzBuilder.Single();
        var kmz = MosmixForecastKmzBuilder.Kmz(kml, CompressionLevel.Optimal);
        var bytes = kmz.ToArray();

        // Flip the lowest bit of the CRC-32 in the local file header (byte 14)
        int localHeaderCrcIndex = -1;
        for (int i = 0; i < bytes.Length - 4; i++)
        {
            if (bytes[i] == 0x50 && bytes[i + 1] == 0x4B && bytes[i + 2] == 0x03 && bytes[i + 3] == 0x04)
            {
                localHeaderCrcIndex = i + 14;
                break;
            }
        }

        if (localHeaderCrcIndex >= 0)
        {
            bytes[localHeaderCrcIndex] ^= 0x01;
        }

        // Flip the lowest bit of the CRC-32 in the central directory header
        int centralDirCrcIndex = -1;
        for (int i = 0; i < bytes.Length - 4; i++)
        {
            if (bytes[i] == 0x50 && bytes[i + 1] == 0x4B && bytes[i + 2] == 0x01 && bytes[i + 3] == 0x02)
            {
                centralDirCrcIndex = i + 16;
                break;
            }
        }

        if (centralDirCrcIndex >= 0)
        {
            bytes[centralDirCrcIndex] ^= 0x01;
        }

        await ShouldFail(() => Single(bytes), MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public void Crc32Stream_CheckValue123456789()
    {
        var s = new KmzCrc32Stream(new MemoryStream(Encoding.ASCII.GetBytes("123456789")), 0xCBF43926u, m => new InvalidDataException(m));
        s.CopyTo(Stream.Null);
        s.Value.Should().Be(0xCBF43926u);
    }

    [TestMethod]
    public void Crc32Stream_EmptyInput_ValueZero()
    {
        var s = new KmzCrc32Stream(new MemoryStream(), 0u, m => new InvalidDataException(m));
        s.CopyTo(Stream.Null);
        s.Value.Should().Be(0u);
    }

    [TestMethod]
    public async Task Crc32Stream_Mismatch_ThrowsAtEnd()
    {
        var s = new KmzCrc32Stream(new MemoryStream(Encoding.ASCII.GetBytes("123456789")), 0u, m => new InvalidDataException(m));
        Func<Task> act = () => Task.Run(() => s.CopyTo(Stream.Null));
        await act.Should().ThrowAsync<InvalidDataException>().Where(e => e.Message.Contains("CRC-32"));
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_UsesOnlyAsynchronousReads()
    {
        var kmz = MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single());
        var innerStream = new MemoryStream(kmz);
        var asyncOnlyStream = new AsyncOnlyStream(innerStream);

        await _subject.ReadSingleLocationAsync(asyncOnlyStream, "10637", CancellationToken.None);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var kmz = MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single());
        await FluentActions.Awaiting(() => _subject.ReadSingleLocationAsync(new MemoryStream(kmz), "10637", cts.Token)).Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task ReadAllStationsAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var kmz = MosmixForecastKmzBuilder.Kmz(AllStationsKml(), CompressionLevel.Optimal, MosmixForecastKmzBuilder.AllStationsMemberName);
        await FluentActions.Awaiting(() => _subject.ReadAllStationsAsync(new MemoryStream(kmz), "10637", cts.Token)).Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_NullArguments_ThrowArgumentNullException()
    {
        Func<Task> act1 = () => _subject.ReadSingleLocationAsync(null!, "10637", CancellationToken.None);
        await act1.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act2 = () => _subject.ReadSingleLocationAsync(new MemoryStream(), null!, CancellationToken.None);
        await act2.Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task ReadAllStationsAsync_NullArguments_ThrowArgumentNullException()
    {
        Func<Task> act1 = () => _subject.ReadAllStationsAsync(null!, "10637", CancellationToken.None);
        await act1.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act2 = () => _subject.ReadAllStationsAsync(new MemoryStream(), null!, CancellationToken.None);
        await act2.Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesMosmixForecastDocumentReader()
    {
        var services = new ServiceCollection();
        services.AddTestConsumerServices();
        services.AddWeatherParsing();
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var reader = provider.GetRequiredService<IMosmixForecastDocumentReader>();
        reader.Should().BeOfType<MosmixForecastDocumentReader>();
    }

    private static async Task<MosmixForecastFormatException> ShouldFail(Func<Task> act, string errorId)
    {
        var ex = await act.Should().ThrowAsync<MosmixForecastFormatException>();
        ex.Which.ErrorId.Should().Be(errorId);
        return ex.Which;
    }

    private static int IndexOf(byte[] haystack, byte[] needle, int startIndex = 0)
    {
        for (int i = startIndex; i <= haystack.Length - needle.Length; i++)
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

    private Task<MosmixForecastDocument> Single(byte[] kmz) => _subject.ReadSingleLocationAsync(new MemoryStream(kmz), "10637", CancellationToken.None);

    private Task<MosmixForecastDocument> All(byte[] kmz) => _subject.ReadAllStationsAsync(new MemoryStream(kmz), "10637", CancellationToken.None);

    private static string AllStationsKml() => MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(), MosmixForecastKmzBuilder.Placemark("A111"), MosmixForecastKmzBuilder.Placemark("10637"), MosmixForecastKmzBuilder.Placemark("B222"));

    private class AsyncOnlyStream : Stream
    {
        private readonly MemoryStream _inner;

        public AsyncOnlyStream(MemoryStream inner)
        {
            _inner = inner;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("Only asynchronous reads are allowed.");
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return _inner.ReadAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
