using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class OsthessenWetterStationMetadataLookupTests
{
    private static OsthessenWetterStationMetadata Entry(string name, decimal latitude = 50.5m, decimal longitude = 9.7m, decimal? altitudeM = null) =>
        new() { Name = name, Latitude = latitude, Longitude = longitude, AltitudeM = altitudeM };

    private static OsthessenWetterStationMetadataLookup Create(params OsthessenWetterStationMetadata[] entries) =>
        new(new OsthessenWetterOptions { Stations = entries.ToList() });

    private static void AssertInvalid(OsthessenWetterOptions options) =>
        FluentActions.Invoking(() => new OsthessenWetterStationMetadataLookup(options))
            .Should().Throw<OsthessenWetterException>().Which.ErrorId.Should().Be(OsthessenWetterException.OptionsInvalidErrorId);

    [TestMethod]
    public void Find_ExactNfcName_ReturnsMetadata()
    {
        var lookup = Create(Entry("Bimbach", 50.53m, 9.72m, 312m), Entry("Ro\u00dfbach"));
        var result = lookup.Find("Bimbach");
        result.Should().NotBeNull();
        result!.Name.Should().Be("Bimbach");
        result.Latitude.Should().Be(50.53m);
        result.Longitude.Should().Be(9.72m);
        result.AltitudeM.Should().Be(312m);
    }

    [TestMethod]
    public void Find_NfdConfiguredName_Matches()
    {
        var lookup = Create(Entry("Mu\u0308s"));
        lookup.Find("M\u00fcs").Should().NotBeNull();
        lookup.Find("Mu\u0308s").Should().NotBeNull();
        var nfcLookup = Create(Entry("M\u00fcs"));
        nfcLookup.Find("Mu\u0308s").Should().NotBeNull();
    }

    [TestMethod]
    public void Find_Unknown_ReturnsNull()
    {
        Create(Entry("Bimbach")).Find("Schlitz").Should().BeNull();
    }

    [TestMethod]
    public void Find_DifferentCaseOrSpelling_ReturnsNull()
    {
        var lookup = Create(Entry("Ro\u00dfbach"));
        lookup.Find("rossbach").Should().BeNull();
        lookup.Find("Rossbach").Should().BeNull();
        lookup.Find("ro\u00dfbach").Should().BeNull();
    }

    [TestMethod]
    public void Find_Null_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => Create(Entry("A")).Find(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Validate_LatitudeBounds()
    {
        FluentActions.Invoking(() => Create(Entry("A", latitude: -90m), Entry("B", latitude: 90m)))
            .Should().NotThrow();
        AssertInvalid(new OsthessenWetterOptions { Stations = new List<OsthessenWetterStationMetadata> { Entry("A", latitude: -90.000001m) } });
        AssertInvalid(new OsthessenWetterOptions { Stations = new List<OsthessenWetterStationMetadata> { Entry("A", latitude: 90.000001m) } });
    }

    [TestMethod]
    public void Validate_LongitudeBounds()
    {
        FluentActions.Invoking(() => Create(Entry("A", longitude: -180m), Entry("B", longitude: 180m)))
            .Should().NotThrow();
        AssertInvalid(new OsthessenWetterOptions { Stations = new List<OsthessenWetterStationMetadata> { Entry("A", longitude: -180.000001m) } });
        AssertInvalid(new OsthessenWetterOptions { Stations = new List<OsthessenWetterStationMetadata> { Entry("A", longitude: 180.000001m) } });
    }

    [TestMethod]
    public void Validate_DuplicateName_ThrowsOptionsInvalid()
    {
        AssertInvalid(new OsthessenWetterOptions { Stations = new List<OsthessenWetterStationMetadata> { Entry("Bimbach"), Entry("Bimbach") } });
        AssertInvalid(new OsthessenWetterOptions { Stations = new List<OsthessenWetterStationMetadata> { Entry("Mu\u0308s"), Entry("M\u00fcs") } });
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Validate_EmptyName_ThrowsOptionsInvalid(string name)
    {
        AssertInvalid(new OsthessenWetterOptions { Stations = new List<OsthessenWetterStationMetadata> { Entry(name) } });
    }

    [TestMethod]
    public void Validate_EmptyName_ThrowsOptionsInvalid_NullName()
    {
        AssertInvalid(new OsthessenWetterOptions { Stations = new List<OsthessenWetterStationMetadata> { new() { Name = null! } } });
    }

    [TestMethod]
    public void Validate_NullEntry_ThrowsOptionsInvalid()
    {
        AssertInvalid(new OsthessenWetterOptions { Stations = new List<OsthessenWetterStationMetadata> { Entry("A"), null! } });
    }

    [TestMethod]
    public void Validate_NullList_ThrowsOptionsInvalid()
    {
        AssertInvalid(new OsthessenWetterOptions { Stations = null! });
    }

    [TestMethod]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new OsthessenWetterStationMetadataLookup(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Validate_MessageDoesNotEchoConfiguration()
    {
        var ex = FluentActions.Invoking(() => new OsthessenWetterStationMetadataLookup(new OsthessenWetterOptions { Stations = new List<OsthessenWetterStationMetadata> { Entry("SECRET-NAME", latitude: 91m) } }))
            .Should().Throw<OsthessenWetterException>().Which;
        ex.ErrorId.Should().Be(OsthessenWetterException.OptionsInvalidErrorId);
        ex.Message.Should().NotContain("SECRET-NAME");
    }

    [TestMethod]
    public void Find_AltitudeNone_IsNullNeverZero()
    {
        Create(Entry("A")).Find("A")!.AltitudeM.Should().BeNull();
    }

    [TestMethod]
    public void Find_ZeroCoordinatesAndAltitude_Valid()
    {
        var lookup = Create(Entry("A", 0m, 0m, 0m));
        var result = lookup.Find("A");
        result.Should().NotBeNull();
        result!.Latitude.Should().Be(0m);
        result.Longitude.Should().Be(0m);
        result.AltitudeM.Should().Be(0m);

        var lookup2 = Create(Entry("B", 1m, 1m, -12m));
        var result2 = lookup2.Find("B");
        result2.Should().NotBeNull();
        result2!.Latitude.Should().Be(1m);
        result2.Longitude.Should().Be(1m);
        result2.AltitudeM.Should().Be(-12m);
    }

    [TestMethod]
    public void Constructor_CopiesOptions_LaterChangesIgnored()
    {
        var options = new OsthessenWetterOptions { Stations = new List<OsthessenWetterStationMetadata> { Entry("A", 10m, 10m) } };
        var lookup = new OsthessenWetterStationMetadataLookup(options);
        options.Stations[0].Latitude = 20m;
        options.Stations.Add(Entry("B"));
        lookup.Find("A")!.Latitude.Should().Be(10m);
        lookup.Find("B").Should().BeNull();
    }

    [TestMethod]
    public void Find_ReturnsCopy()
    {
        var lookup = Create(Entry("A", 10m, 10m));
        var result1 = lookup.Find("A");
        result1!.Latitude = 20m;
        var result2 = lookup.Find("A");
        result2!.Latitude.Should().Be(10m);
    }

    [TestMethod]
    public void Validate_EmptyList_Accepted()
    {
        FluentActions.Invoking(() => Create()).Should().NotThrow();
        Create().Find("A").Should().BeNull();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesStationMetadataLookup()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IOsthessenWetterStationMetadataLookup>().Should().BeOfType<OsthessenWetterStationMetadataLookup>();
    }
}
