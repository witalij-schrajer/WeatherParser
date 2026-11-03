using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class OsthessenWetterObservationMapperTests
{
    private MockRepository _mocks = null!;
    private OsthessenWetterObservationMapper _subject = null!;

    // Local 10:00 CET on 15 January 2026.
    private static DateTime Retrieved => new(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);
    private static DateTime Utc(int month, int day, int hour, int minute) => new(2026, month, day, hour, minute, 0, DateTimeKind.Utc);

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        var options = new OsthessenWetterOptions
        {
            Stations =
            {
                new OsthessenWetterStationMetadata { Name = "Bimbach", Latitude = 50.53m, Longitude = 9.72m, AltitudeM = 312m },
                new OsthessenWetterStationMetadata { Name = "Schlitz", Latitude = 50.67m, Longitude = 9.56m },
            },
        };
        _subject = new OsthessenWetterObservationMapper(
            new OsthessenWetterStationIdentity(),
            new OsthessenWetterStationMetadataLookup(options),
            new OsthessenWetterValueReader(),
            new OsthessenWetterTimeReader(new SourceTimeNormalizer(new TimeZoneResolver())));
    }

    [TestCleanup]
    public void TestCleanup() => _mocks.VerifyAll();

    private static OsthessenWetterStationRow Row(string name, OsthessenWetterStationStatus status = OsthessenWetterStationStatus.Online, string time = "09:31 Uhr", string temperature = "10.3\u00B0C", string humidity = "93%", string? windTime = "09:30 Uhr", string? wind = "23.1km/h") =>
        new(name, status, time, temperature, humidity, windTime, wind);

    private static OsthessenWetterPage Page(params OsthessenWetterStationRow[] rows) => new(rows, Array.Empty<OsthessenWetterPageDiagnostic>());

    private static ObservationReading Reading(OsthessenWetterMappedStation station, QuantityKind kind) => station.Readings.Should().ContainSingle(r => r.Kind == kind).Subject;

    private static NormalizedSourceTime Estimated(DateTime utc) => new(utc, TimestampQuality.EstimatedDate, "Europe/Berlin");

    [TestMethod]
    public void Map_Station_ReadingsWithOwnTimes()
    {
        var station = _subject.Map(Page(Row("Bimbach")), Retrieved).Should().ContainSingle().Subject;

        station.StationId.Should().Be("bimbach");
        station.Name.Should().Be("Bimbach");
        station.Status.Should().Be(OsthessenWetterStationStatus.Online);
        station.Notices.Should().BeEmpty();
        station.Readings.Should().HaveCount(3);

        Reading(station, QuantityKind.Temperature).Should().BeEquivalentTo(
            new ObservationReading
            {
                Kind = QuantityKind.Temperature,
                Value = 10.3,
                Time = Estimated(Utc(1, 15, 8, 31)),
                Period = null,
                Quality = null,
                PressureUnit = null,
                PressureReference = null,
                WindSpeedUnit = null
            });

        Reading(station, QuantityKind.RelativeHumidity).Should().BeEquivalentTo(
            new ObservationReading
            {
                Kind = QuantityKind.RelativeHumidity,
                Value = 93.0,
                Time = Estimated(Utc(1, 15, 8, 31)),
                Period = null,
                Quality = null,
                PressureUnit = null,
                PressureReference = null,
                WindSpeedUnit = null
            });

        Reading(station, QuantityKind.MeanWindSpeed).Should().BeEquivalentTo(
            new ObservationReading
            {
                Kind = QuantityKind.MeanWindSpeed,
                Value = 23.1,
                Time = Estimated(Utc(1, 15, 8, 30)),
                Period = new AveragingPeriod(Utc(1, 15, 8, 20), Utc(1, 15, 8, 30)),
                Quality = null,
                PressureUnit = null,
                PressureReference = null,
                WindSpeedUnit = WindSpeedUnit.KilometresPerHour
            });
    }

    [TestMethod]
    public void Map_WindPeriod_TenMinutesBeforeWindTime()
    {
        var station = _subject.Map(Page(Row("Bimbach")), Retrieved).Should().ContainSingle().Subject;

        var windReading = Reading(station, QuantityKind.MeanWindSpeed);
        windReading.Period.Should().NotBeNull();
        windReading.Period.StartUtc.Should().Be(Utc(1, 15, 8, 20));
        windReading.Period.EndUtc.Should().Be(Utc(1, 15, 8, 30));

        var tempReading = Reading(station, QuantityKind.Temperature);
        tempReading.Period.Should().BeNull();

        var humidityReading = Reading(station, QuantityKind.RelativeHumidity);
        humidityReading.Period.Should().BeNull();
    }

    [TestMethod]
    public void Map_Location_FromMetadata()
    {
        var stations = _subject.Map(Page(Row("Bimbach"), Row("Schlitz")), Retrieved);

        stations[0].Location.Should().NotBeNull();
        stations[0].Location!.StationId.Should().Be("bimbach");
        stations[0].Location!.Name.Should().Be("Bimbach");
        stations[0].Location!.Latitude.Should().Be(50.53m);
        stations[0].Location!.Longitude.Should().Be(9.72m);
        stations[0].Location!.AltitudeM.Should().Be(312m);

        stations[1].Location.Should().NotBeNull();
        stations[1].Location!.StationId.Should().Be("schlitz");
        stations[1].Location!.AltitudeM.Should().BeNull();
    }

    [TestMethod]
    public void Map_PageOrderKept()
    {
        var stations = _subject.Map(Page(Row("Schlitz"), Row("Bimbach")), Retrieved);

        stations.Select(s => s.StationId).Should().ContainInOrder("schlitz", "bimbach");
    }

    [TestMethod]
    public void Map_UnmatchedWind_OverviewReadingsKept()
    {
        var station = _subject.Map(Page(Row("Bimbach", windTime: null, wind: null)), Retrieved).Should().ContainSingle().Subject;

        station.Readings.Should().HaveCount(2);
        station.Readings.Select(r => r.Kind).Should().ContainInOrder(QuantityKind.Temperature, QuantityKind.RelativeHumidity);
        station.Notices.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_UnresolvedOverviewTime_OnlyWindReadings()
    {
        var retrieval = new DateTime(2026, 10, 25, 3, 0, 0, DateTimeKind.Utc); // local 04:00 CET, after the autumn overlap
        var station = _subject.Map(Page(Row("Bimbach", time: "02:30 Uhr", windTime: "03:50 Uhr")), retrieval).Should().ContainSingle().Subject;

        station.Readings.Should().HaveCount(1);
        station.Readings[0].Kind.Should().Be(QuantityKind.MeanWindSpeed);
        station.Readings[0].Time.Should().Be(Estimated(Utc(10, 25, 2, 50)));

        station.Notices.Should().Equal(
            new OsthessenWetterMappingNotice(OsthessenWetterNoticeIds.TimeUnresolved, QuantityKind.Temperature),
            new OsthessenWetterMappingNotice(OsthessenWetterNoticeIds.TimeUnresolved, QuantityKind.RelativeHumidity));
    }

    [TestMethod]
    public void Map_UnresolvedWindTime_OverviewKept()
    {
        var retrieval = new DateTime(2026, 10, 25, 3, 0, 0, DateTimeKind.Utc); // local 04:00 CET, after the autumn overlap
        var station = _subject.Map(Page(Row("Bimbach", time: "03:50 Uhr", windTime: "02:30 Uhr")), retrieval).Should().ContainSingle().Subject;

        station.Readings.Should().HaveCount(2);
        station.Readings.Select(r => r.Kind).Should().ContainInOrder(QuantityKind.Temperature, QuantityKind.RelativeHumidity);

        station.Notices.Should().Equal(new OsthessenWetterMappingNotice(OsthessenWetterNoticeIds.TimeUnresolved, QuantityKind.MeanWindSpeed));
    }

    [TestMethod]
    public void Map_UnreadableValue_NoticeWithoutValueText()
    {
        var station = _subject.Map(Page(Row("Bimbach", temperature: "10,3\u00B0C")), Retrieved).Should().ContainSingle().Subject;

        station.Readings.Should().HaveCount(2);
        station.Readings.Select(r => r.Kind).Should().ContainInOrder(QuantityKind.RelativeHumidity, QuantityKind.MeanWindSpeed);

        station.Notices.Should().Equal(new OsthessenWetterMappingNotice(OsthessenWetterNoticeIds.ValueUnreadable, QuantityKind.Temperature));
    }

    [TestMethod]
    public void Map_UnreadableWind_Notice()
    {
        var station = _subject.Map(Page(Row("Bimbach", wind: "23.1m/s")), Retrieved).Should().ContainSingle().Subject;

        station.Readings.Should().HaveCount(2);
        station.Readings.Select(r => r.Kind).Should().ContainInOrder(QuantityKind.Temperature, QuantityKind.RelativeHumidity);

        station.Notices.Should().Equal(new OsthessenWetterMappingNotice(OsthessenWetterNoticeIds.ValueUnreadable, QuantityKind.MeanWindSpeed));
    }

    [TestMethod]
    public void Map_MissingValue_PassedOnAsNull()
    {
        var station = _subject.Map(Page(Row("Bimbach", humidity: "")), Retrieved).Should().ContainSingle().Subject;

        Reading(station, QuantityKind.RelativeHumidity).Value.Should().BeNull();
        Reading(station, QuantityKind.RelativeHumidity).Time.Should().Be(Estimated(Utc(1, 15, 8, 31)));
        station.Notices.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_MissingTime_PassedOnAsNull()
    {
        var station = _subject.Map(Page(Row("Bimbach", time: "", windTime: "9:30 Uhr")), Retrieved).Should().ContainSingle().Subject;

        Reading(station, QuantityKind.Temperature).Time.Should().BeNull();
        Reading(station, QuantityKind.Temperature).Value.Should().Be(10.3);
        Reading(station, QuantityKind.RelativeHumidity).Time.Should().BeNull();
        Reading(station, QuantityKind.RelativeHumidity).Value.Should().Be(93.0);
        Reading(station, QuantityKind.MeanWindSpeed).Time.Should().BeNull();
        Reading(station, QuantityKind.MeanWindSpeed).Period.Should().BeNull();
        Reading(station, QuantityKind.MeanWindSpeed).Value.Should().Be(23.1);
        station.Notices.Should().BeEmpty();
    }

    [TestMethod]
    public void Map_OfflineStation_NoReadings()
    {
        var station = _subject.Map(Page(Row("Bimbach", status: OsthessenWetterStationStatus.Offline)), Retrieved).Should().ContainSingle().Subject;

        station.Status.Should().Be(OsthessenWetterStationStatus.Offline);
        station.Readings.Should().BeEmpty();
        station.Notices.Should().BeEmpty();
        station.Location.Should().NotBeNull();
    }

    [TestMethod]
    public void Map_MetadataMissing_Marked()
    {
        var station = _subject.Map(Page(Row("Unbekannt")), Retrieved).Should().ContainSingle().Subject;

        station.StationId.Should().Be("unbekannt");
        station.Location.Should().BeNull();
        station.Readings.Should().HaveCount(3);
    }

    [TestMethod]
    public void Map_Collision_ThrowsBeforeAnyStationMapped()
    {
        var metadata = _mocks.Create<IOsthessenWetterStationMetadataLookup>();
        var values = _mocks.Create<IOsthessenWetterValueReader>();
        var times = _mocks.Create<IOsthessenWetterTimeReader>();

        var subject = new OsthessenWetterObservationMapper(new OsthessenWetterStationIdentity(), metadata.Object, values.Object, times.Object);

        Action act = () => subject.Map(Page(Row("Bimbach"), Row("Ro\u00DFbach"), Row("Rossbach")), Retrieved);
        act.Should().Throw<OsthessenWetterException>().Which.ErrorId.Should().Be(OsthessenWetterException.StationIdCollisionErrorId);
    }

    [TestMethod]
    public void Map_NameWithoutSlug_ThrowsFormatChanged()
    {
        Action act = () => _subject.Map(Page(Row("---")), Retrieved);
        act.Should().Throw<OsthessenWetterException>().Which.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
    }

    [TestMethod]
    public void Map_RetrievalNotUtc_ThrowsArgumentException()
    {
        Action act = () => _subject.Map(Page(Row("Bimbach")), new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Unspecified));
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Map_NullPage_ThrowsArgumentNullException()
    {
        Action act = () => _subject.Map(null!, Retrieved);
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Map_EmptyPage_ReturnsEmpty()
    {
        var result = _subject.Map(Page(), Retrieved);
        result.Should().BeEmpty();
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Action act1 = () => new OsthessenWetterObservationMapper(null!, new OsthessenWetterStationMetadataLookup(new OsthessenWetterOptions()), new OsthessenWetterValueReader(), new OsthessenWetterTimeReader(new SourceTimeNormalizer(new TimeZoneResolver())));
        act1.Should().Throw<ArgumentNullException>();

        Action act2 = () => new OsthessenWetterObservationMapper(new OsthessenWetterStationIdentity(), null!, new OsthessenWetterValueReader(), new OsthessenWetterTimeReader(new SourceTimeNormalizer(new TimeZoneResolver())));
        act2.Should().Throw<ArgumentNullException>();

        Action act3 = () => new OsthessenWetterObservationMapper(new OsthessenWetterStationIdentity(), new OsthessenWetterStationMetadataLookup(new OsthessenWetterOptions()), null!, new OsthessenWetterTimeReader(new SourceTimeNormalizer(new TimeZoneResolver())));
        act3.Should().Throw<ArgumentNullException>();

        Action act4 = () => new OsthessenWetterObservationMapper(new OsthessenWetterStationIdentity(), new OsthessenWetterStationMetadataLookup(new OsthessenWetterOptions()), new OsthessenWetterValueReader(), null!);
        act4.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesObservationMapper()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IOsthessenWetterObservationMapper>().Should().BeOfType<OsthessenWetterObservationMapper>();
    }
}
