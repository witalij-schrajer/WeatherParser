using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

/// <summary>
/// Tests for the CDC station list contract.
/// </summary>
[TestClass]
public sealed class CdcStationListContractTests
{
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    private IJsonExportSerializer _jsonSubject = null!;
    private IXmlExportSerializer _xmlSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _jsonSubject = new JsonExportSerializer();
        _xmlSubject = new XmlExportSerializer();
    }

    [TestMethod]
    public async Task SerializeAsync_Json_CdcList_WritesRequiredFields()
    {
        var subject = BuildList(withStation: true);

        using var stream = new MemoryStream();
        await _jsonSubject.SerializeAsync(subject, stream, CancellationToken.None);
        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream.ToArray());

        doc.RootElement.GetProperty("schema_version").GetString().Should().Be("1");
        doc.RootElement.GetProperty("source").GetString().Should().Be("dwd-cdc");
        doc.RootElement.GetProperty("retrieved_at").GetString().Should().Be("2026-07-15T12:05:00");

        var searchParams = doc.RootElement.GetProperty("search_parameters");
        searchParams.GetProperty("latitude").GetDecimal().Should().Be(50.5549m);
        searchParams.GetProperty("longitude").GetDecimal().Should().Be(9.6738m);
        searchParams.GetProperty("altitude_m").GetDecimal().Should().Be(250m);
        searchParams.GetProperty("max_distance_km").GetDecimal().Should().Be(30m);
        searchParams.GetProperty("max_altitude_difference_m").GetDecimal().Should().Be(200m);

        var stations = doc.RootElement.GetProperty("stations");
        stations.GetArrayLength().Should().Be(1);

        var station = stations[0];
        station.GetProperty("station_id").GetString().Should().Be("00044");
        station.GetProperty("name").GetString().Should().Be("Fulda-Horas");
        station.GetProperty("latitude").GetDecimal().Should().Be(50.567m);
        station.GetProperty("longitude").GetDecimal().Should().Be(9.653m);
        station.GetProperty("altitude_m").GetDecimal().Should().Be(242m);
        station.GetProperty("distance_km").GetDecimal().Should().Be(2.1m);
        station.GetProperty("altitude_difference_m").GetDecimal().Should().Be(-8m);

        var measurements = station.GetProperty("available_measurements");
        measurements.GetArrayLength().Should().Be(2);
        measurements[0].GetString().Should().Be("temperature_c");
        measurements[1].GetString().Should().Be("humidity_percent");
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_CdcList_WritesRequiredFields()
    {
        var subject = BuildList(withStation: true);

        using var stream = new MemoryStream();
        await _xmlSubject.SerializeAsync(subject, stream, CancellationToken.None);
        stream.Position = 0;
        var doc = XDocument.Load(stream);

        doc.Root!.Name.Should().Be(Ns + "cdc_station_list");

        doc.Root.Element(Ns + "schema_version")!.Value.Should().Be("1");
        doc.Root.Element(Ns + "source")!.Value.Should().Be("dwd-cdc");
        doc.Root.Element(Ns + "retrieved_at")!.Value.Should().Be("2026-07-15T12:05:00");

        var searchParams = doc.Root.Element(Ns + "search_parameters")!;
        searchParams.Element(Ns + "latitude")!.Value.Should().Be("50.5549");
        searchParams.Element(Ns + "longitude")!.Value.Should().Be("9.6738");
        searchParams.Element(Ns + "altitude_m")!.Value.Should().Be("250");
        searchParams.Element(Ns + "max_distance_km")!.Value.Should().Be("30");
        searchParams.Element(Ns + "max_altitude_difference_m")!.Value.Should().Be("200");

        var stations = doc.Root.Element(Ns + "stations")!;
        stations.Elements(Ns + "station").Should().HaveCount(1);

        var station = stations.Element(Ns + "station")!;
        station.Element(Ns + "station_id")!.Value.Should().Be("00044");
        station.Element(Ns + "name")!.Value.Should().Be("Fulda-Horas");
        station.Element(Ns + "latitude")!.Value.Should().Be("50.567");
        station.Element(Ns + "longitude")!.Value.Should().Be("9.653");
        station.Element(Ns + "altitude_m")!.Value.Should().Be("242");
        station.Element(Ns + "distance_km")!.Value.Should().Be("2.1");
        station.Element(Ns + "altitude_difference_m")!.Value.Should().Be("-8");

        var measurements = station.Element(Ns + "available_measurements")!;
        measurements.Elements(Ns + "measurement").Should().HaveCount(2);
        measurements.Elements(Ns + "measurement").ElementAt(0).Value.Should().Be("temperature_c");
        measurements.Elements(Ns + "measurement").ElementAt(1).Value.Should().Be("humidity_percent");
    }

    [TestMethod]
    public async Task SerializeAsync_Json_EmptyCdcList_WritesEmptyStationsArray()
    {
        var subject = BuildList(withStation: false);

        using var stream = new MemoryStream();
        await _jsonSubject.SerializeAsync(subject, stream, CancellationToken.None);
        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream.ToArray());

        doc.RootElement.GetProperty("stations").GetArrayLength().Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_EmptyCdcList_WritesEmptyStationsElement()
    {
        var subject = BuildList(withStation: false);

        using var stream = new MemoryStream();
        await _xmlSubject.SerializeAsync(subject, stream, CancellationToken.None);
        stream.Position = 0;
        var doc = XDocument.Load(stream);

        var stations = doc.Root!.Element(Ns + "stations")!;
        stations.Elements().Should().BeEmpty();
    }

    private static CdcStationList BuildList(bool withStation)
    {
        var list = new CdcStationList
        {
            SchemaVersion = "1",
            Source = "dwd-cdc",
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            SearchParameters = new()
            {
                Latitude = 50.5549m,
                Longitude = 9.6738m,
                AltitudeM = 250m,
                MaxDistanceKm = 30m,
                MaxAltitudeDifferenceM = 200m
            },
            Stations = withStation
                ? new List<CdcStationListEntry>
                {
                    new()
                    {
                        StationId = "00044",
                        Name = "Fulda-Horas",
                        Latitude = 50.567m,
                        Longitude = 9.653m,
                        AltitudeM = 242m,
                        DistanceKm = 2.1m,
                        AltitudeDifferenceM = -8m,
                        AvailableMeasurements = new List<string> { "temperature_c", "humidity_percent" }
                    }
                }
                : new List<CdcStationListEntry>()
        };

        return list;
    }
}
