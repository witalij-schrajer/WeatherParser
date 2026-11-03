using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace WeatherParser.Coverage.Runner.Tests;

[TestClass]
[TestCategory("T-P1-24")]
public sealed class BaselineWriterTests
{
    private MockRepository _mockRepository = null!;
    private Mock<ICoverageReportReader> _reportReaderMock = null!;
    private Mock<IProvenanceAggregator> _aggregatorMock = null!;
    private Mock<IFileWriter> _fileWriterMock = null!;
    private BaselineWriter _writer = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);
        _reportReaderMock = _mockRepository.Create<ICoverageReportReader>();
        _aggregatorMock = _mockRepository.Create<IProvenanceAggregator>();
        _fileWriterMock = _mockRepository.Create<IFileWriter>();
        _writer = new BaselineWriter(_reportReaderMock.Object, _aggregatorMock.Object, _fileWriterMock.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockRepository.VerifyAll();
    }

    [TestMethod]
    public async Task WriteAsync_WritesDocumentedBaselineFormat()
    {
        // Arrange
        var command = new WriteBaselineCommand(
            "report.xml",
            "abc123",
            "ToolName",
            "1.0.0",
            ["exclude1", "exclude2"],
            "output.json");

        var report = new CoverageReport(
            [
                new ModuleCoverage("Module1", "id1", "path1", new BlockCounters(100, 50))
            ],
            "native-xml");

        var aggregated = new BlockCounters(100, 50);

        _reportReaderMock.Setup(r => r.ReadAsync(command.ReportPath, CancellationToken.None))
            .ReturnsAsync(report);
        _aggregatorMock.Setup(a => a.Aggregate(report, command.Exclusions))
            .Returns(aggregated);

        string writtenJson = null!;
        _fileWriterMock
            .Setup(f => f.WriteAllTextAsync(command.OutputPath, It.IsAny<string>(), CancellationToken.None))
            .Callback<string, string, CancellationToken>((path, contents, _) => writtenJson = contents)
            .Returns(Task.CompletedTask);

        // Act
        await _writer.WriteAsync(command, CancellationToken.None);

        // Assert
        var doc = JsonDocument.Parse(writtenJson);
        doc.RootElement.GetProperty("commitId").GetString().Should().Be("abc123");
        doc.RootElement.GetProperty("toolName").GetString().Should().Be("ToolName");
        doc.RootElement.GetProperty("toolVersion").GetString().Should().Be("1.0.0");
        doc.RootElement.GetProperty("format").GetString().Should().Be("native-xml");

        var exclusions = doc.RootElement.GetProperty("exclusions").EnumerateArray().Select(e => e.GetString()!).ToArray();
        exclusions.Should().ContainInOrder(["exclude1", "exclude2"]);

        doc.RootElement.GetProperty("blocksCovered").GetInt64().Should().Be(100);
        doc.RootElement.GetProperty("blocksNotCovered").GetInt64().Should().Be(50);
    }

    [TestMethod]
    public async Task WriteAsync_ReaderThrowsCoverageInputException_Propagates()
    {
        // Arrange
        var command = new WriteBaselineCommand(
            "report.xml",
            "abc123",
            "ToolName",
            "1.0.0",
            [],
            "output.json");

        _reportReaderMock.Setup(r => r.ReadAsync(command.ReportPath, CancellationToken.None))
            .ThrowsAsync(new CoverageInputException("Report is invalid."));

        // Act & Assert
        await FluentActions.Invoking(async () => await _writer.WriteAsync(command, CancellationToken.None))
            .Should().ThrowExactlyAsync<CoverageInputException>()
            .WithMessage("Report is invalid.");

        _fileWriterMock.Verify(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
