using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Selection;
using FluentAssertions;

namespace WeatherParser.Package.Tests;

[TestClass]
public sealed class PackageSelectionEquivalenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    [TestMethod]
    public async Task PackageSelection_JsonAndXmlEquivalentToSelectedGraph_Cdc()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.ArrangeCdc();
        host.ArrangeMosmix();
        await host.Provider.GetRequiredService<IMosmixSEvidenceRefresher>().RefreshAsync(CancellationToken.None);

        var parameters = ConsumerSelectionHost.Parameters();
        var list = await host.Provider.GetRequiredService<ICdcStationSelector>().SelectAsync(parameters, CancellationToken.None);
        list.Stations.Should().HaveCount(1);

        var jsonBytes = await host.JsonAsync(list);
        var xmlBytes = await host.XmlAsync(list);
        AssertCdcEquivalent(list, jsonBytes, xmlBytes);

        var emptyList = await host.Provider.GetRequiredService<ICdcStationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(-50m), CancellationToken.None);
        emptyList.Stations.Should().BeEmpty();

        var emptyJsonBytes = await host.JsonAsync(emptyList);
        var emptyXmlBytes = await host.XmlAsync(emptyList);
        AssertCdcEquivalent(emptyList, emptyJsonBytes, emptyXmlBytes);
    }

    [TestMethod]
    public async Task PackageSelection_JsonAndXmlEquivalentToSelectedGraph_Mosmix()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.ArrangeCdc();
        host.ArrangeMosmix();
        await host.Provider.GetRequiredService<IMosmixSEvidenceRefresher>().RefreshAsync(CancellationToken.None);

        var parameters = ConsumerSelectionHost.Parameters();
        var list = await host.Provider.GetRequiredService<IMosmixLocationSelector>().SelectAsync(parameters, CancellationToken.None);
        list.Locations.Should().HaveCount(2);

        var jsonBytes = await host.JsonAsync(list);
        var xmlBytes = await host.XmlAsync(list);
        AssertMosmixEquivalent(list, jsonBytes, xmlBytes);

        var emptyList = await host.Provider.GetRequiredService<IMosmixLocationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(-50m), CancellationToken.None);
        emptyList.Locations.Should().BeEmpty();

        var emptyJsonBytes = await host.JsonAsync(emptyList);
        var emptyXmlBytes = await host.XmlAsync(emptyList);
        AssertMosmixEquivalent(emptyList, emptyJsonBytes, emptyXmlBytes);
    }

    private static void AssertSearchParameters(SearchParameters expected, JsonElement json, XElement xml)
    {
        json.GetProperty("latitude").GetDecimal().Should().Be(expected.Latitude);
        XmlDecimal(xml, "latitude").Should().Be(expected.Latitude);

        json.GetProperty("longitude").GetDecimal().Should().Be(expected.Longitude);
        XmlDecimal(xml, "longitude").Should().Be(expected.Longitude);

        json.GetProperty("altitude_m").GetDecimal().Should().Be(expected.AltitudeM);
        XmlDecimal(xml, "altitude_m").Should().Be(expected.AltitudeM);

        json.GetProperty("max_distance_km").GetDecimal().Should().Be(expected.MaxDistanceKm);
        XmlDecimal(xml, "max_distance_km").Should().Be(expected.MaxDistanceKm);

        json.GetProperty("max_altitude_difference_m").GetDecimal().Should().Be(expected.MaxAltitudeDifferenceM);
        XmlDecimal(xml, "max_altitude_difference_m").Should().Be(expected.MaxAltitudeDifferenceM);
    }

    private static void AssertCdcEquivalent(CdcStationList list, byte[] jsonBytes, byte[] xmlBytes)
    {
        using var jsonDoc = JsonDocument.Parse(jsonBytes);
        var j = jsonDoc.RootElement;
        var x = XDocument.Load(new MemoryStream(xmlBytes)).Root!;

        XmlText(x, "source").Should().Be(list.Source);
        XmlText(x, "retrieved_at").Should().Be(list.RetrievedAtWire);
        j.GetProperty("source").GetString().Should().Be(list.Source);
        j.GetProperty("retrieved_at").GetString().Should().Be(list.RetrievedAtWire);

        AssertSearchParameters(list.SearchParameters, j.GetProperty("search_parameters"), x.Element(Ns + "search_parameters")!);

        var jsonStations = j.GetProperty("stations").EnumerateArray().ToList();
        var xmlStations = x.Element(Ns + "stations")!.Elements(Ns + "station").ToList();
        jsonStations.Count.Should().Be(list.Stations.Count);
        xmlStations.Count.Should().Be(list.Stations.Count);

        for (int i = 0; i < list.Stations.Count; i++)
        {
            var station = list.Stations[i];
            var jStation = jsonStations[i];
            var xStation = xmlStations[i];

            XmlText(xStation, "station_id").Should().Be(station.StationId);
            jStation.GetProperty("station_id").GetString().Should().Be(station.StationId);

            XmlText(xStation, "name").Should().Be(station.Name);
            jStation.GetProperty("name").GetString().Should().Be(station.Name);

            XmlDecimal(xStation, "latitude").Should().Be(station.Latitude);
            jStation.GetProperty("latitude").GetDecimal().Should().Be(station.Latitude);

            XmlDecimal(xStation, "longitude").Should().Be(station.Longitude);
            jStation.GetProperty("longitude").GetDecimal().Should().Be(station.Longitude);

            XmlDecimal(xStation, "altitude_m").Should().Be(station.AltitudeM);
            jStation.GetProperty("altitude_m").GetDecimal().Should().Be(station.AltitudeM);

            XmlDecimal(xStation, "distance_km").Should().Be(station.DistanceKm);
            jStation.GetProperty("distance_km").GetDecimal().Should().Be(station.DistanceKm);

            XmlDecimal(xStation, "altitude_difference_m").Should().Be(station.AltitudeDifferenceM);
            jStation.GetProperty("altitude_difference_m").GetDecimal().Should().Be(station.AltitudeDifferenceM);

            var jsonMeasurements = jStation.GetProperty("available_measurements").EnumerateArray().Select(e => e.GetString()!).ToList();
            var xmlMeasurements = xStation.Element(Ns + "available_measurements")!.Elements(Ns + "measurement").Select(e => e.Value).ToList();
            jsonMeasurements.Should().Equal(station.AvailableMeasurements);
            xmlMeasurements.Should().Equal(station.AvailableMeasurements);
        }
    }

    private static void AssertMosmixEquivalent(MosmixLocationList list, byte[] jsonBytes, byte[] xmlBytes)
    {
        using var jsonDoc = JsonDocument.Parse(jsonBytes);
        var j = jsonDoc.RootElement;
        var x = XDocument.Load(new MemoryStream(xmlBytes)).Root!;

        XmlText(x, "source").Should().Be(list.Source);
        XmlText(x, "retrieved_at").Should().Be(list.RetrievedAtWire);
        j.GetProperty("source").GetString().Should().Be(list.Source);
        j.GetProperty("retrieved_at").GetString().Should().Be(list.RetrievedAtWire);

        AssertSearchParameters(list.SearchParameters, j.GetProperty("search_parameters"), x.Element(Ns + "search_parameters")!);

        var jsonLocations = j.GetProperty("locations").EnumerateArray().ToList();
        var xmlLocations = x.Element(Ns + "locations")!.Elements(Ns + "location").ToList();
        jsonLocations.Count.Should().Be(list.Locations.Count);
        xmlLocations.Count.Should().Be(list.Locations.Count);

        for (int i = 0; i < list.Locations.Count; i++)
        {
            var location = list.Locations[i];
            var jLocation = jsonLocations[i];
            var xLocation = xmlLocations[i];

            XmlText(xLocation, "mosmix_id").Should().Be(location.MosmixId);
            jLocation.GetProperty("mosmix_id").GetString().Should().Be(location.MosmixId);

            XmlText(xLocation, "name").Should().Be(location.Name);
            jLocation.GetProperty("name").GetString().Should().Be(location.Name);

            XmlDecimal(xLocation, "latitude").Should().Be(location.Latitude);
            jLocation.GetProperty("latitude").GetDecimal().Should().Be(location.Latitude);

            XmlDecimal(xLocation, "longitude").Should().Be(location.Longitude);
            jLocation.GetProperty("longitude").GetDecimal().Should().Be(location.Longitude);

            XmlDecimal(xLocation, "altitude_m").Should().Be(location.AltitudeM);
            jLocation.GetProperty("altitude_m").GetDecimal().Should().Be(location.AltitudeM);

            XmlDecimal(xLocation, "distance_km").Should().Be(location.DistanceKm);
            jLocation.GetProperty("distance_km").GetDecimal().Should().Be(location.DistanceKm);

            XmlDecimal(xLocation, "altitude_difference_m").Should().Be(location.AltitudeDifferenceM);
            jLocation.GetProperty("altitude_difference_m").GetDecimal().Should().Be(location.AltitudeDifferenceM);

            var jsonProducts = jLocation.GetProperty("products").EnumerateArray().ToList();
            var xmlProducts = xLocation.Element(Ns + "products")!.Elements(Ns + "product_availability").ToList();
            jsonProducts.Count.Should().Be(location.Products.Count);
            xmlProducts.Count.Should().Be(location.Products.Count);

            for (int k = 0; k < location.Products.Count; k++)
            {
                var product = location.Products[k];
                var jProduct = jsonProducts[k];
                var xProduct = xmlProducts[k];

                var expectedProduct = product.Product == MosmixProduct.MosmixS ? "MOSMIX_S" : "MOSMIX_L";
                XmlText(xProduct, "product").Should().Be(expectedProduct);
                jProduct.GetProperty("product").GetString().Should().Be(expectedProduct);

                var expectedAvailability = product.Availability switch
                {
                    AvailabilityState.Available => "available",
                    AvailabilityState.Unavailable => "unavailable",
                    AvailabilityState.Unknown => "unknown",
                    _ => throw new ArgumentOutOfRangeException()
                };
                XmlText(xProduct, "availability").Should().Be(expectedAvailability);
                jProduct.GetProperty("availability").GetString().Should().Be(expectedAvailability);

                var jsonElements = jProduct.GetProperty("available_forecast_elements").EnumerateArray().Select(e => e.GetString()!).ToList();
                var xmlElements = xProduct.Element(Ns + "available_forecast_elements")!.Elements(Ns + "element").Select(e => e.Value).ToList();
                jsonElements.Should().Equal(product.AvailableForecastElements);
                xmlElements.Should().Equal(product.AvailableForecastElements);

                if (product.EvidenceRetrievedAtWire is null)
                {
                    jProduct.TryGetProperty("evidence_retrieved_at", out _).Should().BeFalse();
                    xProduct.Element(Ns + "evidence_retrieved_at").Should().BeNull();
                }
                else
                {
                    XmlText(xProduct, "evidence_retrieved_at").Should().Be(product.EvidenceRetrievedAtWire);
                    jProduct.GetProperty("evidence_retrieved_at").GetString().Should().Be(product.EvidenceRetrievedAtWire);
                }

                if (product.EvidenceIssuedAtWire is null)
                {
                    jProduct.TryGetProperty("evidence_issued_at", out _).Should().BeFalse();
                    xProduct.Element(Ns + "evidence_issued_at").Should().BeNull();
                }
                else
                {
                    XmlText(xProduct, "evidence_issued_at").Should().Be(product.EvidenceIssuedAtWire);
                    jProduct.GetProperty("evidence_issued_at").GetString().Should().Be(product.EvidenceIssuedAtWire);
                }
            }
        }
    }

    private static decimal XmlDecimal(XElement parent, string name) => decimal.Parse(parent.Element(Ns + name)!.Value, CultureInfo.InvariantCulture);

    private static string XmlText(XElement parent, string name) => parent.Element(Ns + name)!.Value;
}
