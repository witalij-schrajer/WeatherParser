using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ForecastGraphValidationBranchTests
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
            case "ProductS":
                run.Product = MosmixProduct.MosmixS;
                run.Source = "dwd-mosmix-s:10637";
                break;
            case "AllPointValues":
                run.Forecasts[1].Values.HumidityPercent = 55m;
                run.Forecasts[1].Values.WindDirectionDeg = 0m;
                run.Forecasts[1].Values.WindSpeedKmh = 0m;
                run.Forecasts[1].Values.IlluminanceLux = 0m;
                break;
            case "SolarIntervalEndNotAfterStart":
                run.Forecasts.Add(new ForecastStep
                {
                    ValidAtUtc = ForecastTestData.Utc(14, 0),
                    Values = new ForecastValues
                    {
                        SolarIrradianceWm2 = ForecastTestData.Interval(600m, ForecastTestData.Utc(13, 0), ForecastTestData.Utc(13, 0))
                    }
                });
                break;
            case "NegativeSolarIrradiance":
                run.Forecasts[1].Values.SolarIrradianceWm2 = ForecastTestData.Interval(-1m, ForecastTestData.Utc(12, 0), ForecastTestData.Utc(13, 0));
                break;
            case "NegativeWindSpeed":
                run.Forecasts[1].Values.WindSpeedKmh = -1m;
                break;
            case "NegativeIlluminance":
                run.Forecasts[1].Values.IlluminanceLux = -1m;
                break;
            case "NegativeWindDirection":
                run.Forecasts[1].Values.WindDirectionDeg = -1m;
                break;
            case "WindSpeedWithDecimalScale":
                run.Forecasts[1].Values.WindSpeedKmh = 12.0m;
                break;
            case "PressureWithDecimalScale":
                run.Forecasts[0].Values.PressureHpa = 1013.0m;
                break;
            case "MmHgWithDecimalScale":
                run.Forecasts[0].Values.PressureMmHg = 760.0m;
                break;
            case "MmHgDirectionOverflows":
                run.Forecasts[0].Values.PressureHpa = 1m;
                run.Forecasts[0].Values.PressureMmHg = 60000000000000000000000000000m;
                break;
            case "NegativePressurePair":
                run.Forecasts[0].Values.PressureHpa = -1m;
                run.Forecasts[0].Values.PressureMmHg = -1m;
                break;
            case "NegativeMmHg":
                run.Forecasts[0].Values.PressureHpa = 0m;
                run.Forecasts[0].Values.PressureMmHg = -0.4m;
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
    [DataRow("SolarIntervalEndNotAfterStart")]
    [DataRow("NegativeSolarIrradiance")]
    [DataRow("NegativeWindSpeed")]
    [DataRow("NegativeIlluminance")]
    [DataRow("NegativeWindDirection")]
    [DataRow("WindSpeedWithDecimalScale")]
    [DataRow("PressureWithDecimalScale")]
    [DataRow("MmHgWithDecimalScale")]
    [DataRow("MmHgDirectionOverflows")]
    [DataRow("NegativePressurePair")]
    [DataRow("NegativeMmHg")]
    public async Task SerializeAsync_GraphViolatingBranchConditions_IsRejected(string scenario)
    {
        await AssertRejectedInBothFormats(Build(scenario));
    }

    [TestMethod]
    [DataRow("Valid")]
    [DataRow("ProductS")]
    [DataRow("AllPointValues")]
    public async Task SerializeAsync_GraphSatisfyingBranchConditions_IsAccepted(string scenario)
    {
        await AssertAcceptedInBothFormats(Build(scenario));
    }
}
