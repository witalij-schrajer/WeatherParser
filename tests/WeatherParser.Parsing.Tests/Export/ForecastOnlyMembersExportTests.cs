using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Schemas;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ForecastOnlyMembersExportTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;
    private IForecastRunBuilder _builder = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
        _builder = new ForecastRunBuilder(
            new QuantityValidator(),
            new OutputRounding(),
            new PressureConverter(new OutputRounding()),
            new PeriodRateConverter(),
            new WindSpeedConverter());
    }

    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);

    private static IntervalValue Interval(decimal value, DateTime start, DateTime end, IntervalAggregation aggregation) => new()
    {
        Value = value,
        PeriodStartUtc = start,
        PeriodEndUtc = end,
        Aggregation = aggregation
    };

    private static ForecastRun RunWithNewMembers() => new()
    {
        Source = "dwd-mosmix-l:10637",
        MosmixId = "10637",
        Location = new ForecastLocation { Name = "FRANKFURT/M", Latitude = 50.05m, Longitude = 8.6m, AltitudeM = 111m },
        Product = MosmixProduct.MosmixL,
        IssuedAtUtc = ForecastTestData.IssuedAtUtc,
        RetrievedAtUtc = ForecastTestData.RetrievedAtUtc,
        Forecasts = new List<ForecastStep>
        {
            new()
            {
                ValidAtUtc = Utc(12, 0),
                Values = new ForecastValues
                {
                    TemperatureC = 21.4m,
                    PressureHpa = 1013m,
                    PressureMmHg = 760m,
                    PressureReference = PressureReference.SeaLevel,
                    RainRateMmH = ForecastTestData.Interval(0.3m, Utc(11, 0), Utc(12, 0)),
                    WindGustKmh = Interval(36m, Utc(11, 0), Utc(12, 0), IntervalAggregation.Maximum),
                    PrecipitationAmountMm = Interval(0.3m, Utc(11, 0), Utc(12, 0), IntervalAggregation.Sum)
                }
            },
            new()
            {
                ValidAtUtc = Utc(13, 0),
                Values = new ForecastValues { TemperatureC = 23.0m }
            }
        }
    };

    private static async Task AssertRejected(Func<Stream, Task> serialize)
    {
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => serialize(stream))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    private static async Task AssertAccepted(Func<Stream, Task> serialize)
    {
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => serialize(stream))
            .Should().NotThrowAsync();
        stream.Length.Should().BeGreaterThan(0);
    }

    private async Task AssertRejectedInBothFormats(ForecastRun run)
    {
        await AssertRejected(stream => _json.SerializeAsync(run, stream, CancellationToken.None));
        await AssertRejected(stream => _xml.SerializeAsync(run, stream, CancellationToken.None));
    }

    private async Task AssertAcceptedInBothFormats(ForecastRun run)
    {
        await AssertAccepted(stream => _json.SerializeAsync(run, stream, CancellationToken.None));
        await AssertAccepted(stream => _xml.SerializeAsync(run, stream, CancellationToken.None));
    }

    private async Task<byte[]> JsonAsync(ForecastRun run)
    {
        using var stream = new MemoryStream();
        await _json.SerializeAsync(run, stream, CancellationToken.None);
        return stream.ToArray();
    }

    private async Task<byte[]> XmlAsync(ForecastRun run)
    {
        using var stream = new MemoryStream();
        await _xml.SerializeAsync(run, stream, CancellationToken.None);
        return stream.ToArray();
    }

    private static XNamespace Ns => ExportNamespaces.V1;

    [TestMethod]
    [DataRow("GustMeanAggregation")]
    [DataRow("AmountMeanAggregation")]
    [DataRow("RainRateSumAggregation")]
    [DataRow("SolarMaximumAggregation")]
    [DataRow("NegativeGust")]
    [DataRow("NegativeAmount")]
    [DataRow("GustEndNotAfterStart")]
    [DataRow("AmountEndNotAfterStart")]
    [DataRow("GustDecimalScale")]
    public async Task Validate_InvalidNewMemberGraph_RejectedInBothFormats(string scenario)
    {
        var run = RunWithNewMembers();
        switch (scenario)
        {
            case "GustMeanAggregation":
                run.Forecasts[0].Values.WindGustKmh!.Aggregation = IntervalAggregation.Mean;
                break;
            case "AmountMeanAggregation":
                run.Forecasts[0].Values.PrecipitationAmountMm!.Aggregation = IntervalAggregation.Mean;
                break;
            case "RainRateSumAggregation":
                run.Forecasts[0].Values.RainRateMmH!.Aggregation = IntervalAggregation.Sum;
                break;
            case "SolarMaximumAggregation":
                run.Forecasts[0].Values.SolarIrradianceWm2 = Interval(500m, Utc(11, 0), Utc(12, 0), IntervalAggregation.Maximum);
                break;
            case "NegativeGust":
                run.Forecasts[0].Values.WindGustKmh!.Value = -1m;
                break;
            case "NegativeAmount":
                run.Forecasts[0].Values.PrecipitationAmountMm!.Value = -0.1m;
                break;
            case "GustEndNotAfterStart":
                run.Forecasts[0].Values.WindGustKmh!.PeriodStartUtc = Utc(12, 0);
                break;
            case "AmountEndNotAfterStart":
                run.Forecasts[0].Values.PrecipitationAmountMm!.PeriodStartUtc = Utc(12, 0);
                break;
            case "GustDecimalScale":
                run.Forecasts[0].Values.WindGustKmh!.Value = 36.0m;
                break;
        }

        await AssertRejectedInBothFormats(run);
    }

    [TestMethod]
    [DataRow("OnlyGust")]
    [DataRow("OnlyAmount")]
    public async Task Validate_StepWithOnlyNewMember_Accepted(string scenario)
    {
        var run = ForecastTestData.ValidRun();
        switch (scenario)
        {
            case "OnlyGust":
                run.Forecasts[1].Values = new ForecastValues
                {
                    WindGustKmh = Interval(36m, Utc(12, 0), Utc(13, 0), IntervalAggregation.Maximum)
                };
                break;
            case "OnlyAmount":
                run.Forecasts[1].Values = new ForecastValues
                {
                    PrecipitationAmountMm = Interval(0m, Utc(12, 0), Utc(13, 0), IntervalAggregation.Sum)
                };
                break;
        }

        await AssertAcceptedInBothFormats(run);
    }

    [TestMethod]
    public async Task Validate_RunWithNewMembers_Accepted()
    {
        await AssertAcceptedInBothFormats(RunWithNewMembers());
    }

    [TestMethod]
    public async Task Serialize_NewMembersAfterRainRate_JsonAndXml()
    {
        // Arrange
        var run = RunWithNewMembers();
        var jsonBytes = await JsonAsync(run);
        var xmlBytes = await XmlAsync(run);

        // Act
        var json = JsonDocument.Parse(jsonBytes);
        var jsonArray = json.RootElement.GetProperty("forecasts")[0].GetProperty("values");
        var jsonNames = jsonArray.EnumerateObject().Select(p => p.Name).ToList();

        var xml = XDocument.Parse(Encoding.UTF8.GetString(xmlBytes));
        var valuesElement = xml.Descendants(Ns + "values").First();
        var xmlNames = valuesElement.Elements().Select(e => e.Name.LocalName).ToList();

        // Assert
        jsonNames.Should().Equal("temperature_c", "pressure_hpa", "pressure_mmhg", "pressure_reference", "rain_rate_mm_h", "wind_gust_kmh", "precipitation_amount_mm");
        xmlNames.Should().Equal("temperature_c", "pressure_hpa", "pressure_mmhg", "pressure_reference", "rain_rate_mm_h", "wind_gust_kmh", "precipitation_amount_mm");
        valuesElement.Elements().Should().OnlyContain(e => e.Name.Namespace == Ns);
    }

    [TestMethod]
    public async Task Serialize_NewMembersEquivalent()
    {
        // Arrange
        var run = RunWithNewMembers();
        var jsonBytes = await JsonAsync(run);
        var xmlBytes = await XmlAsync(run);

        // Act
        var json = JsonDocument.Parse(jsonBytes);
        var gustJson = json.RootElement.GetProperty("forecasts")[0].GetProperty("values").GetProperty("wind_gust_kmh");
        var amountJson = json.RootElement.GetProperty("forecasts")[0].GetProperty("values").GetProperty("precipitation_amount_mm");

        var xml = XDocument.Parse(Encoding.UTF8.GetString(xmlBytes));
        var valuesElement = xml.Descendants(Ns + "values").First();
        var gustXml = valuesElement.Elements(Ns + "wind_gust_kmh").Single()!;
        var amountXml = valuesElement.Elements(Ns + "precipitation_amount_mm").Single()!;

        // Assert
        gustJson.GetProperty("value").GetDecimal().Should().Be(36m);
        gustJson.GetProperty("aggregation").GetString().Should().Be("maximum");
        gustJson.GetProperty("period_start").GetString().Should().Be("2026-07-15T11:00:00");
        gustJson.GetProperty("period_end").GetString().Should().Be("2026-07-15T12:00:00");

        amountJson.GetProperty("value").GetDecimal().Should().Be(0.3m);
        amountJson.GetProperty("aggregation").GetString().Should().Be("sum");

        gustXml.Elements(Ns + "value").Single().Value.Should().Be("36");
        gustXml.Elements(Ns + "aggregation").Single().Value.Should().Be("maximum");
        gustXml.Elements(Ns + "period_start").Single().Value.Should().Be("2026-07-15T11:00:00");
        gustXml.Elements(Ns + "period_end").Single().Value.Should().Be("2026-07-15T12:00:00");

        amountXml.Elements(Ns + "value").Single().Value.Should().Be("0.3");
        amountXml.Elements(Ns + "aggregation").Single().Value.Should().Be("sum");
    }

    [TestMethod]
    public async Task Schema_NewMembers_Valid()
    {
        // Arrange
        var run = RunWithNewMembers();
        var xmlBytes = await XmlAsync(run);

        // Act
        var errors = ExportSchemaValidation.Validate(xmlBytes);

        // Assert
        errors.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Schema_NewMembersBeforeRainRate_Invalid()
    {
        // Arrange
        var run = RunWithNewMembers();
        var xmlBytes = await XmlAsync(run);
        var xml = XDocument.Parse(Encoding.UTF8.GetString(xmlBytes));

        // Act
        var valuesElement = xml.Descendants(Ns + "values").First();
        var rainRate = valuesElement.Elements(Ns + "rain_rate_mm_h").Single()!;
        var gust = valuesElement.Elements(Ns + "wind_gust_kmh").Single()!;
        gust.Remove();
        rainRate.AddBeforeSelf(gust);
        var bytes = Encoding.UTF8.GetBytes(xml.ToString());

        // Assert
        ExportSchemaValidation.Validate(bytes).Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Export_AccumulationRateOverflow_BothSerializersWriteAmount()
    {
        // Arrange
        var input = new ForecastRunInput
        {
            Product = MosmixProduct.MosmixL,
            MosmixId = "10637",
            Location = new ForecastLocation { Name = "FRANKFURT/M", Latitude = 50.05m, Longitude = 8.6m, AltitudeM = 111m },
            IssuedAtUtc = ForecastTestData.IssuedAtUtc,
            RetrievedAtUtc = ForecastTestData.RetrievedAtUtc,
            Timeline = new List<DateTime> { Utc(12, 0) },
            Series = new List<ForecastSeries?>
            {
                new()
                {
                    Kind = QuantityKind.PrecipitationAccumulation,
                    Values = new List<double?> { 1e27 },
                    Periods = new List<AveragingPeriod?> { new(new DateTime(2026, 7, 15, 11, 59, 59, DateTimeKind.Utc), Utc(12, 0)) },
                    Product = MosmixProduct.MosmixL,
                    MosmixId = "10637",
                    IssuedAtUtc = ForecastTestData.IssuedAtUtc
                }
            }
        };

        var result = _builder.Build(input);
        result.OmittedValues.Should().ContainSingle().Which.Should().Be(new OmittedForecastValue(Utc(12, 0), QuantityKind.PrecipitationAccumulation, OmissionReason.Unrepresentable));

        // Act
        var jsonBytes = await JsonAsync(result.Run);
        var xmlBytes = await XmlAsync(result.Run);

        // Assert
        var json = JsonDocument.Parse(jsonBytes);
        var valuesJson = json.RootElement.GetProperty("forecasts")[0].GetProperty("values");
        valuesJson.TryGetProperty("precipitation_amount_mm", out _).Should().BeTrue();
        valuesJson.TryGetProperty("rain_rate_mm_h", out _).Should().BeFalse();

        var xml = XDocument.Parse(Encoding.UTF8.GetString(xmlBytes));
        var valuesElement = xml.Descendants(Ns + "values").First();
        valuesElement.Elements(Ns + "precipitation_amount_mm").Should().ContainSingle();
        valuesElement.Elements(Ns + "rain_rate_mm_h").Should().BeEmpty();
        
        ExportSchemaValidation.Validate(xmlBytes).Should().BeEmpty();
    }
}
