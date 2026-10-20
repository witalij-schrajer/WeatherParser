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
        var dataset = ObservationTestData.ValidDataset();
        dataset.Measurements.TemperatureC = null;
        using var stream = await SerializeToStream(dataset);
        using var doc = JsonDocument.Parse(stream.ToArray());

        doc.RootElement.GetProperty("measurements").TryGetProperty("temperature_c", out _).Should().BeFalse();
    }

    [TestMethod]
    public async Task SerializeAsync_ZeroTemperature_WritesZeroValue()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Measurements.TemperatureC = ObservationTestData.Value(0.0m, ObservationTestData.MeasuredAtUtc);
        using var stream = await SerializeToStream(dataset);
        using var doc = JsonDocument.Parse(stream.ToArray());

        doc.RootElement.GetProperty("measurements").GetProperty("temperature_c").GetProperty("value").GetDecimal().Should().Be(0m);
    }

    [TestMethod]
    public async Task SerializeAsync_NonZeroTemperature_WritesValue()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Measurements.TemperatureC = ObservationTestData.Value(-3.5m, ObservationTestData.MeasuredAtUtc);
        using var stream = await SerializeToStream(dataset);
        using var doc = JsonDocument.Parse(stream.ToArray());

        doc.RootElement.GetProperty("measurements").GetProperty("temperature_c").GetProperty("value").GetDecimal().Should().Be(-3.5m);
    }

    private async Task<MemoryStream> SerializeToStream(ObservationDataset dataset)
    {
        var stream = new MemoryStream();
        await _subject.SerializeAsync(dataset, stream, default);
        stream.Position = 0;
        return stream;
    }
}
