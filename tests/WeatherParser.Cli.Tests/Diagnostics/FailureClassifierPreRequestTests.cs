using FluentAssertions;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Diagnostics;

namespace WeatherParser.Cli.Tests.Diagnostics;

[TestClass]
[TestCategory("T-P3-08")]
public class FailureClassifierPreRequestTests
{
    [DataRow(CliCommand.ParseDwd, "STATION-ID-INVALID")]
    [DataRow(CliCommand.ParseMosmix, "MOSMIX-ID-INVALID")]
    [DataRow(CliCommand.ParseOstHessen, "OSTHESSEN-PATTERN-INVALID")]
    [TestMethod]
    public void Map_PreRequestValidation_IsCommandLevel(CliCommand command, string errorId)
    {
        var classifier = new FailureClassifier();
        var result = classifier.Classify(command, errorId);

        result.Should().Be(FailureKind.CommandLevel);
    }
}
