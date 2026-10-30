using FluentAssertions;
using WeatherParser.Cli.Arguments;

namespace WeatherParser.Cli.Tests.Arguments;

/// <summary>
/// Tests the command context of the command line parser.
/// </summary>
[TestClass]
[TestCategory("T-P3-08")]
public sealed class CommandLineParserCommandContextTests
{
    private ICommandLineParser _parser = null!;

    /// <summary>
    /// Initializes the test fixtures.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _parser = new CommandLineParser();
    }

    [DataRow("--ParseMOSMIX --MOSMIXID 10637", CliCommand.ParseMosmix)]
    [DataRow("--ParseDWD --Latitude 50.5", CliCommand.ParseDwd)]
    [DataRow("--ParseDWD --Output csv", CliCommand.ParseDwd)]
    [DataRow("--ParseMOSMIX --Product X --MOSMIXID 1", CliCommand.ParseMosmix)]
    [DataRow("--ParseDWD --StationID", CliCommand.ParseDwd)]
    [DataRow("--ParseDWD --Bogus x", CliCommand.ParseDwd)]
    [DataRow("--ParseDWD --StationID 1 --stationid 2", CliCommand.ParseDwd)]
    [DataRow("--ParseDWD foo", CliCommand.ParseDwd)]
    [DataRow("--ParseDWD --Help --Verbose", CliCommand.ParseDwd)]
    [TestMethod]
    public void Parse_ErrorAfterCommandKnown_CarriesCommand(string arguments, CliCommand expectedCommand)
    {
        var result = _parser.Parse(arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        result.Should().BeOfType<UsageError>()
            .Which.Command.Should().Be(expectedCommand);
    }

    [DataRow("--Bogus")]
    [DataRow("--ParseDWD --ParseOstHessen")]
    [DataRow("--Bogus --ParseDWD")]
    [DataRow("--ParseDWD --ParseOstHessen --Help")]
    [TestMethod]
    public void Parse_ErrorBeforeCommandKnown_NoCommand(string arguments)
    {
        var result = _parser.Parse(arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        result.Should().BeOfType<UsageError>()
            .Which.Command.Should().BeNull();
    }
}
