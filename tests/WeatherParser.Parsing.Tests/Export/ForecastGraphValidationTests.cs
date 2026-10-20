using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ForecastGraphValidationTests
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

    private static ForecastRun Build(string scenario)
    {
        var run = ForecastTestData.ValidRun();
        switch (scenario)
        {
            case "Valid":
                break;
            case "NoSteps":
                run.Forecasts.Clear();
                break;
            case "StepWithoutValues":
                run.Forecasts[1].Values = new ForecastValues();
                break;
            case "SourceWrongProduct":
                run.Source = "dwd-mosmix-s:10637";
                break;
            case "SourceWrongId":
                run.Source = "dwd-mosmix-l:10638";
                break;
            case "StepsNotAscending":
                run.Forecasts.Reverse();
                break;
            case "DuplicateValidAt":
                run.Forecasts[1].ValidAtUtc = ForecastTestData.Utc(12, 0);
                break;
            case "PressureWithoutReference":
                run.Forecasts[0].Values.PressureReference = null;
                break;
            case "ReferenceWithoutPressure":
                run.Forecasts[1].Values.PressureReference = PressureReference.Sensor;
                break;
            case "PressureHpaOnly":
                run.Forecasts[0].Values.PressureMmHg = null;
                break;
            case "PressureInconsistent":
                run.Forecasts[0].Values.PressureHpa = 1000m;
                break;
            case "IntervalEndNotAfterStart":
                run.Forecasts[0].Values.RainRateMmH = ForecastTestData.Interval(0.3m, Utc(12, 0), Utc(12, 0));
                break;
            case "HumidityAboveRange":
                run.Forecasts[1].Values.HumidityPercent = 101m;
                break;
            case "WindDirectionThreeSixty":
                run.Forecasts[1].Values.WindDirectionDeg = 360m;
                break;
            case "NegativeRainRate":
                run.Forecasts[0].Values.RainRateMmH = ForecastTestData.Interval(-0.5m, Utc(11, 0), Utc(12, 0));
                break;
            case "TemperatureWithoutDecimal":
                run.Forecasts[1].Values.TemperatureC = 23m;
                break;
            case "TemperatureTwoDecimals":
                run.Forecasts[1].Values.TemperatureC = 23.05m;
                break;
            case "HumidityWithDecimalScale":
                run.Forecasts[1].Values.HumidityPercent = 55.0m;
                break;
            case "ZeroTemperature":
                run.Forecasts[1].Values.TemperatureC = 0.0m;
                break;
            case "PressureFromHpa":
                run.Forecasts[0].Values.PressureHpa = 1001m;
                run.Forecasts[0].Values.PressureMmHg = 750m;
                break;
            case "WindDirectionBelowThreeSixty":
                run.Forecasts[1].Values.WindDirectionDeg = 359.9m;
                break;
            case "SolarIrradianceInterval":
                run.Forecasts[1].Values.SolarIrradianceWm2 = ForecastTestData.Interval(612.5m, Utc(12, 0), Utc(13, 0));
                break;
            default:
                throw new ArgumentException($"Unknown scenario: {scenario}");
        }

        return run;
    }

    private async Task AssertRejectedInBothFormats(ForecastRun run)
    {
        await AssertRejected(stream => _json.SerializeAsync(run, stream, CancellationToken.None));
        await AssertRejected(stream => _xml.SerializeAsync(run, stream, CancellationToken.None));
    }

    private static async Task AssertRejected(Func<Stream, Task> serialize)
    {
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => serialize(stream))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    private async Task AssertAcceptedInBothFormats(ForecastRun run)
    {
        await AssertAccepted(stream => _json.SerializeAsync(run, stream, CancellationToken.None));
        await AssertAccepted(stream => _xml.SerializeAsync(run, stream, CancellationToken.None));
    }

    private static async Task AssertAccepted(Func<Stream, Task> serialize)
    {
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => serialize(stream))
            .Should().NotThrowAsync();
        stream.Length.Should().BeGreaterThan(0);
    }

    [TestMethod]
    [DataRow("NoSteps")]
    [DataRow("StepWithoutValues")]
    [DataRow("SourceWrongProduct")]
    [DataRow("SourceWrongId")]
    [DataRow("StepsNotAscending")]
    [DataRow("DuplicateValidAt")]
    [DataRow("PressureWithoutReference")]
    [DataRow("ReferenceWithoutPressure")]
    [DataRow("PressureHpaOnly")]
    [DataRow("PressureInconsistent")]
    [DataRow("IntervalEndNotAfterStart")]
    [DataRow("HumidityAboveRange")]
    [DataRow("WindDirectionThreeSixty")]
    [DataRow("NegativeRainRate")]
    [DataRow("TemperatureWithoutDecimal")]
    [DataRow("TemperatureTwoDecimals")]
    [DataRow("HumidityWithDecimalScale")]
    public async Task SerializeAsync_GraphViolatingC79_IsRejected(string scenario)
    {
        await AssertRejectedInBothFormats(Build(scenario));
    }

    [TestMethod]
    [DataRow("Valid")]
    [DataRow("ZeroTemperature")]
    [DataRow("PressureFromHpa")]
    [DataRow("WindDirectionBelowThreeSixty")]
    [DataRow("SolarIrradianceInterval")]
    public async Task SerializeAsync_GraphSatisfyingC79_IsAccepted(string scenario)
    {
        await AssertAcceptedInBothFormats(Build(scenario));
    }
}
