using System.Globalization;
using System.Text.Json;
using System.Xml.Serialization;
using FluentAssertions;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class TimestampWireReadBackTests
{
    private const string XmlNamespace = "urn:weatherparser:export:v1";

    [TestMethod]
    public void JsonReadBack_WireTimestamps_RestoreUtcValues()
    {
        var json = """
            {"schema_version":"1","source":"s","retrieved_at":"2026-07-15T12:05:00","measurements":{"temperature_c":{"value":1.5,"measured_at":"2026-07-15T12:00:00"}}}
            """;

        var dataset = JsonSerializer.Deserialize<ObservationDataset>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        dataset.Should().NotBeNull();
        dataset!.RetrievedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        dataset!.RetrievedAtUtc.Should().Be(new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc));
        dataset.Measurements.TemperatureC!.Value.Should().Be(1.5m);
        dataset.Measurements.TemperatureC!.MeasuredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        dataset.Measurements.TemperatureC!.MeasuredAtUtc.Should().Be(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));
    }

    [TestMethod]
    public void XmlReadBack_WireTimestamps_RestoreUtcValues()
    {
        var xml = $"""
            <observation_dataset xmlns="{XmlNamespace}">
                <schema_version>1</schema_version>
                <source>s</source>
                <retrieved_at>2026-07-15T12:05:00</retrieved_at>
                <measurements>
                    <temperature_c>
                        <value>1.5</value>
                        <measured_at>2026-07-15T12:00:00</measured_at>
                    </temperature_c>
                </measurements>
            </observation_dataset>
            """;

        var serializer = new XmlSerializer(typeof(ObservationDataset), XmlNamespace);
        using var reader = new StringReader(xml);
        ObservationDataset? dataset = serializer.Deserialize(reader) as ObservationDataset;

        dataset.Should().NotBeNull();
        dataset!.RetrievedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        dataset!.RetrievedAtUtc.Should().Be(new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc));
        dataset.Measurements.TemperatureC!.Value.Should().Be(1.5m);
        dataset.Measurements.TemperatureC!.MeasuredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        dataset.Measurements.TemperatureC!.MeasuredAtUtc.Should().Be(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));
    }

    [TestMethod]
    public void RetrievedAtWire_MalformedText_ThrowsArgumentException()
    {
        var dataset = new ObservationDataset();
        var malformedTimestamps = new[]
        {
            "2026-07-15T12:05:00Z",
            "2026-07-15 12:05:00",
            "2026-07-15T12:05:00.123",
            "2026-02-30T12:00:00"
        };

        foreach (var text in malformedTimestamps)
        {
            FluentActions.Invoking(() => dataset.RetrievedAtWire = text)
                .Should().Throw<ArgumentException>();
        }
    }

    [TestMethod]
    public void MeasuredAtWire_Null_ThrowsArgumentException()
    {
        var measurement = new MeasurementValue();
        FluentActions.Invoking(() => measurement.MeasuredAtWire = null!)
            .Should().Throw<ArgumentException>();
    }
}
