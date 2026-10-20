using System.Globalization;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ForecastSetterRuleTests
{
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public void Location_EmptyOrWhitespaceName_IsRejected(string? value)
    {
        var location = new ForecastLocation();
        FluentActions.Invoking(() => location.Name = value!)
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Location_NameWithSpacesAndSlash_IsKeptExactly()
    {
        var location = new ForecastLocation();
        location.Name = "FRANKFURT/M ";
        location.Name.Should().Be("FRANKFURT/M ");
    }

    [TestMethod]
    [DataRow("90.0001")]
    [DataRow("-90.0001")]
    public void Location_LatitudeOutOfRange_IsRejected(string text)
    {
        var lat = decimal.Parse(text, CultureInfo.InvariantCulture);
        var location = new ForecastLocation();
        FluentActions.Invoking(() => location.Latitude = lat)
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow("180.0001")]
    [DataRow("-180.0001")]
    public void Location_LongitudeOutOfRange_IsRejected(string text)
    {
        var lon = decimal.Parse(text, CultureInfo.InvariantCulture);
        var location = new ForecastLocation();
        FluentActions.Invoking(() => location.Longitude = lon)
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow("90")]
    [DataRow("-90")]
    [DataRow("0")]
    public void Location_CoordinateBoundaries_AreAccepted(string text)
    {
        var lat = decimal.Parse(text, CultureInfo.InvariantCulture);
        var location = new ForecastLocation();
        FluentActions.Invoking(() => location.Latitude = lat)
            .Should().NotThrow();
    }

    [TestMethod]
    [DataRow("180")]
    [DataRow("-180")]
    [DataRow("0")]
    public void Location_LongitudeBoundaries_AreAccepted(string text)
    {
        var lon = decimal.Parse(text, CultureInfo.InvariantCulture);
        var location = new ForecastLocation();
        FluentActions.Invoking(() => location.Longitude = lon)
            .Should().NotThrow();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("  ")]
    [DataRow("106 37")]
    [DataRow("dwd:10637")]
    [DataRow(null)]
    public void Run_InvalidMosmixId_IsRejected(string? value)
    {
        var run = new ForecastRun();
        FluentActions.Invoking(() => run.MosmixId = value!)
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Run_MosmixIdWithLettersAndLeadingZeros_IsKeptExactly()
    {
        var run = new ForecastRun();
        run.MosmixId = "0A123";
        run.MosmixId.Should().Be("0A123");
    }

    [TestMethod]
    public void Run_DatasetTypeOtherThanForecast_IsRejected()
    {
        var run = new ForecastRun();
        FluentActions.Invoking(() => run.DatasetType = "observation")
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void UndefinedEnumValues_AreRejected()
    {
        FluentActions.Invoking(() => new ForecastRun().Product = (MosmixProduct)9)
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new IntervalValue().Aggregation = (IntervalAggregation)9)
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new ForecastValues().PressureReference = (PressureReference)9)
            .Should().Throw<ArgumentException>();
    }
}
