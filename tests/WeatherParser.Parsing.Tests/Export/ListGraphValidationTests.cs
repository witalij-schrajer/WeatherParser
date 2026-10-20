using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ListGraphValidationTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    private async Task AssertRejected<TRoot>(TRoot root) where TRoot : class
    {
        using var jsonStream = new MemoryStream();
        Func<Task> jsonAct = () => _json.SerializeAsync(root, jsonStream, CancellationToken.None);
        await jsonAct.Should().ThrowAsync<ExportSerializationException>();
        jsonStream.Length.Should().Be(0);

        using var xmlStream = new MemoryStream();
        Func<Task> xmlAct = () => _xml.SerializeAsync(root, xmlStream, CancellationToken.None);
        await xmlAct.Should().ThrowAsync<ExportSerializationException>();
        xmlStream.Length.Should().Be(0);
    }

    [TestMethod]
    [DataRow("NullStations")]
    [DataRow("NullStationItem")]
    [DataRow("NullAvailableMeasurements")]
    [DataRow("NullMeasurementItem")]
    [DataRow("NullSearchParameters")]
    public async Task Serialize_CdcListViolatingGraphRule_IsRejectedInBothFormatsAndWritesNothing(string scenario)
    {
        var list = ListTestData.ValidCdcList();

        switch (scenario)
        {
            case "NullStations":
                list.Stations = null!;
                break;
            case "NullStationItem":
                list.Stations.Add(null!);
                break;
            case "NullAvailableMeasurements":
                list.Stations[0].AvailableMeasurements = null!;
                break;
            case "NullMeasurementItem":
                list.Stations[0].AvailableMeasurements.Add(null!);
                break;
            case "NullSearchParameters":
                list.SearchParameters = null!;
                break;
            default:
                throw new ArgumentException(scenario);
        }

        await AssertRejected(list);
    }

    [TestMethod]
    [DataRow("NullLocations")]
    [DataRow("NullLocationItem")]
    [DataRow("NullProducts")]
    [DataRow("NullProductItem")]
    [DataRow("NullElements")]
    [DataRow("NullElementItem")]
    [DataRow("NullSearchParameters")]
    [DataRow("DuplicateProduct")]
    [DataRow("UnknownWithElements")]
    [DataRow("UnavailableWithElements")]
    public async Task Serialize_MosmixListViolatingGraphRule_IsRejectedInBothFormatsAndWritesNothing(string scenario)
    {
        var list = ListTestData.ValidMosmixList();
        var products = list.Locations[0].Products;

        switch (scenario)
        {
            case "NullLocations":
                list.Locations = null!;
                break;
            case "NullLocationItem":
                list.Locations.Add(null!);
                break;
            case "NullProducts":
                list.Locations[0].Products = null!;
                break;
            case "NullProductItem":
                products.Add(null!);
                break;
            case "NullElements":
                products[0].AvailableForecastElements = null!;
                break;
            case "NullElementItem":
                products[0].AvailableForecastElements.Add(null!);
                break;
            case "NullSearchParameters":
                list.SearchParameters = null!;
                break;
            case "DuplicateProduct":
                products.Add(new MosmixProductAvailability { Product = MosmixProduct.MosmixL, Availability = AvailabilityState.Unavailable });
                break;
            case "UnknownWithElements":
                products[1].AvailableForecastElements.Add("TTT");
                break;
            case "UnavailableWithElements":
                products[1].Availability = AvailabilityState.Unavailable;
                products[1].AvailableForecastElements.Add("TTT");
                break;
            default:
                throw new ArgumentException(scenario);
        }

        await AssertRejected(list);
    }

    [TestMethod]
    public async Task Serialize_ValidLists_SucceedInBothFormats()
    {
        var cdcList = ListTestData.ValidCdcList();
        var mosmixList = ListTestData.ValidMosmixList();

        using (var jsonStream = new MemoryStream())
        {
            await _json.SerializeAsync(cdcList, jsonStream, CancellationToken.None);
            jsonStream.Length.Should().BeGreaterThan(0);
        }

        using (var xmlStream = new MemoryStream())
        {
            await _xml.SerializeAsync(cdcList, xmlStream, CancellationToken.None);
            xmlStream.Length.Should().BeGreaterThan(0);
        }

        using (var jsonStream = new MemoryStream())
        {
            await _json.SerializeAsync(mosmixList, jsonStream, CancellationToken.None);
            jsonStream.Length.Should().BeGreaterThan(0);
        }

        using (var xmlStream = new MemoryStream())
        {
            await _xml.SerializeAsync(mosmixList, xmlStream, CancellationToken.None);
            xmlStream.Length.Should().BeGreaterThan(0);
        }
    }
}
