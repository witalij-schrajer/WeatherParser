using System.Text.Json;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

/// <summary>
/// Tests for the JSON serialization contract of <see cref="ObservationDataset"/>.
/// </summary>
[TestClass]
public sealed class ObservationDatasetJsonContractTests
{
    private IJsonExportSerializer _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new JsonExportSerializer();
    }

    [TestMethod]
    public async Task SerializeAsync_MissingTemperature_OmitsTemperatureProperty()
    {
        var dataset = BuildDataset(temperature: null);
        using var stream = await SerializeToStream(dataset);
        using var doc = JsonDocument.Parse(stream.ToArray());

        doc.RootElement.GetProperty("measurements").TryGetProperty("temperature_c", out _).Should().BeFalse();
    }

    [TestMethod]
    public async Task SerializeAsync_ZeroTemperature_WritesZeroValue()
    {
        var dataset = BuildDataset(temperature: new MeasurementValue { Value = 0m, MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc) });
        using var stream = await SerializeToStream(dataset);
        using var doc = JsonDocument.Parse(stream.ToArray());

        doc.RootElement.GetProperty("measurements").GetProperty("temperature_c").GetProperty("value").GetDecimal().Should().Be(0m);
    }

    [TestMethod]
    public async Task SerializeAsync_NonZeroTemperature_WritesValue()
    {
        var dataset = BuildDataset(temperature: new MeasurementValue { Value = -3.5m, MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc) });
        using var stream = await SerializeToStream(dataset);
        using var doc = JsonDocument.Parse(stream.ToArray());

        doc.RootElement.GetProperty("measurements").GetProperty("temperature_c").GetProperty("value").GetDecimal().Should().Be(-3.5m);
    }

    private static ObservationDataset BuildDataset(MeasurementValue? temperature)
    {
        return new()
        {
            Source = "test-source",
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            Measurements = new()
            {
                TemperatureC = temperature
            }
        };
    }

    private async Task<MemoryStream> SerializeToStream(ObservationDataset dataset)
    {
        var stream = new MemoryStream();
        await _subject.SerializeAsync(dataset, stream, default);
        stream.Position = 0;
        return stream;
    }
}
