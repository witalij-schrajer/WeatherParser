using WeatherParser.Parsing.Time;
using FluentAssertions;

namespace WeatherParser.Parsing.Tests.Time;

[TestClass]
public sealed class TimeZoneResolverTests
{
    private ITimeZoneResolver _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new TimeZoneResolver();
    }

    [TestMethod]
    public void Resolve_EuropeBerlin_ReturnsZoneWithCentralEuropeanOffsets()
    {
        // Act
        var result = _subject.Resolve("Europe/Berlin");

        // Assert
        result.Id.Should().Be("Europe/Berlin");
        result.GetUtcOffset(new DateTime(2026, 1, 15, 12, 0, 0)).Should().Be(TimeSpan.FromHours(1));
        result.GetUtcOffset(new DateTime(2026, 7, 15, 12, 0, 0)).Should().Be(TimeSpan.FromHours(2));
    }

    [TestMethod]
    public void Resolve_EmptyZoneId_ThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => _subject.Resolve(""))
            .Should().Throw<ArgumentException>();

        FluentActions.Invoking(() => _subject.Resolve("  "))
            .Should().Throw<ArgumentException>();
    }
}
