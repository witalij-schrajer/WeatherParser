using System;
using FluentAssertions;
using Moq;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations.Dwd;

[TestClass]
public sealed class DwdObservationMapperTimeTests
{
    private MockRepository _mocks = null!;
    private Mock<ISourceTimeNormalizer> _normalizer = null!;
    private DwdObservationMapper _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _normalizer = _mocks.Create<ISourceTimeNormalizer>();
        _subject = new DwdObservationMapper(new QuantityValidator(), _normalizer.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private static readonly DateTime T0 = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    private static CdcNowEvidenceRow Row(DateTime time, string qn, params (string Column, string Token)[] values) => new(time, values.ToDictionary(v => v.Column, v => v.Token, StringComparer.Ordinal)) { QualityLevel = qn };

    private static CdcNowEvidence Evidence(string productCode, params CdcNowEvidenceRow[] rows) => new("00044", productCode, rows);

    private void ExpectNormalize(DateTime utc) => _normalizer.Setup(n => n.NormalizeComplete(new DateTimeOffset(utc))).Returns(new NormalizedSourceTime(utc, TimestampQuality.Source, null));

    [TestMethod]
    public void Map_MeasuredAt_IsUtcSourceQuality()
    {
        ExpectNormalize(T0);
        var evidence = Evidence("rr", Row(T0, "3", ("RWS_10", "0.4")));
        var result = _subject.Map(new[] { evidence });
        var reading = result.Readings.Should().ContainSingle().Subject;
        reading.Time!.UtcInstant.Should().Be(T0);
        reading.Time.Quality.Should().Be(TimestampQuality.Source);
        reading.Time.SourceTimeZone.Should().BeNull();
    }

    [TestMethod]
    public void Map_DifferentColumnsDifferentTimes_Preserved()
    {
        ExpectNormalize(T0);
        ExpectNormalize(T0.AddMinutes(-10));
        var evidence = Evidence("tu", 
            Row(T0.AddMinutes(-10), "3", ("PP_10", "1000.0"), ("TT_10", "11.0"), ("RF_10", "80.0")),
            Row(T0, "3", ("PP_10", "1001.0"), ("TT_10", "12.0"), ("RF_10", "-999")));
        var result = _subject.Map(new[] { evidence });
        var temperature = result.Readings.Single(r => r.Kind == QuantityKind.Temperature);
        var pressure = result.Readings.Single(r => r.Kind == QuantityKind.Pressure);
        var humidity = result.Readings.Single(r => r.Kind == QuantityKind.RelativeHumidity);
        temperature.Time!.UtcInstant.Should().Be(T0);
        pressure.Time!.UtcInstant.Should().Be(T0);
        humidity.Time!.UtcInstant.Should().Be(T0.AddMinutes(-10));
    }

    [TestMethod]
    public void Map_NeverUsesRetrievalTime()
    {
        typeof(DwdObservationMapper).GetConstructors().Should().ContainSingle().Which.GetParameters().Select(p => p.ParameterType).Should().NotContain(typeof(TimeProvider));
    }

    [TestMethod]
    public void Map_NormalizerFailure_Propagates()
    {
        _normalizer.Setup(n => n.NormalizeComplete(It.IsAny<DateTimeOffset?>())).Throws(new SourceTimeNormalizationException("Bad time."));
        var evidence = Evidence("rr", Row(T0, "3", ("RWS_10", "0.4")));
        FluentActions.Invoking(() => _subject.Map(new[] { evidence })).Should().Throw<SourceTimeNormalizationException>();
    }
}
