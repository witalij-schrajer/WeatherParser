using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ObservationGraphValidationTests
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
            case "Valid":
                break;
            case "NoQuantity":
                dataset.Measurements.TemperatureC = null;
                dataset.Measurements.HumidityPercent = null;
                break;
            case "SourceStationMismatch":
                dataset.Source = "dwd:00045";
                break;
            case "SourceUppercaseProvider":
                dataset.Source = "DWD:00044";
                break;
            case "SourceProviderWithNewline":
                dataset.Source = "dwd\n:00044";
                break;
            case "SourceWithoutSeparator":
                dataset.Source = "00044";
                break;
            case "DatasetTimeNotLatest":
                dataset.MeasuredAtUtc = Utc(11, 59);
                break;
            case "DatasetEstimatedWithoutEstimate":
                dataset.TimestampQuality = TimestampQuality.EstimatedDate;
                break;
            case "EstimateHiddenAtDataset":
                dataset.Measurements.HumidityPercent = ObservationTestData.Value(55m, Utc(12, 0));
                dataset.Measurements.HumidityPercent!.TimestampQuality = TimestampQuality.EstimatedDate;
                dataset.Measurements.HumidityPercent!.SourceTimezone = "Europe/Berlin";
                break;
            case "EstimateWithoutBerlin":
                dataset.Measurements.HumidityPercent = ObservationTestData.Value(55m, Utc(12, 0));
                dataset.Measurements.HumidityPercent!.TimestampQuality = TimestampQuality.EstimatedDate;
                dataset.TimestampQuality = TimestampQuality.EstimatedDate;
                break;
            case "PeriodEmpty":
                dataset.Measurements.TemperatureC!.Period = new MeasurementPeriod { StartUtc = Utc(12, 0), EndUtc = Utc(12, 0) };
                break;
            case "PeriodReversed":
                dataset.Measurements.TemperatureC!.Period = new MeasurementPeriod { StartUtc = Utc(12, 0), EndUtc = Utc(11, 50) };
                break;
            case "PressureHpaOnly":
                dataset.Measurements.PressureHpa = Pressure(1013m);
                dataset.Measurements.PressureMmHg = null;
                break;
            case "PressureDifferentTime":
                dataset.Measurements.PressureHpa = Pressure(1013m);
                dataset.Measurements.PressureMmHg = Pressure(760m);
                dataset.Measurements.PressureMmHg!.MeasuredAtUtc = Utc(11, 50);
                break;
            case "PressureDifferentReference":
                dataset.Measurements.PressureHpa = Pressure(1013m);
                dataset.Measurements.PressureMmHg = Pressure(760m);
                dataset.Measurements.PressureMmHg!.PressureReference = PressureReference.Sensor;
                break;
            case "PressureDifferentQuality":
                dataset.Measurements.PressureHpa = Pressure(1013m);
                dataset.Measurements.PressureMmHg = Pressure(760m);
                dataset.Measurements.PressureMmHg!.Quality = "1";
                break;
            case "PressureInconsistentValues":
                dataset.Measurements.PressureHpa = Pressure(1000m);
                dataset.Measurements.PressureMmHg = Pressure(760m);
                break;
            case "PressureImpossiblePair":
                dataset.Measurements.PressureHpa = Pressure(100000000m);
                dataset.Measurements.PressureMmHg = Pressure(75006160m);
                break;
            case "HumidityAboveRange":
                dataset.Measurements.HumidityPercent = ObservationTestData.Value(101m, Utc(12, 0));
                break;
            case "WindDirectionThreeSixty":
                dataset.Measurements.WindDirectionDeg = ObservationTestData.Value(360m, Utc(12, 0));
                break;
            case "NegativeIlluminance":
                dataset.Measurements.IlluminanceLux = ObservationTestData.Value(-1m, Utc(12, 0));
                break;
            case "NegativeRainRate":
                dataset.Measurements.RainRateMmH = ObservationTestData.Value(-0.5m, Utc(12, 0));
                break;
            case "TemperatureWithoutDecimal":
                dataset.Measurements.TemperatureC!.Value = 20m;
                break;
            case "TemperatureTwoDecimals":
                dataset.Measurements.TemperatureC!.Value = 20.25m;
                break;
            case "HumidityWithDecimalScale":
                dataset.Measurements.HumidityPercent = ObservationTestData.Value(55.0m, Utc(12, 0));
                break;
            case "PressureWithDecimalScale":
                dataset.Measurements.PressureHpa = Pressure(1013.0m);
                dataset.Measurements.PressureMmHg = Pressure(760m);
                break;
            case "WindDirectionZero":
                dataset.Measurements.WindDirectionDeg = ObservationTestData.Value(0m, Utc(12, 0));
                break;
            case "WindDirectionBelowThreeSixty":
                dataset.Measurements.WindDirectionDeg = ObservationTestData.Value(359.9m, Utc(12, 0));
                break;
            case "PressureFromHpa":
                dataset.Measurements.PressureHpa = Pressure(1001m);
                dataset.Measurements.PressureMmHg = Pressure(750m);
                break;
            case "PressureFromMmHg":
                dataset.Measurements.PressureHpa = Pressure(1013m);
                dataset.Measurements.PressureMmHg = Pressure(760m);
                break;
            case "PressureRoundedZero":
                dataset.Measurements.PressureHpa = Pressure(0m);
                dataset.Measurements.PressureMmHg = Pressure(0m);
                break;
            case "NegativeTemperature":
                dataset.Measurements.TemperatureC!.Value = -12.5m;
                break;
            case "ZeroTemperature":
                dataset.Measurements.TemperatureC!.Value = 0.0m;
                break;
            case "EstimatedWithBerlin":
                dataset.Measurements.HumidityPercent = ObservationTestData.Value(55m, Utc(12, 0));
                dataset.Measurements.HumidityPercent!.TimestampQuality = TimestampQuality.EstimatedDate;
                dataset.Measurements.HumidityPercent!.SourceTimezone = "Europe/Berlin";
                dataset.TimestampQuality = TimestampQuality.EstimatedDate;
                break;
            case "LaterHumidityDefinesDatasetTime":
                dataset.Measurements.HumidityPercent = ObservationTestData.Value(55m, Utc(12, 10));
                dataset.MeasuredAtUtc = Utc(12, 10);
                break;
            case "ValidPeriod":
                dataset.Measurements.TemperatureC!.Period = new MeasurementPeriod { StartUtc = Utc(11, 50), EndUtc = Utc(12, 0) };
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
    [DataRow("NoQuantity")]
    [DataRow("SourceStationMismatch")]
    [DataRow("SourceUppercaseProvider")]
    [DataRow("SourceProviderWithNewline")]
    [DataRow("SourceWithoutSeparator")]
    [DataRow("DatasetTimeNotLatest")]
    [DataRow("DatasetEstimatedWithoutEstimate")]
    [DataRow("EstimateHiddenAtDataset")]
    [DataRow("EstimateWithoutBerlin")]
    [DataRow("PeriodEmpty")]
    [DataRow("PeriodReversed")]
    [DataRow("PressureHpaOnly")]
    [DataRow("PressureDifferentTime")]
    [DataRow("PressureDifferentReference")]
    [DataRow("PressureDifferentQuality")]
    [DataRow("PressureInconsistentValues")]
    [DataRow("PressureImpossiblePair")]
    [DataRow("HumidityAboveRange")]
    [DataRow("WindDirectionThreeSixty")]
    [DataRow("NegativeIlluminance")]
    [DataRow("NegativeRainRate")]
    [DataRow("TemperatureWithoutDecimal")]
    [DataRow("TemperatureTwoDecimals")]
    [DataRow("HumidityWithDecimalScale")]
    [DataRow("PressureWithDecimalScale")]
    public async Task SerializeAsync_GraphViolatingC64_IsRejected(string scenario)
    {
        await AssertRejectedInBothFormats(Build(scenario));
    }

    [TestMethod]
    [DataRow("Valid")]
    [DataRow("WindDirectionZero")]
    [DataRow("WindDirectionBelowThreeSixty")]
    [DataRow("PressureFromHpa")]
    [DataRow("PressureFromMmHg")]
    [DataRow("PressureRoundedZero")]
    [DataRow("NegativeTemperature")]
    [DataRow("ZeroTemperature")]
    [DataRow("EstimatedWithBerlin")]
    [DataRow("LaterHumidityDefinesDatasetTime")]
    [DataRow("ValidPeriod")]
    public async Task SerializeAsync_GraphSatisfyingC64_IsAccepted(string scenario)
    {
        await AssertAcceptedInBothFormats(Build(scenario));
    }
}
