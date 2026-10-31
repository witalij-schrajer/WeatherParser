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
public sealed class CoverageGateReconciliationTests
{
    private static readonly string[] Exclusions = new[] { "*.Tests.dll" };
    private const string Target = "ed7fc7a03db2f5554c490451f918da9fc6828c84";
    private const string OtherTarget = "1111111111111111111111111111111111111111";
    private const string Repository = "witalij-schrajer/WeatherParser";
    private const string Branch = "features/develop/P9";
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

    private void Arrange(long covered, long notCovered, params string[] changedPaths)
    {
        _reportReader
            .Setup(r => r.ReadAsync("report.xml", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CoverageReport(
                new[] { new ModuleCoverage("WeatherParser.Parsing.dll", "id", "path", new BlockCounters(covered, notCovered)) },
                "native-xml"));

        _changesReader
            .Setup(r => r.ReadAsync("changes.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangedFileList(changedPaths));

        _baselineReader
            .Setup(r => r.ReadAsync("baseline.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BaselineMetadata(Target, "Microsoft.CodeCoverage", "18.11.0", "native-xml", Exclusions, new BlockCounters(11102, 1)));
    }

    private void ArrangeEntries(params ReconciliationEntry[] entries)
    {
        _reconciliationReader
            .Setup(r => r.ReadAsync(ReconciliationFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ReconciliationEntry>)entries);
    }

    private static ReconciliationEntry Entry(string gate = "library", string target = Target, string repository = Repository, string branch = Branch) => new(gate, target, 18, repository, branch, "2026-10-31", "docs/p9/P9_RELEASE_CONTRACT.md#rl-20");

    private static CoverageGateRequest Request(string? gateName = "library", string? repository = Repository, string? branch = Branch, string? reconciliationPath = ReconciliationFile) => new("report.xml", "baseline.json", "changes.txt", Target, "Microsoft.CodeCoverage", "18.11.0", Exclusions, false, GatedInputs: new[] { "src/", "global.json" }, GateName: gateName, ReconciliationPath: reconciliationPath, HeadRepository: repository, HeadBranch: branch);

    private static RuleResult? Rule(CoverageResult result, string id) => result.Rules.SingleOrDefault(r => r.RuleId == id);

    /// <summary>
    /// Gate reconciliation for target with equal coverage accepted.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationForTarget_EqualCoverageAccepted()
    {
        Arrange(11102, 1, "global.json");
        ArrangeEntries(Entry());
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        result.Passed.Should().BeTrue();
        Rule(result, "Reconciliation").Should().NotBeNull();
        Rule(result, "Reconciliation")!.Verdict.Should().BeTrue();
        Rule(result, "Reconciliation")!.Diagnostic.Should().Contain("#18").And.Contain("library").And.Contain(Target);
        Rule(result, "Comparison")!.Verdict.Should().BeTrue();
    }

    /// <summary>
    /// Gate reconciliation for target with decreased coverage rejected.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationForTarget_DecreaseRejected()
    {
        Arrange(11101, 2, "global.json");
        ArrangeEntries(Entry());
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        result.Passed.Should().BeFalse();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Gate reconciliation for target with improved coverage accepted.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationForTarget_ImprovementAccepted()
    {
        Arrange(11103, 0, "global.json");
        ArrangeEntries(Entry());
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        result.Passed.Should().BeTrue();
    }

    /// <summary>
    /// Gate reconciliation for other target requires strict improvement.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationForOtherTarget_StrictImprovementRequired()
    {
        Arrange(11102, 1, "global.json");
        ArrangeEntries(Entry(target: OtherTarget));
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        result.Passed.Should().BeFalse();
        Rule(result, "Reconciliation").Should().BeNull();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Gate reconciliation for other gate not applied.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationForOtherGate_NotApplied()
    {
        Arrange(11102, 1, "global.json");
        ArrangeEntries(Entry(gate: "cli"));
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        result.Passed.Should().BeFalse();
        Rule(result, "Reconciliation").Should().BeNull();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Gate reconciliation for other branch requires strict improvement.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationForOtherBranch_StrictImprovementRequired()
    {
        Arrange(11102, 1, "global.json");
        ArrangeEntries(Entry(branch: "features/develop/other"));
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        result.Passed.Should().BeFalse();
        Rule(result, "Reconciliation").Should().BeNull();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Gate reconciliation for other head repository requires strict improvement.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationForOtherHeadRepository_StrictImprovementRequired()
    {
        Arrange(11102, 1, "global.json");
        ArrangeEntries(Entry(repository: "someone-else/WeatherParser"));
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        result.Passed.Should().BeFalse();
        Rule(result, "Reconciliation").Should().BeNull();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Gate reconciliation without branch not applied.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationWithoutBranch_NotApplied()
    {
        Arrange(11102, 1, "global.json");
        ArrangeEntries(Entry());
        var result = await _gate.EvaluateAsync(Request(branch: null), CancellationToken.None);

        result.Passed.Should().BeFalse();
        Rule(result, "Reconciliation").Should().BeNull();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Gate reconciliation without head repository not applied.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationWithoutHeadRepository_NotApplied()
    {
        Arrange(11102, 1, "global.json");
        ArrangeEntries(Entry());
        var result = await _gate.EvaluateAsync(Request(repository: null), CancellationToken.None);

        result.Passed.Should().BeFalse();
        Rule(result, "Reconciliation").Should().BeNull();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Gate reconciliation without gate name not applied.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationWithoutGateName_NotApplied()
    {
        Arrange(11102, 1, "global.json");
        ArrangeEntries(Entry());
        var result = await _gate.EvaluateAsync(Request(gateName: null), CancellationToken.None);

        result.Passed.Should().BeFalse();
        Rule(result, "Reconciliation").Should().BeNull();
        Rule(result, "Comparison")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Gate reconciliation file missing leaves rules unchanged.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationFileMissing_RulesUnchanged()
    {
        Arrange(11102, 1, "global.json");
        ArrangeEntries();
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        result.Passed.Should().BeFalse();
        Rule(result, "Reconciliation").Should().BeNull();
    }

    /// <summary>
    /// Gate reconciliation path null means reader not called.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationPathNull_ReaderNotCalled()
    {
        Arrange(11102, 1, "global.json");
        var result = await _gate.EvaluateAsync(Request(reconciliationPath: null), CancellationToken.None);

        result.Passed.Should().BeFalse();
        Rule(result, "Reconciliation").Should().BeNull();
    }

    /// <summary>
    /// Gate reconciliation file invalid fails.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationFileInvalid_Fails()
    {
        Arrange(11103, 0, "global.json");
        _reconciliationReader
            .Setup(r => r.ReadAsync(ReconciliationFile, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CoverageInputException("Reconciliation file 'coverage-reconciliation.json' is not valid JSON."));
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        result.Passed.Should().BeFalse();
        Rule(result, "Input")!.Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Gate reconciliation for target with gated inputs unchanged does not need reconciliation.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationForTarget_GatedInputsUnchanged_NotNeeded()
    {
        Arrange(11102, 1, "tools/WeatherParser.Coverage/CoverageGate.cs");
        ArrangeEntries(Entry());
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        result.Passed.Should().BeTrue();
        Rule(result, "Reconciliation").Should().BeNull();
    }

    /// <summary>
    /// Gate reconciliation for target with documentation-only change not applied.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationForTarget_DocumentationOnly_NotApplied()
    {
        Arrange(11102, 1, "docs/p9/P9_VERIFICATION.md");
        ArrangeEntries(Entry());
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        result.Passed.Should().BeTrue();
        Rule(result, "Reconciliation").Should().BeNull();
    }

    /// <summary>
    /// Gate reconciliation for target with uppercase target works.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task Gate_ReconciliationForTarget_TargetCaseInsensitive()
    {
        ArrangeEntries(Entry(target: Target.ToUpperInvariant()));
        Arrange(11102, 1, "global.json");
        var result = await _gate.EvaluateAsync(Request(), CancellationToken.None);

        result.Passed.Should().BeTrue();
    }
}
