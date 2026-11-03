using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace WeatherParser.Coverage.Runner.Tests;

[TestClass]
public sealed class CommandLineParserTests
{
    private ICommandLineParser _parser = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _parser = new CommandLineParser();
    }

    [TestMethod]
    [TestCategory("T-P1-09")]
    public void Parse_EvaluateWithAllOptions_ReturnsEvaluateCommand()
    {
        var args = new[]
        {
            "evaluate",
            "--report", "coverage.xml",
            "--baseline", "baseline.json",
            "--changed-files", "changes.txt",
            "--expected-baseline-commit", "abc123",
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0",
            "--exclude", "*.Tests.dll",
            "--exclude", "WeatherParser.Coverage*.dll"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<EvaluateCommand>().Which
            .Should().NotBeNull();
        var command = (EvaluateCommand)result;

        command.ReportPath.Should().Be("coverage.xml");
        command.BaselinePath.Should().Be("baseline.json");
        command.ChangedFilesPath.Should().Be("changes.txt");
        command.ExpectedBaselineCommit.Should().Be("abc123");
        command.ToolName.Should().Be("Microsoft.CodeCoverage");
        command.ToolVersion.Should().Be("18.11.0");
        command.Exclusions.Should().Contain("*.Tests.dll");
        command.Exclusions.Should().Contain("WeatherParser.Coverage*.dll");
        command.Bootstrap.Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P1-14")]
    public void Parse_EvaluateBootstrapWithoutBaseline_ReturnsEvaluateCommand()
    {
        var args = new[]
        {
            "evaluate",
            "--report", "report-passing.xml",
            "--changed-files", "changes-docs-and-csproj.txt",
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0",
            "--exclude", "*.Tests.dll",
            "--exclude", "WeatherParser.Coverage*.dll",
            "--bootstrap"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<EvaluateCommand>().Which
            .Should().NotBeNull();
        var command = (EvaluateCommand)result;

        command.ReportPath.Should().Be("report-passing.xml");
        command.BaselinePath.Should().BeNull();
        command.ChangedFilesPath.Should().Be("changes-docs-and-csproj.txt");
        command.ExpectedBaselineCommit.Should().BeNull();
        command.ToolName.Should().Be("Microsoft.CodeCoverage");
        command.ToolVersion.Should().Be("18.11.0");
        command.Exclusions.Should().Contain("*.Tests.dll");
        command.Exclusions.Should().Contain("WeatherParser.Coverage*.dll");
        command.Bootstrap.Should().BeTrue();
    }

    [TestMethod]
    public void Parse_RepeatedExclude_KeepsAllPatternsInOrder()
    {
        var args = new[]
        {
            "evaluate",
            "--report", "coverage.xml",
            "--changed-files", "changes.txt",
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0",
            "--exclude", "*.Tests.dll",
            "--exclude", "WeatherParser.Coverage*.dll",
            "--exclude", "System.*.dll"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<EvaluateCommand>().Which
            .Should().NotBeNull();
        var command = (EvaluateCommand)result;

        command.Exclusions.Should().HaveCount(3);
        command.Exclusions[0].Should().Be("*.Tests.dll");
        command.Exclusions[1].Should().Be("WeatherParser.Coverage*.dll");
        command.Exclusions[2].Should().Be("System.*.dll");
    }

    [TestMethod]
    public void Parse_NoExclude_ReturnsEmptyExclusions()
    {
        var args = new[]
        {
            "evaluate",
            "--report", "coverage.xml",
            "--changed-files", "changes.txt",
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<EvaluateCommand>().Which
            .Should().NotBeNull();
        var command = (EvaluateCommand)result;

        command.Exclusions.Should().NotBeNull();
        command.Exclusions.Should().HaveCount(0);
    }

    [TestMethod]
    [DataRow("--report")]
    [DataRow("--changed-files")]
    [DataRow("--tool-name")]
    [DataRow("--tool-version")]
    public void Parse_MissingRequiredOption_ReturnsUsageError(string option)
    {
        var validArgs = new[]
        {
            "evaluate",
            "--report", "coverage.xml",
            "--changed-files", "changes.txt",
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0"
        };

        // Remove the option under test together with its value.
        var args = validArgs.Where((arg, i) => arg != option && (i == 0 || validArgs[i - 1] != option)).ToArray();

        var result = _parser.Parse(args);

        result.Should().BeOfType<UsageError>().Which.Message.Should().Contain(option);
    }

    [TestMethod]
    public void Parse_UnknownOption_ReturnsUsageError()
    {
        var args = new[]
        {
            "evaluate",
            "--unknown-option", "value"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<UsageError>().Which.Message.Should().Contain("--unknown-option");
    }

    [TestMethod]
    public void Parse_OptionWithoutValue_ReturnsUsageError()
    {
        var args = new[]
        {
            "evaluate",
            "--report",
            "--changed-files", "changes.txt"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<UsageError>().Which.Message.Should().Contain("--report");
    }

    [TestMethod]
    public void Parse_OptionWithoutValueAtEnd_ReturnsUsageError()
    {
        var args = new[]
        {
            "evaluate",
            "--changed-files", "changes.txt",
            "--report"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<UsageError>().Which.Message.Should().Contain("--report");
    }

    [TestMethod]
    public void Parse_DuplicateSingleValueOption_ReturnsUsageError()
    {
        var args = new[]
        {
            "evaluate",
            "--report", "coverage.xml",
            "--report", "coverage2.xml",
            "--changed-files", "changes.txt",
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<UsageError>().Which.Message.Should().Contain("--report");
    }

    [TestMethod]
    [TestCategory("T-P1-14")]
    public void Parse_BaselineWithoutExpectedCommit_ReturnsUsageError()
    {
        var args = new[]
        {
            "evaluate",
            "--report", "coverage.xml",
            "--baseline", "baseline.json",
            "--changed-files", "changes.txt",
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<UsageError>().Which.Message.Should().Contain("--expected-baseline-commit");
    }

    [TestMethod]
    [TestCategory("T-P1-14")]
    public void Parse_ExpectedCommitWithoutBaseline_ReturnsUsageError()
    {
        var args = new[]
        {
            "evaluate",
            "--report", "coverage.xml",
            "--changed-files", "changes.txt",
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0",
            "--expected-baseline-commit", "abc123"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<UsageError>().Which.Message.Should().Contain("--expected-baseline-commit");
    }

    [TestMethod]
    [TestCategory("T-P1-14")]
    public void Parse_BootstrapWithBaseline_ReturnsUsageError()
    {
        var args = new[]
        {
            "evaluate",
            "--report", "coverage.xml",
            "--baseline", "baseline.json",
            "--expected-baseline-commit", "abc123",
            "--changed-files", "changes.txt",
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0",
            "--bootstrap"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<UsageError>().Which.Message.Should().Contain("--bootstrap");
    }

    [TestMethod]
    public void Parse_MissingOrUnknownCommand_ReturnsUsageError()
    {
        var args = new[] { "verify" };

        var result = _parser.Parse(args);

        result.Should().BeOfType<UsageError>().Which.Message.Should().Contain("Expected a command");
    }

    [TestMethod]
    public void Parse_MissingOrUnknownCommand_ReturnsUsageError_EmptyArgs()
    {
        var args = new string[] { };

        var result = _parser.Parse(args);

        result.Should().BeOfType<UsageError>().Which.Message.Should().Contain("Expected a command");
    }

    [TestMethod]
    public void Parse_WriteBaselineWithAllOptions_ReturnsWriteBaselineCommand()
    {
        var args = new[]
        {
            "write-baseline",
            "--report", "coverage.xml",
            "--commit", "abc123",
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0",
            "--output", "baseline.json",
            "--exclude", "*.Tests.dll"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<WriteBaselineCommand>().Which
            .Should().NotBeNull();
        var command = (WriteBaselineCommand)result;

        command.ReportPath.Should().Be("coverage.xml");
        command.CommitId.Should().Be("abc123");
        command.ToolName.Should().Be("Microsoft.CodeCoverage");
        command.ToolVersion.Should().Be("18.11.0");
        command.Exclusions.Should().Contain("*.Tests.dll");
        command.OutputPath.Should().Be("baseline.json");
    }

    [TestMethod]
    public void Parse_WriteBaselineMissingOutput_ReturnsUsageError()
    {
        var args = new[]
        {
            "write-baseline",
            "--report", "coverage.xml",
            "--commit", "abc123",
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<UsageError>().Which.Message.Should().Contain("--output");
    }

    [TestMethod]
    public void Parse_WriteBaselineWithEvaluateOnlyOption_ReturnsUsageError()
    {
        var args = new[]
        {
            "write-baseline",
            "--report", "coverage.xml",
            "--commit", "abc123",
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0",
            "--output", "baseline.json",
            "--bootstrap"
        };

        var result = _parser.Parse(args);

        result.Should().BeOfType<UsageError>().Which.Message.Should().Contain("--bootstrap");
    }
}
