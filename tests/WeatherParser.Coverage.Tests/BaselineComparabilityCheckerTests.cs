using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class BaselineComparabilityCheckerTests
{
    private const string Commit = "abc123";
    private const string ToolName = "Microsoft.CodeCoverage";
    private const string ToolVersion = "18.11.0";
    private const string Format = "native-xml";
    private static readonly IReadOnlyList<string> Exclusions = ["*.Tests.dll"];

    private BaselineComparabilityChecker _checker = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _checker = new BaselineComparabilityChecker();
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    public void Check_ComparableBaseline_Passes()
    {
        // Arrange
        var baseline = new BaselineMetadata(Commit, ToolName, ToolVersion, Format, Exclusions, new BlockCounters(57, 33));

        // Act
        var result = _checker.Check(baseline, Commit, ToolName, ToolVersion, Format, Exclusions);

        // Assert
        result.Verdict.Should().BeTrue();
        result.Diagnostic.Should().Be("Baseline is comparable.");
    }

    [TestMethod]
    [TestCategory("T-P1-15")]
    public void Check_StaleCommit_Fails()
    {
        // Arrange
        var baseline = new BaselineMetadata("def456", ToolName, ToolVersion, Format, Exclusions, new BlockCounters(57, 33));

        // Act
        var result = _checker.Check(baseline, Commit, ToolName, ToolVersion, Format, Exclusions);

        // Assert
        result.Verdict.Should().BeFalse();
        result.Diagnostic.Should().Contain("Baseline commit ID 'def456' differs from expected 'abc123'.");
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    public void Check_DifferentToolName_Fails()
    {
        // Arrange
        var baseline = new BaselineMetadata(Commit, "OtherTool", ToolVersion, Format, Exclusions, new BlockCounters(57, 33));

        // Act
        var result = _checker.Check(baseline, Commit, ToolName, ToolVersion, Format, Exclusions);

        // Assert
        result.Verdict.Should().BeFalse();
        result.Diagnostic.Should().Contain("Baseline tool name 'OtherTool' differs from expected 'Microsoft.CodeCoverage'.");
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    public void Check_DifferentToolVersion_Fails()
    {
        // Arrange
        var baseline = new BaselineMetadata(Commit, ToolName, "1.0.0", Format, Exclusions, new BlockCounters(57, 33));

        // Act
        var result = _checker.Check(baseline, Commit, ToolName, ToolVersion, Format, Exclusions);

        // Assert
        result.Verdict.Should().BeFalse();
        result.Diagnostic.Should().Contain("Baseline tool version '1.0.0' differs from expected '18.11.0'.");
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    public void Check_ToolNameCaseDifference_Passes()
    {
        // Arrange
        var baseline = new BaselineMetadata(Commit, "MICROSOFT.CODECOVERAGE", ToolVersion, Format, Exclusions, new BlockCounters(57, 33));

        // Act
        var result = _checker.Check(baseline, Commit, ToolName, ToolVersion, Format, Exclusions);

        // Assert
        result.Verdict.Should().BeTrue();
        result.Diagnostic.Should().Be("Baseline is comparable.");
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    public void Check_DifferentFormat_Fails()
    {
        // Arrange
        var baseline = new BaselineMetadata(Commit, ToolName, ToolVersion, "cobertura", Exclusions, new BlockCounters(57, 33));

        // Act
        var result = _checker.Check(baseline, Commit, ToolName, ToolVersion, Format, Exclusions);

        // Assert
        result.Verdict.Should().BeFalse();
        result.Diagnostic.Should().Contain("Baseline format 'cobertura' differs from expected 'native-xml'.");
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    public void Check_DifferentExclusions_Fails()
    {
        // Arrange
        var baseline = new BaselineMetadata(Commit, ToolName, ToolVersion, Format, ["*.Tools.dll"], new BlockCounters(57, 33));

        // Act
        var result = _checker.Check(baseline, Commit, ToolName, ToolVersion, Format, Exclusions);

        // Assert
        result.Verdict.Should().BeFalse();
        result.Diagnostic.Should().Contain("Baseline exclusions differ from expected exclusions.");
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    public void Check_SameExclusionsDifferentOrderAndCase_Passes()
    {
        // Arrange
        var baseline = new BaselineMetadata(Commit, ToolName, ToolVersion, Format, ["*.TESTS.DLL", "*.Tools.dll"], new BlockCounters(57, 33));

        // Act
        var result = _checker.Check(baseline, Commit, ToolName, ToolVersion, Format, ["*.tools.DLL", "*.Tests.dll"]);

        // Assert
        result.Verdict.Should().BeTrue();
        result.Diagnostic.Should().Be("Baseline is comparable.");
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    public void Check_AdditionalExclusion_Fails()
    {
        // Arrange
        var baseline = new BaselineMetadata(Commit, ToolName, ToolVersion, Format, ["*.Tests.dll", "*.Tools.dll"], new BlockCounters(57, 33));

        // Act
        var result = _checker.Check(baseline, Commit, ToolName, ToolVersion, Format, Exclusions);

        // Assert
        result.Verdict.Should().BeFalse();
        result.Diagnostic.Should().Contain("Baseline exclusions differ from expected exclusions.");
    }

    [TestMethod]
    [TestCategory("T-P1-15")]
    [TestCategory("T-P1-16")]
    public void Check_MultipleMismatches_Fails()
    {
        // Arrange
        var baseline = new BaselineMetadata("def456", "OtherTool", "1.0.0", "cobertura", ["*.Tools.dll"], new BlockCounters(57, 33));

        // Act
        var result = _checker.Check(baseline, Commit, ToolName, ToolVersion, Format, Exclusions);

        // Assert
        result.Verdict.Should().BeFalse();
        result.Diagnostic.Should().ContainEquivalentOf("commit");
        result.Diagnostic.Should().ContainEquivalentOf("version");
        result.Diagnostic.Should().ContainEquivalentOf("exclusions");
    }

    private static BaselineMetadata CreateBaseline(
        string commitId = Commit,
        string toolName = ToolName,
        string toolVersion = ToolVersion,
        string format = Format,
        params string[] exclusions)
    {
        return new BaselineMetadata(commitId, toolName, toolVersion, format, exclusions.Length == 0 ? Exclusions : exclusions, new BlockCounters(57, 33));
    }
}
