using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Cli.Diagnostics;

namespace WeatherParser.Cli.Tests.Diagnostics;

[TestClass]
[TestCategory("T-P3-08")]
public class CliErrorIdsTests
{
    private static readonly Regex C86Regex = new(@"\A[A-Z][A-Z0-9]*(-[A-Z0-9]+)*\z");

    [TestMethod]
    public void ErrorIds_MatchC86Grammar()
    {
        foreach (string id in CliErrorIds.All)
        {
            Assert.IsTrue(C86Regex.IsMatch(id), $"ID '{id}' does not match C-86 grammar.");
        }
    }

    [TestMethod]
    public void All_Has18DistinctEntries()
    {
        Assert.AreEqual(18, CliErrorIds.All.Count);
        Assert.AreEqual(18, CliErrorIds.All.Distinct().Count());
    }

    [TestMethod]
    public void All_NoneStartsWithMQTT()
    {
        foreach (string id in CliErrorIds.All)
        {
            Assert.IsFalse(id.StartsWith("MQTT-"), $"ID '{id}' starts with 'MQTT-'.");
        }
    }
}
