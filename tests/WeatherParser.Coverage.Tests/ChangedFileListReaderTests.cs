using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class ChangedFileListReaderTests
{
    private Mock<IFileReader> _mockFileReader = null!;
    private ChangedFileListReader _reader = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockFileReader = new MockRepository(MockBehavior.Strict).Create<IFileReader>();
        _reader = new ChangedFileListReader(_mockFileReader.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockFileReader.VerifyAll();
    }

    [TestMethod]
    [TestCategory("T-P1-23")]
    public async Task ReadAsync_NullPath_ThrowsCoverageInputException()
    {
        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync(null!, default));
    }

    [TestMethod]
    [TestCategory("T-P1-23")]
    public async Task ReadAsync_EmptyPath_ThrowsCoverageInputException()
    {
        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync(string.Empty, default));
    }

    [TestMethod]
    [TestCategory("T-P1-23")]
    public async Task ReadAsync_NonExistingFile_ThrowsCoverageInputException()
    {
        _mockFileReader.Setup(fr => fr.Exists(It.IsAny<string>())).Returns(false);

        await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _reader.ReadAsync("changes.txt", default));
    }

    [TestMethod]
    [TestCategory("T-P1-23")]
    public async Task ReadAsync_ExistingFile_EmptyContent_ReturnsEmptyList()
    {
        _mockFileReader.Setup(fr => fr.Exists("changes.txt")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("changes.txt", default)).Returns(Task.FromResult(string.Empty));

        var result = await _reader.ReadAsync("changes.txt", default);

        Assert.IsNotNull(result.Paths);
        Assert.AreEqual(0, result.Paths.Count);
    }

    [TestMethod]
    [TestCategory("T-P1-23")]
    public async Task ReadAsync_ExistingFile_ValidContent_ReturnsNormalizedPaths()
    {
        const string content = """
            README.md
            src/WeatherParser.Parsing/Parser.cs

            tests/WeatherParser.Parsing.Tests/ParserTests.cs
            """;

        _mockFileReader.Setup(fr => fr.Exists("changes.txt")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("changes.txt", default)).Returns(Task.FromResult(content));

        var result = await _reader.ReadAsync("changes.txt", default);

        Assert.IsNotNull(result.Paths);
        Assert.AreEqual(3, result.Paths.Count);
        Assert.AreEqual("README.md", result.Paths[0]);
        Assert.AreEqual("src/WeatherParser.Parsing/Parser.cs", result.Paths[1]);
        Assert.AreEqual("tests/WeatherParser.Parsing.Tests/ParserTests.cs", result.Paths[2]);
    }

    [TestMethod]
    [TestCategory("T-P1-23")]
    public async Task ReadAsync_ExistingFile_BackslashPaths_NormalizedToForwardSlash()
    {
        const string content = """
            src\WeatherParser.Parsing\Parser.cs
            tests\WeatherParser.Parsing.Tests\ParserTests.cs
            """;

        _mockFileReader.Setup(fr => fr.Exists("changes.txt")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("changes.txt", default)).Returns(Task.FromResult(content));

        var result = await _reader.ReadAsync("changes.txt", default);

        Assert.IsNotNull(result.Paths);
        Assert.AreEqual(2, result.Paths.Count);
        Assert.AreEqual("src/WeatherParser.Parsing/Parser.cs", result.Paths[0]);
        Assert.AreEqual("tests/WeatherParser.Parsing.Tests/ParserTests.cs", result.Paths[1]);
    }

    [TestMethod]
    [TestCategory("T-P1-23")]
    public async Task ReadAsync_ExistingFile_WhitespaceAndEmptyLines_Skipped()
    {
        const string content = """
            README.md

            src/WeatherParser.Parsing/Parser.cs

            tests/WeatherParser.Parsing.Tests/ParserTests.cs

            """;

        _mockFileReader.Setup(fr => fr.Exists("changes.txt")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("changes.txt", default)).Returns(Task.FromResult(content));

        var result = await _reader.ReadAsync("changes.txt", default);

        Assert.IsNotNull(result.Paths);
        Assert.AreEqual(3, result.Paths.Count);
        Assert.AreEqual("README.md", result.Paths[0]);
        Assert.AreEqual("src/WeatherParser.Parsing/Parser.cs", result.Paths[1]);
        Assert.AreEqual("tests/WeatherParser.Parsing.Tests/ParserTests.cs", result.Paths[2]);
    }
}
