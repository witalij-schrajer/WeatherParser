using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class CoverageGateRealReportTests
{
    private CoverageGate _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _sut = new CoverageGate(
            reportReader: new CoverageReportReader(new FileReader()),
            baselineReader: new BaselineMetadataReader(new FileReader()),
            changesReader: new ChangedFileListReader(new FileReader()),
            aggregator: new ProvenanceAggregator(),
            evaluator: new RuleEvaluator(),
            comparabilityChecker: new BaselineComparabilityChecker(),
            classifier: new PullRequestClassifier(),
            reconciliationReader: new ReconciliationReader(new FileReader()));
    }

    [TestMethod]
    [TestCategory("T-P1-09")]
    [TestCategory("T-P1-24")]
    public async Task EvaluateAsync_RealP1_07Report_ExpectedFail()
    {
        // Arrange
        var reportPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "report-real-p1-07.xml");
        var changesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "changes-docs-and-csproj.txt");
        var request = new CoverageGateRequest(
            ReportPath: reportPath,
            BaselinePath: null,
            ChangedFilesPath: changesPath,
            ExpectedBaselineCommitId: "abc123",
            ToolName: "Microsoft.CodeCoverage",
            ToolVersion: "1.0.0",
            Exclusions: new[] { "*.Tests.dll" },
            IsInitialBootstrap: true);

        // Act
        var result = await _sut.EvaluateAsync(request, default);

        // Assert
        result.Passed.Should().BeFalse();
        result.Current.Should().BeEquivalentTo(new BlockCounters(57, 33));
        result.Rules.Should().ContainSingle(r => r.RuleId == "Minimum")
            .Which.Diagnostic.Should().Contain("57/90").And.Contain("63.33");
    }
}
