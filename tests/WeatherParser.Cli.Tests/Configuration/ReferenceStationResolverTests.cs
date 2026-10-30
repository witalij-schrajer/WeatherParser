using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli;
using FluentAssertions;

namespace WeatherParser.Cli.Tests.Configuration;

[TestClass]
[TestCategory("T-P3-06")]
public sealed class ReferenceStationResolverTests
{
    private readonly IReferenceStationResolver _resolver = new ReferenceStationResolver();

    [TestMethod]
    public void ResolveStation_ArgumentOverConfiguration()
    {
        var result = _resolver.Resolve("00045", new CliConfiguration { Dwd = new DwdSection { ReferenceStationId = "00044" } });
        result.Should().Be("00045");
    }

    [TestMethod]
    public void ResolveStation_ConfigurationWhenNoArgument()
    {
        var result = _resolver.Resolve(null, new CliConfiguration { Dwd = new DwdSection { ReferenceStationId = "00044" } });
        result.Should().Be("00044");
    }

    [TestMethod]
    public void ResolveStation_NeitherPresent_DwdStationMissing()
    {
        var ex = FluentActions.Invoking(() => _resolver.Resolve(null, new CliConfiguration())).Should().Throw<CliException>().Which;
        ex.ErrorId.Should().Be("CLI-DWD-STATION-MISSING");
        ex.Message.Should().Be("No DWD station given; use --StationID <id> or the setting 'Dwd.ReferenceStationId'.");
    }

    [TestMethod]
    public void ResolveStation_EmptyArgument_ReturnedUnchangedForLibraryValidation()
    {
        // An explicitly supplied empty ID is not an omitted option; the library parser rejects it with STATION-ID-INVALID.
        var result = _resolver.Resolve("", new CliConfiguration { Dwd = new DwdSection { ReferenceStationId = "00044" } });
        result.Should().Be("");
    }

    [TestMethod]
    public void ResolveStation_NoArgumentEmptyConfiguration_DwdStationMissing()
    {
        var ex = FluentActions.Invoking(() => _resolver.Resolve(null, new CliConfiguration { Dwd = new DwdSection { ReferenceStationId = "" } })).Should().Throw<CliException>().Which;
        ex.ErrorId.Should().Be("CLI-DWD-STATION-MISSING");
        ex.Message.Should().Be("No DWD station given; use --StationID <id> or the setting 'Dwd.ReferenceStationId'.");
    }
}
