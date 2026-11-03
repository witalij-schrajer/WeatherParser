using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Coverage.Runner.Tests;

[TestClass]
[TestCategory("T-P1-09")]
public sealed class JsonResultWriterTests
{
    private StringWriter _stringWriter = null!;
    private JsonResultWriter _writer = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _stringWriter = new StringWriter();
        _writer = new JsonResultWriter(_stringWriter);
    }

    [TestMethod]
    public async Task Write_PassedResult_ProducesCamelCaseContract()
    {
        // Arrange
        var result = new CoverageResult(
            true,
            new BlockCounters(100, 25),
            [
                new RuleResult("Measurement", true, "Block counters 100/125 are valid."),
                new RuleResult("Minimum", true, "Block coverage 100/125 (80.00%) is at or above the required minimum of 80%."),
                new RuleResult("Baseline", true, "Initial bootstrap: no baseline, comparison skipped"),
                new RuleResult("Comparison", true, "Block coverage 100/125 (80.00%) is at or above baseline 100/125 (80.00%).")
            ]);

        // Act
        await _writer.WriteAsync(result, CancellationToken.None);

        var json = _stringWriter.ToString();

        // Assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("passed").GetBoolean().Should().BeTrue();
        var current = root.GetProperty("current");
        current.GetProperty("covered").GetInt64().Should().Be(100);
        current.GetProperty("notCovered").GetInt64().Should().Be(25);

        var rules = root.GetProperty("rules").EnumerateArray();
        var ruleResults = rules.Select(r => new
        {
            RuleId = r.GetProperty("ruleId").GetString()!,
            Verdict = r.GetProperty("verdict").GetBoolean(),
            Diagnostic = r.GetProperty("diagnostic").GetString()!
        }).ToArray();

        ruleResults.Length.Should().Be(4);
        ruleResults[0].RuleId.Should().Be("Measurement");
        ruleResults[0].Verdict.Should().BeTrue();
        ruleResults[1].RuleId.Should().Be("Minimum");
        ruleResults[1].Verdict.Should().BeTrue();
        ruleResults[2].RuleId.Should().Be("Baseline");
        ruleResults[2].Verdict.Should().BeTrue();
        ruleResults[3].RuleId.Should().Be("Comparison");
        ruleResults[3].Verdict.Should().BeTrue();

        // Verify no "total" property exists
        root.TryGetProperty("total", out _).Should().BeFalse();
    }

    [TestMethod]
    public async Task Write_InputFailureWithoutCurrent_WritesNullCurrent()
    {
        // Arrange
        var result = new CoverageResult(
            false,
            null,
            [
                new RuleResult("Input", false, "Coverage report 'nonexistent.xml' was not found.")
            ]);

        // Act
        await _writer.WriteAsync(result, CancellationToken.None);

        var json = _stringWriter.ToString();

        // Assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("passed").GetBoolean().Should().BeFalse();
        root.TryGetProperty("current", out _).Should().BeTrue();
        root.GetProperty("current").ValueKind.Should().Be(JsonValueKind.Null);

        var rules = root.GetProperty("rules").EnumerateArray().ToArray();
        rules.Length.Should().Be(1);
        rules[0].GetProperty("ruleId").GetString().Should().Be("Input");
        rules[0].GetProperty("verdict").GetBoolean().Should().BeFalse();
    }
}
