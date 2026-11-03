using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Package.Tests;

[TestClass]
public sealed class PackageSerializationTests
{
    private ServiceProvider _provider = null!;
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    [TestInitialize]
    public void TestInitialize()
    {
        _provider = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        _json = _provider.GetRequiredService<IJsonExportSerializer>();
        _xml = _provider.GetRequiredService<IXmlExportSerializer>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _provider.Dispose();
    }

    private async Task AssertBothFormatsAsync<TRoot>(TRoot root, string rootName) where TRoot : class
    {
        using var jsonStream = new MemoryStream();
        using var xmlStream = new MemoryStream();

        await _json.SerializeAsync(root, jsonStream, CancellationToken.None);
        await _xml.SerializeAsync(root, xmlStream, CancellationToken.None);

        using var json = JsonDocument.Parse(jsonStream.ToArray());
        json.RootElement.GetProperty("schema_version").GetString().Should().Be("1");

        var xml = XDocument.Load(new MemoryStream(xmlStream.ToArray())).Root!;
        xml.Name.Should().Be(XNamespace.Get("urn:weatherparser:export:v1") + rootName);

        var jsonPropertyNames = json.RootElement.EnumerateObject().Select(p => p.Name).ToList();
        var xmlChildElementNames = xml.Elements().Select(e => e.Name.LocalName).ToList();
        jsonPropertyNames.Should().Equal(xmlChildElementNames);
        jsonPropertyNames.Should().HaveCountGreaterThan(2);

        if (json.RootElement.TryGetProperty("source", out var source))
        {
            source.GetString().Should().NotBeEmpty();
            source.GetString().Should().Be(xml.Element(Ns + "source")?.Value);
        }

        if (rootName == "export_document")
        {
            json.RootElement.GetProperty("observations").GetArrayLength().Should().Be(1);
            xml.Element(Ns + "observations")!.Elements(Ns + "observation_dataset").Should().HaveCount(1);
            json.RootElement.GetProperty("observations").EnumerateArray().First().GetProperty("source").GetString().Should().Be("dwd:00044");
            xml.Element(Ns + "observations")!.Elements(Ns + "observation_dataset").First().Element(Ns + "source")!.Value.Should().Be("dwd:00044");
        }
    }

    [TestMethod]
    [DataRow("export_document")]
    [DataRow("cdc_station_list")]
    [DataRow("mosmix_location_list")]
    [DataRow("observation_dataset")]
    [DataRow("forecast_run")]
    public async Task Package_SerializesAllFiveRootsInBothFormats(string rootName)
    {
        var root = rootName switch
        {
            "export_document" => (object)ConsumerTestData.ValidDocument(),
            "cdc_station_list" => (object)ConsumerTestData.ValidCdcList(),
            "mosmix_location_list" => (object)ConsumerTestData.ValidMosmixList(),
            "observation_dataset" => (object)ConsumerTestData.ValidDataset(),
            "forecast_run" => (object)ConsumerTestData.ValidRun(),
            _ => default
        };

        switch (rootName)
        {
            case "export_document":
                await AssertBothFormatsAsync(ConsumerTestData.ValidDocument(), rootName);
                break;
            case "cdc_station_list":
                await AssertBothFormatsAsync(ConsumerTestData.ValidCdcList(), rootName);
                break;
            case "mosmix_location_list":
                await AssertBothFormatsAsync(ConsumerTestData.ValidMosmixList(), rootName);
                break;
            case "observation_dataset":
                await AssertBothFormatsAsync(ConsumerTestData.ValidDataset(), rootName);
                break;
            case "forecast_run":
                await AssertBothFormatsAsync(ConsumerTestData.ValidRun(), rootName);
                break;
            default:
                Assert.Fail(rootName);
                break;
        }
    }
}
