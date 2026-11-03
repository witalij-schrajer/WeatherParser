using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluentAssertions;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class CoverageGateTests
{
    private MockRepository _mockRepository = null!;
    private Mock<ICoverageReportReader> _mockReportReader = null!;
    private Mock<IBaselineMetadataReader> _mockBaselineReader = null!;
    private Mock<IChangedFileListReader> _mockChangesReader = null!;
    private Mock<IProvenanceAggregator> _mockAggregator = null!;
    private Mock<IRuleEvaluator> _mockEvaluator = null!;
    private Mock<IBaselineComparabilityChecker> _mockComparabilityChecker = null!;
    private Mock<IPullRequestClassifier> _mockClassifier = null!;
    private Mock<IReconciliationReader> _mockReconciliationReader = null!;

    private CoverageGate _coverageGate = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);
        _mockReportReader = _mockRepository.Create<ICoverageReportReader>();
        _mockBaselineReader = _mockRepository.Create<IBaselineMetadataReader>();
        _mockChangesReader = _mockRepository.Create<IChangedFileListReader>();
        _mockAggregator = _mockRepository.Create<IProvenanceAggregator>();
        _mockEvaluator = _mockRepository.Create<IRuleEvaluator>();
        _mockComparabilityChecker = _mockRepository.Create<IBaselineComparabilityChecker>();
        _mockClassifier = _mockRepository.Create<IPullRequestClassifier>();
        _mockReconciliationReader = _mockRepository.Create<IReconciliationReader>();

        _coverageGate = new CoverageGate(
            _mockReportReader.Object,
            _mockBaselineReader.Object,
            _mockChangesReader.Object,
            _mockAggregator.Object,
            _mockEvaluator.Object,
            _mockComparabilityChecker.Object,
            _mockClassifier.Object,
            _mockReconciliationReader.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockRepository.VerifyAll();
    }

    [TestMethod]
    [TestCategory("T-P1-09")]
    public async Task EvaluateAsync_PassingFlow_ReturnsPassedTrueWithRules()
    {
        // Arrange
        var request = new CoverageGateRequest(
            ReportPath: "report.xml",
            BaselinePath: "baseline.json",
            ChangedFilesPath: "changes.txt",
            ExpectedBaselineCommitId: "abc123",
            ToolName: "Microsoft.CodeCoverage",
            ToolVersion: "1.0.0",
            Exclusions: new[] { "*.Tests.dll" },
            IsInitialBootstrap: false);

        var report = new CoverageReport(
            Modules: new[] { new ModuleCoverage("TestModule", "id1", "path1", new BlockCounters(57, 33)) },
            Format: "native-xml");

        var changes = new ChangedFileList(new[] { "src/WeatherParser.Parsing/Parser.cs" });
        var baseline = new BaselineMetadata(
            CommitId: "abc123",
            ToolName: "Microsoft.CodeCoverage",
            ToolVersion: "1.0.0",
            Format: "native-xml",
            Exclusions: new[] { "*.Tests.dll" },
            Blocks: new BlockCounters(50, 40));

        var aggregated = new BlockCounters(57, 33);
        var category = PullRequestCategory.Code;
        var ruleResults = new[]
        {
            new RuleResult("Comparability", true, string.Empty),
            new RuleResult("Minimum", true, string.Empty),
            new RuleResult("Baseline", true, string.Empty),
            new RuleResult("Comparison", true, string.Empty)
        };

        _mockReportReader.Setup(r => r.ReadAsync(request.ReportPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _mockAggregator.Setup(a => a.Aggregate(report, request.Exclusions))
            .Returns(aggregated);

        _mockChangesReader.Setup(c => c.ReadAsync(request.ChangedFilesPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(changes);

        _mockClassifier.Setup(c => c.Classify(changes))
            .Returns(category);

        _mockBaselineReader.Setup(b => b.ReadAsync(request.BaselinePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseline);

        _mockComparabilityChecker.Setup(c => c.Check(
                baseline,
                request.ExpectedBaselineCommitId,
                request.ToolName,
                request.ToolVersion,
                "native-xml",
                request.Exclusions))
            .Returns(new RuleResult("Comparability", true, string.Empty));

        _mockEvaluator.Setup(e => e.Evaluate(aggregated, baseline.Blocks, category, request.IsInitialBootstrap))
            .Returns(ruleResults);

        // Act
        var result = await _coverageGate.EvaluateAsync(request, CancellationToken.None);

        // Assert
        result.Passed.Should().BeTrue();
        result.Current.Should().Be(aggregated);
        result.Rules.Should().ContainInOrder(ruleResults);
    }

    [TestMethod]
    [TestCategory("T-P1-09")]
    public async Task EvaluateAsync_ReportReaderThrowsCoverageInputException_ReturnsFailedWithInputRule()
    {
        // Arrange
        var request = new CoverageGateRequest(
            ReportPath: "report.xml",
            BaselinePath: null,
            ChangedFilesPath: "changes.txt",
            ExpectedBaselineCommitId: "abc123",
            ToolName: "Microsoft.CodeCoverage",
            ToolVersion: "1.0.0",
            Exclusions: new[] { "*.Tests.dll" },
            IsInitialBootstrap: false);

        var exception = new CoverageInputException("Report not found");

        _mockReportReader.Setup(r => r.ReadAsync(request.ReportPath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _coverageGate.EvaluateAsync(request, CancellationToken.None);

        // Assert
        result.Passed.Should().BeFalse();
        result.Current.Should().BeNull();
        result.Rules.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new RuleResult("Input", false, exception.Message));
    }

    [TestMethod]
    [TestCategory("T-P1-09")]
    public async Task EvaluateAsync_FailedComparability_EvaluatorCalledWithNullBaselineAndFalseBootstrap()
    {
        // Arrange
        var request = new CoverageGateRequest(
            ReportPath: "report.xml",
            BaselinePath: "baseline.json",
            ChangedFilesPath: "changes.txt",
            ExpectedBaselineCommitId: "abc123",
            ToolName: "Microsoft.CodeCoverage",
            ToolVersion: "1.0.0",
            Exclusions: new[] { "*.Tests.dll" },
            IsInitialBootstrap: false);

        var report = new CoverageReport(
            Modules: new[] { new ModuleCoverage("TestModule", "id1", "path1", new BlockCounters(57, 33)) },
            Format: "native-xml");

        var changes = new ChangedFileList(new[] { "src/WeatherParser.Parsing/Parser.cs" });
        var baseline = new BaselineMetadata(
            CommitId: "wrong-commit",
            ToolName: "Microsoft.CodeCoverage",
            ToolVersion: "1.0.0",
            Format: "native-xml",
            Exclusions: new[] { "*.Tests.dll" },
            Blocks: new BlockCounters(50, 40));

        var aggregated = new BlockCounters(57, 33);
        var category = PullRequestCategory.Code;
        var comparabilityResult = new RuleResult("Comparability", false, "Baseline commit ID 'wrong-commit' differs from expected 'abc123'.");

        _mockReportReader.Setup(r => r.ReadAsync(request.ReportPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _mockAggregator.Setup(a => a.Aggregate(report, request.Exclusions))
            .Returns(aggregated);

        _mockChangesReader.Setup(c => c.ReadAsync(request.ChangedFilesPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(changes);

        _mockClassifier.Setup(c => c.Classify(changes))
            .Returns(category);

        _mockBaselineReader.Setup(b => b.ReadAsync(request.BaselinePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(baseline);

        _mockComparabilityChecker.Setup(c => c.Check(
                baseline,
                request.ExpectedBaselineCommitId,
                request.ToolName,
                request.ToolVersion,
                "native-xml",
                request.Exclusions))
            .Returns(comparabilityResult);

        // Evaluator should be called with baseline=null and isInitialBootstrap=false
        _mockEvaluator.Setup(e => e.Evaluate(aggregated, null, category, false))
            .Returns(new[] { new RuleResult("Minimum", true, string.Empty) });

        // Act
        var result = await _coverageGate.EvaluateAsync(request, CancellationToken.None);

        // Assert
        result.Passed.Should().BeFalse();
        result.Current.Should().Be(aggregated);
        result.Rules.Should().HaveCount(2);
        result.Rules[0].Should().BeEquivalentTo(comparabilityResult);
        result.Rules[1].RuleId.Should().Be("Minimum");
        result.Rules[1].Verdict.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("T-P1-09")]
    public async Task EvaluateAsync_NoBaseline_EvaluatorCalledWithBootstrapFlag()
    {
        // Arrange
        var request = new CoverageGateRequest(
            ReportPath: "report.xml",
            BaselinePath: null,
            ChangedFilesPath: "changes.txt",
            ExpectedBaselineCommitId: "abc123",
            ToolName: "Microsoft.CodeCoverage",
            ToolVersion: "1.0.0",
            Exclusions: new[] { "*.Tests.dll" },
            IsInitialBootstrap: true);

        var report = new CoverageReport(
            Modules: new[] { new ModuleCoverage("TestModule", "id1", "path1", new BlockCounters(57, 33)) },
            Format: "native-xml");

        var changes = new ChangedFileList(new[] { "src/WeatherParser.Parsing/Parser.cs" });
        var aggregated = new BlockCounters(57, 33);
        var category = PullRequestCategory.Code;
        var ruleResults = new[]
        {
            new RuleResult("Minimum", true, string.Empty),
            new RuleResult("Baseline", true, "Initial bootstrap: no baseline, comparison skipped")
        };

        _mockReportReader.Setup(r => r.ReadAsync(request.ReportPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _mockAggregator.Setup(a => a.Aggregate(report, request.Exclusions))
            .Returns(aggregated);

        _mockChangesReader.Setup(c => c.ReadAsync(request.ChangedFilesPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(changes);

        _mockClassifier.Setup(c => c.Classify(changes))
            .Returns(category);

        _mockBaselineReader.Setup(b => b.ReadAsync(request.BaselinePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BaselineMetadata?)null);

        // Comparability checker should not be called
        _mockEvaluator.Setup(e => e.Evaluate(aggregated, null, category, request.IsInitialBootstrap))
            .Returns(ruleResults);

        // Act
        var result = await _coverageGate.EvaluateAsync(request, CancellationToken.None);

        // Assert
        result.Passed.Should().BeTrue();
        result.Current.Should().Be(aggregated);
        result.Rules.Should().ContainInOrder(ruleResults);
    }

    [TestMethod]
    [TestCategory("T-P1-09")]
    public async Task EvaluateAsync_OperationCanceledException_Propagates()
    {
        // Arrange
        var request = new CoverageGateRequest(
            ReportPath: "report.xml",
            BaselinePath: null,
            ChangedFilesPath: "changes.txt",
            ExpectedBaselineCommitId: "abc123",
            ToolName: "Microsoft.CodeCoverage",
            ToolVersion: "1.0.0",
            Exclusions: new[] { "*.Tests.dll" },
            IsInitialBootstrap: false);

        var cancellationToken = new CancellationToken(canceled: true);

        _mockReportReader.Setup(r => r.ReadAsync(request.ReportPath, cancellationToken))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await FluentActions.Invoking(async () =>
            await _coverageGate.EvaluateAsync(request, cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}
