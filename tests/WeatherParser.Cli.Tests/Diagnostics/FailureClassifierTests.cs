using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Diagnostics;

namespace WeatherParser.Cli.Tests.Diagnostics;

[TestClass]
public class FailureClassifierTests
{
    private IFailureClassifier _classifier = null!;

    [TestInitialize]
    public void Initialize()
    {
        _classifier = new FailureClassifier();
    }

    [DataRow(CliCommand.SelectDwd, "CATALOG-RETRIEVAL-FAILED", FailureKind.CommandLevel)]
    [DataRow(CliCommand.SelectMosmix, "MOSMIX-S-EVIDENCE-MISSING", FailureKind.CommandLevel)]
    [DataRow(CliCommand.ParseOstHessen, "OSTHESSEN-PAGE-UNAVAILABLE", FailureKind.CommandLevel)]
    [DataRow(CliCommand.ParseOstHessen, "OSTHESSEN-PATTERN-NO-MATCH", FailureKind.CommandLevel)]
    [DataRow(CliCommand.ParseOstHessen, "OSTHESSEN-FORMAT-CHANGED", FailureKind.CommandLevel)]
    [DataRow(CliCommand.ParseOstHessen, "OSTHESSEN-STATION-ID-COLLISION", FailureKind.CommandLevel)]
    [DataRow(CliCommand.ParseOstHessen, "OSTHESSEN-DEADLINE-EXCEEDED", FailureKind.CommandLevel)]
    [DataRow(CliCommand.ParseOstHessen, "OSTHESSEN-OPTIONS-INVALID", FailureKind.CommandLevel)]
    [DataRow(CliCommand.ParseOstHessen, "OSTHESSEN-STATION-OFFLINE", FailureKind.Item)]
    [DataRow(CliCommand.ParseOstHessen, "OSTHESSEN-NO-USABLE-STATION", FailureKind.Item)]
    [DataRow(CliCommand.ParseDwd, "CATALOG-RETRIEVAL-FAILED", FailureKind.Item)]
    [DataRow(CliCommand.ParseDwd, "STATION-UNKNOWN", FailureKind.Item)]
    [DataRow(CliCommand.ParseMosmix, "MOSMIX-RETRIEVAL-FAILED", FailureKind.Item)]
    [TestMethod]
    public void Map_FailureKind_ItemOrCommandLevel(CliCommand command, string errorId, FailureKind expected)
    {
        Assert.AreEqual(expected, _classifier.Classify(command, errorId));
    }

    [DataRow(CliCommand.ParseDwd, "CLI-CONFIG-INVALID")]
    [DataRow(CliCommand.ParseMosmix, "CLI-OUTPUT-FAILED")]
    [DataRow(CliCommand.ParseOstHessen, "CLI-MQTT-CONNECT-FAILED")]
    [DataRow(CliCommand.ParseDwd, "CLI-UNEXPECTED")]
    [DataRow(CliCommand.ParseMosmix, "CLI-DEADLINE-EXCEEDED")]
    [TestMethod]
    public void Map_CliFailures_AreCommandLevelForEveryCommand(CliCommand command, string errorId)
    {
        Assert.AreEqual(FailureKind.CommandLevel, _classifier.Classify(command, errorId));
    }
}
