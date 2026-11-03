using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Forecasts.Mosmix;

namespace WeatherParser.Parsing.Tests.Live;

[TestClass]
public class ResourceCheckRunnerTests
{
    private string _tempDir = null!;
    private string? _repoRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _repoRoot = OutputDirectoryGuard.RepositoryRoot();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [DataRow("generate", "generate")]
    [DataRow("measure", "measure")]
    [DataRow(null, null)]
    [DataRow("GENERATE", null)]
    [DataRow("", null)]
    [TestMethod]
    public async Task Mode_ReadsVariable(string? value, string? expected)
    {
        var result = ResourceCheckRunner.Mode(name => name == ResourceCheckRunner.Variable ? value : null);
        result.Should().Be(expected);
    }

    [TestMethod]
    public async Task Directory_ReadsVariable()
    {
        var result = ResourceCheckRunner.Directory(name => name == ResourceCheckRunner.DirectoryVariable ? "some/dir" : null);
        result.Should().Be("some/dir");
    }

    [TestMethod]
    public async Task GenerateAsync_WritesFileAndCounts()
    {
        var path = await ResourceCheckRunner.GenerateAsync(_tempDir, 20, 5, 6, CancellationToken.None);
        path.Should().Be(Path.Combine(_tempDir, ResourceCheckRunner.FileName));

        File.Exists(path).Should().BeTrue();
        using var archive = new ZipArchive(File.OpenRead(path), ZipArchiveMode.Read);
        archive.Entries.Should().HaveCount(1);
        archive.Entries[0].FullName.Should().Be(MosmixForecastKmzBuilder.AllStationsMemberName);

        var countsPath = Path.Combine(_tempDir, $"{ResourceCheckRunner.FileName}.counts");
        File.Exists(countsPath).Should().BeTrue();
        var counts = File.ReadAllText(countsPath);
        counts.Should().Contain("placemarks=20");
        counts.Should().Contain("elements=5");
        counts.Should().Contain("steps=6");

        // Verify the file can be parsed
        var reader = new MosmixForecastDocumentReader();
        await using var kmz = File.OpenRead(path);
        var doc = await reader.ReadAllStationsAsync(kmz, ResourceCheckRunner.MosmixId, CancellationToken.None);
        doc.Timeline.Count.Should().Be(6);
        doc.Series.Count.Should().Be(5); // TTT + E0..E3
    }

    [TestMethod]
    public async Task GenerateAsync_NeverWritesIntoTheRepository()
    {
        var path = await ResourceCheckRunner.GenerateAsync(_tempDir, 20, 5, 6, CancellationToken.None);
        path.Should().StartWith(_tempDir);

        var repoPath = Path.Combine(AppContext.BaseDirectory, ResourceCheckRunner.FileName);
        File.Exists(repoPath).Should().BeFalse();

        var currentPath = Path.Combine(Directory.GetCurrentDirectory(), ResourceCheckRunner.FileName);
        File.Exists(currentPath).Should().BeFalse();
    }

    [TestMethod]
    public async Task GenerateAsync_RejectsRepositoryDirectory()
    {
        if (_repoRoot is null)
        {
            Assert.Inconclusive("Repository root not detected");
        }

        var directory = Path.Combine(_repoRoot, "tests");
        Func<Task> act = () => ResourceCheckRunner.GenerateAsync(directory, 2, 2, 2, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();

        File.Exists(Path.Combine(directory, ResourceCheckRunner.FileName)).Should().BeFalse();
    }

    [TestMethod]
    public async Task MeasureAsync_MissingFile_ThrowsFileNotFound()
    {
        Func<Task> act = () => ResourceCheckRunner.MeasureAsync(_tempDir, TimeProvider.System, CancellationToken.None);
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [TestMethod]
    public async Task MeasureAsync_SmallFile_ParsesAndRecords()
    {
        await ResourceCheckRunner.GenerateAsync(_tempDir, 20, 5, 6, CancellationToken.None);

        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 26, 16, 16, 30, TimeSpan.Zero));
        var result = await ResourceCheckRunner.MeasureAsync(_tempDir, clock, CancellationToken.None, startPeakLimitBytes: long.MaxValue);

        result.FileBytes.Should().BeGreaterThan(0);
        result.Placemarks.Should().Be(20);
        result.Elements.Should().Be(5);
        result.Steps.Should().Be(6);
        result.MosmixId.Should().Be(ResourceCheckRunner.MosmixId);
        result.ParseSteps.Should().Be(6);
        result.Freshness.Should().Be(MosmixRunFreshness.Fresh);
        result.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        result.WorkingSetBeforeBytes.Should().BeGreaterThan(0);
        result.PeakWorkingSetBytes.Should().BeGreaterThanOrEqualTo(result.WorkingSetBeforeBytes);
        result.WithinDeadline.Should().BeTrue();
    }

