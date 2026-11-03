using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Time;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Tests.Observations.Dwd;

[TestClass]
public sealed class DwdObservationMapperTests
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

    private static CdcNowEvidence Tu(DateTime time, string pp = "1001.2", string tt = "12.3", string rf = "85.0") => Evidence("tu", Row(time, "3", ("PP_10", pp), ("TT_10", tt), ("TM5_10", "10.1"), ("RF_10", rf), ("TD_10", "8.9")));

    private static CdcNowEvidence Ff(params (DateTime Time, string Ff, string Dd)[] rows) => Evidence("ff", rows.Select(r => Row(r.Time, "3", ("FF_10", r.Ff), ("DD_10", r.Dd))).ToArray());

    [TestMethod]
    public void Map_TuArchive_ProducesTemperatureHumidityPressure()
    {
        var result = _subject.Map(new[] { Tu(T0) });
        Single(result, QuantityKind.Temperature).Value.Should().Be(12.3);
        Single(result, QuantityKind.RelativeHumidity).Value.Should().Be(85.0);
        Single(result, QuantityKind.Pressure).Value.Should().Be(1001.2);
    }

    [TestMethod]
    public void Map_FfArchive_ProducesWindSpeedAndDirection()
    {
        var result = _subject.Map(new[] { Evidence("ff", Row(T0, "3", ("FF_10", "3.4"), ("DD_10", "270"))) });
        Single(result, QuantityKind.MeanWindSpeed).Value.Should().Be(3.4);
        Single(result, QuantityKind.WindDirection).Value.Should().Be(270.0);
    }

    [TestMethod]
    public void Map_RrArchive_ProducesPrecipitationAccumulation()
    {
        var result = _subject.Map(new[] { Evidence("rr", Row(T0, "3", ("RWS_DAU_10", "10"), ("RWS_10", "0.4"), ("RWS_IND_10", "1"))) });
        Single(result, QuantityKind.PrecipitationAccumulation).Value.Should().Be(0.4);
    }

    [TestMethod]
    public void Map_SdArchive_ProducesRadiationEnergy()
    {
        var result = _subject.Map(new[] { Evidence("sd", Row(T0, "3", ("DS_10", "1.0"), ("GS_10", "12.5"), ("SD_10", "0.1"), ("LS_10", "20.0"))) });
        result.Readings.Should().ContainSingle(r => r.Kind == QuantityKind.RadiationEnergy);
    }

    [TestMethod]
    public void Map_Pressure_HectopascalSensorReference()
    {
        var result = _subject.Map(new[] { Tu(T0) });
        var pressure = Single(result, QuantityKind.Pressure);
        pressure.PressureUnit.Should().Be(PressureUnit.Hectopascal);
        pressure.PressureReference.Should().Be(PressureReference.Sensor);
    }

    [TestMethod]
    public void Map_WindSpeed_MetresPerSecondUnit()
    {
        var result = _subject.Map(new[] { Evidence("ff", Row(T0, "3", ("FF_10", "3.4"), ("DD_10", "270"))) });
        Single(result, QuantityKind.MeanWindSpeed).WindSpeedUnit.Should().Be(WindSpeedUnit.MetresPerSecond);
    }

    [TestMethod]
    public void Map_LatestRowWins()
    {
        var result = _subject.Map(new[]
        {
            Evidence("tu",
                Row(T0, "3", ("PP_10", "1000.0"), ("TT_10", "11.0"), ("RF_10", "80.0")),
                Row(T0.AddMinutes(10), "3", ("PP_10", "1001.0"), ("TT_10", "12.0"), ("RF_10", "81.0")))
        });
        Single(result, QuantityKind.Temperature).Value.Should().Be(12.0);
    }

    [TestMethod]
    public void Map_RowsOutOfOrder_LatestByTimeNotByPosition()
    {
        var result = _subject.Map(new[]
        {
            Evidence("tu",
                Row(T0.AddMinutes(10), "3", ("PP_10", "1001.0"), ("TT_10", "12.0"), ("RF_10", "81.0")),
                Row(T0, "3", ("PP_10", "1000.0"), ("TT_10", "11.0"), ("RF_10", "80.0")))
        });
        Single(result, QuantityKind.Temperature).Value.Should().Be(12.0);
    }

    [TestMethod]
    public void Map_LatestRowRejectedByValidator_OlderAcceptedRowChosen()
    {
        var result = _subject.Map(new[]
        {
            Evidence("tu",
                Row(T0, "3", ("PP_10", "1000.0"), ("TT_10", "11.0"), ("RF_10", "80.0")),
                Row(T0.AddMinutes(10), "3", ("PP_10", "1000.0"), ("TT_10", "11.0"), ("RF_10", "101.0")))
        });
        Single(result, QuantityKind.RelativeHumidity).Value.Should().Be(80.0);
    }

    [TestMethod]
    public void Map_UnmappedColumnsIgnored()
    {
        var result = _subject.Map(new[]
        {
            Tu(T0),
            Evidence("rr", Row(T0, "3", ("RWS_DAU_10", "10"), ("RWS_10", "0.4"), ("RWS_IND_10", "1"))),
            Evidence("sd", Row(T0, "3", ("DS_10", "1.0"), ("GS_10", "12.5"), ("SD_10", "0.1"), ("LS_10", "20.0")))
        });
        result.Readings.Select(r => r.Kind!.Value).Should().BeEquivalentTo(
            new[]
            {
                QuantityKind.Temperature,
                QuantityKind.RelativeHumidity,
                QuantityKind.Pressure,
                QuantityKind.PrecipitationAccumulation,
                QuantityKind.RadiationEnergy
            });
    }

    [TestMethod]
    public void Map_ReadingTimeIsRowTime()
    {
        var result = _subject.Map(new[] { Tu(T0) });
        var temp = Single(result, QuantityKind.Temperature);
        temp.Time.Should().NotBeNull();
        temp.Time!.UtcInstant.Should().Be(T0);
        temp.Time.Quality.Should().Be(TimestampQuality.Source);
        temp.Time.SourceTimeZone.Should().BeNull();
    }

    [TestMethod]
    public void Map_EmptyEvidence_NoReadingsAndNoOmissions()
    {
        var result = _subject.Map(Array.Empty<CdcNowEvidence>());
        result.Readings.Should().BeEmpty();
        result.Omissions.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_NullEvidence_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => _subject.Map(null!)).Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Map_UnknownProductCode_ThrowsArgumentException()
    {
        FluentActions.Invoking(() => _subject.Map(new[] { Evidence("tx", Row(T0, "3")) })).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Map_DuplicateProductCode_ThrowsArgumentException()
    {
        FluentActions.Invoking(() => _subject.Map(new[] { Tu(T0), Tu(T0) })).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Constructor_NullValidator_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new DwdObservationMapper(null!, new SourceTimeNormalizer(new TimeZoneResolver()))).Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Constructor_NullNormalizer_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new DwdObservationMapper(new QuantityValidator(), null!)).Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesDwdObservationMapper()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IDwdObservationMapper>().Should().BeOfType<DwdObservationMapper>();
    }

    [TestMethod]
    public void Map_MinusNineNineNine_IsMissing()
    {
        var result = _subject.Map(new[] { Tu(T0, tt: "-999") });
        Single(result, QuantityKind.Temperature).Value.Should().BeNull();
    }

    [DataRow("-999.0")]
    [DataRow("-999.000")]
    [TestMethod]
    public void Map_MinusNineNineNineWithDecimals_IsMissing(string token)
    {
        var result = _subject.Map(new[] { Tu(T0, tt: token) });
        Single(result, QuantityKind.Temperature).Value.Should().BeNull();
    }

    [TestMethod]
    public void Map_ZeroIsUsable()
    {
        var result = _subject.Map(new[]
        {
            Tu(T0, tt: "0.0"),
            Evidence("ff", Row(T0, "3", ("FF_10", "0"), ("DD_10", "270"))),
            Evidence("rr", Row(T0, "3", ("RWS_10", "0.00")))
        });
        Single(result, QuantityKind.Temperature).Value.Should().Be(0.0);
        Single(result, QuantityKind.MeanWindSpeed).Value.Should().Be(0.0);
        Single(result, QuantityKind.PrecipitationAccumulation).Value.Should().Be(0.0);
    }

    [TestMethod]
    public void Map_OnlyMissingRows_YieldsReadingWithNullValue()
    {
        var result = _subject.Map(new[]
        {
            Evidence("tu", 
                Row(T0, "3", ("PP_10", "1000.0"), ("TT_10", "11.0"), ("RF_10", "-999")),
                Row(T0.AddMinutes(10), "3", ("PP_10", "1000.0"), ("TT_10", "11.0"), ("RF_10", "-999")))
        });
        var humidity = Single(result, QuantityKind.RelativeHumidity);
        humidity.Value.Should().BeNull();
        humidity.Time.Should().BeNull();
    }

    [TestMethod]
    public void Map_AllRowsOutOfRange_PassesLatestValueForBuilderOutOfRange()
    {
        var result = _subject.Map(new[]
        {
            Evidence("tu",
                Row(T0, "3", ("PP_10", "1000.0"), ("TT_10", "11.0"), ("RF_10", "101.0")),
                Row(T0.AddMinutes(10), "3", ("PP_10", "1000.0"), ("TT_10", "11.0"), ("RF_10", "101.0")))
        });
        var humidity = Single(result, QuantityKind.RelativeHumidity);
        humidity.Value.Should().Be(101.0);
        humidity.Time!.UtcInstant.Should().Be(T0.AddMinutes(10));

        var builder = new ObservationDatasetBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter());
        var built = builder.Build(new ObservationInput { Provider = "dwd", Location = new ObservationLocation { StationId = "00044", Name = "Station", Latitude = 50.1m, Longitude = 8.6m }, RetrievedAtUtc = T0.AddMinutes(20), Readings = result.Readings });
        built.OmittedReadings.Should().ContainSingle(o => o.Kind == QuantityKind.RelativeHumidity).Which.Reason.Should().Be(OmissionReason.OutOfRange);
        built.Dataset.Measurements.TemperatureC.Should().NotBeNull();
    }

    [TestMethod]
    public void Map_BoundaryValues_AcceptedByRealValidator()
    {
        var result1 = _subject.Map(new[] { Tu(T0, rf: "0.0") });
        Single(result1, QuantityKind.RelativeHumidity).Value.Should().Be(0.0);

        var result2 = _subject.Map(new[] { Tu(T0, rf: "100.0") });
        Single(result2, QuantityKind.RelativeHumidity).Value.Should().Be(100.0);

        var result3 = _subject.Map(new[] { Evidence("ff", Row(T0, "3", ("FF_10", "3.0"), ("DD_10", "360"))) });
        Single(result3, QuantityKind.WindDirection).Value.Should().Be(360.0);
    }

    [TestMethod]
    public void Map_LatestMissingOlderUsable_OlderChosen()
    {
        var result = _subject.Map(new[]
        {
            Evidence("tu",
                Row(T0, "3", ("PP_10", "1000.0"), ("TT_10", "11.0"), ("RF_10", "80.0")),
                Row(T0.AddMinutes(10), "3", ("PP_10", "1000.0"), ("TT_10", "-999"), ("RF_10", "80.0")))
        });
        Single(result, QuantityKind.Temperature).Value.Should().Be(11.0);
        Single(result, QuantityKind.Temperature).Time!.UtcInstant.Should().Be(T0);
    }

    [TestMethod]
    public void Map_TokenNotNumeric_TreatedAsUnusable()
    {
        var result = _subject.Map(new[]
        {
            Evidence("tu",
                Row(T0, "3", ("PP_10", "1000.0"), ("TT_10", "11.0"), ("RF_10", "80.0")),
                Row(T0.AddMinutes(10), "3", ("PP_10", "1000.0"), ("TT_10", "abc"), ("RF_10", "80.0")))
        });
        Single(result, QuantityKind.Temperature).Value.Should().Be(11.0);
    }

    [TestMethod]
    public void Map_CalmWind_DirectionOmittedPerContract()
    {
        var result = _subject.Map(new[] { Ff((T0, "3.0", "270"), (T0.AddMinutes(10), "0.0", "180")) });
        Single(result, QuantityKind.WindDirection).Value.Should().Be(270.0);
        result.Omissions.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_DirectionZero_OmittedAsNoReliableDirection()
    {
        var result = _subject.Map(new[] { Ff((T0, "3.0", "90"), (T0.AddMinutes(10), "3.0", "0")) });
        Single(result, QuantityKind.WindDirection).Value.Should().Be(90.0);
    }

    [TestMethod]
    public void Map_MissingSpeed_DoesNotSuppressDirection()
    {
        var result = _subject.Map(new[] { Ff((T0, "-999", "270")) });
        Single(result, QuantityKind.WindDirection).Value.Should().Be(270.0);
    }

    [TestMethod]
    public void Map_WindWithSpeed_DirectionKept()
    {
        var result = _subject.Map(new[] { Ff((T0, "3.0", "180")) });
        Single(result, QuantityKind.WindDirection).Value.Should().Be(180.0);
    }

    [TestMethod]
    public void Map_Direction360_PassedThroughForC43()
    {
        var result = _subject.Map(new[] { Ff((T0, "3.0", "360")) });
        Single(result, QuantityKind.WindDirection).Value.Should().Be(360.0);
    }

    [TestMethod]
    public void Map_AllRowsCalm_NoDirectionReadingAndOneCalmWindOmission()
    {
        var result = _subject.Map(new[] { Tu(T0), Ff((T0, "0.0", "180"), (T0.AddMinutes(10), "0.0", "180")) });
        result.Readings.Should().NotContain(r => r.Kind == QuantityKind.WindDirection);
        result.Omissions.Should().ContainSingle().Which.Should().Be(new DwdMappingOmission(QuantityKind.WindDirection, DwdMappingOmissionReason.CalmWind));
        Single(result, QuantityKind.Temperature).Value.Should().Be(12.3);
        Single(result, QuantityKind.MeanWindSpeed).Value.Should().Be(0.0);
    }

    [TestMethod]
    public void Map_AllRowsZeroDirection_NoDirectionReadingAndOneZeroDirectionOmission()
    {
        var result = _subject.Map(new[] { Ff((T0, "3.0", "0"), (T0.AddMinutes(10), "3.0", "0")) });
        result.Readings.Should().NotContain(r => r.Kind == QuantityKind.WindDirection);
        result.Omissions.Should().ContainSingle().Which.Should().Be(new DwdMappingOmission(QuantityKind.WindDirection, DwdMappingOmissionReason.ZeroDirection));
    }

    [TestMethod]
    public void Map_CalmAndZeroDirectionInSameRow_ReasonCalmWind()
    {
        var result = _subject.Map(new[] { Ff((T0, "0.0", "0")) });
        result.Readings.Should().NotContain(r => r.Kind == QuantityKind.WindDirection);
        result.Omissions.Should().ContainSingle().Which.Should().Be(new DwdMappingOmission(QuantityKind.WindDirection, DwdMappingOmissionReason.CalmWind));
    }

    [TestMethod]
    public void Map_NewestNonMissingRowDecidesReason()
    {
        var result = _subject.Map(new[] { Ff((T0, "0.0", "180"), (T0.AddMinutes(10), "3.0", "0"), (T0.AddMinutes(20), "3.0", "-999")) });
        result.Readings.Should().NotContain(r => r.Kind == QuantityKind.WindDirection);
        result.Omissions.Should().ContainSingle().Which.Should().Be(new DwdMappingOmission(QuantityKind.WindDirection, DwdMappingOmissionReason.ZeroDirection));
    }

    [TestMethod]
    public void Map_ExcludedNewestAndRejectedOlder_ReasonFromNewest()
    {
        var result = _subject.Map(new[] { Ff((T0, "3.0", "400"), (T0.AddMinutes(10), "0.0", "180")) });
        result.Readings.Should().NotContain(r => r.Kind == QuantityKind.WindDirection);
        result.Omissions.Should().ContainSingle().Which.Should().Be(new DwdMappingOmission(QuantityKind.WindDirection, DwdMappingOmissionReason.CalmWind));
    }

    [TestMethod]
    public void Map_RejectedNewestAndExcludedOlder_PassesRejectedValue()
    {
        var result = _subject.Map(new[] { Ff((T0, "0.0", "180"), (T0.AddMinutes(10), "3.0", "400")) });
        Single(result, QuantityKind.WindDirection).Value.Should().Be(400.0);
        result.Omissions.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_NoExclusion_EmptyOmissions()
    {
        var result = _subject.Map(new[] { Ff((T0, "3.0", "180")) });
        result.Omissions.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_QualityFromChosenRow()
    {
        var result = _subject.Map(new[] { Evidence("tu", Row(T0, "2", ("PP_10", "1000.0"), ("TT_10", "11.0"), ("RF_10", "80.0")), Row(T0.AddMinutes(10), "3", ("PP_10", "1000.0"), ("TT_10", "-999"), ("RF_10", "80.0"))) });
        Single(result, QuantityKind.Temperature).Quality.Should().Be("2");
        Single(result, QuantityKind.RelativeHumidity).Quality.Should().Be("3");
    }

    [TestMethod]
    public void Map_QualityFormPerContract()
    {
        var result = _subject.Map(new[] { Tu(T0) });
        foreach (var reading in result.Readings)
        {
            reading.Quality.Should().Be("3");
        }
    }

    [TestMethod]
    public void Map_RowWithoutQualityLevel_NoQuality()
    {
        var result = _subject.Map(new[] { Evidence("tu", Row(T0, "", ("PP_10", "1000.0"), ("TT_10", "11.0"), ("RF_10", "80.0"))) });
        Single(result, QuantityKind.Temperature).Quality.Should().BeNull();
    }
}
