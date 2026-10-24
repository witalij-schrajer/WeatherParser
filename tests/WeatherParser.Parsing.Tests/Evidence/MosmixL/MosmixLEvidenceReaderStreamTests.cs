using System.IO;
using System.IO.Compression;
using System.Threading;
using FluentAssertions;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Tests.Evidence.MosmixL;

namespace WeatherParser.Parsing.Tests.Evidence.MosmixL;

[TestClass]
public sealed class MosmixLEvidenceReaderStreamTests
{
    private IMosmixLEvidenceReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new MosmixLEvidenceReader();
    }

    private sealed class CancelOnSecondReadStream(byte[] bytes, CancellationTokenSource source) : MemoryStream(bytes)
    {
        public int ReadCount { get; private set; }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            if (ReadCount == 2)
            {
                source.Cancel();
            }
            return await base.ReadAsync(buffer.Length > 64 ? buffer[..64] : buffer, cancellationToken);
        }
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenMemberDataIsCorrupt()
    {
        var kmz = MosmixKmzBuilder.Kmz(MosmixKmzBuilder.Kml(), CompressionLevel.Optimal);
        for (var i = 80; i < 120; i++)
        {
            kmz[i] = 0xFF;
        }

        (await FluentActions.Awaiting(() => _subject.ReadAsync(new MemoryStream(kmz), "X123A", CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>())
            .Which.ErrorId.Should().Be(CatalogFormatException.MosmixEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task Read_CancelledDuringRead_ThrowsOperationCanceledAndLeavesStreamOpen()
    {
        using var source = new CancellationTokenSource();
        using var stream = new CancelOnSecondReadStream(MosmixKmzBuilder.Kmz(MosmixKmzBuilder.Kml()), source);

        await FluentActions.Awaiting(() => _subject.ReadAsync(stream, "X123A", source.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        stream.ReadCount.Should().BeGreaterThanOrEqualTo(2);
        stream.CanRead.Should().BeTrue();
    }
}
