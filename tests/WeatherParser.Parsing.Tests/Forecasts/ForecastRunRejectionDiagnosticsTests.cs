using System;
using System.Collections.Generic;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Forecasts;

[TestClass]
public sealed class ForecastRunRejectionDiagnosticsTests
{
    private IForecastRunBuilder _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new ForecastRunBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter());
    }

    private static readonly DateTime IssuedAt = new(2026, 7, 15, 9, 0, 0, DateTimeKind.Utc);
    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);

    private static ForecastSeries Series(QuantityKind kind, params double?[] values) => new()
    {
        Kind = kind, Values = values, Product = MosmixProduct.MosmixL, MosmixId = "10637", IssuedAtUtc = IssuedAt
    };

    private static ForecastRunInput Input(DateTime[] timeline, params ForecastSeries[] series) => new()
    {
        Product = MosmixProduct.MosmixL, MosmixId = "10637",
        Location = new ForecastLocation { Name = "FRANKFURT/M", Latitude = 50.05m, Longitude = 8.6m },
        IssuedAtUtc = IssuedAt, RetrievedAtUtc = Utc(10, 12), Timeline = timeline, Series = series
    };

    [TestMethod]
    public void Build_NoUsableValue_ExceptionCarriesOmissions()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0), Utc(13, 0), Utc(14, 0) },
            Series(QuantityKind.Temperature, null, double.NaN, null),
            Series(QuantityKind.RelativeHumidity, 120.0, -5.0, null));

        // Act
        Action act = () => _subject.Build(input);

        // Assert
        act.Should().Throw<ForecastRunRejectedException>()
            .Which.OmittedValues.Should().Equal(
                new OmittedForecastValue(Utc(12, 0), QuantityKind.Temperature, OmissionReason.Missing),
                new OmittedForecastValue(Utc(13, 0), QuantityKind.Temperature, OmissionReason.NonFinite),
                new OmittedForecastValue(Utc(14, 0), QuantityKind.Temperature, OmissionReason.Missing),
                new OmittedForecastValue(Utc(12, 0), QuantityKind.RelativeHumidity, OmissionReason.OutOfRange),
                new OmittedForecastValue(Utc(13, 0), QuantityKind.RelativeHumidity, OmissionReason.OutOfRange),
                new OmittedForecastValue(Utc(14, 0), QuantityKind.RelativeHumidity, OmissionReason.Missing));
    }

    [TestMethod]
    public void Build_EmptyTimeline_ExceptionHasNoOmissions()
    {
        // Arrange
        var input = Input(Array.Empty<DateTime>(), Series(QuantityKind.Temperature, Array.Empty<double?>()));

        // Act
        Action act = () => _subject.Build(input);

        // Assert
        act.Should().Throw<ForecastRunRejectedException>()
            .Which.OmittedValues.Should().BeEmpty();
    }

    [TestMethod]
    public void Build_LengthMismatch_ExceptionHasNoOmissions()
    {
        // Arrange
        var input = Input(
            new[] { Utc(12, 0), Utc(13, 0) },
            Series(QuantityKind.Temperature, 1.0, 2.0, 3.0));

        // Act
        Action act = () => _subject.Build(input);

        // Assert
        act.Should().Throw<ForecastRunRejectedException>()
            .Which.OmittedValues.Should().BeEmpty();
    }

    [TestMethod]
    public void Constructor_MessageOnly_OmittedValuesEmpty()
    {
        // Arrange
        var exception = new ForecastRunRejectedException("m");

        // Act
        // Assert
        exception.Message.Should().Be("m");
        exception.OmittedValues.Should().BeEmpty();
    }

    [TestMethod]
    public void Constructor_WithOmissions_KeepsList()
    {
        // Arrange
        var list = new List<OmittedForecastValue>
        {
            new OmittedForecastValue(Utc(12, 0), QuantityKind.Temperature, OmissionReason.Missing)
        };

        // Act
        var exception = new ForecastRunRejectedException("m", list);

        // Assert
        exception.OmittedValues.Should().BeSameAs(list);
    }

    [TestMethod]
    public void Constructor_NullOmissions_ThrowsArgumentNullException()
    {
        // Arrange
        // Act
        // Assert
        FluentActions.Invoking(() => new ForecastRunRejectedException("m", null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("omittedValues");
    }
}
