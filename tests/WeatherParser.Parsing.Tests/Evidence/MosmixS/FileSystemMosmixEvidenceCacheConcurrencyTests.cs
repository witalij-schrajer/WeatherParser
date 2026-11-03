using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using WeatherParser.Parsing.Evidence.MosmixS;

namespace WeatherParser.Parsing.Tests.Evidence.MosmixS;

[TestClass]
public sealed class FileSystemMosmixEvidenceCacheConcurrencyTests
{
    private string _directory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "wp-mosmix-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    [TestMethod]
    public async Task WriteAsync_WhileAReaderHoldsTheOldIndex_ReplacesItAndBothSnapshotsStayReadable()
    {
        var cache = MosmixEvidenceIndexTestData.Cache(_directory);
        var oldIndex = MosmixEvidenceIndexTestData.Index();
        await cache.WriteAsync(oldIndex, CancellationToken.None);
        var path = MosmixEvidenceIndexTestData.IndexPath(_directory);
        var oldBytes = await File.ReadAllBytesAsync(path);

        using (var heldReader = FileSystemMosmixEvidenceCache.OpenIndexForRead(path))
        {
            var newIndex = oldIndex with { DownloadedAtUtc = oldIndex.DownloadedAtUtc.AddHours(1) };
            await FluentActions.Awaiting(() => cache.WriteAsync(newIndex, CancellationToken.None)).Should().NotThrowAsync();

            var held = new byte[oldBytes.Length];
            await heldReader.ReadExactlyAsync(held, CancellationToken.None);
            held.Should().Equal(oldBytes);
        }

        var current = await cache.ReadAsync(CancellationToken.None);
        current.Should().NotBeNull();
        current!.DownloadedAtUtc.Should().Be(MosmixEvidenceIndexTestData.DownloadedAt.AddHours(1));
    }
}
