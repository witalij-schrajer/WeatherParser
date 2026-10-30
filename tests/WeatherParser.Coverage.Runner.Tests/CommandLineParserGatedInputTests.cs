using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Coverage.Runner;
using FluentAssertions;

namespace WeatherParser.Coverage.Runner.Tests;

/// <summary>
/// Tests for command line parsing with gated inputs.
/// </summary>
[TestClass]
public sealed class CommandLineParserGatedInputTests
{
    private MockRepository _mockRepository = null!;
    private ICommandLineParser _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = new(MockBehavior.Strict);
        _subject = new CommandLineParser();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockRepository.VerifyAll();
    }

    /// <summary>
    /// Tests that gated inputs are collected in order for evaluate command.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-20")]
    public void Parse_EvaluateWithGatedInputs_CollectsThemInOrder()
    {
        // Arrange
        var args = new[] { "evaluate", "--report", "r.xml", "--changed-files", "c.txt", "--tool-name", "T", "--tool-version", "1", "--gated-input", "src/", "--gated-input", "global.json" };

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<EvaluateCommand>(result);
        var command = (EvaluateCommand)result;
        command.GatedInputs.Should().NotBeNull();
        command.GatedInputs.Should().Equal("src/", "global.json");
    }

    /// <summary>
    /// Tests that gated inputs is an empty list when not specified for evaluate command.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-20")]
    public void Parse_EvaluateWithoutGatedInputs_EmptyList()
    {
        // Arrange
        var args = new[] { "evaluate", "--report", "r.xml", "--changed-files", "c.txt", "--tool-name", "T", "--tool-version", "1" };

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<EvaluateCommand>(result);
        var command = (EvaluateCommand)result;
        command.GatedInputs.Should().NotBeNull();
        command.GatedInputs.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a usage error is returned when gated-input option has no value.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-20")]
    public void Parse_GatedInputWithoutValue_UsageError()
    {
        // Arrange
        var args = new[] { "evaluate", "--report", "r.xml", "--changed-files", "c.txt", "--tool-name", "T", "--tool-version", "1", "--gated-input" };

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<UsageError>(result);
        var error = (UsageError)result;
        error.Message.Should().Be("Option '--gated-input' requires a value.");
    }

    /// <summary>
    /// Tests that a usage error is returned when gated-input is used with write-baseline.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-20")]
    public void Parse_WriteBaselineWithGatedInput_UsageError()
    {
        // Arrange
        var args = new[] { "write-baseline", "--report", "r.xml", "--commit", "abc", "--tool-name", "T", "--tool-version", "1", "--output", "o.json", "--gated-input", "src/" };

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<UsageError>(result);
        var error = (UsageError)result;
        error.Message.Should().Be("Unknown option '--gated-input' for 'write-baseline'.");
    }
}
