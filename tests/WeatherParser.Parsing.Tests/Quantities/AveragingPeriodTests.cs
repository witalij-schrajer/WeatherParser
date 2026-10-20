using FluentAssertions;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Quantities;

[TestClass]
public sealed class AveragingPeriodTests
{
    private static DateTime Utc(int hour, int minute, int second) => new DateTime(2026, 7, 15, hour, minute, second, DateTimeKind.Utc);

    [TestMethod]
    public void AveragingPeriod_KeepsEndpointsAndDuration()
    {
        var start = Utc(10, 0, 0);
        var end = Utc(10, 10, 0);
        var period = new AveragingPeriod(start, end);

        period.StartUtc.Should().Be(start);
        period.EndUtc.Should().Be(end);
        period.Duration.Should().Be(TimeSpan.FromMinutes(10));
    }

    [TestMethod]
    public void AveragingPeriod_OneWholeSecond_IsAccepted()
    {
        var start = Utc(10, 0, 0);
        var end = Utc(10, 0, 1);
        var period = new AveragingPeriod(start, end);

        period.Duration.Should().Be(TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void AveragingPeriod_EndNotAfterStart_IsRejected()
    {
        FluentActions.Invoking(() => new AveragingPeriod(Utc(10, 0, 0), Utc(10, 0, 0)))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new AveragingPeriod(Utc(10, 0, 1), Utc(10, 0, 0)))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow(DateTimeKind.Local)]
    [DataRow(DateTimeKind.Unspecified)]
    public void AveragingPeriod_NonUtcEndpoint_IsRejected(DateTimeKind kind)
    {
        var nonUtcStart = DateTime.SpecifyKind(Utc(10, 0, 0), kind);
        var nonUtcEnd = DateTime.SpecifyKind(Utc(10, 10, 0), kind);

        FluentActions.Invoking(() => new AveragingPeriod(nonUtcStart, Utc(10, 10, 0)))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new AveragingPeriod(Utc(10, 0, 0), nonUtcEnd))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void AveragingPeriod_FractionalSecondEndpoint_IsRejected()
    {
        var start = Utc(10, 0, 0).AddTicks(1);
        var end = Utc(10, 10, 0).AddMilliseconds(1);

        FluentActions.Invoking(() => new AveragingPeriod(start, Utc(10, 10, 0)))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new AveragingPeriod(Utc(10, 0, 0), end))
            .Should().Throw<ArgumentException>();
    }
}
