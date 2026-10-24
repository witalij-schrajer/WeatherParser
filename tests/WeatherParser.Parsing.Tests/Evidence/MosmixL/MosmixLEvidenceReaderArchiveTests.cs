using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Tests.Evidence.MosmixL;

[TestClass]
public sealed class MosmixLEvidenceReaderArchiveTests
{
    private IMosmixLEvidenceReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new MosmixLEvidenceReader();
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenCentralDirectoryIsCorrupt()
    {
        var kmz = MosmixKmzBuilder.Kmz(MosmixKmzBuilder.Kml());
        var i = kmz.Length - 1;
        for (; i >= 0; i--)
        {
            if (kmz[i] == 0x50 && kmz[i + 1] == 0x4B && kmz[i + 2] == 0x01 && kmz[i + 3] == 0x02)
            {
                break;
            }
        }

        i.Should().BeGreaterThan(0);
        kmz[i + 2] = 0x07;
        var stream = new MemoryStream(kmz);

        Func<Task> act = () => _subject.ReadAsync(stream, "X123A", CancellationToken.None);

        var exception = await FluentActions.Awaiting(act).Should().ThrowAsync<CatalogFormatException>();
        exception.Which.ErrorId.Should().Be(CatalogFormatException.MosmixEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task Read_LeavesTheCallersStreamOpenOnSuccessAndOnFailure()
    {
        using var valid = new MemoryStream(MosmixKmzBuilder.Kmz(MosmixKmzBuilder.Kml()));
        await _subject.ReadAsync(valid, "X123A", CancellationToken.None);
        valid.CanRead.Should().BeTrue();

        using var invalid = new MemoryStream(new byte[] { 1, 2, 3 });
        Func<Task> act = () => _subject.ReadAsync(invalid, "X123A", CancellationToken.None);

        var exception = await FluentActions.Awaiting(act).Should().ThrowAsync<CatalogFormatException>();
        exception.Which.ErrorId.Should().Be(CatalogFormatException.MosmixEvidenceInvalidErrorId);
        invalid.CanRead.Should().BeTrue();
    }
}
