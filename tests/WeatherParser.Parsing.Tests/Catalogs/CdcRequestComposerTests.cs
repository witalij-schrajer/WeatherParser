using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class CdcRequestComposerTests
{
    private CdcRequestComposer _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new();
    }

    [TestMethod]
    public void StationLists_FourListsInProductOrderWithEndpointNames()
    {
        var endpoints = Endpoints();
        var result = _subject.StationLists(endpoints);
        result.Should().Equal(
            new CdcListRequestTarget("tu", "AirTemperatureStationList", new Uri("https://example.org/cdc/air_temperature/now/list.txt?version=2")),
            new CdcListRequestTarget("ff", "WindStationList", new Uri("https://example.org/cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt")),
            new CdcListRequestTarget("rr", "PrecipitationStationList", new Uri("https://example.org/cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt")),
            new CdcListRequestTarget("sd", "SolarStationList", new Uri("https://example.org/cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt")));
    }

    [TestMethod]
    [DataRow("tu", "AirTemperatureNowDirectory", "https://example.org/cdc/air_temperature/now/10minutenwerte_TU_00044_now.zip")]
    [DataRow("ff", "WindNowDirectory", "https://example.org/cdc/wind/now/10minutenwerte_wind_00044_now.zip")]
    [DataRow("rr", "PrecipitationNowDirectory", "https://example.org/cdc/precipitation/now/10minutenwerte_nieder_00044_now.zip")]
    [DataRow("sd", "SolarNowDirectory", "https://example.org/cdc/solar/now/10minutenwerte_SOLAR_00044_now.zip")]
    public void Archive_ComposesUriPerProduct(string code, string name, string uri)
    {
        var endpoints = Endpoints();
        var result = _subject.Archive(endpoints, "00044", code);
        result.EndpointName.Should().Be(name);
        result.Uri.OriginalString.Should().Be(uri);
    }

    [TestMethod]
    public void Archive_UnknownProductCode_ThrowsArgumentException()
    {
        var endpoints = Endpoints();
        Func<CdcArchiveRequestTarget> act = () => _subject.Archive(endpoints, "00044", "tx");
        act.Should().Throw<ArgumentException>().WithMessage("*tx*");
    }

    [TestMethod]
    public void Archive_EscapesStationId()
    {
        var endpoints = Endpoints();
        _subject.Archive(endpoints, "a b", "tu").Uri.OriginalString.Should().EndWith("10minutenwerte_TU_a%20b_now.zip");
    }

    [TestMethod]
    public void StationLists_NullEndpoints_ThrowsArgumentNullException()
    {
        Func<IReadOnlyList<CdcListRequestTarget>> act = () => _subject.StationLists(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Archive_NullEndpoints_ThrowsArgumentNullException()
    {
        Func<CdcArchiveRequestTarget> act = () => _subject.Archive(null!, "00044", "tu");
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Archive_NullStationId_ThrowsArgumentNullException()
    {
        var endpoints = Endpoints();
        Func<CdcArchiveRequestTarget> act = () => _subject.Archive(endpoints, null!, "tu");
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Archive_NullProductCode_ThrowsArgumentNullException()
    {
        var endpoints = Endpoints();
        Func<CdcArchiveRequestTarget> act = () => _subject.Archive(endpoints, "00044", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesCdcRequestComposer()
    {
        var services = new ServiceCollection();
        services.AddWeatherParsing();
        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<ICdcRequestComposer>();
        resolved.Should().BeOfType<CdcRequestComposer>();
    }

    private static CdcCatalogEndpoints Endpoints() => new()
    {
        AirTemperatureStationList = new Uri("https://example.org/cdc/air_temperature/now/list.txt?version=2"),
        AirTemperatureNowDirectory = new Uri("https://example.org/cdc/air_temperature/now/"),
        WindStationList = new Uri("https://example.org/cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt"),
        WindNowDirectory = new Uri("https://example.org/cdc/wind/now/"),
        PrecipitationStationList = new Uri("https://example.org/cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt"),
        PrecipitationNowDirectory = new Uri("https://example.org/cdc/precipitation/now/"),
        SolarStationList = new Uri("https://example.org/cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt"),
        SolarNowDirectory = new Uri("https://example.org/cdc/solar/now/"),
    };
}
