using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Coverage;
using WeatherParser.Coverage.Runner;
using FluentAssertions;

namespace WeatherParser.Coverage.Runner.Tests;

/// <summary>
/// Tests for command line parsing with reconciliation options.
/// </summary>
[TestClass]
public sealed class CommandLineParserReconciliationTests
{
    private MockRepository _mockRepository = null!;
    private ICommandLineParser _subject = null!;
    private static readonly string[] Base = { "evaluate", "--report", "r.xml", "--changed-files", "c.txt", "--tool-name", "T", "--tool-version", "1" };

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
    /// Tests that gated input set 'cli' expands to the CLI set.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Parse_GatedInputSetCli_ExpandsToCliSet()
    {
        // Arrange
        var args = Base.Concat(new[] { "--gated-input-set", "cli" }).ToArray();

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<EvaluateCommand>(result);
        var command = (EvaluateCommand)result;
        command.GatedInputs.Should().NotBeNull();
        command.GatedInputs.Should().Equal(GatedInputSets.Cli);
    }

    /// <summary>
    /// Tests that gated input set 'library' expands to the library set.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Parse_GatedInputSetLibrary_ExpandsToLibrarySet()
    {
        // Arrange
        var args = Base.Concat(new[] { "--gated-input-set", "library" }).ToArray();

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<EvaluateCommand>(result);
        var command = (EvaluateCommand)result;
        command.GatedInputs.Should().NotBeNull();
        command.GatedInputs.Should().Equal(GatedInputSets.Library);
    }

    /// <summary>
    /// Tests that an unknown gated input set name results in a usage error.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Parse_GatedInputSetUnknown_UsageError()
    {
        // Arrange
        var args = Base.Concat(new[] { "--gated-input-set", "parsing" }).ToArray();

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<UsageError>(result);
        var error = (UsageError)result;
        error.Message.Should().Be("Unknown gated-input set 'parsing'. Expected library or cli.");
    }

    /// <summary>
    /// Tests that using both --gated-input and --gated-input-set results in a usage error.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Parse_GatedInputSetWithGatedInput_UsageError()
    {
        // Arrange
        var args = Base.Concat(new[] { "--gated-input", "src/", "--gated-input-set", "cli" }).ToArray();

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<UsageError>(result);
        var error = (UsageError)result;
        error.Message.Should().Be("Options '--gated-input' and '--gated-input-set' cannot be combined.");
    }

    /// <summary>
    /// Tests that specifying --gated-input-set twice results in a usage error.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Parse_GatedInputSetTwice_UsageError()
    {
        // Arrange
        var args = Base.Concat(new[] { "--gated-input-set", "cli", "--gated-input-set", "cli" }).ToArray();

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<UsageError>(result);
        var error = (UsageError)result;
        error.Message.Should().Be("Option '--gated-input-set' is specified more than once.");
    }

    /// <summary>
    /// Tests that reconciliation options are stored correctly.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Parse_ReconciliationOptions_AreStored()
    {
        // Arrange
        var args = Base.Concat(new[] { "--gate-name", "library", "--reconciliation", "coverage-reconciliation.json", "--head-repository", "witalij-schrajer/WeatherParser", "--head-branch", "features/develop/P9" }).ToArray();

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<EvaluateCommand>(result);
        var command = (EvaluateCommand)result;
        command.GateName.Should().Be("library");
        command.ReconciliationPath.Should().Be("coverage-reconciliation.json");
        command.HeadRepository.Should().Be("witalij-schrajer/WeatherParser");
        command.HeadBranch.Should().Be("features/develop/P9");
    }

    /// <summary>
    /// Tests that reconciliation options are null when not specified.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Parse_WithoutReconciliationOptions_AllNull()
    {
        // Arrange
        var args = Base;

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<EvaluateCommand>(result);
        var command = (EvaluateCommand)result;
        command.GateName.Should().BeNull();
        command.ReconciliationPath.Should().BeNull();
        command.HeadRepository.Should().BeNull();
        command.HeadBranch.Should().BeNull();
    }

    /// <summary>
    /// Tests that specifying --reconciliation without --gate-name results in a usage error.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Parse_ReconciliationWithoutGateName_UsageError()
    {
        // Arrange
        var args = Base.Concat(new[] { "--reconciliation", "x.json" }).ToArray();

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<UsageError>(result);
        var error = (UsageError)result;
        error.Message.Should().Be("Option '--reconciliation' requires '--gate-name'.");
    }

    /// <summary>
    /// Tests that an unknown gate name results in a usage error.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Parse_UnknownGateName_UsageError()
    {
        // Arrange
        var args = Base.Concat(new[] { "--gate-name", "parsing" }).ToArray();

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<UsageError>(result);
        var error = (UsageError)result;
        error.Message.Should().Be("Unknown gate name 'parsing'. Expected library or cli.");
    }

    /// <summary>
    /// Tests that specifying --head-branch twice results in a usage error.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Parse_HeadBranchTwice_UsageError()
    {
        // Arrange
        var args = Base.Concat(new[] { "--head-branch", "a", "--head-branch", "b" }).ToArray();

        // Act
        var result = _subject.Parse(args);

        // Assert
        Assert.IsInstanceOfType<UsageError>(result);
        var error = (UsageError)result;
        error.Message.Should().Be("Option '--head-branch' is specified more than once.");
    }
}
