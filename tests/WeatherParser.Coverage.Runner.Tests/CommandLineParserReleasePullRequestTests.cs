using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Coverage.Runner;

namespace WeatherParser.Coverage.Runner.Tests;

[TestClass]
public sealed class CommandLineParserReleasePullRequestTests
{
    private ICommandLineParser _subject = null!;
    private static readonly string[] Base = { "evaluate", "--report", "r.xml", "--changed-files", "c.txt", "--tool-name", "T", "--tool-version", "1" };

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new CommandLineParser();
    }

    [TestMethod]
    [TestCategory("T-REL-01")]
    public void Parse_BaseRepositoryAndBranch_AreOnTheCommand()
    {
        // Arrange
        var args = Base.Concat(new[] { "--base-repository", "o/r", "--base-branch", "master" }).ToArray();

        // Act
        var result = _subject.Parse(args);

        // Assert
        result.Should().BeOfType<EvaluateCommand>();
        var evaluate = (EvaluateCommand)result;
        evaluate.BaseRepository.Should().Be("o/r");
        evaluate.BaseBranch.Should().Be("master");
    }

    [TestMethod]
    [TestCategory("T-REL-01")]
    public void Parse_WithoutBaseOptions_BaseValuesAreNull()
    {
        // Arrange
        var args = Base;

        // Act
        var result = _subject.Parse(args);

        // Assert
        result.Should().BeOfType<EvaluateCommand>();
        var evaluate = (EvaluateCommand)result;
        evaluate.BaseRepository.Should().BeNull();
        evaluate.BaseBranch.Should().BeNull();
    }

    [TestMethod]
    [TestCategory("T-REL-01")]
    public void Parse_BaseBranchTwice_UsageError()
    {
        // Arrange
        var args = Base.Concat(new[] { "--base-branch", "master", "--base-branch", "develop" }).ToArray();

        // Act
        var result = _subject.Parse(args);

        // Assert
        result.Should().BeOfType<UsageError>();
    }
}
