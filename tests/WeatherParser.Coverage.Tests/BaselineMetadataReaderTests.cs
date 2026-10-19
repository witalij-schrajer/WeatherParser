using FluentAssertions;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class BaselineMetadataReaderTests
{
    private Mock<IFileReader> _mockFileReader = null!;
    private BaselineMetadataReader _reader = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockFileReader = new MockRepository(MockBehavior.Strict).Create<IFileReader>();
        _reader = new BaselineMetadataReader(_mockFileReader.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockFileReader.VerifyAll();
    }

    [TestMethod]
    [TestCategory("T-P1-14a")]
    public async Task ReadAsync_NullPath_ReturnsNull()
    {
        var result = await _reader.ReadAsync(null, default);
        Assert.IsNull(result);
    }

    [TestMethod]
    [TestCategory("T-P1-14a")]
    public async Task ReadAsync_EmptyPath_ReturnsNull()
    {
        var result = await _reader.ReadAsync(string.Empty, default);
        Assert.IsNull(result);
    }

    [TestMethod]
    [TestCategory("T-P1-14a")]
    public async Task ReadAsync_NonExistingFile_ReturnsNull()
    {
        _mockFileReader.Setup(fr => fr.Exists(It.IsAny<string>())).Returns(false);

        var result = await _reader.ReadAsync("baseline.json", default);
        Assert.IsNull(result);
    }

    [TestMethod]
    [TestCategory("T-P1-14a")]
    public async Task ReadAsync_ExistingFile_ValidJson_ReturnsBaseline()
    {
        const string json = """
            {
                "commitId": "6a68906592d1b5a5f85f3751659f509d22bd8463",
                "toolName": "Microsoft.CodeCoverage",
                "toolVersion": "18.11.0",
                "format": "native-xml",
                "exclusions": ["*.Tests.dll"],
                "blocksCovered": 57,
                "blocksNotCovered": 33
            }
            """;

        _mockFileReader.Setup(fr => fr.Exists("baseline.json")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("baseline.json", default)).Returns(Task.FromResult(json));

        var result = await _reader.ReadAsync("baseline.json", default);

        Assert.IsNotNull(result);
        Assert.AreEqual("6a68906592d1b5a5f85f3751659f509d22bd8463", result.CommitId);
        Assert.AreEqual("Microsoft.CodeCoverage", result.ToolName);
        Assert.AreEqual("18.11.0", result.ToolVersion);
        Assert.AreEqual("native-xml", result.Format);
        Assert.IsNotNull(result.Exclusions);
        Assert.AreEqual(1, result.Exclusions.Count);
        Assert.AreEqual("*.Tests.dll", result.Exclusions[0]);
        Assert.AreEqual(57, result.Blocks.Covered);
        Assert.AreEqual(33, result.Blocks.NotCovered);
    }

    [TestMethod]
    [TestCategory("T-P1-14a")]
    public async Task ReadAsync_ExistingFile_MalformedJson_ThrowsCoverageInputException()
    {
        const string json = "{ \"commitId\": \"abc123\", \"toolName\": \"Microsoft.CodeCoverage\", \"toolVersion\": \"1.0.0\", \"format\": \"native-xml\", \"exclusions\": [\"*.Tests.dll\"], \"blocksCovered\": 57, \"blocksNotCovered\": 33";

        _mockFileReader.Setup(fr => fr.Exists("baseline.json")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("baseline.json", default)).Returns(Task.FromResult(json));

        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync("baseline.json", default));
    }

    [TestMethod]
    [TestCategory("T-P1-14a")]
    public async Task ReadAsync_ExistingFile_MissingCommitId_ThrowsCoverageInputException()
    {
        const string json = """
            {
                "toolName": "Microsoft.CodeCoverage",
                "toolVersion": "1.0.0",
                "format": "native-xml",
                "exclusions": ["*.Tests.dll"],
                "blocksCovered": 57,
                "blocksNotCovered": 33
            }
            """;

        _mockFileReader.Setup(fr => fr.Exists("baseline.json")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("baseline.json", default)).Returns(Task.FromResult(json));

        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync("baseline.json", default));
    }

    [TestMethod]
    [TestCategory("T-P1-14a")]
    public async Task ReadAsync_ExistingFile_MissingToolName_ThrowsCoverageInputException()
    {
        const string json = """
            {
                "commitId": "6a68906592d1b5a5f85f3751659f509d22bd8463",
                "toolVersion": "1.0.0",
                "format": "native-xml",
                "exclusions": ["*.Tests.dll"],
                "blocksCovered": 57,
                "blocksNotCovered": 33
            }
            """;

        _mockFileReader.Setup(fr => fr.Exists("baseline.json")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("baseline.json", default)).Returns(Task.FromResult(json));

        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync("baseline.json", default));
    }

    [TestMethod]
    [TestCategory("T-P1-14a")]
    public async Task ReadAsync_ExistingFile_MissingToolVersion_ThrowsCoverageInputException()
    {
        const string json = """
            {
                "commitId": "6a68906592d1b5a5f85f3751659f509d22bd8463",
                "toolName": "Microsoft.CodeCoverage",
                "format": "native-xml",
                "exclusions": ["*.Tests.dll"],
                "blocksCovered": 57,
                "blocksNotCovered": 33
            }
            """;

        _mockFileReader.Setup(fr => fr.Exists("baseline.json")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("baseline.json", default)).Returns(Task.FromResult(json));

        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync("baseline.json", default));
    }

    [TestMethod]
    [TestCategory("T-P1-14a")]
    public async Task ReadAsync_ExistingFile_MissingFormat_ThrowsCoverageInputException()
    {
        const string json = """
            {
                "commitId": "6a68906592d1b5a5f85f3751659f509d22bd8463",
                "toolName": "Microsoft.CodeCoverage",
                "toolVersion": "1.0.0",
                "exclusions": ["*.Tests.dll"],
                "blocksCovered": 57,
                "blocksNotCovered": 33
            }
            """;

        _mockFileReader.Setup(fr => fr.Exists("baseline.json")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("baseline.json", default)).Returns(Task.FromResult(json));

        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync("baseline.json", default));
    }

    [TestMethod]
    [TestCategory("T-P1-14a")]
    public async Task ReadAsync_ExistingFile_MissingExclusions_ThrowsCoverageInputException()
    {
        const string json = """
            {
                "commitId": "6a68906592d1b5a5f85f3751659f509d22bd8463",
                "toolName": "Microsoft.CodeCoverage",
                "toolVersion": "1.0.0",
                "format": "native-xml",
                "blocksCovered": 57,
                "blocksNotCovered": 33
            }
            """;

        _mockFileReader.Setup(fr => fr.Exists("baseline.json")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("baseline.json", default)).Returns(Task.FromResult(json));

        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync("baseline.json", default));
    }

    [TestMethod]
    [TestCategory("T-P1-14a")]
    public async Task ReadAsync_ExistingFile_NegativeBlocks_ThrowsCoverageInputException()
    {
        const string json = """
            {
                "commitId": "6a68906592d1b5a5f85f3751659f509d22bd8463",
                "toolName": "Microsoft.CodeCoverage",
                "toolVersion": "1.0.0",
                "format": "native-xml",
                "exclusions": ["*.Tests.dll"],
                "blocksCovered": -5,
                "blocksNotCovered": 33
            }
            """;

        _mockFileReader.Setup(fr => fr.Exists("baseline.json")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("baseline.json", default)).Returns(Task.FromResult(json));

        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync("baseline.json", default));
    }

    [TestMethod]
    [TestCategory("T-P1-14a")]
    public async Task ReadAsync_ExistingFile_NegativeBlocksNotCovered_ThrowsCoverageInputException()
    {
        const string json = """
            {
                "commitId": "6a68906592d1b5a5f85f3751659f509d22bd8463",
                "toolName": "Microsoft.CodeCoverage",
                "toolVersion": "1.0.0",
                "format": "native-xml",
                "exclusions": ["*.Tests.dll"],
                "blocksCovered": 57,
                "blocksNotCovered": -33
            }
            """;

        _mockFileReader.Setup(fr => fr.Exists("baseline.json")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("baseline.json", default)).Returns(Task.FromResult(json));

        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync("baseline.json", default));
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    public async Task ReadAsync_ExistingFile_MalformedBaselineJson_ThrowsCoverageInputException()
    {
        var path = "baseline.json";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline-malformed.json"));
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, default)).Returns(Task.FromResult(json));

        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync(path, default));
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    public async Task ReadAsync_ExistingFile_MissingCounters_ThrowsCoverageInputException()
    {
        var path = "baseline.json";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline-missing-counters.json"));
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, default)).Returns(Task.FromResult(json));

        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync(path, default));
    }

    [TestMethod]
    [TestCategory("T-P1-15")]
    public async Task ReadAsync_StaleCommitFixture_ReadsCommitUnchanged()
    {
        var path = "baseline.json";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline-stale-commit.json"));
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, default)).Returns(Task.FromResult(json));

        var result = await _reader.ReadAsync(path, default);

        result.Should().NotBeNull();
        result!.CommitId.Should().Be("1962d6b437cc52186b1fb0f7bbdc061c22ba97a2");
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    public async Task ReadAsync_OtherToolVersionFixture_ReadsToolVersionUnchanged()
    {
        var path = "baseline.json";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline-other-tool-version.json"));
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, default)).Returns(Task.FromResult(json));

        var result = await _reader.ReadAsync(path, default);

        result.Should().NotBeNull();
        result!.ToolVersion.Should().Be("17.0.0");
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    public async Task ReadAsync_OtherExclusionsFixture_ReadsBothExclusions()
    {
        var path = "baseline.json";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline-other-exclusions.json"));
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, default)).Returns(Task.FromResult(json));

        var result = await _reader.ReadAsync(path, default);

        result.Should().NotBeNull();
        result!.Exclusions.Should().Equal("*.Tests.dll", "WeatherParser.Parsing.dll");
    }

    [TestMethod]
    [TestCategory("T-P1-24")]
    public async Task ReadAsync_ExistingFile_ValidBaseline_ReturnsBaseline()
    {
        var path = "baseline.json";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline-valid.json"));
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, default)).Returns(Task.FromResult(json));

        var result = await _reader.ReadAsync(path, default);

        Assert.IsNotNull(result);
        Assert.AreEqual("6a68906592d1b5a5f85f3751659f509d22bd8463", result.CommitId);
        Assert.AreEqual("Microsoft.CodeCoverage", result.ToolName);
        Assert.AreEqual("18.11.0", result.ToolVersion);
        Assert.AreEqual("native-xml", result.Format);
        Assert.IsNotNull(result.Exclusions);
        Assert.AreEqual(1, result.Exclusions.Count);
        Assert.AreEqual("*.Tests.dll", result.Exclusions[0]);
        Assert.AreEqual(57, result.Blocks.Covered);
        Assert.AreEqual(33, result.Blocks.NotCovered);
    }

    [TestMethod]
    [TestCategory("T-P1-13")]
    [TestCategory("T-P1-14")]
    public async Task ReadAsync_ExistingFile_CountersSumExceedsInt64_ThrowsCoverageInputException()
    {
        const string json = """
            {
                "commitId": "6a68906592d1b5a5f85f3751659f509d22bd8463",
                "toolName": "Microsoft.CodeCoverage",
                "toolVersion": "18.11.0",
                "format": "native-xml",
                "exclusions": ["*.Tests.dll"],
                "blocksCovered": 9223372036854775807,
                "blocksNotCovered": 9223372036854775807
            }
            """;

        _mockFileReader.Setup(fr => fr.Exists("baseline.json")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("baseline.json", default)).Returns(Task.FromResult(json));

        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync("baseline.json", default));
    }
}
