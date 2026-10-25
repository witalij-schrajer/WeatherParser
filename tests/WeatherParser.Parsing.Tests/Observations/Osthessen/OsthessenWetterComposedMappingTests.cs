using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class OsthessenWetterComposedMappingTests
{
    private OsthessenWetterPageReader _reader = null!;
    private OsthessenWetterObservationMapper _mapper = null!;
    private ObservationDatasetBuilder _builder = null!;

    // Local 10:00 CET on 15 January 2026.
    private static DateTime Retrieved => new(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);
    private static DateTime Utc(int day, int hour, int minute) => new(2026, 1, day, hour, minute, 0, DateTimeKind.Utc);

    [TestInitialize]
    public void TestInitialize()
    {
        _reader = new OsthessenWetterPageReader();
        var options = new OsthessenWetterOptions
        {
            Stations =
            {
                new OsthessenWetterStationMetadata { Name = "Bimbach", Latitude = 50.53m, Longitude = 9.72m, AltitudeM = 312m },
                new OsthessenWetterStationMetadata { Name = "Ro\u00dfbach", Latitude = 50.6m, Longitude = 9.9m },
            },
        };
        _mapper = new OsthessenWetterObservationMapper(new OsthessenWetterStationIdentity(), new OsthessenWetterStationMetadataLookup(options), new OsthessenWetterValueReader(), new OsthessenWetterTimeReader(new SourceTimeNormalizer(new TimeZoneResolver())));
        _builder = new ObservationDatasetBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter());
    }

    private async Task<OsthessenWetterMappedStation> MapSingle(string overviewRow, string? windRow)
    {
        var html = OsthessenWetterHtml.Page(
            OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(), overviewRow),
            windRow is null ? null : OsthessenWetterHtml.Table(OsthessenWetterHtml.WindHeader(), windRow));
        var page = await _reader.ReadAsync(OsthessenWetterHtml.ToStream(html), CancellationToken.None);
        return _mapper.Map(page, Retrieved).Should().ContainSingle().Subject;
    }

    private static ObservationBuildResult Build(ObservationDatasetBuilder builder, OsthessenWetterMappedStation station) =>
        builder.Build(new ObservationInput { Provider = "osthessenwetter", Location = station.Location, RetrievedAtUtc = Retrieved, Readings = station.Readings });

    private ObservationBuildResult Build(OsthessenWetterMappedStation station) => Build(_builder, station);

    [TestMethod]
    public async Task MapThenBuild_OnlyTemperatureHumidityAndMeanWindExported()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach", temperature: "10.3", change: "+1.1", humidity: "93"),
            OsthessenWetterHtml.WindRow("Bimbach", mean10: "23.1"));

        station.Readings.Select(r => r.Kind).Should().Equal(QuantityKind.Temperature, QuantityKind.RelativeHumidity, QuantityKind.MeanWindSpeed);
        station.Readings.Select(r => r.Value).Should().Equal(10.3, 93.0, 23.1);

        var result = Build(station);
        result.OmittedReadings.Should().BeEmpty();

        var measurements = result.Dataset.Measurements;
        measurements.TemperatureC!.Value.Should().Be(10.3m);
        measurements.HumidityPercent!.Value.Should().Be(93m);
        measurements.WindSpeedKmh!.Value.Should().Be(23m); // Rounded to 1 km/h

        measurements.PressureHpa.Should().BeNull();
        measurements.PressureMmHg.Should().BeNull();
        measurements.WindDirectionDeg.Should().BeNull();
        measurements.IlluminanceLux.Should().BeNull();
        measurements.SolarIrradianceWm2.Should().BeNull();
        measurements.RainRateMmH.Should().BeNull();
    }

    [TestMethod]
    public async Task MapThenBuild_Humidity0And100_Kept()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach", humidity: "0"),
            OsthessenWetterHtml.WindRow("Bimbach", mean10: "23.1"));

        var result = Build(station);
        result.Dataset.Measurements.HumidityPercent!.Value.Should().Be(0m);

        station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach", humidity: "100"),
            OsthessenWetterHtml.WindRow("Bimbach", mean10: "23.1"));

        result = Build(station);
        result.Dataset.Measurements.HumidityPercent!.Value.Should().Be(100m);
    }

    [TestMethod]
    public async Task MapThenBuild_HumidityBelow0_OutOfRange()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach", humidity: "-1"),
            OsthessenWetterHtml.WindRow("Bimbach", mean10: "23.1"));

        var result = Build(station);
        result.OmittedReadings.Should().ContainSingle()
            .Which.Should().Be(new OmittedReading(QuantityKind.RelativeHumidity, OmissionReason.OutOfRange));
        result.Dataset.Measurements.HumidityPercent.Should().BeNull();
        result.Dataset.Measurements.TemperatureC.Should().NotBeNull();
    }

    [TestMethod]
    public async Task MapThenBuild_Humidity100Point1_OutOfRangeNeverRoundedFirst()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach", humidity: "100.1"),
            OsthessenWetterHtml.WindRow("Bimbach", mean10: "23.1"));

        var result = Build(station);
        result.OmittedReadings.Should().ContainSingle()
            .Which.Should().Be(new OmittedReading(QuantityKind.RelativeHumidity, OmissionReason.OutOfRange));
        result.Dataset.Measurements.HumidityPercent.Should().BeNull();
    }

    [TestMethod]
    public async Task MapThenBuild_NegativeWind_OutOfRangeNeverClamped()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach"),
            OsthessenWetterHtml.WindRow("Bimbach", mean10: "-0.5"));

        var result = Build(station);
        result.OmittedReadings.Should().ContainSingle()
            .Which.Should().Be(new OmittedReading(QuantityKind.MeanWindSpeed, OmissionReason.OutOfRange));
        result.Dataset.Measurements.WindSpeedKmh.Should().BeNull();
    }

    [TestMethod]
    public async Task MapThenBuild_ZeroWind_Kept()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach"),
            OsthessenWetterHtml.WindRow("Bimbach", mean10: "0.0"));

        var result = Build(station);
        result.Dataset.Measurements.WindSpeedKmh!.Value.Should().Be(0m);
    }

    [TestMethod]
    public async Task MapThenBuild_OverflowDigits_NonFinite()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach", temperature: new string('9', 400)),
            OsthessenWetterHtml.WindRow("Bimbach", mean10: "23.1"));

        var result = Build(station);
        result.OmittedReadings.Should().ContainSingle()
            .Which.Should().Be(new OmittedReading(QuantityKind.Temperature, OmissionReason.NonFinite));
        result.Dataset.Measurements.TemperatureC.Should().BeNull();
    }

    [TestMethod]
    public async Task MapThenBuild_TemperatureWithPositiveNegativeOrZeroChange_OnlyTemperatureExported()
    {
        foreach (var change in new[] { "+1.1", "-0.5", "+0" })
        {
            var station = await MapSingle(
                OsthessenWetterHtml.OverviewRow("Bimbach", temperature: "7.4", change: change),
                OsthessenWetterHtml.WindRow("Bimbach", mean10: "23.1"));

            var result = Build(station);
            result.Dataset.Measurements.TemperatureC!.Value.Should().Be(7.4m);
        }
    }

    [TestMethod]
    public async Task MapThenBuild_MissingTemperatureWithValidChange_TemperatureOmittedNeverChange()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach", temperature: "", change: "+1.1"),
            OsthessenWetterHtml.WindRow("Bimbach", mean10: "23.1"));

        var result = Build(station);
        result.OmittedReadings.Should().ContainSingle()
            .Which.Should().Be(new OmittedReading(QuantityKind.Temperature, OmissionReason.Missing));
        result.Dataset.Measurements.TemperatureC.Should().BeNull();
        result.Dataset.Measurements.HumidityPercent.Should().NotBeNull();
    }

    [TestMethod]
    public async Task MapThenBuild_UnreadableTemperatureWithValidChange_NoticeAndNoTemperature()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach", temperature: "10,3", change: "+1.1"),
            OsthessenWetterHtml.WindRow("Bimbach", mean10: "23.1"));

        station.Notices.Should().ContainSingle()
            .Which.Should().Be(new OsthessenWetterMappingNotice(OsthessenWetterNoticeIds.ValueUnreadable, QuantityKind.Temperature));
        station.Readings.Should().NotContain(r => r.Kind == QuantityKind.Temperature);
        Build(station).Dataset.Measurements.TemperatureC.Should().BeNull();
    }

    [TestMethod]
    public async Task MapThenBuild_MissingOverviewTime_MissingTimeForTemperatureAndHumidity_WindKept()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach", time: ""),
            OsthessenWetterHtml.WindRow("Bimbach", mean10: "23.1"));

        var result = Build(station);
        result.OmittedReadings.Should().Equal(
            new OmittedReading(QuantityKind.Temperature, OmissionReason.MissingTime),
            new OmittedReading(QuantityKind.RelativeHumidity, OmissionReason.MissingTime));
        result.Dataset.Measurements.WindSpeedKmh!.MeasuredAtUtc.Should().Be(Utc(15, 8, 31));
    }

    [TestMethod]
    public async Task MapThenBuild_InvalidWindTime_MissingTimeForWind_OverviewKept()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach"),
            OsthessenWetterHtml.WindRow("Bimbach", time: "9:31 Uhr", mean10: "23.1"));

        var result = Build(station);
        result.OmittedReadings.Should().ContainSingle()
            .Which.Should().Be(new OmittedReading(QuantityKind.MeanWindSpeed, OmissionReason.MissingTime));
        result.Dataset.Measurements.TemperatureC.Should().NotBeNull();
    }

    [TestMethod]
    public async Task MapThenBuild_MissingTime_RetrievalTimeNeverSubstituted()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach", time: ""),
            OsthessenWetterHtml.WindRow("Bimbach", time: "", mean10: "23.1"));

        Action act = () => Build(station);
        act.Should().Throw<ObservationDatasetRejectedException>();
    }

    [TestMethod]
    public async Task MapThenBuild_OldOnlineReading_KeptWithInferredTime()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach", time: "23:00 Uhr"),
            OsthessenWetterHtml.WindRow("Bimbach", time: "23:00 Uhr", mean10: "23.1"));

        var result = Build(station);
        result.Dataset.Measurements.TemperatureC!.MeasuredAtUtc.Should().Be(Utc(14, 22, 0));
        result.Dataset.Measurements.TemperatureC.TimestampQuality.Should().Be(TimestampQuality.EstimatedDate);
        result.Dataset.Measurements.TemperatureC.SourceTimezone.Should().Be("Europe/Berlin");
        result.Dataset.Measurements.WindSpeedKmh!.MeasuredAtUtc.Should().Be(Utc(14, 22, 0));
    }

    [TestMethod]
    public async Task MapThenBuild_WindPeriodExported()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach"),
            OsthessenWetterHtml.WindRow("Bimbach", mean10: "23.1"));

        var result = Build(station);
        result.Dataset.Measurements.WindSpeedKmh!.Period!.StartUtc.Should().Be(Utc(15, 8, 21));
        result.Dataset.Measurements.WindSpeedKmh!.Period!.EndUtc.Should().Be(Utc(15, 8, 31));
    }

    [TestMethod]
    public async Task MapThenBuild_JoinedSpellings_WindExported()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Ro&szligbach"),
            OsthessenWetterHtml.WindRow("Rossbach", mean10: "12.5"));

        station.StationId.Should().Be("rossbach");
        station.Name.Should().Be("Ro\u00dfbach");
        station.Location!.Latitude.Should().Be(50.6m);
        var result = Build(station);
        result.Dataset.Measurements.WindSpeedKmh!.Value.Should().Be(13m); // Rounded away from zero
    }

    [TestMethod]
    public async Task MapThenBuild_NoWindTable_OverviewOnly()
    {
        var station = await MapSingle(
            OsthessenWetterHtml.OverviewRow("Bimbach"),
            null);

        station.Readings.Select(r => r.Kind).Should().Equal(QuantityKind.Temperature, QuantityKind.RelativeHumidity);
        var result = Build(station);
        result.Dataset.Measurements.WindSpeedKmh.Should().BeNull();
        result.OmittedReadings.Should().BeEmpty();
    }
}
