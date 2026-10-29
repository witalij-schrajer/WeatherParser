using FluentAssertions;
using WeatherParser.Cli.Arguments;

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

    /// <summary>
    /// Verifies that general help lists options, units and examples.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-04")]
    public async Task Help_General_ListsOptionsUnitsAndExamples()
    {
        var writer = new StringWriter();
        _subject.WriteHelp(writer);
        var text = writer.ToString();

        text.Should().Contain("--Latitude");
        text.Should().Contain("--Longitude");
        text.Should().Contain("--Altitude");
        text.Should().Contain("--MaxDistance");
        text.Should().Contain("--MaxAltDifference");
        text.Should().Contain("--StationID");
        text.Should().Contain("--StationNamePattern");
        text.Should().Contain("--Product");
        text.Should().Contain("--MOSMIXID");
        text.Should().Contain("--RefreshMosmixSEvidence");
        text.Should().Contain("--Output");
        text.Should().Contain("--OutputFile");
        text.Should().Contain("--Config");
        text.Should().Contain("--Verbose");
        text.Should().Contain("--Help");

        text.Should().Contain("decimal degrees or degrees, minutes and seconds with hemisphere");
        text.Should().Contain("metres");
        text.Should().Contain("kilometres");
        text.Should().Contain("mqtt|json|xml");

        text.Should().Contain("WeatherParser --SelectDWD --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200");
        text.Should().Contain("WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID>");
    }

    /// <summary>
    /// Verifies that command help prints usage and example.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-04")]
    [DataRow(CliCommand.SelectDwd, "--SelectDWD", "--MaxAltDifference")]
    [DataRow(CliCommand.ParseDwd, "--ParseDWD", "--StationID")]
    [DataRow(CliCommand.ParseOstHessen, "--ParseOstHessen", "--StationNamePattern")]
    [DataRow(CliCommand.SelectMosmix, "--SelectMOSMIX", "--RefreshMosmixSEvidence")]
    [DataRow(CliCommand.ParseMosmix, "--ParseMOSMIX", "--MOSMIXID")]
    public async Task Help_WithCommand_PrintsCommandHelp(CliCommand command, string commandOption, string option)
    {
        var writer = new StringWriter();
        _subject.WriteCommandHelp(writer, command);
        var text = writer.ToString();

        text.Should().Contain("Usage: WeatherParser " + commandOption);
        text.Should().Contain(option);
        text.Should().Contain("--Output");
        text.Should().Contain("Example: WeatherParser " + commandOption);
    }

    /// <summary>
    /// Verifies that ParseDWD command help does not list search options.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-04")]
    public async Task Help_ForParseDwd_DoesNotListSearchOptions()
    {
        var writer = new StringWriter();
        _subject.WriteCommandHelp(writer, CliCommand.ParseDwd);
        var text = writer.ToString();

        text.Should().NotContain("--Latitude");
    }
}
