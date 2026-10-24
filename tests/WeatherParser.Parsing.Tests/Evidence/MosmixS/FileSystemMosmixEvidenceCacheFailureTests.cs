using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Tests.Evidence.MosmixS;

[TestClass]
public sealed class FileSystemMosmixEvidenceCacheFailureTests
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

    [TestMethod]
    public async Task Read_ReturnsNull_WhenRootIsNotAnObject()
    {
        // Arrange
        MosmixEvidenceIndexTestData.WriteRaw(_directory, "[]");

        // Act
        var result = await _subject.ReadAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Read_ReturnsNull_WhenDirectoryIsMissing()
    {
        // Arrange
        var missingCache = MosmixEvidenceIndexTestData.Cache(Path.Combine(_directory, "missing"));

        // Act
        var result = await missingCache.ReadAsync(CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Write_ThrowsWhenTargetPathIsADirectory_AndLeavesNoTemporaryFile()
    {
        // Arrange
        var indexPath = MosmixEvidenceIndexTestData.IndexPath(_directory);
        Directory.CreateDirectory(indexPath);

        // Act
        var act = FluentActions.Awaiting(() => _subject.WriteAsync(MosmixEvidenceIndexTestData.Index(), CancellationToken.None));

        // Assert
        (await FluentActions.Awaiting(() => _subject.WriteAsync(MosmixEvidenceIndexTestData.Index(), CancellationToken.None))
            .Should().ThrowAsync<EvidenceCacheException>())
            .Which.ErrorId.Should().Be(EvidenceCacheException.CacheFailedErrorId);

        // Verify no temporary file was left behind
        Directory.GetFiles(_directory).Should().BeEmpty();
    }

    [TestMethod]
    public async Task Write_ThrowsWhenLocationsOrNamesAreNull()
    {
        // Act
        var act1 = FluentActions.Awaiting(() => _subject.WriteAsync(MosmixEvidenceIndexTestData.Index() with { Locations = null! }, CancellationToken.None));

        // Assert
        (await FluentActions.Awaiting(() => _subject.WriteAsync(MosmixEvidenceIndexTestData.Index() with { Locations = null! }, CancellationToken.None))
            .Should().ThrowAsync<EvidenceCacheException>())
            .Which.ErrorId.Should().Be(EvidenceCacheException.CacheFailedErrorId);

        // Act
        var act2 = FluentActions.Awaiting(() => _subject.WriteAsync(MosmixEvidenceIndexTestData.Index(new Dictionary<string, IReadOnlyList<string>> { ["X123A"] = null! }), CancellationToken.None));

        // Assert
        (await FluentActions.Awaiting(() => _subject.WriteAsync(MosmixEvidenceIndexTestData.Index(new Dictionary<string, IReadOnlyList<string>> { ["X123A"] = null! }), CancellationToken.None))
            .Should().ThrowAsync<EvidenceCacheException>())
            .Which.ErrorId.Should().Be(EvidenceCacheException.CacheFailedErrorId);
    }
}
