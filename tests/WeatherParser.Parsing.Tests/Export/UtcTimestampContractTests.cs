using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

/// <summary>
/// Tests for the UTC timestamp contract.
/// </summary>
[TestClass]
public sealed class UtcTimestampContractTests
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
    public async Task SerializeAsync_Json_UtcTimestamps_WriteSecondsWithoutSuffix()
    {
        var dataset = new ObservationDataset
        {
            Source = "test-source",
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            Measurements = new()
            {
                TemperatureC = new MeasurementValue
                {
                    Value = 12.3m,
                    MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc)
                }
            }
        };

        using var jsonStream = await SerializeJsonToStream(dataset);
        using var doc = JsonDocument.Parse(jsonStream.ToArray());

        doc.RootElement.GetProperty("retrieved_at").GetString().Should().Be("2026-07-15T12:05:00");
        doc.RootElement.GetProperty("measurements")
            .GetProperty("temperature_c")
            .GetProperty("measured_at")
            .GetString()
            .Should()
            .Be("2026-07-15T12:00:00");
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_UtcTimestamps_WriteSecondsWithoutSuffix()
    {
        var dataset = new ObservationDataset
        {
            Source = "test-source",
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            Measurements = new()
            {
                TemperatureC = new MeasurementValue
                {
                    Value = 12.3m,
                    MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc)
                }
            }
        };

        using var xmlStream = await SerializeXmlToStream(dataset);
        var doc = XDocument.Load(xmlStream);

        var retrievedAtElement = doc.Root!.Element(Ns + "retrieved_at")!;
        retrievedAtElement.Value.Should().Be("2026-07-15T12:05:00");

        var measuredAtElement = doc.Root!
            .Element(Ns + "measurements")!
            .Element(Ns + "temperature_c")!
            .Element(Ns + "measured_at")!;
        measuredAtElement.Value.Should().Be("2026-07-15T12:00:00");
    }

    [TestMethod]
    public void RetrievedAtUtc_NonUtcKind_ThrowsArgumentException()
    {
        var dataset = new ObservationDataset();

        FluentActions.Invoking(() => dataset.RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Local))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => dataset.RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Unspecified))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void RetrievedAtUtc_SubSecondValue_ThrowsArgumentException()
    {
        var dataset = new ObservationDataset();

        FluentActions.Invoking(() => dataset.RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc).AddMilliseconds(1))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void MeasuredAtUtc_NonUtcKind_ThrowsArgumentException()
    {
        var dataset = new ObservationDataset();

        FluentActions.Invoking(() => dataset.Measurements = new()
        {
            TemperatureC = new MeasurementValue
            {
                Value = 12.3m,
                MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Local)
            }
        })
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void MeasuredAtUtc_SubSecondValue_ThrowsArgumentException()
    {
        var dataset = new ObservationDataset();

        FluentActions.Invoking(() => dataset.Measurements = new()
        {
            TemperatureC = new MeasurementValue
            {
                Value = 12.3m,
                MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc).AddMilliseconds(1)
            }
        })
            .Should().Throw<ArgumentException>();
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
