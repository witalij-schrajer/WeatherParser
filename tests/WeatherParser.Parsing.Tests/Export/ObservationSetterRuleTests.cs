using System.Globalization;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ObservationSetterRuleTests
{
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public void Location_EmptyOrWhitespaceIdentity_IsRejected(string? value)
    {
        var dataset = new ObservationDataset();
        FluentActions.Invoking(() => dataset.Location = new ObservationLocation { StationId = value! })
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => dataset.Location = new ObservationLocation { Name = value! })
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Location_IdentityWithSpacesAndUmlauts_IsKeptExactly()
    {
        var dataset = new ObservationDataset();
        dataset.Location = new ObservationLocation { StationId = " 00044 ", Name = "Großenkneten" };
        dataset.Location.StationId.Should().Be(" 00044 ");
        dataset.Location.Name.Should().Be("Großenkneten");
    }

    [TestMethod]
    [DataRow("90.0001")]
    [DataRow("-90.0001")]
    public void Location_LatitudeOutOfRange_IsRejected(string text)
    {
        var lat = decimal.Parse(text, CultureInfo.InvariantCulture);
        var dataset = new ObservationDataset();
        FluentActions.Invoking(() => dataset.Location = new ObservationLocation { Latitude = lat })
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow("90")]
    [DataRow("-90")]
    [DataRow("0")]
    public void Location_LatitudeBoundaries_AreAccepted(string text)
    {
        var lat = decimal.Parse(text, CultureInfo.InvariantCulture);
        var dataset = new ObservationDataset();
        FluentActions.Invoking(() => dataset.Location = new ObservationLocation { Latitude = lat })
            .Should().NotThrow();
    }

    [TestMethod]
    [DataRow("180.0001")]
    [DataRow("-180.0001")]
    public void Location_LongitudeOutOfRange_IsRejected(string text)
    {
        var lon = decimal.Parse(text, CultureInfo.InvariantCulture);
        var dataset = new ObservationDataset();
        FluentActions.Invoking(() => dataset.Location = new ObservationLocation { Longitude = lon })
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow("180")]
    [DataRow("-180")]
    [DataRow("0")]
    public void Location_LongitudeBoundaries_AreAccepted(string text)
    {
        var lon = decimal.Parse(text, CultureInfo.InvariantCulture);
        var dataset = new ObservationDataset();
        FluentActions.Invoking(() => dataset.Location = new ObservationLocation { Longitude = lon })
            .Should().NotThrow();
    }

    [TestMethod]
    [DataRow("Mars")]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("europe/berlin")]
    public void Value_SourceTimezoneOtherThanBerlin_IsRejected(string value)
    {
        var temp = new MeasurementValue();
        FluentActions.Invoking(() => temp.SourceTimezone = value)
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow("Europe/Berlin")]
    [DataRow(null)]
    public void Value_SourceTimezoneBerlinOrNull_IsAccepted(string? value)
    {
        var temp = new MeasurementValue();
        FluentActions.Invoking(() => temp.SourceTimezone = value)
            .Should().NotThrow();
        temp.SourceTimezone.Should().Be(value);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("  ")]
    public void Value_EmptyOrWhitespaceQuality_IsRejected(string value)
    {
        var temp = new MeasurementValue();
        FluentActions.Invoking(() => temp.Quality = value)
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow(" 1 ")]
    [DataRow(null)]
    public void Value_NonEmptyQuality_IsKeptUnchanged(string? value)
    {
        var temp = new MeasurementValue();
        FluentActions.Invoking(() => temp.Quality = value)
            .Should().NotThrow();
        temp.Quality.Should().Be(value);
    }
    [TestMethod]
    public void UndefinedEnumValues_AreRejected()
    {
        FluentActions.Invoking(() => new ObservationDataset().TimestampQuality = (TimestampQuality)99)
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new MeasurementValue().TimestampQuality = (TimestampQuality)99)
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new PressureMeasurementValue().PressureReference = (PressureReference)99)
            .Should().Throw<ArgumentException>();
    }
}
