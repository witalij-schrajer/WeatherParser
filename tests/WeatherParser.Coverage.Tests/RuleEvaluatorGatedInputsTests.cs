using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class RuleEvaluatorGatedInputsTests
{
    private static readonly string[] LibraryGatedInputs = { "src/", "Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.Config", ".editorconfig", "coverage.runsettings" };

    private RuleEvaluator _evaluator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _evaluator = new RuleEvaluator();
    }

    [TestMethod]
    [TestCategory("T-P3-20")]
    public void Gate_LibraryUnchanged_EqualCoverageAccepted()
    {
        // Arrange
        var current = new BlockCounters(11102, 1);
        var baseline = new BlockCounters(11102, 1);
        var category = PullRequestCategory.GatedInputsUnchanged;
        var isInitialBootstrap = false;

        // Act
        var results = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        results.Single(r => r.RuleId == "Comparison").Verdict.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("T-P3-20")]
    public void Evaluate_GatedInputsUnchanged_LowerCoverage_Fails()
    {
        // Arrange
        var current = new BlockCounters(11101, 2);
        var baseline = new BlockCounters(11102, 1);
        var category = PullRequestCategory.GatedInputsUnchanged;
        var isInitialBootstrap = false;

        // Act
        var results = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        results.Single(r => r.RuleId == "Comparison").Verdict.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P3-20")]
    public void Evaluate_GatedInputsUnchanged_HigherCoverage_Passes()
    {
        // Arrange
        var current = new BlockCounters(11104, 1);
        var baseline = new BlockCounters(11102, 1);
        var category = PullRequestCategory.GatedInputsUnchanged;
        var isInitialBootstrap = false;

        // Act
        var results = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        results.Single(r => r.RuleId == "Comparison").Verdict.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("T-P3-20")]
    public void Evaluate_GatedInputsUnchanged_ExactHundredBaseline_RequiresHundred()
    {
        // Arrange
        var current = new BlockCounters(10, 1);
        var baseline = new BlockCounters(10, 0);
        var category = PullRequestCategory.GatedInputsUnchanged;
        var isInitialBootstrap = false;

        // Act
        var results = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        results.Single(r => r.RuleId == "Comparison").Verdict.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P3-20")]
    public void Gate_LibraryBuildInputChanged_StrictImprovementRequired()
    {
        // Arrange
        var current = new BlockCounters(11102, 1);
        var baseline = new BlockCounters(11102, 1);
        var category = PullRequestCategory.Code;
        var isInitialBootstrap = false;

        // Act
        var results = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        results.Single(r => r.RuleId == "Comparison").Verdict.Should().BeFalse();
    }
}
