using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Evidence.MosmixS;

[TestClass]
public sealed class FileSystemMosmixEvidenceCacheWriteTests
{
    private string _directory = null!;
    private IMosmixEvidenceCache _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_directory);
        _subject = MosmixEvidenceIndexTestData.Cache(_directory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    private Task Write(MosmixSEvidenceIndex index) => _subject.WriteAsync(index, CancellationToken.None);

    private static async Task ShouldFail(Func<Task> act)
    {
        var exception = await FluentActions.Awaiting(act).Should().ThrowAsync<EvidenceCacheException>();
        exception.Which.ErrorId.Should().Be(EvidenceCacheException.CacheFailedErrorId);
    }

    private string[] Files() => Directory.GetFiles(_directory).Select(Path.GetFileName).ToArray()!;

    [TestMethod]
    public async Task Write_RoundTripIndexSuccessfully()
    {
        var index = MosmixEvidenceIndexTestData.Index();
        await Write(index);

        var read = await _subject.ReadAsync(CancellationToken.None);
        read.Should().NotBeNull();
        read!.SourceUri.Should().Be(index.SourceUri);
        read.DownloadedAtUtc.Should().Be(index.DownloadedAtUtc);
        read.IssuedAtUtc.Should().Be(index.IssuedAtUtc);
        read.FileSha256.Should().Be(index.FileSha256);
        read.FileSize.Should().Be(index.FileSize);
        read.Locations["X123A"].Should().Equal("FF", "TTT");
        read.Locations["01234"].Should().Equal("TTT");

        Files().Should().ContainSingle().And.Contain(MosmixEvidenceIndexTestData.FileName(MosmixEvidenceIndexTestData.SourceUri));

        var json = JsonDocument.Parse(File.ReadAllText(MosmixEvidenceIndexTestData.IndexPath(_directory)));
        json.RootElement.GetProperty("format_version").GetInt32().Should().Be(1);
        json.RootElement.GetProperty("source_key").GetString().Should().Be(MosmixEvidenceIndexTestData.KeyOf(MosmixEvidenceIndexTestData.SourceUri));
        json.RootElement.GetProperty("downloaded_at").GetString().Should().Be("2026-10-21T04:12:07");
    }

    [TestMethod]
    public async Task Write_ReplacesPreviousIndex()
    {
        var index1 = MosmixEvidenceIndexTestData.Index();
        await Write(index1);

        var index2 = MosmixEvidenceIndexTestData.Index(new Dictionary<string, IReadOnlyList<string>> { ["B456"] = new[] { "PPPP" } });
        await Write(index2);

        var read = await _subject.ReadAsync(CancellationToken.None);
        read.Should().NotBeNull();
        read!.Locations.Should().ContainSingle();
        read.Locations.Keys.Should().Contain("B456");
        read.Locations["B456"].Should().Equal("PPPP");

        Files().Should().ContainSingle();
    }

    [TestMethod]
    public async Task Write_ThrowsWhenFailedWriteKeepsPreviousIndex()
    {
        var index1 = MosmixEvidenceIndexTestData.Index();
        await Write(index1);

        await ShouldFail(() => Write(MosmixEvidenceIndexTestData.Index(MosmixEvidenceIndexTestData.ManyLocations(20001))));

        var read = await _subject.ReadAsync(CancellationToken.None);
        read.Should().NotBeNull();
        read!.Locations.Should().HaveCount(2);
        read.Locations.Keys.Should().Contain("X123A");
        read.Locations.Keys.Should().Contain("01234");

        Files().Should().ContainSingle();
    }

    [TestMethod]
    public async Task Write_ThrowsWhenDirectoryMissing()
    {
        var missingDir = Path.Combine(_directory, "missing");
        _subject = MosmixEvidenceIndexTestData.Cache(missingDir);
        await ShouldFail(() => Write(MosmixEvidenceIndexTestData.Index()));

        Directory.Exists(missingDir).Should().BeFalse();
    }

    [TestMethod]
    public async Task Write_ThrowsWhenBoundsExceeded_LocationCount()
    {
        await ShouldFail(() => Write(MosmixEvidenceIndexTestData.Index(MosmixEvidenceIndexTestData.ManyLocations(20001))));

        Files().Should().BeEmpty();
    }

    [TestMethod]
    public async Task Write_ThrowsWhenBoundsExceeded_ElementNameCount()
    {
        var locations = new Dictionary<string, IReadOnlyList<string>> { ["X123A"] = Enumerable.Range(0, 201).Select(i => "E" + i.ToString("D3")).ToArray() };
        await ShouldFail(() => Write(MosmixEvidenceIndexTestData.Index(locations)));

        Files().Should().BeEmpty();
    }

    [TestMethod]
    public async Task Write_ThrowsWhenBoundsExceeded_IndexFileSize()
    {
        await ShouldFail(() => Write(MosmixEvidenceIndexTestData.Index(MosmixEvidenceIndexTestData.ManyLocations(20000, 70))));

        Files().Should().BeEmpty();
    }

    [TestMethod]
    public async Task Write_AtLimits_Succeeds()
    {
        var locations = MosmixEvidenceIndexTestData.ManyLocations(19999);
        locations["X123A"] = Enumerable.Range(0, 200).Select(i => "E" + i.ToString("D3")).ToArray();
        await Write(MosmixEvidenceIndexTestData.Index(locations));

        var read = await _subject.ReadAsync(CancellationToken.None);
        read.Should().NotBeNull();
        read!.Locations.Should().HaveCount(20000);
        read.Locations["X123A"].Should().HaveCount(200);
    }

    [TestMethod]
    public async Task Write_ThrowsWhenContentInvalid()
    {
        // Location ID "a1"
        await ShouldFail(() => Write(MosmixEvidenceIndexTestData.Index() with { Locations = new Dictionary<string, IReadOnlyList<string>> { ["a1"] = new[] { "TTT" } } }));

        // Element name "T-T"
        await ShouldFail(() => Write(MosmixEvidenceIndexTestData.Index() with { Locations = new Dictionary<string, IReadOnlyList<string>> { ["X123A"] = new[] { "T-T" } } }));

        // Duplicate names
        await ShouldFail(() => Write(MosmixEvidenceIndexTestData.Index() with { Locations = new Dictionary<string, IReadOnlyList<string>> { ["X123A"] = new[] { "TTT", "TTT" } } }));

        // Local time
        await ShouldFail(() => Write(MosmixEvidenceIndexTestData.Index() with { DownloadedAtUtc = DateTime.SpecifyKind(MosmixEvidenceIndexTestData.DownloadedAt, DateTimeKind.Local) }));

        // Fractional seconds
        await ShouldFail(() => Write(MosmixEvidenceIndexTestData.Index() with { IssuedAtUtc = MosmixEvidenceIndexTestData.IssuedAt.AddMilliseconds(500) }));

        // Uppercase SHA256
        await ShouldFail(() => Write(MosmixEvidenceIndexTestData.Index() with { FileSha256 = MosmixEvidenceIndexTestData.FileSha256.ToUpperInvariant() }));

        // Negative file size
        await ShouldFail(() => Write(MosmixEvidenceIndexTestData.Index() with { FileSize = -1 }));

        // Empty source URI
        await ShouldFail(() => Write(MosmixEvidenceIndexTestData.Index() with { SourceUri = "" }));

        Files().Should().BeEmpty();
    }

    [TestMethod]
    public async Task Write_NullIndex_ThrowsArgumentNullException()
    {
        await FluentActions.Awaiting(() => _subject.WriteAsync(null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>();
    }
}
