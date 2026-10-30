using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using FluentAssertions;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class CoverageGateGatedInputsTests
{
    private MockRepository _mockRepository = null!;
    private Mock<ICoverageReportReader> _mockReportReader = null!;
    private Mock<IBaselineMetadataReader> _mockBaselineReader = null!;
    private Mock<IChangedFileListReader> _mockChangesReader = null!;
    private Mock<IProvenanceAggregator> _mockAggregator = null!;
    private Mock<IRuleEvaluator> _mockEvaluator = null!;
    private Mock<IBaselineComparabilityChecker> _mockComparabilityChecker = null!;
    private Mock<IPullRequestClassifier> _mockClassifier = null!;

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

        _coverageGate = new CoverageGate(
            _mockReportReader.Object,
            _mockBaselineReader.Object,
            _mockChangesReader.Object,
            _mockAggregator.Object,
            _mockEvaluator.Object,
            _mockComparabilityChecker.Object,
            _mockClassifier.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockRepository.VerifyAll();
    }

    private static readonly string[] LibraryGatedInputs = { "src/", "Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.Config", ".editorconfig", "coverage.runsettings" };

    [TestMethod]
    [TestCategory("T-P3-20")]
    public async Task EvaluateAsync_CodeChangeWithGatedInputsUntouched_EvaluatesAsGatedInputsUnchanged()
    {
        // Arrange
        var request = new CoverageGateRequest(
            ReportPath: "report.xml",
            BaselinePath: null,
            ChangedFilesPath: "changes.txt",
            ExpectedBaselineCommitId: string.Empty,
            ToolName: "Microsoft.CodeCoverage",
            ToolVersion: "18.11.0",
            Exclusions: new[] { "*.Tests.dll" },
            IsInitialBootstrap: true,
            GatedInputs: LibraryGatedInputs);

        var report = new CoverageReport(
            Modules: new[] { new ModuleCoverage("WeatherParser.Parsing.dll", "id", "path", new BlockCounters(80, 20)) },
            Format: "native-xml");

        var changes = new ChangedFileList(new[] { "examples/WeatherParser.Cli/Program.cs" });
        var aggregated = new BlockCounters(80, 20);
        var category = PullRequestCategory.GatedInputsUnchanged;
        var ruleResults = new[]
        {
            new RuleResult("Minimum", true, string.Empty)
        };

        _mockReportReader.Setup(r => r.ReadAsync(request.ReportPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _mockAggregator.Setup(a => a.Aggregate(report, request.Exclusions))
            .Returns(aggregated);

        _mockChangesReader.Setup(c => c.ReadAsync(request.ChangedFilesPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(changes);

        _mockClassifier.Setup(c => c.Classify(changes))
            .Returns(PullRequestCategory.Code);

        _mockClassifier.Setup(c => c.TouchesGatedInputs(changes, request.GatedInputs!))
            .Returns(false);

        _mockBaselineReader.Setup(b => b.ReadAsync(request.BaselinePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BaselineMetadata?)null);

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
    [TestCategory("T-P3-20")]
    public async Task EvaluateAsync_CodeChangeTouchingGatedInputs_EvaluatesAsCode()
    {
        // Arrange
        var request = new CoverageGateRequest(
            ReportPath: "report.xml",
            BaselinePath: null,
            ChangedFilesPath: "changes.txt",
            ExpectedBaselineCommitId: string.Empty,
            ToolName: "Microsoft.CodeCoverage",
            ToolVersion: "18.11.0",
            Exclusions: new[] { "*.Tests.dll" },
            IsInitialBootstrap: true,
            GatedInputs: LibraryGatedInputs);

        var report = new CoverageReport(
            Modules: new[] { new ModuleCoverage("WeatherParser.Parsing.dll", "id", "path", new BlockCounters(80, 20)) },
            Format: "native-xml");

        var changes = new ChangedFileList(new[] { "src/WeatherParser.Parsing/A.cs" });
        var aggregated = new BlockCounters(80, 20);
        var category = PullRequestCategory.Code;
        var ruleResults = new[]
        {
            new RuleResult("Minimum", true, string.Empty)
        };

        _mockReportReader.Setup(r => r.ReadAsync(request.ReportPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _mockAggregator.Setup(a => a.Aggregate(report, request.Exclusions))
            .Returns(aggregated);

        _mockChangesReader.Setup(c => c.ReadAsync(request.ChangedFilesPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(changes);

        _mockClassifier.Setup(c => c.Classify(changes))
            .Returns(category);

        _mockClassifier.Setup(c => c.TouchesGatedInputs(changes, request.GatedInputs!))
            .Returns(true);

        _mockBaselineReader.Setup(b => b.ReadAsync(request.BaselinePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BaselineMetadata?)null);

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
    [TestCategory("T-P3-20")]
    public async Task EvaluateAsync_DocumentationOnly_DoesNotCheckGatedInputs()
    {
        // Arrange
        var request = new CoverageGateRequest(
            ReportPath: "report.xml",
            BaselinePath: null,
            ChangedFilesPath: "changes.txt",
            ExpectedBaselineCommitId: string.Empty,
            ToolName: "Microsoft.CodeCoverage",
            ToolVersion: "18.11.0",
            Exclusions: new[] { "*.Tests.dll" },
            IsInitialBootstrap: true,
            GatedInputs: LibraryGatedInputs);

        var report = new CoverageReport(
            Modules: new[] { new ModuleCoverage("WeatherParser.Parsing.dll", "id", "path", new BlockCounters(80, 20)) },
            Format: "native-xml");

        var changes = new ChangedFileList(new[] { "README.md" });
        var aggregated = new BlockCounters(80, 20);
        var category = PullRequestCategory.DocumentationOnly;
        var ruleResults = new[]
        {
            new RuleResult("Minimum", true, string.Empty)
        };

        _mockReportReader.Setup(r => r.ReadAsync(request.ReportPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _mockAggregator.Setup(a => a.Aggregate(report, request.Exclusions))
            .Returns(aggregated);

        _mockChangesReader.Setup(c => c.ReadAsync(request.ChangedFilesPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(changes);

        _mockClassifier.Setup(c => c.Classify(changes))
            .Returns(category);

        // TouchesGatedInputs should not be called for DocumentationOnly
        _mockBaselineReader.Setup(b => b.ReadAsync(request.BaselinePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BaselineMetadata?)null);

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
    [TestCategory("T-P3-20")]
    public async Task EvaluateAsync_NoGatedInputs_DoesNotCheckGatedInputs()
    {
        // Arrange
        var request = new CoverageGateRequest(
            ReportPath: "report.xml",
            BaselinePath: null,
            ChangedFilesPath: "changes.txt",
            ExpectedBaselineCommitId: string.Empty,
            ToolName: "Microsoft.CodeCoverage",
            ToolVersion: "18.11.0",
            Exclusions: new[] { "*.Tests.dll" },
            IsInitialBootstrap: true,
            GatedInputs: null);

        var report = new CoverageReport(
            Modules: new[] { new ModuleCoverage("WeatherParser.Parsing.dll", "id", "path", new BlockCounters(80, 20)) },
            Format: "native-xml");

        var changes = new ChangedFileList(new[] { "examples/WeatherParser.Cli/Program.cs" });
        var aggregated = new BlockCounters(80, 20);
        var category = PullRequestCategory.Code;
        var ruleResults = new[]
        {
            new RuleResult("Minimum", true, string.Empty)
        };

        _mockReportReader.Setup(r => r.ReadAsync(request.ReportPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _mockAggregator.Setup(a => a.Aggregate(report, request.Exclusions))
            .Returns(aggregated);

        _mockChangesReader.Setup(c => c.ReadAsync(request.ChangedFilesPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(changes);

        _mockClassifier.Setup(c => c.Classify(changes))
            .Returns(category);

        // TouchesGatedInputs should not be set up for null GatedInputs
        _mockBaselineReader.Setup(b => b.ReadAsync(request.BaselinePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BaselineMetadata?)null);

        _mockEvaluator.Setup(e => e.Evaluate(aggregated, null, category, request.IsInitialBootstrap))
            .Returns(ruleResults);

        // Act
        var result = await _coverageGate.EvaluateAsync(request, CancellationToken.None);

        // Assert
        result.Passed.Should().BeTrue();
        result.Current.Should().Be(aggregated);
        result.Rules.Should().ContainInOrder(ruleResults);
    }
}
