using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class RuleEvaluatorTests
{
    private IRuleEvaluator _evaluator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _evaluator = new RuleEvaluator();
    }

    [TestMethod]
    [TestCategory("T-P1-09")]
    public void Evaluate_80_20_Null_Code_True_AllRulesPass()
    {
        // Arrange
        var current = new BlockCounters(80, 20);
        BlockCounters? baseline = null;
        var category = PullRequestCategory.Code;
        var isInitialBootstrap = true;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(3);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeTrue();
        result[1].RuleId.Should().Be("Minimum");
        result[1].Verdict.Should().BeTrue();
        result[2].RuleId.Should().Be("Baseline");
        result[2].Verdict.Should().BeTrue();
        result[2].Diagnostic.Should().Contain("Initial bootstrap: no baseline, comparison skipped");
    }

    [TestMethod]
    [TestCategory("T-P1-09")]
    public void Evaluate_79_21_Null_Code_True_MinimumFailsWhileBaselinePasses()
    {
        // Arrange
        var current = new BlockCounters(79, 21);
        BlockCounters? baseline = null;
        var category = PullRequestCategory.Code;
        var isInitialBootstrap = true;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(3);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeTrue();
        result[1].RuleId.Should().Be("Minimum");
        result[1].Verdict.Should().BeFalse();
        result[1].Diagnostic.Should().Contain("79/100");
        result[2].RuleId.Should().Be("Baseline");
        result[2].Verdict.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("T-P1-09")]
    public void Evaluate_0_0_Null_Code_True_OnlyMeasurementRuleFailed()
    {
        // Arrange
        var current = new BlockCounters(0, 0);
        BlockCounters? baseline = null;
        var category = PullRequestCategory.Code;
        var isInitialBootstrap = true;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(1);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeFalse();
        result[0].Diagnostic.Should().Contain("zero denominator");
    }

    [TestMethod]
    [TestCategory("T-P1-14b")]
    public void Evaluate_85_15_Null_Code_False_BaselineFailsMissingAfterBootstrap()
    {
        // Arrange
        var current = new BlockCounters(85, 15);
        BlockCounters? baseline = null;
        var category = PullRequestCategory.Code;
        var isInitialBootstrap = false;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(3);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeTrue();
        result[1].RuleId.Should().Be("Minimum");
        result[1].Verdict.Should().BeTrue();
        result[2].RuleId.Should().Be("Baseline");
        result[2].Verdict.Should().BeFalse();
        result[2].Diagnostic.Should().Contain("Baseline is missing after initial bootstrap");
    }

    [TestMethod]
    [TestCategory("T-P1-10")]
    public void Evaluate_85_15_Vs_85_15_Code_ComparisonFailsEqualityBelow100()
    {
        // Arrange
        var current = new BlockCounters(85, 15);
        var baseline = new BlockCounters(85, 15);
        var category = PullRequestCategory.Code;
        var isInitialBootstrap = false;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(4);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeTrue();
        result[1].RuleId.Should().Be("Minimum");
        result[1].Verdict.Should().BeTrue();
        result[2].RuleId.Should().Be("Baseline");
        result[2].Verdict.Should().BeTrue();
        result[3].RuleId.Should().Be("Comparison");
        result[3].Verdict.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P1-10")]
    public void Evaluate_170_30_Vs_85_15_Code_ComparisonFailsSameRatioDifferentCounters()
    {
        // Arrange
        var current = new BlockCounters(170, 30);
        var baseline = new BlockCounters(85, 15);
        var category = PullRequestCategory.Code;
        var isInitialBootstrap = false;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(4);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeTrue();
        result[1].RuleId.Should().Be("Minimum");
        result[1].Verdict.Should().BeTrue();
        result[2].RuleId.Should().Be("Baseline");
        result[2].Verdict.Should().BeTrue();
        result[3].RuleId.Should().Be("Comparison");
        result[3].Verdict.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P1-10")]
    public void Evaluate_84_16_Vs_85_15_Code_ComparisonFailsDecrease()
    {
        // Arrange
        var current = new BlockCounters(84, 16);
        var baseline = new BlockCounters(85, 15);
        var category = PullRequestCategory.Code;
        var isInitialBootstrap = false;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(4);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeTrue();
        result[1].RuleId.Should().Be("Minimum");
        result[1].Verdict.Should().BeTrue();
        result[2].RuleId.Should().Be("Baseline");
        result[2].Verdict.Should().BeTrue();
        result[3].RuleId.Should().Be("Comparison");
        result[3].Verdict.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P1-10")]
    public void Evaluate_86_14_Vs_85_15_Code_ComparisonPasses()
    {
        // Arrange
        var current = new BlockCounters(86, 14);
        var baseline = new BlockCounters(85, 15);
        var category = PullRequestCategory.Code;
        var isInitialBootstrap = false;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(4);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeTrue();
        result[1].RuleId.Should().Be("Minimum");
        result[1].Verdict.Should().BeTrue();
        result[2].RuleId.Should().Be("Baseline");
        result[2].Verdict.Should().BeTrue();
        result[3].RuleId.Should().Be("Comparison");
        result[3].Verdict.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("T-P1-12")]
    public void Evaluate_100_0_Vs_100_0_Code_ComparisonPassesExact100()
    {
        // Arrange
        var current = new BlockCounters(100, 0);
        var baseline = new BlockCounters(100, 0);
        var category = PullRequestCategory.Code;
        var isInitialBootstrap = false;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(4);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeTrue();
        result[1].RuleId.Should().Be("Minimum");
        result[1].Verdict.Should().BeTrue();
        result[2].RuleId.Should().Be("Baseline");
        result[2].Verdict.Should().BeTrue();
        result[3].RuleId.Should().Be("Comparison");
        result[3].Verdict.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("T-P1-12")]
    public void Evaluate_99999_1_Vs_100_0_Code_ComparisonFailsRounded100NotExact()
    {
        // Arrange
        var current = new BlockCounters(99999, 1);
        var baseline = new BlockCounters(100, 0);
        var category = PullRequestCategory.Code;
        var isInitialBootstrap = false;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(4);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeTrue();
        result[1].RuleId.Should().Be("Minimum");
        result[1].Verdict.Should().BeTrue();
        result[2].RuleId.Should().Be("Baseline");
        result[2].Verdict.Should().BeTrue();
        result[3].RuleId.Should().Be("Comparison");
        result[3].Verdict.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P1-18a")]
    public void Evaluate_85_15_Vs_85_15_DocumentationOnly_AllPass()
    {
        // Arrange
        var current = new BlockCounters(85, 15);
        var baseline = new BlockCounters(85, 15);
        var category = PullRequestCategory.DocumentationOnly;
        var isInitialBootstrap = false;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(4);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeTrue();
        result[1].RuleId.Should().Be("Minimum");
        result[1].Verdict.Should().BeTrue();
        result[2].RuleId.Should().Be("Baseline");
        result[2].Verdict.Should().BeTrue();
        result[3].RuleId.Should().Be("Comparison");
        result[3].Verdict.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("T-P1-18b")]
    public void Evaluate_84_16_Vs_85_15_DocumentationOnly_ComparisonFails()
    {
        // Arrange
        var current = new BlockCounters(84, 16);
        var baseline = new BlockCounters(85, 15);
        var category = PullRequestCategory.DocumentationOnly;
        var isInitialBootstrap = false;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(4);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeTrue();
        result[1].RuleId.Should().Be("Minimum");
        result[1].Verdict.Should().BeTrue();
        result[2].RuleId.Should().Be("Baseline");
        result[2].Verdict.Should().BeTrue();
        result[3].RuleId.Should().Be("Comparison");
        result[3].Verdict.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P1-18b")]
    public void Evaluate_70_30_Vs_70_30_DocumentationOnly_MinimumFailsEvenThoughEqual()
    {
        // Arrange
        var current = new BlockCounters(70, 30);
        var baseline = new BlockCounters(70, 30);
        var category = PullRequestCategory.DocumentationOnly;
        var isInitialBootstrap = false;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(4);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeTrue();
        result[1].RuleId.Should().Be("Minimum");
        result[1].Verdict.Should().BeFalse();
        result[3].RuleId.Should().Be("Comparison");
        result[3].Verdict.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("T-P1-14a")]
    public void Evaluate_80_20_Vs_0_0_Code_BaselineFailsInvalidBaseline()
    {
        // Arrange
        var current = new BlockCounters(80, 20);
        var baseline = new BlockCounters(0, 0);
        var category = PullRequestCategory.Code;
        var isInitialBootstrap = false;

        // Act
        var result = _evaluator.Evaluate(current, baseline, category, isInitialBootstrap);

        // Assert
        result.Should().HaveCount(3);
        result[0].RuleId.Should().Be("Measurement");
        result[0].Verdict.Should().BeTrue();
        result[1].RuleId.Should().Be("Minimum");
        result[1].Verdict.Should().BeTrue();
        result[2].RuleId.Should().Be("Baseline");
        result[2].Verdict.Should().BeFalse();
        result[2].Diagnostic.Should().Contain("invalid");
    }
}
