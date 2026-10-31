using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class RuleEvaluatorReconciledTests
{
    private RuleEvaluator _evaluator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _evaluator = new RuleEvaluator();
    }

    /// <summary>
    /// Verifies that equal coverage passes for reconciled PRs.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Evaluate_Reconciled_EqualCoverage_Passes()
    {
        // Arrange
        var current = new BlockCounters(11102, 1);
        var baseline = new BlockCounters(11102, 1);
        var category = PullRequestCategory.Reconciled;
        var isInitialBootstrap = false;

        // Act
        var results = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        var comparisonResult = results.Single(r => r.RuleId == "Comparison");
        comparisonResult.Verdict.Should().BeTrue();
        comparisonResult.Diagnostic.Should().Contain("reconciliation");
    }

    /// <summary>
    /// Verifies that higher coverage passes for reconciled PRs.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Evaluate_Reconciled_HigherCoverage_Passes()
    {
        // Arrange
        var current = new BlockCounters(11103, 0);
        var baseline = new BlockCounters(11102, 1);
        var category = PullRequestCategory.Reconciled;
        var isInitialBootstrap = false;

        // Act
        var results = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        results.Single(r => r.RuleId == "Comparison").Verdict.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that lower coverage fails for reconciled PRs.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Evaluate_Reconciled_LowerCoverage_Fails()
    {
        // Arrange
        var current = new BlockCounters(11101, 2);
        var baseline = new BlockCounters(11102, 1);
        var category = PullRequestCategory.Reconciled;
        var isInitialBootstrap = false;

        // Act
        var results = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        results.Single(r => r.RuleId == "Comparison").Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that exact hundred baseline requires exact hundred coverage for reconciled PRs.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Evaluate_Reconciled_ExactHundredBaseline_RequiresExactHundred()
    {
        // Arrange
        var current = new BlockCounters(99, 1);
        var baseline = new BlockCounters(100, 0);
        var category = PullRequestCategory.Reconciled;
        var isInitialBootstrap = false;

        // Act
        var results = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        results.Single(r => r.RuleId == "Comparison").Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that coverage below the minimum fails for reconciled PRs.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Evaluate_Reconciled_BelowMinimum_Fails()
    {
        // Arrange
        var current = new BlockCounters(79, 21);
        var baseline = new BlockCounters(79, 21);
        var category = PullRequestCategory.Reconciled;
        var isInitialBootstrap = false;

        // Act
        var results = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        results.Single(r => r.RuleId == "Minimum").Verdict.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that missing baseline fails for reconciled PRs.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Evaluate_Reconciled_MissingBaseline_Fails()
    {
        // Arrange
        var current = new BlockCounters(11102, 1);
        BlockCounters? baseline = null;
        var category = PullRequestCategory.Reconciled;
        var isInitialBootstrap = false;

        // Act
        var results = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        results.Single(r => r.RuleId == "Baseline").Verdict.Should().BeFalse();
    }
}
