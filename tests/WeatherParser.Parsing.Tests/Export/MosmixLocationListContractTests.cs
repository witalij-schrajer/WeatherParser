using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

/// <summary>
/// Tests for the MOSMIX location list contract.
/// </summary>
[TestClass]
public sealed class MosmixLocationListContractTests
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
    public async Task SerializeAsync_Json_MosmixList_PreservesAlphanumericAndLeadingZeroIds()
    {
        var subject = BuildList(
            new MosmixLocationListEntry
            {
                MosmixId = "X123A",
                Name = "Example",
                Latitude = 50.567m,
                Longitude = 9.653m,
                AltitudeM = 242m,
                DistanceKm = 2.1m,
                AltitudeDifferenceM = -8m,
                Products = new List<MosmixProductAvailability>()
            },
            new MosmixLocationListEntry
            {
                MosmixId = "01234",
                Name = "Example",
                Latitude = 50.567m,
                Longitude = 9.653m,
                AltitudeM = 242m,
                DistanceKm = 2.1m,
                AltitudeDifferenceM = -8m,
                Products = new List<MosmixProductAvailability>()
            });

        using var stream = new MemoryStream();
        await _jsonSubject.SerializeAsync(subject, stream, CancellationToken.None);
        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream.ToArray());

        doc.RootElement.GetProperty("schema_version").GetString().Should().Be("1");
        doc.RootElement.GetProperty("source").GetString().Should().Be("dwd-mosmix");
        doc.RootElement.GetProperty("retrieved_at").GetString().Should().Be("2026-07-15T12:05:00");

        var locations = doc.RootElement.GetProperty("locations");
        locations.GetArrayLength().Should().Be(2);

        var loc0 = locations[0];
        loc0.GetProperty("mosmix_id").GetString().Should().Be("X123A");

        var loc1 = locations[1];
        loc1.GetProperty("mosmix_id").GetString().Should().Be("01234");
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_MosmixList_PreservesAlphanumericAndLeadingZeroIds()
    {
        var subject = BuildList(
            new MosmixLocationListEntry
            {
                MosmixId = "X123A",
                Name = "Example",
                Latitude = 50.567m,
                Longitude = 9.653m,
                AltitudeM = 242m,
                DistanceKm = 2.1m,
                AltitudeDifferenceM = -8m,
                Products = new List<MosmixProductAvailability>()
            },
            new MosmixLocationListEntry
            {
                MosmixId = "01234",
                Name = "Example",
                Latitude = 50.567m,
                Longitude = 9.653m,
                AltitudeM = 242m,
                DistanceKm = 2.1m,
                AltitudeDifferenceM = -8m,
                Products = new List<MosmixProductAvailability>()
            });

        using var stream = new MemoryStream();
        await _xmlSubject.SerializeAsync(subject, stream, CancellationToken.None);
        stream.Position = 0;
        var doc = XDocument.Load(stream);

        doc.Root!.Name.Should().Be(Ns + "mosmix_location_list");

        var locations = doc.Root.Element(Ns + "locations")!;
        locations.Elements(Ns + "location").Should().HaveCount(2);

        var loc0 = locations.Elements(Ns + "location").ElementAt(0);
        loc0.Element(Ns + "mosmix_id")!.Value.Should().Be("X123A");

        var loc1 = locations.Elements(Ns + "location").ElementAt(1);
        loc1.Element(Ns + "mosmix_id")!.Value.Should().Be("01234");
    }

    [TestMethod]
    public async Task SerializeAsync_Json_MosmixList_WritesProductAvailabilityExplicitly()
    {
        var subject = BuildList(
            BuildLocation("00001"));

        using var stream = new MemoryStream();
        await _jsonSubject.SerializeAsync(subject, stream, CancellationToken.None);
        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream.ToArray());

        var locations = doc.RootElement.GetProperty("locations");
        locations.GetArrayLength().Should().Be(1);

        var loc = locations[0];
        var products = loc.GetProperty("products");
        products.GetArrayLength().Should().Be(2);

        var prod0 = products[0];
        prod0.GetProperty("product").GetString().Should().Be("MOSMIX_L");
        prod0.GetProperty("availability").GetString().Should().Be("available");

        var elements0 = prod0.GetProperty("available_forecast_elements");
        elements0.GetArrayLength().Should().Be(2);
        elements0[0].GetString().Should().Be("TTT");
        elements0[1].GetString().Should().Be("PPPP");

        var prod1 = products[1];
        prod1.GetProperty("product").GetString().Should().Be("MOSMIX_S");
        prod1.GetProperty("availability").GetString().Should().Be("unknown");
        var elements1 = prod1.GetProperty("available_forecast_elements");
        elements1.GetArrayLength().Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_MosmixList_WritesProductAvailabilityExplicitly()
    {
        var subject = BuildList(
            BuildLocation("00001"));

        using var stream = new MemoryStream();
        await _xmlSubject.SerializeAsync(subject, stream, CancellationToken.None);
        stream.Position = 0;
        var doc = XDocument.Load(stream);

        doc.Root!.Name.Should().Be(Ns + "mosmix_location_list");

        var locations = doc.Root.Element(Ns + "locations")!;
        locations.Elements(Ns + "location").Should().HaveCount(1);

        var loc = locations.Element(Ns + "location")!;
        var items = loc.Element(Ns + "products")!.Elements(Ns + "product_availability").ToList();
        items.Should().HaveCount(2);

        // First product availability
        var firstProduct = items[0];
        firstProduct.Element(Ns + "product")!.Value.Should().Be("MOSMIX_L");
        firstProduct.Element(Ns + "availability")!.Value.Should().Be("available");
        var elements0 = firstProduct.Element(Ns + "available_forecast_elements")!;
        elements0.Elements(Ns + "element").Should().HaveCount(2);
        elements0.Elements(Ns + "element").ElementAt(0).Value.Should().Be("TTT");
        elements0.Elements(Ns + "element").ElementAt(1).Value.Should().Be("PPPP");

        // Second product availability
        var secondProduct = items[1];
        secondProduct.Element(Ns + "product")!.Value.Should().Be("MOSMIX_S");
        secondProduct.Element(Ns + "availability")!.Value.Should().Be("unknown");
        var elements1 = secondProduct.Element(Ns + "available_forecast_elements")!;
        elements1.Elements().Should().BeEmpty();
    }

    [TestMethod]
    public async Task SerializeAsync_Json_EmptyMosmixList_WritesEmptyLocationsArray()
    {
        var subject = BuildList();

        using var stream = new MemoryStream();
        await _jsonSubject.SerializeAsync(subject, stream, CancellationToken.None);
        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream.ToArray());

        doc.RootElement.GetProperty("locations").GetArrayLength().Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_EmptyMosmixList_WritesEmptyLocationsElement()
    {
        var subject = BuildList();

        using var stream = new MemoryStream();
        await _xmlSubject.SerializeAsync(subject, stream, CancellationToken.None);
        stream.Position = 0;
        var doc = XDocument.Load(stream);

        var locations = doc.Root!.Element(Ns + "locations")!;
        locations.Elements().Should().BeEmpty();
    }

    private static MosmixLocationList BuildList(params MosmixLocationListEntry[] locations)
    {
        return new MosmixLocationList
        {
            SchemaVersion = "1",
            Source = "dwd-mosmix",
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            SearchParameters = new()
            {
                Latitude = 50.5549m,
                Longitude = 9.6738m,
                AltitudeM = 250m,
                MaxDistanceKm = 30m,
                MaxAltitudeDifferenceM = 200m
            },
            Locations = locations.ToList()
        };
    }

    private static MosmixLocationListEntry BuildLocation(string id)
    {
        return new MosmixLocationListEntry
        {
            MosmixId = id,
            Name = "Example",
            Latitude = 50.567m,
            Longitude = 9.653m,
            AltitudeM = 242m,
            DistanceKm = 2.1m,
            AltitudeDifferenceM = -8m,
            Products = new List<MosmixProductAvailability>
            {
                new()
                {
                    Product = MosmixProduct.MosmixL,
                    Availability = AvailabilityState.Available,
                    AvailableForecastElements = new List<string> { "TTT", "PPPP" }
                },
                new()
                {
                    Product = MosmixProduct.MosmixS,
                    Availability = AvailabilityState.Unknown,
                    AvailableForecastElements = new List<string>()
                }
            }
        };
    }
}
