using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherParser.Cli.Diagnostics;

namespace WeatherParser.Cli.Tests.Diagnostics;

[TestClass]
[TestCategory("T-P3-08")]
public class StandardErrorLoggerProviderTests
{
    private sealed class FakeClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [TestMethod]
    public void Logger_WritesOneLinePerEntryToStandardError()
    {
        var writer = new StringWriter();
        var provider = new StandardErrorLoggerProvider(writer, new FakeClock(new DateTimeOffset(2026, 10, 29, 19, 5, 7, TimeSpan.Zero)), new LogLineFormatter(), LogLevel.Information, "ParseDWD");
        var logger = provider.CreateLogger("Test");
        logger.LogError(new EventId(0, "STATION-UNKNOWN"), "Station {Source} is unknown.", "dwd:00044");
        writer.ToString().Should().Be("2026-10-29T19:05:07 UTC ERROR STATION-UNKNOWN command=ParseDWD source=dwd:00044 Station dwd:00044 is unknown." + Environment.NewLine);
    }

    [TestMethod]
    public void Logger_DefaultLevelInformation_DebugSuppressed()
    {
        var writer = new StringWriter();
        var provider = new StandardErrorLoggerProvider(writer, new FakeClock(new DateTimeOffset(2026, 10, 29, 19, 5, 7, TimeSpan.Zero)), new LogLineFormatter(), LogLevel.Information, "ParseDWD");
        var logger = provider.CreateLogger("Test");
        logger.LogDebug("hidden");
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        writer.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public void Logger_VerboseLowersToDebug()
    {
        var writer = new StringWriter();
        var provider = new StandardErrorLoggerProvider(writer, new FakeClock(new DateTimeOffset(2026, 10, 29, 19, 5, 7, TimeSpan.Zero)), new LogLineFormatter(), LogLevel.Debug, "ParseDWD");
        var logger = provider.CreateLogger("Test");
        logger.LogDebug("shown");
        writer.ToString().Should().Contain(" DEBUG - command=ParseDWD shown");
    }

    [TestMethod]
    public void Logger_NoneLevelNeverWrites()
    {
        var writer = new StringWriter();
        var provider = new StandardErrorLoggerProvider(writer, new FakeClock(new DateTimeOffset(2026, 10, 29, 19, 5, 7, TimeSpan.Zero)), new LogLineFormatter(), LogLevel.None, "ParseDWD");
        var logger = provider.CreateLogger("Test");
        logger.Log(LogLevel.None, "x");
        writer.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public void LibraryLogger_ResolvesForLibraryServices()
    {
        var writer = new StringWriter();
        var provider = new StandardErrorLoggerProvider(writer, new FakeClock(new DateTimeOffset(2026, 10, 29, 19, 5, 7, TimeSpan.Zero)), new LogLineFormatter(), LogLevel.Debug, "ParseDWD");
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.ClearProviders().AddProvider(provider).SetMinimumLevel(LogLevel.Debug));
        var logger = services.BuildServiceProvider().GetRequiredService<ILogger<StandardErrorLoggerProviderTests>>();
        logger.LogInformation(new EventId(0, "SELECTION-EMPTY"), "No station.");
        writer.ToString().Should().Contain(" INFORMATION SELECTION-EMPTY command=ParseDWD No station.");
    }
}
