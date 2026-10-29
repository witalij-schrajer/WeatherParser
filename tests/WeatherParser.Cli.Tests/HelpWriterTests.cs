using FluentAssertions;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Tests the <see cref="HelpWriter"/> class.
/// </summary>
[TestClass]
public sealed class HelpWriterTests
{
    private HelpWriter _subject = null!;

    /// <summary>
    /// Initializes the test fixtures.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _subject = new HelpWriter();
    }

    /// <summary>
    /// Verifies that help contains usage line.
    /// </summary>
    [TestMethod]
    public async Task WriteHelp_ContainsUsageLine()
    {
        var writer = new StringWriter();
        _subject.WriteHelp(writer);
        writer.ToString().Should().Contain("Usage: WeatherParser --<command> [options]");
    }

    /// <summary>
    /// Verifies that help lists five commands.
    /// </summary>
    [TestMethod]
    public async Task WriteHelp_ListsFiveCommands()
    {
        var writer = new StringWriter();
        _subject.WriteHelp(writer);
        var text = writer.ToString();
        text.Should().Contain("--SelectDWD");
        text.Should().Contain("--ParseDWD");
        text.Should().Contain("--ParseOstHessen");
        text.Should().Contain("--SelectMOSMIX");
        text.Should().Contain("--ParseMOSMIX");
    }
}
