using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ForecastRequiredMemberGuardTests
{
    private IJsonExportSerializer _jsonSubject = null!;
    private IXmlExportSerializer _xmlSubject = null!;
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    [TestInitialize]
    public void TestInitialize()
    {
        _jsonSubject = new JsonExportSerializer();
        _xmlSubject = new XmlExportSerializer();
    }

    private static ForecastRun BuildRun(string? missing)
    {
        var run = new ForecastRun();
        if (missing != "Source") run.Source = "dwd-mosmix-l:10637";
        if (missing != "MosmixId") run.MosmixId = "10637";
        if (missing != "Product") run.Product = MosmixProduct.MosmixL;
        if (missing != "IssuedAtUtc") run.IssuedAtUtc = ForecastTestData.IssuedAtUtc;
        if (missing != "RetrievedAtUtc") run.RetrievedAtUtc = ForecastTestData.RetrievedAtUtc;

        if (missing != "Location")
        {
            var location = new ForecastLocation();
            if (missing != "Location.Name") location.Name = "FRANKFURT/M";
            if (missing != "Location.Latitude") location.Latitude = 50.05m;
            if (missing != "Location.Longitude") location.Longitude = 8.6m;
            run.Location = location;
        }

        if (missing != "Step.ValidAtUtc" || missing != "Step.Values")
        {
            var step = new ForecastStep();
            if (missing != "Step.ValidAtUtc") step.ValidAtUtc = ForecastTestData.Utc(12, 0);

            if (missing != "Step.Values")
            {
                var values = new ForecastValues();
                if (missing != "Interval.Value" || missing != "Interval.PeriodStartUtc" || missing != "Interval.PeriodEndUtc" || missing != "Interval.Aggregation")
                {
                    var interval = new IntervalValue();
                    if (missing != "Interval.Value") interval.Value = 0.3m;
                    if (missing != "Interval.PeriodStartUtc") interval.PeriodStartUtc = ForecastTestData.Utc(11, 0);
                    if (missing != "Interval.PeriodEndUtc") interval.PeriodEndUtc = ForecastTestData.Utc(12, 0);
                    if (missing != "Interval.Aggregation") interval.Aggregation = IntervalAggregation.Mean;
                    values.RainRateMmH = interval;
                }
        values.TemperatureC = 21.4m;
                step.Values = values;
            }

            run.Forecasts.Add(step);
        }

        return run;
    }

    [TestMethod]
    [DataRow("Source")]
    [DataRow("MosmixId")]
    [DataRow("Location")]
    [DataRow("Location.Name")]
    [DataRow("Location.Latitude")]
    [DataRow("Location.Longitude")]
    [DataRow("Product")]
    [DataRow("IssuedAtUtc")]
    [DataRow("RetrievedAtUtc")]
    [DataRow("Step.ValidAtUtc")]
    [DataRow("Step.Values")]
    [DataRow("Interval.Value")]
    [DataRow("Interval.PeriodStartUtc")]
    [DataRow("Interval.PeriodEndUtc")]
    [DataRow("Interval.Aggregation")]
    public async Task SerializeAsync_Json_UnassignedRequiredMember_FailsAndWritesNothing(string missing)
    {
        var run = BuildRun(missing);
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _jsonSubject.SerializeAsync(run, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    [DataRow("Source")]
    [DataRow("MosmixId")]
    [DataRow("Location")]
    [DataRow("Location.Name")]
    [DataRow("Location.Latitude")]
    [DataRow("Location.Longitude")]
    [DataRow("Product")]
    [DataRow("IssuedAtUtc")]
    [DataRow("RetrievedAtUtc")]
    [DataRow("Step.ValidAtUtc")]
    [DataRow("Step.Values")]
    [DataRow("Interval.Value")]
    [DataRow("Interval.PeriodStartUtc")]
    [DataRow("Interval.PeriodEndUtc")]
    [DataRow("Interval.Aggregation")]
    public async Task SerializeAsync_Xml_UnassignedRequiredMember_FailsAndWritesNothing(string missing)
    {
        var run = BuildRun(missing);
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _xmlSubject.SerializeAsync(run, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Json_CompleteGraph_Succeeds()
    {
        var run = BuildRun(null);
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _jsonSubject.SerializeAsync(run, stream, CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_CompleteGraph_Succeeds()
    {
        var run = BuildRun(null);
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _xmlSubject.SerializeAsync(run, stream, CancellationToken.None))
            .Should().NotThrowAsync();
    }
}
