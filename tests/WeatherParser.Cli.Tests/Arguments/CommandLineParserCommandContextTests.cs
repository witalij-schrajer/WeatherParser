using FluentAssertions;
using WeatherParser.Cli.Arguments;

namespace WeatherParser.Cli.Tests.Arguments;

[TestClass]
[TestCategory("T-P3-08")]
public class CommandLineParserCommandContextTests
{
    [DataRow("--ParseMOSMIX --MOSMIXID 10637", CliCommand.ParseMosmix)]
    [DataRow("--ParseDWD --Latitude 50.5", CliCommand.ParseDwd)]
    [DataRow("--ParseDWD --Output csv", CliCommand.ParseDwd)]
    [DataRow("--ParseMOSMIX --Product X --MOSMIXID 1", CliCommand.ParseMosmix)]
    [TestMethod]
    public void Parse_ErrorAfterCommandKnown_CarriesCommand(string arguments, CliCommand expectedCommand)
    {
        var parser = new CommandLineParser();
        var result = parser.Parse(arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        result.Should().BeOfType<UsageError>()
            .Which.Command.Should().Be(expectedCommand);
    }

    [DataRow("--Bogus")]
    [DataRow("--ParseDWD --ParseOstHessen")]
    [TestMethod]
    public void Parse_ErrorBeforeCommandKnown_NoCommand(string arguments)
    {
        var parser = new CommandLineParser();
        var result = parser.Parse(arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        result.Should().BeOfType<UsageError>()
            .Which.Command.Should().BeNull();
    }
}
