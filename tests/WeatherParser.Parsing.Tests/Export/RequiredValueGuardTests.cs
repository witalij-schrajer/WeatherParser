using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class RequiredValueGuardTests
{
    private JsonExportSerializer? _jsonSubject;
    private XmlExportSerializer? _xmlSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        _jsonSubject = new JsonExportSerializer();
        _xmlSubject = new XmlExportSerializer();
    }

    [TestMethod]
    public async Task SerializeAsync_Json_UnsetRetrievedAt_ThrowsExportSerializationException()
    {
        var dataset = new ObservationDataset { Source = "s" };
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _jsonSubject!.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_UnsetRetrievedAt_ThrowsExportSerializationException()
    {
        var dataset = new ObservationDataset { Source = "s" };
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _xmlSubject!.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Json_UnsetMeasuredAt_ThrowsExportSerializationException()
    {
        var dataset = new ObservationDataset
        {
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            Measurements = new ObservationMeasurements { TemperatureC = new MeasurementValue { Value = 1m } }
        };
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _jsonSubject!.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_UnsetMeasuredAt_ThrowsExportSerializationException()
    {
        var dataset = new ObservationDataset
        {
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            Measurements = new ObservationMeasurements { TemperatureC = new MeasurementValue { Value = 1m } }
        };
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _xmlSubject!.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Json_UnsetMeasurementValue_ThrowsExportSerializationException()
    {
        var dataset = new ObservationDataset
        {
            Source = "s",
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            Measurements = new ObservationMeasurements { TemperatureC = new MeasurementValue { MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc) } }
        };
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _jsonSubject!.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_UnsetMeasurementValue_ThrowsExportSerializationException()
    {
        var dataset = new ObservationDataset
        {
            Source = "s",
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            Measurements = new ObservationMeasurements { TemperatureC = new MeasurementValue { MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc) } }
        };
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _xmlSubject!.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public void SchemaVersion_AssignedOtherValue_ThrowsArgumentException()
    {
        FluentActions.Invoking(() => new ObservationDataset().SchemaVersion = "2")
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void SchemaVersion_Default_IsApprovedLiteral()
    {
        new ObservationDataset().SchemaVersion.Should().Be("1");
    }

    [TestMethod]
    public void JsonReadBack_OtherSchemaVersion_IsRejected()
    {
        FluentActions.Invoking(() => JsonSerializer.Deserialize<ObservationDataset>("{\"schema_version\":\"2\",\"source\":\"s\",\"retrieved_at\":\"2026-07-15T12:05:00\",\"measurements\":{}}"))
            .Should().Throw<Exception>().Where(e => e is ArgumentException || e.InnerException is ArgumentException);
    }

    [TestMethod]
    public void XmlReadBack_OtherSchemaVersion_IsRejected()
    {
        var xml = "<observation_dataset xmlns=\"urn:weatherparser:export:v1\"><schema_version>2</schema_version><source>s</source><retrieved_at>2026-07-15T12:05:00</retrieved_at><measurements /></observation_dataset>";
        FluentActions.Invoking(() => new XmlSerializer(typeof(ObservationDataset)).Deserialize(new StringReader(xml)))
            .Should().Throw<InvalidOperationException>().Where(e => e.InnerException is ArgumentException);
    }
}
