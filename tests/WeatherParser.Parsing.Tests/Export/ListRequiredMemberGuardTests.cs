using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ListRequiredMemberGuardTests
{
    private IJsonExportSerializer _jsonSubject = null!;
    private IXmlExportSerializer _xmlSubject = null!;
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    [TestInitialize]
    public void TestInitialize()
    {
        _jsonSubject = new JsonExportSerializer();
        _xmlSubject = new XmlExportSerializer();
    }

    private static CdcStationList BuildCdcList(string? missing)
    {
        var list = new CdcStationList();
        if (missing != "Source") list.Source = "dwd-cdc";
        if (missing != "RetrievedAtUtc") list.RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc);
        var search = new SearchParameters();
        if (missing != "Search.Latitude") search.Latitude = 50.5549m;
        if (missing != "Search.Longitude") search.Longitude = 9.6738m;
        if (missing != "Search.AltitudeM") search.AltitudeM = 250m;
        if (missing != "Search.MaxDistanceKm") search.MaxDistanceKm = 30m;
        if (missing != "Search.MaxAltitudeDifferenceM") search.MaxAltitudeDifferenceM = 200m;
        list.SearchParameters = search;
        var station = new CdcStationListEntry();
        if (missing != "Station.StationId") station.StationId = "00044";
        if (missing != "Station.Name") station.Name = "Fulda-Horas";
        if (missing != "Station.Latitude") station.Latitude = 50.567m;
        if (missing != "Station.Longitude") station.Longitude = 9.653m;
        if (missing != "Station.AltitudeM") station.AltitudeM = 242m;
        if (missing != "Station.DistanceKm") station.DistanceKm = 2.1m;
        if (missing != "Station.AltitudeDifferenceM") station.AltitudeDifferenceM = -8m;
        station.AvailableMeasurements.Add("temperature_c");
        list.Stations.Add(station);
        return list;
    }

    private static MosmixLocationList BuildMosmixList(string? missing)
    {
        var list = new MosmixLocationList();
        if (missing != "Source") list.Source = "dwd-mosmix";
        if (missing != "RetrievedAtUtc") list.RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc);
        if (missing != "Search.Latitude") list.SearchParameters.Latitude = 50.5549m;
        if (missing != "Search.Longitude") list.SearchParameters.Longitude = 9.6738m;
        if (missing != "Search.AltitudeM") list.SearchParameters.AltitudeM = 250m;
        if (missing != "Search.MaxDistanceKm") list.SearchParameters.MaxDistanceKm = 30m;
        if (missing != "Search.MaxAltitudeDifferenceM") list.SearchParameters.MaxAltitudeDifferenceM = 200m;
        var location = new MosmixLocationListEntry();
        if (missing != "Location.MosmixId") location.MosmixId = "X123A";
        if (missing != "Location.Name") location.Name = "Example";
        if (missing != "Location.Latitude") location.Latitude = 50.567m;
        if (missing != "Location.Longitude") location.Longitude = 9.653m;
        if (missing != "Location.AltitudeM") location.AltitudeM = 242m;
        if (missing != "Location.DistanceKm") location.DistanceKm = 2.1m;
        if (missing != "Location.AltitudeDifferenceM") location.AltitudeDifferenceM = -8m;
        var product = new MosmixProductAvailability();
        if (missing != "Product.Product") product.Product = MosmixProduct.MosmixL;
        if (missing != "Product.Availability") product.Availability = AvailabilityState.Available;
        product.AvailableForecastElements.Add("TTT");
        location.Products.Add(product);
        list.Locations.Add(location);
        return list;
    }

    [TestMethod]
    [DataRow("Source")]
    [DataRow("RetrievedAtUtc")]
    [DataRow("Search.Latitude")]
    [DataRow("Search.Longitude")]
    [DataRow("Search.AltitudeM")]
    [DataRow("Search.MaxDistanceKm")]
    [DataRow("Search.MaxAltitudeDifferenceM")]
    [DataRow("Station.StationId")]
    [DataRow("Station.Name")]
    [DataRow("Station.Latitude")]
    [DataRow("Station.Longitude")]
    [DataRow("Station.AltitudeM")]
    [DataRow("Station.DistanceKm")]
    [DataRow("Station.AltitudeDifferenceM")]
    public async Task SerializeAsync_Json_CdcListMissingRequiredMember_ThrowsAndWritesNothing(string missing)
    {
        var list = BuildCdcList(missing);
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _jsonSubject.SerializeAsync(list, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    [DataRow("Source")]
    [DataRow("RetrievedAtUtc")]
    [DataRow("Search.Latitude")]
    [DataRow("Search.Longitude")]
    [DataRow("Search.AltitudeM")]
    [DataRow("Search.MaxDistanceKm")]
    [DataRow("Search.MaxAltitudeDifferenceM")]
    [DataRow("Station.StationId")]
    [DataRow("Station.Name")]
    [DataRow("Station.Latitude")]
    [DataRow("Station.Longitude")]
    [DataRow("Station.AltitudeM")]
    [DataRow("Station.DistanceKm")]
    [DataRow("Station.AltitudeDifferenceM")]
    public async Task SerializeAsync_Xml_CdcListMissingRequiredMember_ThrowsAndWritesNothing(string missing)
    {
        var list = BuildCdcList(missing);
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _xmlSubject.SerializeAsync(list, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    [DataRow("Source")]
    [DataRow("RetrievedAtUtc")]
    [DataRow("Search.Latitude")]
    [DataRow("Search.Longitude")]
    [DataRow("Search.AltitudeM")]
    [DataRow("Search.MaxDistanceKm")]
    [DataRow("Search.MaxAltitudeDifferenceM")]
    [DataRow("Location.MosmixId")]
    [DataRow("Location.Name")]
    [DataRow("Location.Latitude")]
    [DataRow("Location.Longitude")]
    [DataRow("Location.AltitudeM")]
    [DataRow("Location.DistanceKm")]
    [DataRow("Location.AltitudeDifferenceM")]
    [DataRow("Product.Product")]
    [DataRow("Product.Availability")]
    public async Task SerializeAsync_Json_MosmixListMissingRequiredMember_ThrowsAndWritesNothing(string missing)
    {
        var list = BuildMosmixList(missing);
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _jsonSubject.SerializeAsync(list, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    [DataRow("Source")]
    [DataRow("RetrievedAtUtc")]
    [DataRow("Search.Latitude")]
    [DataRow("Search.Longitude")]
    [DataRow("Search.AltitudeM")]
    [DataRow("Search.MaxDistanceKm")]
    [DataRow("Search.MaxAltitudeDifferenceM")]
    [DataRow("Location.MosmixId")]
    [DataRow("Location.Name")]
    [DataRow("Location.Latitude")]
    [DataRow("Location.Longitude")]
    [DataRow("Location.AltitudeM")]
    [DataRow("Location.DistanceKm")]
    [DataRow("Location.AltitudeDifferenceM")]
    [DataRow("Product.Product")]
    [DataRow("Product.Availability")]
    public async Task SerializeAsync_Xml_MosmixListMissingRequiredMember_ThrowsAndWritesNothing(string missing)
    {
        var list = BuildMosmixList(missing);
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _xmlSubject.SerializeAsync(list, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Json_ExplicitZeroValues_AreWritten()
    {
        var list = BuildCdcList(null);
        list.SearchParameters.MaxAltitudeDifferenceM = 0m;
        var s = list.Stations[0];
        s.Latitude = 0m;
        s.Longitude = 0m;
        s.DistanceKm = 0m;
        s.AltitudeDifferenceM = 0m;

        using var stream = new MemoryStream();
        await _jsonSubject.SerializeAsync(list, stream, CancellationToken.None);

        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        var searchParams = doc.RootElement.GetProperty("search_parameters");
        searchParams.GetProperty("max_altitude_difference_m").GetDecimal().Should().Be(0m);
        var station = doc.RootElement.GetProperty("stations")[0];
        station.GetProperty("latitude").GetDecimal().Should().Be(0m);
        station.GetProperty("longitude").GetDecimal().Should().Be(0m);
        station.GetProperty("distance_km").GetDecimal().Should().Be(0m);
        station.GetProperty("altitude_difference_m").GetDecimal().Should().Be(0m);
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_ExplicitZeroValues_AreWritten()
    {
        var list = BuildCdcList(null);
        list.SearchParameters.MaxAltitudeDifferenceM = 0m;
        var s = list.Stations[0];
        s.Latitude = 0m;
        s.Longitude = 0m;
        s.DistanceKm = 0m;
        s.AltitudeDifferenceM = 0m;

        using var stream = new MemoryStream();
        await _xmlSubject.SerializeAsync(list, stream, CancellationToken.None);

        stream.Position = 0;
        var doc = XDocument.Load(stream);
        var searchParams = doc.Root!.Element(Ns + "search_parameters")!;
        decimal.Parse(searchParams.Element(Ns + "max_altitude_difference_m")!.Value, CultureInfo.InvariantCulture)
            .Should().Be(0m);
        var station = doc.Root.Element(Ns + "stations")!.Elements(Ns + "station").First();
        decimal.Parse(station.Element(Ns + "latitude")!.Value, CultureInfo.InvariantCulture).Should().Be(0m);
        decimal.Parse(station.Element(Ns + "longitude")!.Value, CultureInfo.InvariantCulture).Should().Be(0m);
        decimal.Parse(station.Element(Ns + "distance_km")!.Value, CultureInfo.InvariantCulture).Should().Be(0m);
        decimal.Parse(station.Element(Ns + "altitude_difference_m")!.Value, CultureInfo.InvariantCulture).Should().Be(0m);
    }

    [TestMethod]
    public async Task SerializeAsync_Json_SchemaVersionNotAssigned_IsWrittenAsOne()
    {
        var list = BuildMosmixList(null);
        using var stream = new MemoryStream();
        await _jsonSubject.SerializeAsync(list, stream, CancellationToken.None);

        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        doc.RootElement.GetProperty("schema_version").GetString().Should().Be("1");
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_SchemaVersionNotAssigned_IsWrittenAsOne()
    {
        var list = BuildMosmixList(null);
        using var stream = new MemoryStream();
        await _xmlSubject.SerializeAsync(list, stream, CancellationToken.None);

        stream.Position = 0;
        var doc = XDocument.Load(stream);
        doc.Root!.Element(Ns + "schema_version")!.Value.Should().Be("1");
    }
}
