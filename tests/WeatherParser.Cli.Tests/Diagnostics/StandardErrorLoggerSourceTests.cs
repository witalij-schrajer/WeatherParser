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

    private StringWriter _writer = null!;
    private StandardErrorLoggerProvider _provider = null!;
    private ILogger _logger = null!;

    [TestInitialize]
    public void Initialize()
    {
        _writer = new StringWriter();
        _provider = new StandardErrorLoggerProvider(_writer, new FakeClock(new DateTimeOffset(2026, 10, 29, 19, 5, 7, TimeSpan.Zero)), new LogLineFormatter(), LogLevel.Information, "ParseOstHessen");
        _logger = _provider.CreateLogger("Test");
    }

    [TestCleanup]
    public void Cleanup()
    {
        _provider.Dispose();
        _writer.Dispose();
    }

    [TestMethod]
    public void Logger_ProviderOnlySource_StationNameWins()
    {
        _logger.LogWarning("{Source} station {StationName} offline.", "osthessenwetter", "Fulda");
        _writer.ToString().Should().Contain(" source=Fulda ");
    }

    [TestMethod]
    public void Logger_CompleteSource_Wins()
    {
        _logger.LogWarning("{Source} {StationName}", "osthessenwetter:fulda", "Fulda");
        _writer.ToString().Should().Contain(" source=osthessenwetter:fulda ");
    }

    [TestMethod]
    public void Logger_ProviderOnlySourceWithoutStation_ProviderUsed()
    {
        _logger.LogWarning("{Source} page unavailable.", "osthessenwetter");
        _writer.ToString().Should().Contain(" source=osthessenwetter ");
    }

    [TestMethod]
    public void Logger_NewlinesInStructuredFields_SingleLine()
    {
        _logger.LogWarning(new EventId(0, "ID\r\nFORGED"), "{StationName} x", "a\nforged");
        var output = _writer.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        output.Should().NotContain("\r");
        output.Should().NotContain("\n");
    }
}
