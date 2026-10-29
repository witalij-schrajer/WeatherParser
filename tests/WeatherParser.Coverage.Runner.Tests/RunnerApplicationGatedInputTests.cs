using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Coverage;

namespace WeatherParser.Coverage.Runner.Tests;

/// <summary>
/// Tests for runner application with gated inputs.
/// </summary>
[TestClass]
public sealed class RunnerApplicationGatedInputTests
{
    private MockRepository _mockRepository = null!;
    private Mock<ICommandLineParser> _parserMock = null!;
    private Mock<ICoverageGate> _gateMock = null!;
    private Mock<IBaselineWriter> _baselineWriterMock = null!;
    private Mock<IResultWriter> _resultWriterMock = null!;
    private Mock<IFileReader> _fileReaderMock = null!;
    private StringWriter _errorWriter = null!;
    private IRunnerApplication _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = new(MockBehavior.Strict);
        _parserMock = _mockRepository.Create<ICommandLineParser>();
        _gateMock = _mockRepository.Create<ICoverageGate>();
        _baselineWriterMock = _mockRepository.Create<IBaselineWriter>();
        _resultWriterMock = _mockRepository.Create<IResultWriter>();
        _fileReaderMock = _mockRepository.Create<IFileReader>();
        _errorWriter = new();

        _subject = new RunnerApplication(
            _parserMock.Object,
            _gateMock.Object,
            _baselineWriterMock.Object,
            _resultWriterMock.Object,
            _fileReaderMock.Object,
            _errorWriter);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockRepository.VerifyAll();
    }

    /// <summary>
    /// Tests that gated inputs are passed to the gate for evaluate command.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-20")]
    public async Task RunAsync_EvaluateWithGatedInputs_PassesThemIntoTheRequest()
    {
        // Arrange
        var args = new[] { "evaluate", "--report", "report.xml", "--changed-files", "changes.txt", "--tool-name", "Tool", "--tool-version", "1.0" };
        var command = new EvaluateCommand("report.xml", null, "changes.txt", null, "Tool", "1.0", [], false, new[] { "src/", "global.json" });
        var result = new CoverageResult(true, new BlockCounters(100, 25), []);
        _parserMock.Setup(p => p.Parse(args)).Returns(command);
        _gateMock.Setup(g => g.EvaluateAsync(
            It.Is<CoverageGateRequest>(req =>
                req.GatedInputs != null &&
                req.GatedInputs.SequenceEqual(new[] { "src/", "global.json" })),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        _resultWriterMock.Setup(r => r.WriteAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var exitCode = await _subject.RunAsync(args, CancellationToken.None);

        // Assert
        Assert.AreEqual(0, exitCode);
    }
}
