using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Coverage;
using WeatherParser.Coverage.Runner;

namespace WeatherParser.Coverage.Runner.Tests;

[TestClass]
public sealed class RunnerEndToEndTests
{
    private const string Commit = "6a68906592d1b5a5f85f3751659f509d22bd8463";

    private StringWriter _stdout = null!;
    private StringWriter _stderr = null!;
    private IRunnerApplication _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _stdout = new StringWriter();
        _stderr = new StringWriter();
        _sut = RunnerComposition.Create(_stdout, _stderr);
    }

    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    private async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(params string[] args)
    {
        var exitCode = await _sut.RunAsync(args, CancellationToken.None);
        return (exitCode, _stdout.ToString(), _stderr.ToString());
    }

    private static string[] Evaluate(string report, string changes = "changes-docs-and-csproj.txt", params string[] extra)
    {
        var list = new List<string>
        {
            "evaluate",
            "--report", Fixture(report),
            "--changed-files", Fixture(changes),
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0",
            "--exclude", "*.Tests.dll",
            "--exclude", "WeatherParser.Coverage*.dll"
        };

        list.AddRange(extra);
        return [.. list];
    }

    private static JsonElement Rule(string stdout, string ruleId)
    {
        using var doc = JsonDocument.Parse(stdout);
        foreach (var rule in doc.RootElement.GetProperty("rules").EnumerateArray())
        {
            if (rule.GetProperty("ruleId").GetString() == ruleId)
            {
                return rule.Clone();
            }
        }

        Assert.Fail($"Rule '{ruleId}' not found in stdout");
        return default;
    }

    [TestMethod]
    [TestCategory("T-P1-09")]
    public async Task RealP107Report_Bootstrap_FailsMinimum()
    {
        var (exitCode, stdout, stderr) = await RunAsync(Evaluate("report-real-p1-07.xml", extra: ["--bootstrap"]));

        exitCode.Should().Be(1);
        stderr.Should().NotBeEmpty();

        using var doc = JsonDocument.Parse(stdout);
        doc.RootElement.GetProperty("current").GetProperty("covered").GetInt64().Should().Be(57);
        doc.RootElement.GetProperty("current").GetProperty("notCovered").GetInt64().Should().Be(33);

        var minimumRule = Rule(stdout, "Minimum");
        minimumRule.GetProperty("verdict").GetBoolean().Should().BeFalse();
        minimumRule.GetProperty("diagnostic").GetString()!.Should().Contain("57/90");
    }

    [TestMethod]
    [TestCategory("T-P1-09")]
    [TestCategory("T-P1-14")]
    public async Task PassingReport_P109BootstrapInvocation_Passes()
    {
        var (exitCode, stdout, stderr) = await RunAsync(Evaluate("report-passing.xml", extra: ["--bootstrap"]));

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();

        using var doc = JsonDocument.Parse(stdout);
        doc.RootElement.GetProperty("passed").GetBoolean().Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("T-P1-14")]
    public async Task Bootstrap_WithExpectedCommit_IsUsageError()
    {
        var (exitCode, stdout, stderr) = await RunAsync(Evaluate("report-passing.xml", extra: ["--bootstrap", "--expected-baseline-commit", Commit]));

        exitCode.Should().Be(2);
        stdout.Should().BeEmpty();
        stderr.Should().NotBeEmpty().And.Contain("--expected-baseline-commit");
    }

    [TestMethod]
    [TestCategory("T-P1-14")]
    public async Task NoBaselineNoBootstrap_FailsBaselineRule()
    {
        var (exitCode, stdout, stderr) = await RunAsync(Evaluate("report-passing.xml"));

        exitCode.Should().Be(1);
        stderr.Should().NotBeEmpty();

        var baselineRule = Rule(stdout, "Baseline");
        baselineRule.GetProperty("verdict").GetBoolean().Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P1-14")]
    public async Task SuppliedBaselineMissing_IsInputError()
    {
        var (exitCode, stdout, stderr) = await RunAsync(Evaluate("report-passing.xml", extra: ["--baseline", Fixture("does-not-exist.json"), "--expected-baseline-commit", Commit]));

        exitCode.Should().Be(2);
        stdout.Should().BeEmpty();
        stderr.Should().NotBeEmpty().And.Contain("does-not-exist.json");
    }

    [TestMethod]
    [TestCategory("T-P1-18")]
    [TestCategory("T-P1-23")]
    public async Task EqualBaseline_DocumentationOnly_Passes()
    {
        var (exitCode, stdout, stderr) = await RunAsync(
            "evaluate",
            "--report", Fixture("report-passing.xml"),
            "--changed-files", Fixture("changes-docs-only.txt"),
            "--baseline", Fixture("baseline-passing.json"),
            "--expected-baseline-commit", Commit,
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0",
            "--exclude", "*.Tests.dll",
            "--exclude", "WeatherParser.Coverage*.dll");

        exitCode.Should().Be(0);
        stderr.Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("T-P1-10")]
    [TestCategory("T-P1-23")]
    public async Task EqualBaseline_CodeChange_FailsComparison()
    {
        var (exitCode, stdout, stderr) = await RunAsync(
            "evaluate",
            "--report", Fixture("report-passing.xml"),
            "--changed-files", Fixture("changes-docs-and-csproj.txt"),
            "--baseline", Fixture("baseline-passing.json"),
            "--expected-baseline-commit", Commit,
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0",
            "--exclude", "*.Tests.dll",
            "--exclude", "WeatherParser.Coverage*.dll");

        exitCode.Should().Be(1);
        stderr.Should().NotBeEmpty();

        var comparisonRule = Rule(stdout, "Comparison");
        comparisonRule.GetProperty("verdict").GetBoolean().Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P1-12")]
    public async Task Rounded100_AgainstExact100Baseline_FailsComparison()
    {
        var (exitCode, stdout, stderr) = await RunAsync(
            "evaluate",
            "--report", Fixture("report-rounded-100.xml"),
            "--changed-files", Fixture("changes-docs-and-csproj.txt"),
            "--baseline", Fixture("baseline-exact-100.json"),
            "--expected-baseline-commit", Commit,
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0",
            "--exclude", "*.Tests.dll",
            "--exclude", "WeatherParser.Coverage*.dll");

        exitCode.Should().Be(1);
        stderr.Should().Contain("Comparison");

        var comparisonRule = Rule(stdout, "Comparison");
        comparisonRule.GetProperty("verdict").GetBoolean().Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P1-11")]
    [DataRow("report-cobertura-branch-free.xml")]
    [DataRow("report-no-block-counters.xml")]
    public async Task NonNativeOrIncompleteReport_IsInputError(string report)
    {
        var (exitCode, stdout, stderr) = await RunAsync(Evaluate(report, extra: ["--bootstrap"]));

        exitCode.Should().Be(2);
        Rule(stdout, "Input").GetProperty("verdict").GetBoolean().Should().BeFalse();
        stderr.Should().NotBeEmpty();
    }

    [TestMethod]
    [TestCategory("T-P1-13")]
    [DataRow("report-malformed.xml")]
    [DataRow("does-not-exist.xml")]
    public async Task MalformedOrMissingReport_IsInputError(string report)
    {
        var (exitCode, stdout, stderr) = await RunAsync(Evaluate(report, extra: ["--bootstrap"]));

        exitCode.Should().Be(2);
        Rule(stdout, "Input").GetProperty("verdict").GetBoolean().Should().BeFalse();
        stderr.Should().NotBeEmpty();
    }

    [TestMethod]
    [TestCategory("T-P1-13")]
    public async Task ZeroDenominator_FailsMeasurement()
    {
        var (exitCode, stdout, stderr) = await RunAsync(Evaluate("report-zero-denominator.xml", extra: ["--bootstrap"]));

        exitCode.Should().Be(1);
        stderr.Should().NotBeEmpty();

        var measurementRule = Rule(stdout, "Measurement");
        measurementRule.GetProperty("verdict").GetBoolean().Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P1-17")]
    [TestCategory("T-P1-24")]
    public async Task DuplicateModule_IsInputError()
    {
        var (exitCode, stdout, stderr) = await RunAsync(Evaluate("report-duplicate-module.xml", extra: ["--bootstrap"]));

        exitCode.Should().Be(2);
        Rule(stdout, "Input").GetProperty("verdict").GetBoolean().Should().BeFalse();
        stderr.Should().NotBeEmpty();
    }

    [TestMethod]
    [TestCategory("T-P1-15")]
    [TestCategory("T-P1-16")]
    [DataRow("baseline-stale-commit.json")]
    [DataRow("baseline-other-tool-version.json")]
    [DataRow("baseline-other-exclusions.json")]
    public async Task IncomparableBaseline_FailsComparability(string baseline)
    {
        var (exitCode, stdout, stderr) = await RunAsync(
            "evaluate",
            "--report", Fixture("report-passing.xml"),
            "--changed-files", Fixture("changes-docs-and-csproj.txt"),
            "--baseline", Fixture(baseline),
            "--expected-baseline-commit", Commit,
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0",
            "--exclude", "*.Tests.dll",
            "--exclude", "WeatherParser.Coverage*.dll");

        exitCode.Should().Be(1);
        stderr.Should().NotBeEmpty();

        var comparabilityRule = Rule(stdout, "Comparability");
        comparabilityRule.GetProperty("verdict").GetBoolean().Should().BeFalse();
    }

    [TestMethod]
    [TestCategory("T-P1-16")]
    [DataRow("baseline-malformed.json")]
    [DataRow("baseline-missing-counters.json")]
    public async Task InvalidBaselineFile_IsInputError(string baseline)
    {
        var (exitCode, stdout, stderr) = await RunAsync(
            "evaluate",
            "--report", Fixture("report-passing.xml"),
            "--changed-files", Fixture("changes-docs-and-csproj.txt"),
            "--baseline", Fixture(baseline),
            "--expected-baseline-commit", Commit,
            "--tool-name", "Microsoft.CodeCoverage",
            "--tool-version", "18.11.0",
            "--exclude", "*.Tests.dll",
            "--exclude", "WeatherParser.Coverage*.dll");

        exitCode.Should().Be(2);
        Rule(stdout, "Input").GetProperty("verdict").GetBoolean().Should().BeFalse();
        stderr.Should().NotBeEmpty();
    }

    [TestMethod]
    [TestCategory("T-P1-24")]
    public async Task WriteBaseline_RoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var (exitCode, stdout, stderr) = await RunAsync(
                "write-baseline",
                "--report", Fixture("report-real-p1-07.xml"),
                "--commit", Commit,
                "--tool-name", "Microsoft.CodeCoverage",
                "--tool-version", "18.11.0",
                "--exclude", "*.Tests.dll",
                "--exclude", "WeatherParser.Coverage*.dll",
                "--output", path);

            exitCode.Should().Be(0);
            stdout.Should().BeEmpty();
            stderr.Should().BeEmpty();

            var baseline = await new BaselineMetadataReader(new FileReader()).ReadAsync(path, CancellationToken.None);
            baseline.Should().NotBeNull();
            baseline.CommitId.Should().Be(Commit);
            baseline.ToolName.Should().Be("Microsoft.CodeCoverage");
            baseline.ToolVersion.Should().Be("18.11.0");
            baseline.Format.Should().Be("native-xml");
            baseline.Exclusions.Should().Contain("*.Tests.dll");
            baseline.Exclusions.Should().Contain("WeatherParser.Coverage*.dll");
            baseline.Blocks.Covered.Should().Be(57);
            baseline.Blocks.NotCovered.Should().Be(33);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [TestMethod]
    [TestCategory("T-P1-13")]
    [TestCategory("T-P1-17")]
    public async Task OverflowingCounters_IsInputError()
    {
        var (exitCode, stdout, stderr) = await RunAsync(Evaluate("report-overflow.xml", extra: ["--bootstrap"]));

        exitCode.Should().Be(2);
        Rule(stdout, "Input").GetProperty("verdict").GetBoolean().Should().BeFalse();
        stderr.Should().NotBeEmpty();
    }

    [TestMethod]
    [TestCategory("T-P1-17")]
    [TestCategory("T-P1-24")]
    public async Task ExclusionPattern_DoesNotExcludeLongerAssemblyName_FailsMinimum()
    {
        var (exitCode, stdout, stderr) = await RunAsync(Evaluate("report-unanchored-exclusion.xml", extra: ["--bootstrap"]));

        exitCode.Should().Be(1);
        stderr.Should().NotBeEmpty();

        using var doc = JsonDocument.Parse(stdout);
        doc.RootElement.GetProperty("current").GetProperty("covered").GetInt64().Should().Be(10);
        doc.RootElement.GetProperty("current").GetProperty("notCovered").GetInt64().Should().Be(10);

        var minimumRule = Rule(stdout, "Minimum");
        minimumRule.GetProperty("verdict").GetBoolean().Should().BeFalse();
    }
}
