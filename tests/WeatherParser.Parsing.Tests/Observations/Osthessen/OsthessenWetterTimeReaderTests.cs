using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class OsthessenWetterTimeReaderTests
{
    private MockRepository _mocks = null!;
    private OsthessenWetterTimeReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _subject = new OsthessenWetterTimeReader(new SourceTimeNormalizer(new TimeZoneResolver()));
    }

    [TestCleanup]
    public void TestCleanup() => _mocks.VerifyAll();

    private static DateTime Utc(int year, int month, int day, int hour, int minute, int second = 0) => new(year, month, day, hour, minute, second, DateTimeKind.Utc);

    private void Resolved(string text, DateTime retrievedAtUtc, DateTime expectedUtc) =>
        _subject.Read(text, retrievedAtUtc).Should().Be(new OsthessenWetterTimeValue(OsthessenWetterTimeStatus.Resolved, new NormalizedSourceTime(expectedUtc, TimestampQuality.EstimatedDate, "Europe/Berlin")));

    [TestMethod]
    public void Read_HhMmUhr_ReturnsEstimatedDate()
    {
        Resolved("09:31 Uhr", Utc(2026, 1, 15, 9, 0), Utc(2026, 1, 15, 8, 31));
        Resolved("09:31 Uhr", Utc(2026, 7, 15, 8, 0), Utc(2026, 7, 15, 7, 31));
    }

    [TestMethod]
    public void Read_Missing_ReturnsMissing()
    {
        _subject.Read(null, Utc(2026, 1, 15, 9, 0))
            .Should().Be(new OsthessenWetterTimeValue(OsthessenWetterTimeStatus.Missing, null));
        _subject.Read("", Utc(2026, 1, 15, 9, 0))
            .Should().Be(new OsthessenWetterTimeValue(OsthessenWetterTimeStatus.Missing, null));
        _subject.Read("   ", Utc(2026, 1, 15, 9, 0))
            .Should().Be(new OsthessenWetterTimeValue(OsthessenWetterTimeStatus.Missing, null));
    }

    [TestMethod]
    public void Read_InvalidText_ReturnsMissing()
    {
        var invalidInputs = new[]
        {
            "25:00 Uhr",
            "24:00 Uhr",
            "23:60 Uhr",
            "9:05 Uhr",
            "9:5 Uhr",
            "09:31",
            "09:31 uhr",
            "09:31  Uhr",
            " 09:31 Uhr",
            "09.31 Uhr",
            "09:31 Uhr ",
            "\u0660\u0669:31 Uhr"
        };

        foreach (var input in invalidInputs)
        {
            _subject.Read(input, Utc(2026, 1, 15, 9, 0))
                .Should().Be(new OsthessenWetterTimeValue(OsthessenWetterTimeStatus.Missing, null));
        }
    }

    [TestMethod]
    public void Read_23_58_RetrievedAt_00_02_PreviousDay()
    {
        Resolved("23:58 Uhr", Utc(2026, 1, 14, 23, 2), Utc(2026, 1, 14, 22, 58));
    }

    [TestMethod]
    public void Read_00_00_RetrievedAt_00_01_SameDay()
    {
        Resolved("00:00 Uhr", Utc(2026, 1, 14, 23, 1), Utc(2026, 1, 14, 23, 0));
    }

    [TestMethod]
    public void Read_FiveMinutesLater_SameDay()
    {
        Resolved("10:05 Uhr", Utc(2026, 1, 15, 9, 0), Utc(2026, 1, 15, 9, 5));
    }

    [TestMethod]
    public void Read_FiveMinutesOneSecondLater_PreviousDay()
    {
        Resolved("10:05 Uhr", Utc(2026, 1, 15, 8, 59, 59), Utc(2026, 1, 14, 9, 5));
        Resolved("10:06 Uhr", Utc(2026, 1, 15, 9, 0), Utc(2026, 1, 14, 9, 6));
    }

    [TestMethod]
    public void Read_SpringGap_Unresolved()
    {
        _subject.Read("02:30 Uhr", Utc(2026, 3, 29, 1, 10))
            .Should().Be(new OsthessenWetterTimeValue(OsthessenWetterTimeStatus.Unresolved, null));
    }

    [TestMethod]
    public void Read_AutumnOverlap_Unresolved()
    {
        _subject.Read("02:30 Uhr", Utc(2026, 10, 25, 3, 0))
            .Should().Be(new OsthessenWetterTimeValue(OsthessenWetterTimeStatus.Unresolved, null));
    }

    [TestMethod]
    public void Read_RetrievalBothSidesOfTransition()
    {
        Resolved("01:55 Uhr", Utc(2026, 3, 29, 0, 58), Utc(2026, 3, 29, 0, 55));
        Resolved("03:00 Uhr", Utc(2026, 3, 29, 1, 2), Utc(2026, 3, 29, 1, 0));
    }

    [TestMethod]
    public void Read_RetrievalSubSecond_Truncated()
    {
        Resolved("10:00 Uhr", Utc(2026, 1, 15, 9, 0).AddTicks(7_000_000), Utc(2026, 1, 15, 9, 0));
    }

    [TestMethod]
    public void Read_OldDisplay_KeptAsPreviousDay()
    {
        Resolved("23:00 Uhr", Utc(2026, 1, 15, 9, 0), Utc(2026, 1, 14, 22, 0));
    }

    [TestMethod]
    public void Read_RetrievalNotUtc_ThrowsArgumentException()
    {
        Action local = () => _subject.Read("09:31 Uhr", new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Local));
        local.Should().Throw<ArgumentException>();

        Action unspecified = () => _subject.Read("09:31 Uhr", new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Unspecified));
        unspecified.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Read_TimeZoneUnavailable_Propagates()
    {
        var resolver = _mocks.Create<ITimeZoneResolver>();
        resolver.Setup(r => r.Resolve("Europe/Berlin")).Throws(new TimeZoneNotFoundException("missing"));
        var subject = new OsthessenWetterTimeReader(new SourceTimeNormalizer(resolver.Object));
        Action act = () => subject.Read("09:31 Uhr", Utc(2026, 1, 15, 9, 0));
        act.Should().Throw<SourceTimeNormalizationException>();
    }

    [TestMethod]
    public void Read_TimeZoneInvalid_Propagates()
    {
        var resolver = _mocks.Create<ITimeZoneResolver>();
        resolver.Setup(r => r.Resolve("Europe/Berlin")).Throws(new InvalidTimeZoneException("corrupt"));
        var subject = new OsthessenWetterTimeReader(new SourceTimeNormalizer(resolver.Object));
        Action act = () => subject.Read("09:31 Uhr", Utc(2026, 1, 15, 9, 0));
        act.Should().Throw<SourceTimeNormalizationException>();
    }

    [TestMethod]
    public void Constructor_NullNormalizer_ThrowsArgumentNullException()
    {
        Action act = () => new OsthessenWetterTimeReader(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesTimeReader()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IOsthessenWetterTimeReader>().Should().BeOfType<OsthessenWetterTimeReader>();
    }
}
