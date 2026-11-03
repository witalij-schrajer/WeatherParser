using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Cli.Diagnostics;

namespace WeatherParser.Cli.Tests.Diagnostics;

[TestClass]
[TestCategory("T-P3-09")]
public class ExitCodePolicyTests
{
    private IExitCodePolicy _policy = null!;

    [TestInitialize]
    public void Initialize()
    {
        _policy = new ExitCodePolicy();
    }

    [DataRow(CliErrorIds.ArgumentInvalid)]
    [DataRow(CliErrorIds.CommandMissing)]
    [DataRow(CliErrorIds.CommandConflict)]
    [DataRow(CliErrorIds.ConfigMissing)]
    [DataRow(CliErrorIds.ConfigInvalid)]
    [DataRow(CliErrorIds.CredentialsInvalid)]
    [DataRow(CliErrorIds.DwdStationMissing)]
    [DataRow(CliErrorIds.OsthessenPublicationNotPermitted)]
    [DataRow(CliErrorIds.OutputExists)]
    [DataRow(CliErrorIds.OutputFailed)]
    [DataRow("COORDINATE-INVALID")]
    [DataRow("SEARCH-PARAMETER-INVALID")]
    [DataRow("STATION-ID-INVALID")]
    [DataRow("MOSMIX-ID-INVALID")]
    [DataRow("OSTHESSEN-PATTERN-INVALID")]
    [TestMethod]
    public void Map_UsageAndConfigurationErrors_ExitUsage(string errorId)
    {
        Assert.AreEqual(ExitCodes.Usage, _policy.ForError(errorId));
    }

    [TestMethod]
    public void Map_CancelledError_ExitCancelled()
    {
        Assert.AreEqual(ExitCodes.Cancelled, _policy.ForError(CliErrorIds.Cancelled));
    }

    [DataRow(CliErrorIds.Unexpected)]
    [DataRow(CliErrorIds.MqttConnectFailed)]
    [DataRow(CliErrorIds.DeadlineExceeded)]
    [DataRow("OSTHESSEN-PAGE-UNAVAILABLE")]
    [DataRow("STATION-UNKNOWN")]
    [DataRow("CATALOG-RETRIEVAL-FAILED")]
    [TestMethod]
    public void Map_OtherErrors_ExitFailure(string errorId)
    {
        Assert.AreEqual(ExitCodes.Failure, _policy.ForError(errorId));
    }

    [TestMethod]
    public void Outcome_AllSucceeded_ExitSuccess()
    {
        Assert.AreEqual(ExitCodes.Success, _policy.ForOutcome(new CommandOutcome(3, 0, false)));
    }

    [TestMethod]
    public void Outcome_EmptySelection_ExitSuccess()
    {
        Assert.AreEqual(ExitCodes.Success, _policy.ForOutcome(new CommandOutcome(0, 0, false)));
    }

    [TestMethod]
    public void Outcome_SomeFailed_ExitPartial()
    {
        Assert.AreEqual(ExitCodes.Partial, _policy.ForOutcome(new CommandOutcome(2, 1, false)));
    }

    [TestMethod]
    public void Outcome_AllFailed_ExitFailure()
    {
        Assert.AreEqual(ExitCodes.Failure, _policy.ForOutcome(new CommandOutcome(0, 2, false)));
    }

    [TestMethod]
    public void Outcome_Cancelled_ExitCancelled()
    {
        Assert.AreEqual(ExitCodes.Cancelled, _policy.ForOutcome(new CommandOutcome(2, 0, true)));
    }

    [DataRow("CLI-OUTPUT-EXISTS")]
    [DataRow("CLI-OUTPUT-FAILED")]
    [DataRow("CLI-MQTT-PUBLISH-FAILED")]
    [DataRow("STATION-UNKNOWN")]
    [TestMethod]
    public void Map_RuntimeErrors_ExitFailure(string errorId)
    {
        Assert.AreEqual(1, _policy.ForRuntimeError(errorId));
    }

    [TestMethod]
    public void Map_RuntimeCancelled_ExitCancelled()
    {
        Assert.AreEqual(4, _policy.ForRuntimeError("CLI-CANCELLED"));
    }
}
