using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Export;

namespace WeatherParser.Parsing.Tests.Forecasts;

[TestClass]
public sealed class ForecastReviewRegressionTests
{
    private IForecastRunBuilder _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new ForecastRunBuilder(
            new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()),
            new PeriodRateConverter(), new WindSpeedConverter());
    }

    private static readonly DateTime IssuedAt = new(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc);
    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);
    private static DateTime[] Noon() => new[] { Utc(12, 0) };
    private static ForecastLocation Location() => new() { Name = "FRANKFURT/M", Latitude = 50.05m, Longitude = 8.6m };

    private static ForecastRunInput Input(DateTime[] timeline, params ForecastSeries?[] series) => new()
    {
        Product = MosmixProduct.MosmixL, MosmixId = "10637", Location = Location(),
        IssuedAtUtc = IssuedAt, RetrievedAtUtc = Utc(10, 12), Timeline = timeline, Series = series
    };

    [TestMethod]
    public void Build_MissingSeriesKind_IsCallerError()
    {
        var series = new ForecastSeries { Kind = null, Values = new double?[] { 20.0 }, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt };

        FluentActions.Invoking(() => _subject.Build(Input(Noon(), series)))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Build_UndefinedSeriesProduct_IsCallerError()
    {
        var series = new ForecastSeries { Kind = QuantityKind.Temperature, Values = new double?[] { 20.0 }, Product = (MosmixProduct)99, MosmixId = "10637", IssuedAtUtc = IssuedAt };

        FluentActions.Invoking(() => _subject.Build(Input(Noon(), series)))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("106 37")]
    [DataRow("dwd:10637")]
    public void Build_MalformedSeriesMosmixId_IsCallerError(string mosmixId)
    {
        var series = new ForecastSeries { Kind = QuantityKind.Temperature, Values = new double?[] { 20.0 }, Product = MosmixProduct.MosmixL, MosmixId = mosmixId, IssuedAtUtc = IssuedAt };

        FluentActions.Invoking(() => _subject.Build(Input(Noon(), series)))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public async Task SerializeAsync_NullStep_FailsWithExportSerializationExceptionInBothFormats()
    {
        var run = ForecastTestData.ValidRun();
        run.Forecasts.Add(null!);

        using var json = new MemoryStream();
        await FluentActions.Awaiting(() => new JsonExportSerializer().SerializeAsync(run, json, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        json.Length.Should().Be(0);

        using var xml = new MemoryStream();
        await FluentActions.Awaiting(() => new XmlExportSerializer().SerializeAsync(run, xml, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        xml.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_ExtremePressurePair_FailsWithExportSerializationExceptionInBothFormats()
    {
        var run = ForecastTestData.ValidRun();
        run.Forecasts[0].Values.PressureHpa = decimal.MaxValue;
        run.Forecasts[0].Values.PressureMmHg = decimal.MinValue;

        using var json = new MemoryStream();
        await FluentActions.Awaiting(() => new JsonExportSerializer().SerializeAsync(run, json, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        json.Length.Should().Be(0);

        using var xml = new MemoryStream();
        await FluentActions.Awaiting(() => new XmlExportSerializer().SerializeAsync(run, xml, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        xml.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_RootMembers_FollowContractOrder()
    {
        var expected = new[] { "schema_version", "dataset_type", "source", "mosmix_id", "location", "product", "issued_at", "retrieved_at", "forecasts" };

        using var json = new MemoryStream();
        await new JsonExportSerializer().SerializeAsync(ForecastTestData.ValidRun(), json, CancellationToken.None);
        using var document = JsonDocument.Parse(json.ToArray());
        document.RootElement.EnumerateObject().Select(p => p.Name).Should().Equal(expected);

        using var xml = new MemoryStream();
        await new XmlExportSerializer().SerializeAsync(ForecastTestData.ValidRun(), xml, CancellationToken.None);
        XDocument.Parse(System.Text.Encoding.UTF8.GetString(xml.ToArray())).Root!.Elements().Select(e => e.Name.LocalName).Should().Equal(expected);
    }
}
