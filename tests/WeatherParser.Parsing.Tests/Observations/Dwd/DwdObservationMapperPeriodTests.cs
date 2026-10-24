using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations.Dwd;

[TestClass]
public sealed class DwdObservationMapperPeriodTests
{
    private DwdObservationMapper _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new DwdObservationMapper(new QuantityValidator(), new SourceTimeNormalizer(new TimeZoneResolver()));
    }

    private static readonly DateTime T0 = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    private static CdcNowEvidenceRow Row(DateTime time, string qn, params (string Column, string Token)[] values) => new(time, values.ToDictionary(v => v.Column, v => v.Token, StringComparer.Ordinal)) { QualityLevel = qn };

    private static CdcNowEvidence Evidence(string productCode, params CdcNowEvidenceRow[] rows) => new("00044", productCode, rows);

    private static ObservationReading Single(DwdMappingResult result, QuantityKind kind) => result.Readings.Should().ContainSingle(r => r.Kind == kind).Subject;

    private static void ShouldHaveTenMinutePeriod(ObservationReading reading)
    {
        reading.Period.Should().NotBeNull();
        reading.Period!.StartUtc.Should().Be(T0.AddMinutes(-10));
        reading.Period.EndUtc.Should().Be(T0);
    }

    [TestMethod]
    public void Map_WindSpeed_PeriodTenMinutesEndingAtMeasurement()
    {
        var result = _subject.Map(new[] { Evidence("ff", Row(T0, "3", ("FF_10", "3.4"), ("DD_10", "270"))) });
        ShouldHaveTenMinutePeriod(Single(result, QuantityKind.MeanWindSpeed));
    }

    [TestMethod]
    public void Map_WindDirection_PeriodTenMinutes()
    {
        var result = _subject.Map(new[] { Evidence("ff", Row(T0, "3", ("FF_10", "3.4"), ("DD_10", "270"))) });
        ShouldHaveTenMinutePeriod(Single(result, QuantityKind.WindDirection));
    }

    [TestMethod]
    public void Map_Precipitation_PeriodTenMinutes()
    {
        var result = _subject.Map(new[] { Evidence("rr", Row(T0, "3", ("RWS_10", "0.4"))) });
        ShouldHaveTenMinutePeriod(Single(result, QuantityKind.PrecipitationAccumulation));
    }

    [TestMethod]
    public void Map_RadiationEnergy_PeriodTenMinutes()
    {
        var result = _subject.Map(new[] { Evidence("sd", Row(T0, "3", ("GS_10", "36.0"))) });
        ShouldHaveTenMinutePeriod(Single(result, QuantityKind.RadiationEnergy));
    }

    [TestMethod]
    public void Map_Temperature_NoPeriod()
    {
        var result = _subject.Map(new[] { Evidence("tu", Row(T0, "3", ("PP_10", "1001.2"), ("TT_10", "12.3"), ("RF_10", "85.0"))) });
        Single(result, QuantityKind.Temperature).Period.Should().BeNull();
    }

    [TestMethod]
    public void Map_Humidity_NoPeriod()
    {
        var result = _subject.Map(new[] { Evidence("tu", Row(T0, "3", ("PP_10", "1001.2"), ("TT_10", "12.3"), ("RF_10", "85.0"))) });
        Single(result, QuantityKind.RelativeHumidity).Period.Should().BeNull();
    }

    [TestMethod]
    public void Map_Pressure_NoPeriod()
    {
        var result = _subject.Map(new[] { Evidence("tu", Row(T0, "3", ("PP_10", "1001.2"), ("TT_10", "12.3"), ("RF_10", "85.0"))) });
        Single(result, QuantityKind.Pressure).Period.Should().BeNull();
    }

    [TestMethod]
    public void Map_RadiationEnergy_ConvertsJoulePerSquareCentimetreToSquareMetre()
    {
        var result = _subject.Map(new[] { Evidence("sd", Row(T0, "3", ("GS_10", "36.0"))) });
        Single(result, QuantityKind.RadiationEnergy).Value.Should().Be(360000.0);
    }

    [TestMethod]
    public void Map_PrecipitationValue_Unconverted()
    {
        var result = _subject.Map(new[] { Evidence("rr", Row(T0, "3", ("RWS_10", "0.4"))) });
        Single(result, QuantityKind.PrecipitationAccumulation).Value.Should().Be(0.4);
    }

    [TestMethod]
    public void Map_RadiationEnergyZero_IsZeroNotMissing()
    {
        var result = _subject.Map(new[] { Evidence("sd", Row(T0, "3", ("GS_10", "0.0"))) });
        var reading = Single(result, QuantityKind.RadiationEnergy);
        reading.Value.Should().Be(0.0);
        ShouldHaveTenMinutePeriod(reading);
        result.Omissions.Should().BeEmpty();
    }
}
