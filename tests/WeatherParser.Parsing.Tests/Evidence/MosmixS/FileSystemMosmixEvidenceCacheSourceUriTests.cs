using System;
using System.IO;
using System.Text.Json.Nodes;
using FluentAssertions;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Tests.Evidence.MosmixS;

[TestClass]
public sealed class FileSystemMosmixEvidenceCacheSourceUriTests
{
    private string _directory = null!;
    private IMosmixEvidenceCache _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "wp-mosmix-cache-" + Guid.NewGuid().ToString("N"));
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

    [DataRow("https://user:password@example.org/mosmix/MOSMIX_S_LATEST_240.kmz")]
    [DataRow("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz?token=secret")]
    [DataRow("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz#part")]
    [DataRow("mosmix/MOSMIX_S_LATEST_240.kmz")]
    [DataRow("ftp://example.org/mosmix/MOSMIX_S_LATEST_240.kmz")]
    [TestMethod]
    public async Task Write_ThrowsWhenSourceUriIsNotSanitized(string sourceUri)
    {
        var index = MosmixEvidenceIndexTestData.Index() with { SourceUri = sourceUri };

        Func<Task> act = () => _subject.WriteAsync(index, CancellationToken.None);

        var exception = await FluentActions.Awaiting(act).Should().ThrowAsync<EvidenceCacheException>();
        exception.Which.ErrorId.Should().Be(EvidenceCacheException.CacheFailedErrorId);
        Directory.GetFiles(_directory).Should().BeEmpty();
    }

    [DataRow("https://user:password@example.org/mosmix/MOSMIX_S_LATEST_240.kmz")]
    [DataRow("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz?token=secret")]
    [DataRow("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz#part")]
    [DataRow("mosmix/MOSMIX_S_LATEST_240.kmz")]
    [DataRow("ftp://example.org/mosmix/MOSMIX_S_LATEST_240.kmz")]
    [TestMethod]
    public async Task Read_ReturnsNull_WhenStoredSourceUriIsNotSanitized(string sourceUri)
    {
        var json = MosmixEvidenceIndexTestData.ValidJson();
        json["source_uri"] = sourceUri;
        MosmixEvidenceIndexTestData.WriteRaw(_directory, json.ToJsonString());

        var result = await _subject.ReadAsync(CancellationToken.None);

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Write_AcceptsSanitizedSourceUri()
    {
        var index = MosmixEvidenceIndexTestData.Index();

        await _subject.WriteAsync(index, CancellationToken.None);

        var result = await _subject.ReadAsync(CancellationToken.None);
        result.Should().NotBeNull();
        result!.SourceUri.Should().Be(MosmixEvidenceIndexTestData.SourceUri);
    }
}
