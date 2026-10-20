using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ObservationPressurePairValidationTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);

    private static PressureMeasurementValue Pressure(decimal value) => new() { Value = value, MeasuredAtUtc = Utc(12, 0), TimestampQuality = TimestampQuality.Source, PressureReference = PressureReference.SeaLevel };

    private static ObservationDataset Build(string scenario)
    {
        var dataset = ObservationTestData.ValidDataset();
        switch (scenario)
        {
            case "MmHgOnly":
                dataset.Measurements.PressureMmHg = Pressure(760m);
                dataset.Measurements.PressureHpa = null;
                break;
            case "DifferentTimestampQuality":
                dataset.Measurements.PressureHpa = Pressure(1013m);
                dataset.Measurements.PressureMmHg = Pressure(760m);
                dataset.Measurements.PressureMmHg!.TimestampQuality = TimestampQuality.EstimatedDate;
                dataset.Measurements.PressureMmHg!.SourceTimezone = "Europe/Berlin";
                dataset.TimestampQuality = TimestampQuality.EstimatedDate;
                break;
            case "DifferentTimezone":
                dataset.Measurements.PressureHpa = Pressure(1013m);
                dataset.Measurements.PressureMmHg = Pressure(760m);
                dataset.Measurements.PressureMmHg!.SourceTimezone = "Europe/Berlin";
                break;
            case "PeriodOnlyOnOne":
                dataset.Measurements.PressureHpa = Pressure(1013m);
                dataset.Measurements.PressureHpa!.Period = new MeasurementPeriod { StartUtc = Utc(11, 50), EndUtc = Utc(12, 0) };
                dataset.Measurements.PressureMmHg = Pressure(760m);
                break;
            case "DifferentPeriods":
                dataset.Measurements.PressureHpa = Pressure(1013m);
                dataset.Measurements.PressureHpa!.Period = new MeasurementPeriod { StartUtc = Utc(11, 50), EndUtc = Utc(12, 0) };
                dataset.Measurements.PressureMmHg = Pressure(760m);
                dataset.Measurements.PressureMmHg!.Period = new MeasurementPeriod { StartUtc = Utc(11, 40), EndUtc = Utc(12, 0) };
                break;
            case "MmHgDirectionOverflows":
                dataset.Measurements.PressureHpa = Pressure(1m);
                dataset.Measurements.PressureMmHg = Pressure(60000000000000000000000000000m);
                break;
            case "SamePeriods":
                var period = new MeasurementPeriod { StartUtc = Utc(11, 50), EndUtc = Utc(12, 0) };
                dataset.Measurements.PressureHpa = Pressure(1013m);
                dataset.Measurements.PressureHpa!.Period = period;
                dataset.Measurements.PressureMmHg = Pressure(760m);
                dataset.Measurements.PressureMmHg!.Period = period;
                break;
            case "LargestHpa":
                dataset.Measurements.PressureHpa = Pressure(decimal.MaxValue);
                dataset.Measurements.PressureMmHg = Pressure(59426010263325383304787484413m);
                break;
            default:
                throw new ArgumentException($"Unknown scenario: {scenario}");
        }

        return dataset;
    }

    private async Task AssertRejectedInBothFormats(ObservationDataset dataset)
    {
        await AssertRejected(stream => _json.SerializeAsync(dataset, stream, CancellationToken.None));
        await AssertRejected(stream => _xml.SerializeAsync(dataset, stream, CancellationToken.None));
    }

    private static async Task AssertRejected(Func<Stream, Task> serialize)
    {
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => serialize(stream))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    private async Task AssertAcceptedInBothFormats(ObservationDataset dataset)
    {
        await AssertAccepted(stream => _json.SerializeAsync(dataset, stream, CancellationToken.None));
        await AssertAccepted(stream => _xml.SerializeAsync(dataset, stream, CancellationToken.None));
    }

    private static async Task AssertAccepted(Func<Stream, Task> serialize)
    {
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => serialize(stream))
            .Should().NotThrowAsync();
        stream.Length.Should().BeGreaterThan(0);
    }

    [TestMethod]
    [DataRow("MmHgOnly")]
    [DataRow("DifferentTimestampQuality")]
    [DataRow("DifferentTimezone")]
    [DataRow("PeriodOnlyOnOne")]
    [DataRow("DifferentPeriods")]
    [DataRow("MmHgDirectionOverflows")]
    public async Task SerializeAsync_PressurePairViolatingC64_IsRejected(string scenario)
    {
        await AssertRejectedInBothFormats(Build(scenario));
    }

    [TestMethod]
    [DataRow("SamePeriods")]
    [DataRow("LargestHpa")]
    public async Task SerializeAsync_PressurePairSatisfyingC64_IsAccepted(string scenario)
    {
        await AssertAcceptedInBothFormats(Build(scenario));
    }
}
