using FluentAssertions;
using Microsoft.Extensions.Logging;
using WeatherParser.Cli.Diagnostics;

namespace WeatherParser.Cli.Tests.Diagnostics;

[TestClass]
[TestCategory("T-P3-08")]
public class StandardErrorLoggerSourceTests
{
    private sealed class FakeClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [TestMethod]
    public void Logger_ProviderOnlySource_StationNameWins()
    {
        var writer = new StringWriter();
        var provider = new StandardErrorLoggerProvider(writer, new FakeClock(new DateTimeOffset(2026, 10, 29, 19, 5, 7, TimeSpan.Zero)), new LogLineFormatter(), LogLevel.Information, "ParseOstHessen");
        var logger = provider.CreateLogger("Test");

        logger.LogWarning("{Source} station {StationName} offline.", "osthessenwetter", "Fulda");
        writer.ToString().Should().Contain(" source=Fulda ");
    }

    [TestMethod]
    public void Logger_CompleteSource_Wins()
    {
        var writer = new StringWriter();
        var provider = new StandardErrorLoggerProvider(writer, new FakeClock(new DateTimeOffset(2026, 10, 29, 19, 5, 7, TimeSpan.Zero)), new LogLineFormatter(), LogLevel.Information, "ParseOstHessen");
        var logger = provider.CreateLogger("Test");

        logger.LogWarning("{Source} {StationName}", "osthessenwetter:fulda", "Fulda");
        writer.ToString().Should().Contain(" source=osthessenwetter:fulda ");
    }

    [TestMethod]
    public void Logger_ProviderOnlySourceWithoutStation_ProviderUsed()
    {
        var writer = new StringWriter();
        var provider = new StandardErrorLoggerProvider(writer, new FakeClock(new DateTimeOffset(2026, 10, 29, 19, 5, 7, TimeSpan.Zero)), new LogLineFormatter(), LogLevel.Information, "ParseOstHessen");
        var logger = provider.CreateLogger("Test");

        logger.LogWarning("{Source} page unavailable.", "osthessenwetter");
        writer.ToString().Should().Contain(" source=osthessenwetter ");
    }

    [TestMethod]
    public void Logger_NewlinesInStructuredFields_SingleLine()
    {
        var writer = new StringWriter();
        var provider = new StandardErrorLoggerProvider(writer, new FakeClock(new DateTimeOffset(2026, 10, 29, 19, 5, 7, TimeSpan.Zero)), new LogLineFormatter(), LogLevel.Information, "ParseOstHessen");
        var logger = provider.CreateLogger("Test");

        logger.LogWarning(new EventId(0, "ID\r\nFORGED"), "{StationName} x", "a\nforged");
        var output = writer.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        output.Should().NotContain("\r");
        output.Should().NotContain("\n");
    }
}
