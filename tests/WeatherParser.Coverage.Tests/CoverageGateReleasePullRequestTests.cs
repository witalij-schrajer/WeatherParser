using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class CoverageGateReleasePullRequestTests
{
    private static readonly string[] Exclusions = new[] { "*.Tests.dll" };
    private const string Target = "cb8568840a7e6565712b66e72ca732ce50e13fd7";
    private const string Repository = "witalij-schrajer/WeatherParser";
    private const string ReconciliationFile = "coverage-reconciliation.json";

    private MockRepository _mockRepository = null!;
    private Mock<ICoverageReportReader> _reportReader = null!;
    private Mock<IBaselineMetadataReader> _baselineReader = null!;
    private Mock<IChangedFileListReader> _changesReader = null!;
    private Mock<IReconciliationReader> _reconciliationReader = null!;
    private CoverageGate _gate = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = new(MockBehavior.Strict);
        _reportReader = _mockRepository.Create<ICoverageReportReader>();
        _baselineReader = _mockRepository.Create<IBaselineMetadataReader>();
        _changesReader = _mockRepository.Create<IChangedFileListReader>();
        _reconciliationReader = _mockRepository.Create<IReconciliationReader>();

        _reconciliationReader
            .Setup(r => r.ReadAsync(ReconciliationFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ReconciliationEntry>)Array.Empty<ReconciliationEntry>());

        _gate = new CoverageGate(
            _reportReader.Object,
            _baselineReader.Object,
            _changesReader.Object,
            new ProvenanceAggregator(),
            new RuleEvaluator(),
            new BaselineComparabilityChecker(),
            new PullRequestClassifier(),
            _reconciliationReader.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockRepository.VerifyAll();
    }

    private void Arrange(long covered, long notCovered, string baselineCommit = Target, long baselineCovered = 11102, long baselineNotCovered = 1)
    {
        _reportReader
            .Setup(r => r.ReadAsync("report.xml", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CoverageReport(
                new[] { new ModuleCoverage("WeatherParser.Parsing.dll", "id", "path", new BlockCounters(covered, notCovered)) },
                "native-xml"));

        _changesReader
            .Setup(r => r.ReadAsync("changes.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangedFileList(new[] { "src/WeatherParser.Parsing/WeatherParser.Parsing.csproj" }));

        _baselineReader
            .Setup(r => r.ReadAsync("baseline.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BaselineMetadata(baselineCommit, "Microsoft.CodeCoverage", "18.11.0", "native-xml", Exclusions, new BlockCounters(baselineCovered, baselineNotCovered)));
    }

    private static CoverageGateRequest Request(string? headRepository = Repository, string? headBranch = "develop", string? baseRepository = Repository, string? baseBranch = "master") => new("report.xml", "baseline.json", "changes.txt", Target, "Microsoft.CodeCoverage", "18.11.0", Exclusions, false, GatedInputs: new[] { "src/", "global.json" }, GateName: "library", ReconciliationPath: ReconciliationFile, HeadRepository: headRepository, HeadBranch: headBranch, BaseRepository: baseRepository, BaseBranch: baseBranch);

    private static RuleResult? Rule(CoverageResult result, string id) => result.Rules.SingleOrDefault(r => r.RuleId == id);

    /// <summary>
    /// Tests that a release pull request with equal coverage is accepted.
    /// </summary>
    [TestMethod]
    [TestCategory("T-REL-01")]
    public async Task Gate_ReleasePullRequest_EqualCoverageAccepted()
    {
        // Arrange
        Arrange(11102, 1);

        // Act
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        // Assert
        result.Passed.Should().BeTrue();
        var rule = Rule(result, "ReleasePullRequest");
        rule.Should().NotBeNull();
        rule!.Verdict.Should().BeTrue();
        rule.Diagnostic.Should().Contain("develop").And.Contain("master").And.Contain(Repository);
        Rule(result, "Comparison")!.Verdict.Should().BeTrue();
    }

    /// <summary>
    /// Tests that a release pull request with improved coverage is accepted.
    /// </summary>
    [TestMethod]
    [TestCategory("T-REL-01")]
    public async Task Gate_ReleasePullRequest_ImprovementAccepted()
    {
        // Arrange
        Arrange(11103, 0);

        // Act
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        // Assert
        result.Passed.Should().BeTrue();
        Rule(result, "Comparison")!.Verdict.Should().BeTrue();
    }

    /// <summary>
    /// Tests that a release pull request with decreased coverage is rejected.
    /// </summary>
    [TestMethod]
    [TestCategory("T-REL-01")]
    public async Task Gate_ReleasePullRequest_DecreaseRejected()
    {
        // Arrange
        Arrange(11101, 2);

        // Act
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        // Assert
        result.Passed.Should().BeFalse();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Tests that a release pull request with exact hundred baseline keeps the rule.
    /// </summary>
    [TestMethod]
    [TestCategory("T-REL-01")]
    public async Task Gate_ReleasePullRequest_ExactHundredBaselineKeepsTheRule()
    {
        // Arrange
        Arrange(11102, 1, baselineCovered: 11103, baselineNotCovered: 0);

        // Act
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        // Assert
        result.Passed.Should().BeFalse();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Tests that a release pull request with incomparable baseline does not apply the rule.
    /// </summary>
    [TestMethod]
    [TestCategory("T-REL-01")]
    public async Task Gate_ReleasePullRequest_IncomparableBaseline_RuleNotApplied()
    {
        // Arrange
        Arrange(11102, 1, baselineCommit: "1111111111111111111111111111111111111111");

        // Act
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        // Assert
        result.Passed.Should().BeFalse();
        var rule = Rule(result, "ReleasePullRequest");
        rule.Should().BeNull();
    }

    /// <summary>
    /// Tests that a feature branch into master requires strict improvement.
    /// </summary>
    [TestMethod]
    [TestCategory("T-REL-01")]
    public async Task Gate_FeatureBranchIntoMaster_StrictImprovementRequired()
    {
        // Arrange
        Arrange(11102, 1);
        var request = Request(headBranch: "features/develop/x");

        // Act
        var result = await _gate.EvaluateAsync(request, CancellationToken.None);

        // Assert
        result.Passed.Should().BeFalse();
        var rule = Rule(result, "ReleasePullRequest");
        rule.Should().BeNull();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Tests that a develop branch into other base requires strict improvement.
    /// </summary>
    [TestMethod]
    [TestCategory("T-REL-01")]
    public async Task Gate_DevelopIntoOtherBase_StrictImprovementRequired()
    {
        // Arrange
        Arrange(11102, 1);
        var request = Request(baseBranch: "develop");

        // Act
        var result = await _gate.EvaluateAsync(request, CancellationToken.None);

        // Assert
        result.Passed.Should().BeFalse();
        var rule = Rule(result, "ReleasePullRequest");
        rule.Should().BeNull();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Tests that a fork develop into master requires strict improvement.
    /// </summary>
    [TestMethod]
    [TestCategory("T-REL-01")]
    public async Task Gate_ForkDevelopIntoMaster_StrictImprovementRequired()
    {
        // Arrange
        Arrange(11102, 1);
        var request = Request(headRepository: "someone/WeatherParser");

        // Act
        var result = await _gate.EvaluateAsync(request, CancellationToken.None);

        // Assert
        result.Passed.Should().BeFalse();
        var rule = Rule(result, "ReleasePullRequest");
        rule.Should().BeNull();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Tests that a release identity without base requires strict improvement.
    /// </summary>
    [TestMethod]
    [TestCategory("T-REL-01")]
    public async Task Gate_ReleaseIdentityWithoutBase_StrictImprovementRequired()
    {
        // Arrange
        Arrange(11102, 1);
        var request = Request(baseRepository: null, baseBranch: null);

        // Act
        var result = await _gate.EvaluateAsync(request, CancellationToken.None);

        // Assert
        result.Passed.Should().BeFalse();
        var rule = Rule(result, "ReleasePullRequest");
        rule.Should().BeNull();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Tests that a release identity without head requires strict improvement.
    /// </summary>
    [TestMethod]
    [TestCategory("T-REL-01")]
    public async Task Gate_ReleaseIdentityWithoutHead_StrictImprovementRequired()
    {
        // Arrange
        Arrange(11102, 1);
        var request = Request(headRepository: null, headBranch: null);

        // Act
        var result = await _gate.EvaluateAsync(request, CancellationToken.None);

        // Assert
        result.Passed.Should().BeFalse();
        var rule = Rule(result, "ReleasePullRequest");
        rule.Should().BeNull();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }
}
