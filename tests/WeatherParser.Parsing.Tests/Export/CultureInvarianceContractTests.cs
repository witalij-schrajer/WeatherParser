using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

/// <summary>
/// Tests for the culture invariance contract.
/// </summary>
[TestClass]
public sealed class CultureInvarianceContractTests
{
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";
    private IJsonExportSerializer _jsonSubject = null!;
    private IXmlExportSerializer _xmlSubject = null!;
    private CultureInfo _originalCulture = null!;
    private CultureInfo _originalUICulture = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _jsonSubject = new JsonExportSerializer();
        _xmlSubject = new XmlExportSerializer();
        _originalCulture = CultureInfo.CurrentCulture;
        _originalUICulture = CultureInfo.CurrentUICulture;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        CultureInfo.CurrentUICulture = _originalUICulture;
    }

    [TestMethod]
    [DataRow("de-DE")]
    [DataRow("en-US")]
    public async Task SerializeAsync_UnderCulture_WritesInvariantNumbersAndTimestamps(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var dataset = new ObservationDataset
        {
            Source = "test-source",
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            Measurements = new()
            {
                TemperatureC = new MeasurementValue
                {
                    Value = -1234.5m,
                    MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc)
                }
            }
        };

        // Test JSON
        using var jsonStream = await SerializeJsonToStream(dataset);
        var jsonBytes = jsonStream.ToArray();
        var jsonString = Encoding.UTF8.GetString(jsonBytes);
        using var doc = JsonDocument.Parse(jsonBytes);

        jsonString.Should().Contain("\"value\":-1234.5");
        doc.RootElement.GetProperty("retrieved_at").GetString().Should().Be("2026-07-15T12:05:00");
        doc.RootElement.GetProperty("measurements")
            .GetProperty("temperature_c")
            .GetProperty("measured_at")
            .GetString()
            .Should()
            .Be("2026-07-15T12:00:00");

        // Test XML
        using var xmlStream = await SerializeXmlToStream(dataset);
        var docXml = XDocument.Load(xmlStream);

        var valueElement = docXml.Root!.Element(Ns + "measurements")!.Element(Ns + "temperature_c")!.Element(Ns + "value")!;
        valueElement.Value.Should().Be("-1234.5");

        var retrievedAtElement = docXml.Root!.Element(Ns + "retrieved_at")!;
        retrievedAtElement.Value.Should().Be("2026-07-15T12:05:00");

        var measuredAtElement = docXml.Root!
            .Element(Ns + "measurements")!
            .Element(Ns + "temperature_c")!
            .Element(Ns + "measured_at")!;
        measuredAtElement.Value.Should().Be("2026-07-15T12:00:00");
    }

    private async Task<MemoryStream> SerializeJsonToStream(ObservationDataset dataset)
    {
        var stream = new MemoryStream();
        await _jsonSubject.SerializeAsync(dataset, stream, default);
        stream.Position = 0;
        return stream;
    }

    private async Task<MemoryStream> SerializeXmlToStream(ObservationDataset dataset)
    {
        var stream = new MemoryStream();
        await _xmlSubject.SerializeAsync(dataset, stream, default);
        stream.Position = 0;
        return stream;
    }
}
