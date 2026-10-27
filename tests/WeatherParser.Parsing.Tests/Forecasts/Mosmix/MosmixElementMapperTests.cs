using System;
using System.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public class MosmixElementMapperTests
{
    private IMosmixElementMapper _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new MosmixElementMapper(new MosmixUnitScaler());
    }

    private static DateTime Utc(int hour) => new(2026, 10, 26, hour, 0, 0, DateTimeKind.Utc);

    private static MosmixForecastSeries Series(string name, params string?[] values) =>
        new() { ElementName = name, Values = values };

    private static MosmixForecastDocument Document(params MosmixForecastSeries[] series) =>
        new()
        {
            MosmixId = "10637",
            IssuedAtUtc = Utc(15),
            Timeline = new[] { Utc(16), Utc(17), Utc(18) },
            LocationName = "BEISPIELSTADT",
            Latitude = 50.5m,
            Longitude = 9.6m,
            AltitudeM = 250.0m,
            Series = series
        };

    private MosmixMappingResult Map(MosmixForecastDocument d) =>
        _subject.Map(d, MosmixProduct.MosmixL, Utc(20).AddMinutes(12), false);

    private static ForecastSeries SeriesOf(MosmixMappingResult r, QuantityKind kind) =>
        r.Input.Series!.Single(s => s!.Kind == kind)!;

    [TestMethod]
    public void Map_Temperature_SeriesInCelsius()
    {
        // Arrange
        var document = Document(Series("TTT", "273.15", null, "253.15"));

        // Act
        var result = Map(document);

        // Assert
        var series = SeriesOf(result, QuantityKind.Temperature);
        series.Values.Should().Equal(0.0, null, -20.0);
        series.Periods.Should().BeNull();
        series.Product.Should().Be(MosmixProduct.MosmixL);
        series.MosmixId.Should().Be("10637");
        series.IssuedAtUtc.Should().Be(Utc(15));
    }

    [TestMethod]
    public void Map_Pressure_HectopascalWithUnknownReference()
    {
        // Arrange
        var document = Document(Series("PPPP", "101325", null, "99000"));

        // Act
        var result = Map(document);

        // Assert
        var series = SeriesOf(result, QuantityKind.Pressure);
        series.Values.Should().Equal(1013.25, null, 990.0);
        series.PressureUnit.Should().Be(PressureUnit.Hectopascal);
        series.PressureReference.Should().Be(PressureReference.Unknown);
    }

    [TestMethod]
    public void Map_WindDirection_Degrees()
    {
        // Arrange
        var document = Document(Series("DD", "0", "360", "180.5"));

        // Act
        var result = Map(document);

        // Assert
        var series = SeriesOf(result, QuantityKind.WindDirection);
        series.Values.Should().Equal(0.0, 360.0, 180.5);
    }

    [TestMethod]
    public void Map_WindSpeed_MetresPerSecondForBuilder()
    {
        // Arrange
        var document = Document(Series("FF", "3.10", null, "0"));

        // Act
        var result = Map(document);

        // Assert
        var series = SeriesOf(result, QuantityKind.MeanWindSpeed);
        series.Values.Should().Equal(3.1, null, 0.0);
        series.WindSpeedUnit.Should().Be(WindSpeedUnit.MetresPerSecond);
    }

    [TestMethod]
    public void Map_FX1_WindGustWithHourPeriodAndMetresPerSecond()
    {
        // Arrange
        var document = Document(Series("FX1", "10.0", null, "12.5"));

        // Act
        var result = Map(document);

        // Assert
        var series = SeriesOf(result, QuantityKind.WindGust);
        series.Values.Should().Equal(10.0, null, 12.5);
        series.WindSpeedUnit.Should().Be(WindSpeedUnit.MetresPerSecond);
        series.Periods!.Select(p => (p!.StartUtc, p.EndUtc)).Should().Equal(
            (Utc(15), Utc(16)),
            (Utc(16), Utc(17)),
            (Utc(17), Utc(18)));
    }

    [TestMethod]
    public void Map_Rad1h_RadiationEnergyWithHourPeriod()
    {
        // Arrange
        var document = Document(Series("Rad1h", "1.5", "0", null));

        // Act
        var result = Map(document);

        // Assert
        var series = SeriesOf(result, QuantityKind.RadiationEnergy);
        series.Values.Should().Equal(1500.0, 0.0, null);
        series.Periods!.Select(p => (p!.StartUtc, p.EndUtc)).Should().Equal(
            (Utc(15), Utc(16)),
            (Utc(16), Utc(17)),
            (Utc(17), Utc(18)));
    }

    [TestMethod]
    public void Map_RR1c_SingleAccumulationSeries()
    {
        // Arrange
        var document = Document(Series("RR1c", "0.3", "0", null));

        // Act
        var result = Map(document);

        // Assert
        result.Input.Series.Should().HaveCount(1);
        var series = SeriesOf(result, QuantityKind.PrecipitationAccumulation);
        series.Values.Should().Equal(0.3, 0.0, null);
        series.Periods!.Select(p => (p!.StartUtc, p.EndUtc)).Should().Equal(
            (Utc(15), Utc(16)),
            (Utc(16), Utc(17)),
            (Utc(17), Utc(18)));
        result.Input.Series.Should().NotContain(s => s!.Kind == QuantityKind.PrecipitationRate);
    }

    [TestMethod]
    public void Map_RunIdentityAndLocation()
    {
        // Arrange
        var document = Document(Series("TTT", "273.15", "273.15", "273.15"));

        // Act
        var result = Map(document);

        // Assert
        result.Input.Product.Should().Be(MosmixProduct.MosmixL);
        result.Input.MosmixId.Should().Be("10637");
        result.Input.IssuedAtUtc.Should().Be(Utc(15));
        result.Input.RetrievedAtUtc.Should().Be(Utc(20).AddMinutes(12));
        result.Input.RunAgeExceeded.Should().Be(false);
        result.Input.Timeline.Should().BeSameAs(document.Timeline);
        result.Input.Location.Should().NotBeNull();
        result.Input.Location!.Name.Should().Be("BEISPIELSTADT");
        result.Input.Location!.Latitude.Should().Be(50.5m);
        result.Input.Location!.Longitude.Should().Be(9.6m);
        result.Input.Location!.AltitudeM.Should().Be(250.0m);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Map_RunAgeExceeded_PassedThrough(bool value)
    {
        // Arrange
        var document = Document(Series("TTT", "273.15", "273.15", "273.15"));

        // Act
        var result = _subject.Map(document, MosmixProduct.MosmixL, Utc(20).AddMinutes(12), value);

        // Assert
        result.Input.RunAgeExceeded.Should().Be(value);
    }

    [TestMethod]
    public void Map_NotMappedElements_ListedByNameInDocumentOrder()
    {
        // Arrange
        var document = Document(
            Series("N", "10", "20", "30"),
            Series("TTT", "273.15", "273.15", "273.15"),
            Series("FX3", "1", "2", "3"),
            Series("Td", "270", "270", "270"),
            Series("wwP", "5", "5", "5"));

        // Act
        var result = Map(document);

        // Assert
        result.NotExportedElements.Should().Equal("N", "FX3", "Td", "wwP");
        result.Input.Series.Should().HaveCount(1);
        result.Input.Series.Should().ContainSingle(s => s!.Kind == QuantityKind.Temperature);
    }

    [TestMethod]
    public void Map_AbsentMappedElements_ReportedInTableOrder()
    {
        // Arrange
        var document = Document(Series("TTT", "273.15", "273.15", "273.15"));

        // Act
        var result = Map(document);

        // Assert
        result.AbsentElements.Should().Equal("PPPP", "DD", "FF", "FX1", "Rad1h", "RR1c");
    }

    [TestMethod]
    public void Map_AllMappedElementsAbsent_EmptySeriesList()
    {
        // Arrange
        var document = Document(Series("N", "10", "20", "30"));

        // Act
        var result = Map(document);

        // Assert
        result.Input.Series.Should().BeEmpty();
        result.AbsentElements.Should().HaveCount(7);
        result.NotExportedElements.Should().Equal("N");
    }

    [TestMethod]
    public void Map_HumidityAndIlluminance_NoSeries()
    {
        // Arrange
        var document = Document(Series("TTT", "273.15", "273.15", "273.15"), Series("Td", "270", "270", "270"));

        // Act
        var result = Map(document);

        // Assert
        result.Input.Series.Should().NotContain(s => s!.Kind == QuantityKind.RelativeHumidity);
        result.Input.Series.Should().NotContain(s => s!.Kind == QuantityKind.Illuminance);
        result.NotExportedElements.Should().Equal("Td");
    }

    [TestMethod]
    public void Map_FirstStepPeriodBeforeIssueTime_Kept()
    {
        // Arrange
        var document = new MosmixForecastDocument
        {
            MosmixId = "10637",
            IssuedAtUtc = Utc(15),
            Timeline = new[] { Utc(15), Utc(16), Utc(17) },
            LocationName = "BEISPIELSTADT",
            Latitude = 50.5m,
            Longitude = 9.6m,
            AltitudeM = 250.0m,
            Series = new[] { Series("FX1", "10.0", null, "12.5") }
        };

        // Act
        var result = Map(document);

        // Assert
        var series = SeriesOf(result, QuantityKind.WindGust);
        series.Periods!.Select(p => (p!.StartUtc, p.EndUtc)).Should().Equal(
            (Utc(14), Utc(15)),
            (Utc(15), Utc(16)),
            (Utc(16), Utc(17)));
    }

    [TestMethod]
    public void Map_LeadingMissingValue_KeptAtItsOwnIndex()
    {
        // Arrange
        var document = Document(Series("TTT", null, "283.15", "284.15"), Series("FX1", null, "10", "12"));

        // Act
        var result = Map(document);

        // Assert
        var tempSeries = SeriesOf(result, QuantityKind.Temperature);
        tempSeries.Values.Should().Equal(null, 10.0, 11.0);
        var windGustSeries = SeriesOf(result, QuantityKind.WindGust);
        windGustSeries.Values.Should().Equal(null, 10.0, 12.0);
        tempSeries.Periods.Should().BeNull();
        windGustSeries.Periods!.Select(p => (p!.StartUtc, p.EndUtc)).Should().Equal(
            (Utc(15), Utc(16)),
            (Utc(16), Utc(17)),
            (Utc(17), Utc(18)));
    }

    [TestMethod]
    public void Map_NullDocument_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        _subject.Invoking(s => s.Map(null!, MosmixProduct.MosmixL, Utc(20), false))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Constructor_NullScaler_ThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Action act = () => new MosmixElementMapper(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
