using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Export;

/// <summary>
/// Tests for the normalized time serialization contract.
/// </summary>
[TestClass]
public sealed class NormalizedTimeSerializationTests
{
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";
    private IJsonExportSerializer _jsonSubject = null!;
    private IXmlExportSerializer _xmlSubject = null!;
    private SourceTimeNormalizer _normalizer = null!;
    private CultureInfo _originalCulture = null!;
    private CultureInfo _originalUICulture = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _jsonSubject = new JsonExportSerializer();
        _xmlSubject = new XmlExportSerializer();
        _normalizer = new SourceTimeNormalizer(new TimeZoneResolver());
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
    public async Task NormalizedTimes_SerializeWithoutSuffixInBothFormatsUnderDeDe()
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var measured = _normalizer.InferDate(new TimeOnly(23, 55), new DateTime(2026, 7, 15, 22, 5, 0, DateTimeKind.Utc));
        var retrieved = _normalizer.NormalizeComplete(new DateTimeOffset(2026, 7, 16, 0, 5, 0, TimeSpan.FromHours(2)));

        var dataset = new ObservationDataset
        {
            Source = "s",
            RetrievedAtUtc = retrieved.UtcInstant,
            Measurements = new()
            {
                TemperatureC = new MeasurementValue
                {
                    Value = 21.5m,
                    MeasuredAtUtc = measured.UtcInstant
                }
            }
        };

        // Test JSON
        using var jsonStream = await SerializeJsonToStream(dataset);
        var jsonBytes = jsonStream.ToArray();
        using var doc = JsonDocument.Parse(jsonBytes);

        doc.RootElement.GetProperty("retrieved_at").GetString().Should().Be("2026-07-15T22:05:00");
        doc.RootElement.GetProperty("measurements")
            .GetProperty("temperature_c")
            .GetProperty("measured_at")
            .GetString()
            .Should()
            .Be("2026-07-15T21:55:00");

        // Test XML
        using var xmlStream = await SerializeXmlToStream(dataset);
        var docXml = XDocument.Load(xmlStream);

        var retrievedAtElement = docXml.Root!.Element(Ns + "retrieved_at")!;
        retrievedAtElement.Value.Should().Be("2026-07-15T22:05:00");

        var measuredAtElement = docXml.Root!
            .Element(Ns + "measurements")!
            .Element(Ns + "temperature_c")!
            .Element(Ns + "measured_at")!;
        measuredAtElement.Value.Should().Be("2026-07-15T21:55:00");
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
