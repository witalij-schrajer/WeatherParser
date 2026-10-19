using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Coverage;

namespace WeatherParser.Coverage.Runner.Tests;

[TestClass]
public sealed class RunnerApplicationTests
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

    [TestMethod]
    [TestCategory("T-P1-09")]
    public async Task RunAsync_GatePassed_Returns0AndWritesResult()
    {
        // Arrange
        var args = new[] { "evaluate", "--report", "report.xml", "--changed-files", "changes.txt", "--tool-name", "Tool", "--tool-version", "1.0" };
        var command = new EvaluateCommand("report.xml", null, "changes.txt", null, "Tool", "1.0", [], false);
        var result = new CoverageResult(true, new BlockCounters(100, 25), []);
        _parserMock.Setup(p => p.Parse(args)).Returns(command);
        _gateMock.Setup(g => g.EvaluateAsync(It.IsAny<CoverageGateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        _resultWriterMock.Setup(r => r.WriteAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var exitCode = await _subject.RunAsync(args, CancellationToken.None);

        // Assert
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    [TestCategory("T-P1-13")]
    public async Task RunAsync_CoverageRuleFailed_Returns1AndReportsRule()
    {
        // Arrange
        var args = new[] { "evaluate", "--report", "report.xml", "--changed-files", "changes.txt", "--tool-name", "Tool", "--tool-version", "1.0" };
        var command = new EvaluateCommand("report.xml", null, "changes.txt", null, "Tool", "1.0", [], false);
        var result = new CoverageResult(false, new BlockCounters(100, 25), [new RuleResult("Minimum", false, "Block coverage 100/125 (80.00%) is below the required minimum of 80%.")]);
        _parserMock.Setup(p => p.Parse(args)).Returns(command);
        _gateMock.Setup(g => g.EvaluateAsync(It.IsAny<CoverageGateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        _resultWriterMock.Setup(r => r.WriteAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var exitCode = await _subject.RunAsync(args, CancellationToken.None);

        // Assert
        Assert.AreEqual(1, exitCode);
        Assert.IsTrue(_errorWriter.ToString().Contains("Minimum"));
    }

    [TestMethod]
    [TestCategory("T-P1-13")]
    public async Task RunAsync_InputRuleFailed_Returns2()
    {
        // Arrange
        var args = new[] { "evaluate", "--report", "report.xml", "--changed-files", "changes.txt", "--tool-name", "Tool", "--tool-version", "1.0" };
        var command = new EvaluateCommand("report.xml", null, "changes.txt", null, "Tool", "1.0", [], false);
        var result = new CoverageResult(false, null, [new RuleResult("Input", false, "Coverage report 'report.xml' was not found.")]);
        _parserMock.Setup(p => p.Parse(args)).Returns(command);
        _gateMock.Setup(g => g.EvaluateAsync(It.IsAny<CoverageGateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        _resultWriterMock.Setup(r => r.WriteAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var exitCode = await _subject.RunAsync(args, CancellationToken.None);

        // Assert
        Assert.AreEqual(2, exitCode);
        Assert.IsTrue(_errorWriter.ToString().Contains("Coverage report 'report.xml' was not found."));
    }

    [TestMethod]
    [TestCategory("T-P1-14")]
    public async Task RunAsync_UsageError_Returns2AndWritesUsage()
    {
        // Arrange
        var args = new[] { "unknown-command" };
        var command = new UsageError("Expected a command: evaluate or write-baseline.");
        _parserMock.Setup(p => p.Parse(args)).Returns(command);

        // Act
        var exitCode = await _subject.RunAsync(args, CancellationToken.None);

        // Assert
        Assert.AreEqual(2, exitCode);
        Assert.IsTrue(_errorWriter.ToString().Contains("Usage error: Expected a command: evaluate or write-baseline."));
        Assert.IsTrue(_errorWriter.ToString().Contains("Expected a command: evaluate or write-baseline."));
    }

    [TestMethod]
    [TestCategory("T-P1-14")]
    public async Task RunAsync_SuppliedBaselineMissing_Returns2WithoutCallingGate()
    {
        // Arrange
        var args = new[] { "evaluate", "--report", "report.xml", "--baseline", "baseline.json", "--changed-files", "changes.txt", "--tool-name", "Tool", "--tool-version", "1.0" };
        var command = new EvaluateCommand("report.xml", "baseline.json", "changes.txt", null, "Tool", "1.0", [], false);
        _parserMock.Setup(p => p.Parse(args)).Returns(command);
        _fileReaderMock.Setup(f => f.Exists("baseline.json")).Returns(false);

        // Act
        var exitCode = await _subject.RunAsync(args, CancellationToken.None);

        // Assert
        Assert.AreEqual(2, exitCode);
        Assert.IsTrue(_errorWriter.ToString().Contains("Baseline file 'baseline.json' was not found."));
        _gateMock.Verify(g => g.EvaluateAsync(It.IsAny<CoverageGateRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("T-P1-14")]
    public async Task RunAsync_SuppliedBaselineExists_PassesRequestToGate()
    {
        // Arrange
        var args = new[] { "evaluate", "--report", "report.xml", "--baseline", "baseline.json", "--expected-baseline-commit", "abc123", "--changed-files", "changes.txt", "--tool-name", "Tool", "--tool-version", "1.0" };
        var command = new EvaluateCommand("report.xml", "baseline.json", "changes.txt", "abc123", "Tool", "1.0", ["exclude1"], false);
        var result = new CoverageResult(true, new BlockCounters(100, 25), []);
        _parserMock.Setup(p => p.Parse(args)).Returns(command);
        _fileReaderMock.Setup(f => f.Exists("baseline.json")).Returns(true);
        _gateMock.Setup(g => g.EvaluateAsync(
            It.Is<CoverageGateRequest>(req =>
                req.ReportPath == "report.xml" &&
                req.BaselinePath == "baseline.json" &&
                req.ChangedFilesPath == "changes.txt" &&
                req.ExpectedBaselineCommitId == "abc123" &&
                req.ToolName == "Tool" &&
                req.ToolVersion == "1.0" &&
                req.Exclusions.SequenceEqual(new[] { "exclude1" }) &&
                req.IsInitialBootstrap == false),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        _resultWriterMock.Setup(r => r.WriteAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var exitCode = await _subject.RunAsync(args, CancellationToken.None);

        // Assert
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    [TestCategory("T-P1-14")]
    public async Task RunAsync_BootstrapWithoutBaseline_PassesEmptyExpectedCommitAndBootstrap()
    {
        // Arrange
        var args = new[] { "evaluate", "--report", "report.xml", "--bootstrap", "--changed-files", "changes.txt", "--tool-name", "Tool", "--tool-version", "1.0" };
        var command = new EvaluateCommand("report.xml", null, "changes.txt", null, "Tool", "1.0", [], true);
        var result = new CoverageResult(true, new BlockCounters(100, 25), []);
        _parserMock.Setup(p => p.Parse(args)).Returns(command);
        _gateMock.Setup(g => g.EvaluateAsync(
            It.Is<CoverageGateRequest>(req =>
                req.ReportPath == "report.xml" &&
                req.BaselinePath == null &&
                req.ChangedFilesPath == "changes.txt" &&
                req.ExpectedBaselineCommitId == "" &&
                req.ToolName == "Tool" &&
                req.ToolVersion == "1.0" &&
                req.Exclusions.Count == 0 &&
                req.IsInitialBootstrap == true),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        _resultWriterMock.Setup(r => r.WriteAsync(result, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var exitCode = await _subject.RunAsync(args, CancellationToken.None);

        // Assert
        Assert.AreEqual(0, exitCode);
        _fileReaderMock.Verify(f => f.Exists(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [TestCategory("T-P1-09")]
    public async Task RunAsync_WriteBaselineSucceeds_Returns0()
    {
        // Arrange
        var args = new[] { "write-baseline", "--report", "report.xml", "--commit", "abc123", "--tool-name", "Tool", "--tool-version", "1.0", "--output", "baseline.json" };
        var command = new WriteBaselineCommand("report.xml", "abc123", "Tool", "1.0", [], "baseline.json");
        _parserMock.Setup(p => p.Parse(args)).Returns(command);
        _baselineWriterMock.Setup(b => b.WriteAsync(command, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var exitCode = await _subject.RunAsync(args, CancellationToken.None);

        // Assert
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    public async Task RunAsync_WriteBaselineInputError_Returns2()
    {
        // Arrange
        var args = new[] { "write-baseline", "--report", "report.xml", "--commit", "abc123", "--tool-name", "Tool", "--tool-version", "1.0", "--output", "baseline.json" };
        var command = new WriteBaselineCommand("report.xml", "abc123", "Tool", "1.0", [], "baseline.json");
        _parserMock.Setup(p => p.Parse(args)).Returns(command);
        _baselineWriterMock.Setup(b => b.WriteAsync(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CoverageInputException("Baseline metadata 'baseline.json' is malformed or empty."));

        // Act
        var exitCode = await _subject.RunAsync(args, CancellationToken.None);

        // Assert
        Assert.AreEqual(2, exitCode);
        Assert.IsTrue(_errorWriter.ToString().Contains("Baseline metadata 'baseline.json' is malformed or empty."));
    }

    [TestMethod]
    public async Task RunAsync_GateCancelled_PropagatesOperationCanceledException()
    {
        // Arrange
        var args = new[] { "evaluate", "--report", "report.xml", "--changed-files", "changes.txt", "--tool-name", "Tool", "--tool-version", "1.0" };
        var command = new EvaluateCommand("report.xml", null, "changes.txt", null, "Tool", "1.0", [], false);
        _parserMock.Setup(p => p.Parse(args)).Returns(command);
        _gateMock.Setup(g => g.EvaluateAsync(It.IsAny<CoverageGateRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => _subject.RunAsync(args, CancellationToken.None));
    }
}
