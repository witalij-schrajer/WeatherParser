using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class GatedInputsClassifierTests
{
    private static readonly string[] LibraryGatedInputs = { "src/", "Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.Config", ".editorconfig", "coverage.runsettings" };

    private PullRequestClassifier _classifier = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _classifier = new PullRequestClassifier();
    }

    [TestMethod]
    [TestCategory("T-P3-20")]
    public void TouchesGatedInputs_PathBelowDirectoryPrefix_True()
    {
        // Arrange
        var changes = new ChangedFileList(new[] { "src/WeatherParser.Parsing/Mqtt/MqttMessage.cs" });

        // Act
        var result = _classifier.TouchesGatedInputs(changes, LibraryGatedInputs);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("T-P3-20")]
    [DataRow("Directory.Build.props")]
    [DataRow("Directory.Packages.props")]
    [DataRow("global.json")]
    [DataRow("NuGet.Config")]
    [DataRow(".editorconfig")]
    [DataRow("coverage.runsettings")]
    public void TouchesGatedInputs_ExactBuildInput_True(string buildInput)
    {
        // Arrange
        var changes = new ChangedFileList(new[] { buildInput, "examples/WeatherParser.Cli/Program.cs" });

        // Act
        var result = _classifier.TouchesGatedInputs(changes, LibraryGatedInputs);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("T-P3-20")]
    public void TouchesGatedInputs_OnlyCliTestsAndDocumentation_False()
    {
        // Arrange
        var changes = new ChangedFileList(new[] { "examples/WeatherParser.Cli/Program.cs", "tests/WeatherParser.Cli.Tests/ProcessTests.cs", "scripts/ci-local.ps1", "docs/p3/P3_VERIFICATION.md" });

        // Act
        var result = _classifier.TouchesGatedInputs(changes, LibraryGatedInputs);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P3-20")]
    public void TouchesGatedInputs_SimilarNames_NotMatched()
    {
        // Arrange
        var changes = new ChangedFileList(new[] { "srcx/File.cs", "Directory.Build.props.bak", "tools/Directory.Build.props", "examples/global.json" });

        // Act
        var result = _classifier.TouchesGatedInputs(changes, LibraryGatedInputs);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P3-20")]
    public void TouchesGatedInputs_NoChanges_False()
    {
        // Arrange
        var changes = new ChangedFileList(Array.Empty<string>());

        // Act
        var result = _classifier.TouchesGatedInputs(changes, LibraryGatedInputs);

        // Assert
        result.Should().BeFalse();
    }
}
