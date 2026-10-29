using FluentAssertions;
using WeatherParser.Cli.Arguments;

namespace WeatherParser.Cli.Tests.Arguments;

/// <summary>
/// Verifies the command-line grammar of the example CLI: commands, options, case-insensitivity, duplicates, allowed and required options, output and product values, help (CL-02, CL-03, T-P3-02).
/// </summary>
[TestClass]
[TestCategory("T-P3-02")]
public sealed class CommandLineParserTests
{
    private ICommandLineParser _parser = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _parser = new CommandLineParser();
    }

    private UsageError ParseError(params string[] args)
    {
        var result = _parser.Parse(args);
        result.Should().BeOfType<UsageError>();
        return (UsageError)result;
    }

    private ParsedCommandLine ParseOk(params string[] args)
    {
        var result = _parser.Parse(args);
        result.Should().BeOfType<ParsedCommandLine>();
        return (ParsedCommandLine)result;
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when no command is provided.
    /// </summary>
    public void Parse_NoArguments_UsageErrorCommandMissing()
    {
        var error = ParseError();
        error.ErrorId.Should().Be("CLI-COMMAND-MISSING");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when two commands are provided.
    /// </summary>
    public void Parse_TwoCommands_UsageErrorCommandConflict()
    {
        var error = ParseError("--ParseDWD", "--ParseOstHessen", "--StationNamePattern", "x");
        error.ErrorId.Should().Be("CLI-COMMAND-CONFLICT");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when an unknown command is provided.
    /// </summary>
    public void Parse_UnknownCommand_UsageError()
    {
        var error = ParseError("--ParseFoo");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Unknown option '--ParseFoo'.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when an unknown option is provided.
    /// </summary>
    public void Parse_UnknownOption_UsageError()
    {
        var error = ParseError("--ParseDWD", "--Colour", "red");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Unknown option '--Colour'.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when an option is specified multiple times.
    /// </summary>
    public void Parse_DuplicateOption_UsageError()
    {
        var error = ParseError("--ParseDWD", "--StationID", "00044", "--StationID", "00045");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Option '--StationID' is specified more than once.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when an option is specified with different cases.
    /// </summary>
    public void Parse_DuplicateOptionDifferentCase_UsageError()
    {
        var error = ParseError("--ParseDWD", "--StationID", "00044", "--stationid", "00045");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Option '--StationID' is specified more than once.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that option names are case-insensitive.
    /// </summary>
    public void Parse_OptionNamesAnyCase_Accepted()
    {
        var parsed = ParseOk("--selectdwd", "--LATITUDE", "50.5", "--longitude", "9.7", "--Altitude", "300", "--maxdistance", "50", "--MaxAltDifference", "200");
        parsed.Command.Should().Be(CliCommand.SelectDwd);
        parsed.Values["Latitude"].Should().Be("50.5");
        parsed.Values["MaxDistance"].Should().Be("50");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that commands can be placed at any position in the argument list.
    /// </summary>
    public void Parse_CommandAtAnyPosition_Accepted()
    {
        var parsed = ParseOk("--StationNamePattern", "Fulda*", "--ParseOstHessen");
        parsed.Command.Should().Be(CliCommand.ParseOstHessen);
        parsed.Values["StationNamePattern"].Should().Be("Fulda*");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when an option is missing a value.
    /// </summary>
    public void Parse_MissingValue_UsageError()
    {
        var error = ParseError("--ParseDWD", "--StationID");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Option '--StationID' requires a value.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when an option's value starts with "--".
    /// </summary>
    public void Parse_ValueStartingWithDoubleDash_UsageError()
    {
        var error = ParseError("--ParseDWD", "--StationID", "--Verbose");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Option '--StationID' requires a value.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that typos in option names are treated as unknown options.
    /// </summary>
    public void Parse_LangitudeTypo_UnknownOption()
    {
        var error = ParseError("--SelectDWD", "--Langitude", "9.7");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Unknown option '--Langitude'.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that the equals delimiter is not supported for options.
    /// </summary>
    public void Parse_EqualsDelimiter_UnknownOption()
    {
        var error = ParseError("--ParseDWD", "--Output=json");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Unknown option '--Output=json'.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when an unexpected argument is provided.
    /// </summary>
    public void Parse_UnexpectedArgument_UsageError()
    {
        var error = ParseError("--ParseDWD", "foo");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Unexpected argument 'foo'.");
    }

    /// <summary>
    /// Tests that options are rejected when used with incompatible commands.
    /// </summary>
    [DataRow("--SelectDWD --Latitude 50.5 --Longitude 9.7 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200 --StationID 00044", "--StationID", "--SelectDWD")]
    [DataRow("--ParseDWD --Latitude 50.5", "--Latitude", "--ParseDWD")]
    [DataRow("--ParseOstHessen --StationNamePattern x --Product MOSMIX_S", "--Product", "--ParseOstHessen")]
    [DataRow("--ParseMOSMIX --Product MOSMIX_S --MOSMIXID 10637 --StationNamePattern x", "--StationNamePattern", "--ParseMOSMIX")]
    [DataRow("--SelectDWD --Latitude 50.5 --Longitude 9.7 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200 --RefreshMosmixSEvidence", "--RefreshMosmixSEvidence", "--SelectDWD")]
    [TestMethod]
    [TestCategory("T-P3-02")]
    public void Parse_OptionNotAllowedForCommand_UsageError(string args, string option, string command)
    {
        var error = ParseError(args.Split(' '));
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be($"Option '{option}' is not allowed with '{command}'.");
    }

    /// <summary>
    /// Tests that required options are rejected when missing for a command.
    /// </summary>
    [DataRow("--SelectDWD --Latitude 50.5 --Longitude 9.7 --MaxDistance 50 --MaxAltDifference 200", "--Altitude", "--SelectDWD")]
    [DataRow("--SelectMOSMIX --Latitude 50.5 --Longitude 9.7 --Altitude 300 --MaxDistance 50", "--MaxAltDifference", "--SelectMOSMIX")]
    [DataRow("--ParseOstHessen", "--StationNamePattern", "--ParseOstHessen")]
    [DataRow("--ParseMOSMIX --MOSMIXID 10637", "--Product", "--ParseMOSMIX")]
    [DataRow("--ParseMOSMIX --Product MOSMIX_L", "--MOSMIXID", "--ParseMOSMIX")]
    [TestMethod]
    [TestCategory("T-P3-02")]
    public void Parse_MissingRequiredOption_UsageError(string args, string option, string command)
    {
        var error = ParseError(args.Split(' '));
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be($"Option '{option}' is required with '{command}'.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when --OutputFile is used with --Output mqtt.
    /// </summary>
    public void Parse_OutputFileWithMqtt_UsageError()
    {
        var error = ParseError("--ParseDWD", "--Output", "mqtt", "--OutputFile", "out.json");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Option '--OutputFile' requires '--Output json' or '--Output xml'.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when --OutputFile is used without --Output.
    /// </summary>
    public void Parse_OutputFileWithoutOutput_UsageError()
    {
        var error = ParseError("--ParseDWD", "--OutputFile", "out.json");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Option '--OutputFile' requires '--Output json' or '--Output xml'.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when an unknown output format is provided.
    /// </summary>
    public void Parse_UnknownOutputFormat_UsageError()
    {
        var error = ParseError("--ParseDWD", "--Output", "csv");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Unknown output format 'csv'; expected mqtt, json or xml.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when an unknown product is provided.
    /// </summary>
    public void Parse_UnknownProduct_UsageError()
    {
        var error = ParseError("--ParseMOSMIX", "--Product", "MOSMIX_X", "--MOSMIXID", "10637");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Unknown product 'MOSMIX_X'; expected MOSMIX_S or MOSMIX_L.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing returns a help request when --Help is provided alone.
    /// </summary>
    public void Parse_HelpAlone_HelpRequestWithoutCommand()
    {
        var result = _parser.Parse(new[] { "--Help" });
        result.Should().BeOfType<HelpRequest>().Which.Command.Should().BeNull();
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing returns a help request for a specific command.
    /// </summary>
    public void Parse_HelpWithCommand_HelpRequestForCommand()
    {
        var result = _parser.Parse(new[] { "--ParseMOSMIX", "--help" });
        result.Should().BeOfType<HelpRequest>().Which.Command.Should().Be(CliCommand.ParseMosmix);
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing fails when --Help is used with other options.
    /// </summary>
    public void Parse_HelpWithOtherOption_UsageError()
    {
        var error = ParseError("--Help", "--Verbose");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Option '--Help' is allowed alone or with exactly one command.");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that parsing returns the correct values and flags for a valid --ParseMOSMIX command.
    /// </summary>
    public void Parse_ValidParseMosmix_ReturnsValuesAndFlags()
    {
        var parsed = ParseOk("--ParseMOSMIX", "--Product", "mosmix_s", "--MOSMIXID", "10637", "--Output", "json", "--OutputFile", "out.json", "--Config", "c.json", "--Verbose");
        parsed.Command.Should().Be(CliCommand.ParseMosmix);
        parsed.Values["Product"].Should().Be("mosmix_s");
        parsed.Values["MOSMIXID"].Should().Be("10637");
        parsed.Values["Output"].Should().Be("json");
        parsed.Values["OutputFile"].Should().Be("out.json");
        parsed.Values["Config"].Should().Be("c.json");
        parsed.Flags.Should().Contain("Verbose");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that the --RefreshMosmixSEvidence flag is accepted for --SelectMOSMIX.
    /// </summary>
    public void Parse_SelectMosmixRefreshFlag_Accepted()
    {
        var parsed = ParseOk("--SelectMOSMIX", "--Latitude", "50.5", "--Longitude", "9.7", "--Altitude", "300", "--MaxDistance", "50", "--MaxAltDifference", "200", "--RefreshMosmixSEvidence");
        parsed.Flags.Should().Contain("RefreshMosmixSEvidence");
    }

    [TestMethod]
    [TestCategory("T-P3-02")]
    /// <summary>
    /// Verifies that values are preserved as provided during parsing.
    /// </summary>
    public void Parse_ValuesKeptUnchanged()
    {
        var parsed = ParseOk("--ParseDWD", "--StationID", "00044");
        parsed.Values["StationID"].Should().Be("00044");
    }

    /// <summary>
    /// Verifies that the same command given twice, also in another casing, is rejected as a duplicate option.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-02")]
    public void Parse_SameCommandTwiceDifferentCase_UsageError()
    {
        var error = ParseError("--ParseDWD", "--parsedwd");
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Option '--ParseDWD' is specified more than once.");
    }
}