    [TestMethod]
    public async Task MeasureAsync_RefusesWhenStartPeakAboveLimit()
    {
        await ResourceCheckRunner.GenerateAsync(_tempDir, 20, 5, 6, CancellationToken.None);

        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 26, 16, 16, 30, TimeSpan.Zero));
        Func<Task> act = () => ResourceCheckRunner.MeasureAsync(_tempDir, clock, CancellationToken.None, startPeakLimitBytes: 1);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*isolation limit*");
    }

    [TestMethod]
    public async Task MeasureAsync_RecordsStartPeak()
    {
        await ResourceCheckRunner.GenerateAsync(_tempDir, 20, 5, 6, CancellationToken.None);

        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 26, 16, 16, 30, TimeSpan.Zero));
        var result = await ResourceCheckRunner.MeasureAsync(_tempDir, clock, CancellationToken.None, startPeakLimitBytes: long.MaxValue);

        result.StartPeakWorkingSetBytes.Should().BeGreaterThan(0);
        result.StartPeakWorkingSetBytes.Should().BeLessThanOrEqualTo(result.PeakWorkingSetBytes);
    }

    [TestMethod]
    public async Task WriteAsync_WritesEveryLine()
    {
        var path = await ResourceCheckRunner.GenerateAsync(_tempDir, 20, 5, 6, CancellationToken.None);
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 26, 16, 16, 30, TimeSpan.Zero));
        var result = await ResourceCheckRunner.MeasureAsync(_tempDir, clock, CancellationToken.None, startPeakLimitBytes: long.MaxValue);

        await ResourceCheckRunner.WriteAsync(result, _tempDir, CancellationToken.None);

        var resultPath = Path.Combine(_tempDir, ResourceCheckRunner.ResultFileName);
        File.Exists(resultPath).Should().BeTrue();

        var lines = File.ReadAllLines(resultPath);
        lines.Should().ContainSingle(l => l.StartsWith("file_bytes="));
        lines.Should().ContainSingle(l => l.StartsWith("placemarks=20"));
        lines.Should().ContainSingle(l => l.StartsWith("elements=5"));
        lines.Should().ContainSingle(l => l.StartsWith("steps=6"));
        lines.Should().ContainSingle(l => l.StartsWith("mosmix_id=10637"));
        lines.Should().ContainSingle(l => l.StartsWith("parse_steps=6"));
        lines.Should().ContainSingle(l => l.StartsWith("freshness=Fresh"));
        lines.Should().ContainSingle(l => l.StartsWith("duration_ms="));
        lines.Should().ContainSingle(l => l.StartsWith("working_set_before_bytes="));
        lines.Should().ContainSingle(l => l.StartsWith("peak_working_set_bytes="));
        lines.Should().ContainSingle(l => l.StartsWith("start_peak_working_set_bytes="));
        lines.Should().ContainSingle(l => l.StartsWith("deadline_threshold_ms=300000"));
        lines.Should().ContainSingle(l => l.StartsWith("peak_threshold_bytes=268435456"));
        lines.Should().ContainSingle(l => l.StartsWith("within_deadline=true"));
        lines.Should().ContainSingle(l => l.StartsWith("within_peak="));
    }

    [TestMethod]
    public async Task OutputDirectoryGuard_AllowsTempAndArtifacts()
    {
        var fullPath = Path.GetFullPath(_tempDir);
        var result = OutputDirectoryGuard.EnsureAllowed(_tempDir);
        result.Should().Be(fullPath);

        if (_repoRoot is not null)
        {
            var artifactsPath = Path.Combine(_repoRoot, "artifacts", "ci", "x");
            var fullArtifactsPath = Path.GetFullPath(artifactsPath);
            result = OutputDirectoryGuard.EnsureAllowed(artifactsPath);
            result.Should().Be(fullArtifactsPath);
        }
    }

    [TestMethod]
    public async Task OutputDirectoryGuard_RejectsRepositoryRootAndTraversal()
    {
        if (_repoRoot is null)
        {
            Assert.Inconclusive("Repository root not detected");
        }

        var fullPath = Path.GetFullPath(_repoRoot);
        Action act = () => OutputDirectoryGuard.EnsureAllowed(_repoRoot);
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{fullPath}*");

        act = () => OutputDirectoryGuard.EnsureAllowed(Path.Combine(_repoRoot, "docs"));
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{Path.Combine(fullPath, "docs")}*");

        act = () => OutputDirectoryGuard.EnsureAllowed(Path.Combine(_repoRoot, "artifacts", "..", "src"));
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{Path.Combine(fullPath, "src")}*");
        await Task.CompletedTask;
    }

    [TestMethod]
    public void OutputDirectoryGuard_RejectsDotDotPrefixedChild()
    {
        if (_repoRoot is null)
        {
            Assert.Inconclusive("Repository root not detected");
        }

        var child = Path.Combine(_repoRoot, "..private");
        Action act = () => OutputDirectoryGuard.EnsureAllowed(child);
        act.Should().Throw<InvalidOperationException>().WithMessage($"*{Path.GetFullPath(child)}*");
    }

    [TestMethod]
    public void OutputDirectoryGuard_ArtifactsCaseFollowsTheFileSystem()
    {
        if (_repoRoot is null)
        {
            Assert.Inconclusive("Repository root not detected");
        }

        var upperCase = Path.Combine(_repoRoot, "Artifacts", "ci", "x");
        Action act = () => OutputDirectoryGuard.EnsureAllowed(upperCase);
        if (OperatingSystem.IsWindows())
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<InvalidOperationException>();
        }
    }

    [TestMethod]
    public async Task Thresholds_MatchTheApprovedValues()
    {
        ResourceCheckRunner.DeadlineThreshold.Should().Be(TimeSpan.FromMinutes(5));
        ResourceCheckRunner.PeakWorkingSetThresholdBytes.Should().Be(256L * 1024 * 1024);
        ResourceCheckRunner.RecordedPlacemarks.Should().Be(5648);
        ResourceCheckRunner.RecordedElements.Should().Be(40);
        ResourceCheckRunner.RecordedSteps.Should().Be(240);
        ResourceCheckRunner.StartPeakLimitBytes.Should().Be(128L * 1024 * 1024);
    }

    [TestMethod]
    public async Task MeasureAsync_RejectsFileThatDoesNotMatchCompanion()
    {
        await ResourceCheckRunner.GenerateAsync(_tempDir, 20, 5, 6, CancellationToken.None);

        var otherTempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(otherTempDir);
        try
        {
            var pathOther = await ResourceCheckRunner.GenerateAsync(otherTempDir, 3, 2, 4, CancellationToken.None);
            var pathToOverwrite = Path.Combine(_tempDir, ResourceCheckRunner.FileName);
            File.Copy(pathOther, pathToOverwrite, overwrite: true);

            var clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 26, 16, 16, 30, TimeSpan.Zero));
            Func<Task> act = () => ResourceCheckRunner.MeasureAsync(_tempDir, clock, CancellationToken.None, startPeakLimitBytes: long.MaxValue);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*does not match its companion*");
        }
        finally
        {
            if (Directory.Exists(otherTempDir))
            {
                Directory.Delete(otherTempDir, true);
            }
        }
    }

    [TestMethod]
    public async Task GenerateAsync_WritesTheFileHashIntoTheCompanion()
    {
        var path = await ResourceCheckRunner.GenerateAsync(_tempDir, 2, 2, 2, CancellationToken.None);

        var countsPath = Path.Combine(_tempDir, $"{ResourceCheckRunner.FileName}.counts");
        File.Exists(countsPath).Should().BeTrue();
        var counts = File.ReadAllText(countsPath);
        counts.Should().Contain("sha256=");

        var hash = SHA256.HashData(File.ReadAllBytes(path));
        var expectedHashHex = Convert.ToHexStringLower(hash);
        counts.Should().Contain($"sha256={expectedHashHex}");
    }
}
