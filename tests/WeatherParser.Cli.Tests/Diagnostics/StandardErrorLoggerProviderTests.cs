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

    private StringWriter _writer = null!;
    private StandardErrorLoggerProvider? _provider;

    [TestInitialize]
    public void Initialize()
    {
        _writer = new StringWriter();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _provider?.Dispose();
        _writer.Dispose();
    }

    private StandardErrorLoggerProvider CreateProvider(LogLevel level)
    {
        return _provider = new StandardErrorLoggerProvider(_writer, new FakeClock(new DateTimeOffset(2026, 10, 29, 19, 5, 7, TimeSpan.Zero)), new LogLineFormatter(), level, "ParseDWD");
    }

    [TestMethod]
    public void Logger_WritesOneLinePerEntryToStandardError()
    {
        var provider = CreateProvider(LogLevel.Information);
        var logger = provider.CreateLogger("Test");
        logger.LogError(new EventId(0, "STATION-UNKNOWN"), "Station {Source} is unknown.", "dwd:00044");
        _writer.ToString().Should().Be("2026-10-29T19:05:07 UTC ERROR STATION-UNKNOWN command=ParseDWD source=dwd:00044 Station dwd:00044 is unknown." + Environment.NewLine);
    }

    [TestMethod]
    public void Logger_DefaultLevelInformation_DebugSuppressed()
    {
        var provider = CreateProvider(LogLevel.Information);
        var logger = provider.CreateLogger("Test");
        logger.LogDebug("hidden");
        logger.IsEnabled(LogLevel.Debug).Should().BeFalse();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
        _writer.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public void Logger_VerboseLowersToDebug()
    {
        var provider = CreateProvider(LogLevel.Debug);
        var logger = provider.CreateLogger("Test");
        logger.LogDebug("shown");
        _writer.ToString().Should().Contain(" DEBUG - command=ParseDWD shown");
    }

    [TestMethod]
    public void Logger_NoneLevelNeverWrites()
    {
        var provider = CreateProvider(LogLevel.None);
        var logger = provider.CreateLogger("Test");
        logger.Log(LogLevel.None, "x");
        _writer.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public void LibraryLogger_ResolvesForLibraryServices()
    {
        var provider = CreateProvider(LogLevel.Debug);
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.ClearProviders().AddProvider(provider).SetMinimumLevel(LogLevel.Debug));
        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<StandardErrorLoggerProviderTests>>();
        logger.LogInformation(new EventId(0, "SELECTION-EMPTY"), "No station.");
        _writer.ToString().Should().Contain(" INFORMATION SELECTION-EMPTY command=ParseDWD No station.");
    }

    [TestMethod]
    [TestCategory("T-P3-08")]
    public void Logger_BeginScope_ReturnsNoScope()
    {
        var provider = CreateProvider(LogLevel.Information);
        var logger = provider.CreateLogger("Test");
        var scope = logger.BeginScope("scope");
        scope.Should().BeNull();
    }

    [TestMethod]
    [TestCategory("T-P3-08")]
    public void Logger_StateWithoutKeyValuePairs_NoSourceField()
    {
        var provider = CreateProvider(LogLevel.Information);
        var logger = provider.CreateLogger("Test");
        logger.Log(LogLevel.Information, new EventId(0, "X-ID"), "plain state", null, (state, _) => state);
        _writer.ToString().Should().EndWith($" INFORMATION X-ID command=ParseDWD plain state{Environment.NewLine}");
        _writer.ToString().Should().NotContain("source=");
    }

    [TestMethod]
    [TestCategory("T-P3-08")]
    public void Logger_LibrarySourceKeys_WrittenAsSource()
    {
        var provider = CreateProvider(LogLevel.Warning);
        var logger = provider.CreateLogger("Test");

        // Test StationId
        logger.LogWarning("Station {StationId} unknown.", "00044");
        _writer.ToString().Should().Contain(" command=ParseDWD source=00044 ");

        _writer.GetStringBuilder().Clear();

        // Test MosmixId
        logger.LogWarning("Location {MosmixId} unknown.", "10637");
        _writer.ToString().Should().Contain(" command=ParseDWD source=10637 ");

        _writer.GetStringBuilder().Clear();

        // Test StationName
        logger.LogWarning("Station {StationName} offline.", "Fulda");
        _writer.ToString().Should().Contain(" command=ParseDWD source=Fulda ");
    }

    [TestMethod]
    [TestCategory("T-P3-08")]
    public void Logger_SourceKeyPreferredOverStationId()
    {
        var provider = CreateProvider(LogLevel.Warning);
        var logger = provider.CreateLogger("Test");

        logger.LogWarning("{Source} {StationId}", "dwd:00044", "00044");
        _writer.ToString().Should().Contain(" command=ParseDWD source=dwd:00044 ");
    }
}
