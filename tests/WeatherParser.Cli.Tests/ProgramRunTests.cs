using FluentAssertions;
using Moq;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Diagnostics;
using System.Text;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Verifies the argument handling of the entry point before any container exists: help, usage errors and validated commands (CL-03, CL-07, T-P3-04).
/// </summary>
[TestClass]
public sealed class ProgramRunTests
{
    private MockRepository _mocks = null!;
    private Mock<IHelpWriter> _help = null!;
    private Mock<ICommandLineParser> _parser = null!;
    private Mock<ICommandRequestValidator> _validator = null!;
    private Mock<ICommandExecutor> _executor = null!;
    private StringWriter _stdout = null!;
    private StringWriter _stderr = null!;

    /// <summary>
    /// Initializes the test fixtures.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _help = _mocks.Create<IHelpWriter>();
        _parser = _mocks.Create<ICommandLineParser>();
        _validator = _mocks.Create<ICommandRequestValidator>();
        _executor = _mocks.Create<ICommandExecutor>();
        _stdout = new StringWriter();
        _stderr = new StringWriter();
    }

    /// <summary>
    /// Cleans up the test fixtures.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
    }

    private int Run(params string[] args) => Program.Run(args, _stdout, _stderr, _help.Object, _parser.Object, _validator.Object, _executor.Object, new ExitCodePolicy());

    /// <summary>
    /// Verifies that help is written to stdout and returns zero.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-04")]
    public void Run_HelpRequest_WritesHelpToStdout_ReturnsZero()
    {
        _parser.Setup(p => p.Parse(It.Is<IReadOnlyList<string>>(a => a.SequenceEqual(new[] { "--Help" }))))
            .Returns(new HelpRequest(null));
        _help.Setup(h => h.WriteHelp(_stdout));

        var exitCode = Run("--Help");
        exitCode.Should().Be(0);

        _stderr.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that help for a command is written to stdout and returns zero.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-04")]
    public void Run_HelpRequestForCommand_WritesCommandHelp()
    {
        _parser.Setup(p => p.Parse(It.Is<IReadOnlyList<string>>(a => a.SequenceEqual(new[] { "--ParseDWD", "--Help" }))))
            .Returns(new HelpRequest(CliCommand.ParseDwd));
        _help.Setup(h => h.WriteCommandHelp(_stdout, CliCommand.ParseDwd));

        var exitCode = Run("--ParseDWD", "--Help");
        exitCode.Should().Be(0);

        _stderr.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a parser usage error is written to stderr and help is written to stderr, then returns one.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-04")]
    public void Run_ParserUsageError_WritesOneLogLine_ExitUsage()
    {
        _parser.Setup(p => p.Parse(It.Is<IReadOnlyList<string>>(a => a.SequenceEqual(new[] { "--ParseDWD", "--Bogus" }))))
            .Returns(new UsageError("CLI-ARGUMENT-INVALID", "Unknown option '--Bogus'."));
        // No help writing to stderr anymore

        var exitCode = Run("--ParseDWD", "--Bogus");
        exitCode.Should().Be(2);

        _stderr.ToString().Should().Contain(" ERROR CLI-ARGUMENT-INVALID command=- Unknown option '--Bogus'. Use --Help for the commands and options.");
        _stderr.ToString().TrimEnd().Should().NotContain("\n");
        _stdout.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a validator usage error is written to stderr and help is written to stderr, then returns one.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-04")]
    public void Run_ValidatorUsageError_WritesOneLogLine_ExitUsage()
    {
        var parsed = new ParsedCommandLine(CliCommand.SelectDwd, new Dictionary<string, string>(), new HashSet<string>());
        _parser.Setup(p => p.Parse(It.Is<IReadOnlyList<string>>(a => a.SequenceEqual(new[] { "--SelectDWD" }))))
            .Returns(parsed);
        _validator.Setup(v => v.Validate(parsed))
            .Returns(new UsageError("COORDINATE-INVALID", "Latitude out of range."));
        // No help writing to stderr anymore

        var exitCode = Run("--SelectDWD");
        exitCode.Should().Be(2);

        _stderr.ToString().Should().Contain(" ERROR COORDINATE-INVALID command=SelectDWD Latitude out of range. Use --Help for the commands and options.");
        _stderr.ToString().TrimEnd().Should().NotContain("\n");
        _stdout.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a valid command is executed and returns the exit code.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-04")]
    public void Run_ValidCommand_ExecutedOnce_ReturnsItsExitCode()
    {
        var parsed = new ParsedCommandLine(CliCommand.ParseDwd, new Dictionary<string, string>(), new HashSet<string>());
        _parser.Setup(p => p.Parse(It.Is<IReadOnlyList<string>>(a => a.SequenceEqual(new[] { "--ParseDWD" }))))
            .Returns(parsed);
        var request = new ParseDwdRequest(new CommonOptions(OutputFormat.Mqtt, null, null, false), "00044");
        _validator.Setup(v => v.Validate(parsed))
            .Returns(new ValidatedCommand(request));
        _executor.Setup(e => e.Execute(request, _stdout, _stderr)).Returns(7);

        var exitCode = Run("--ParseDWD");
        exitCode.Should().Be(7);

        _executor.Verify(e => e.Execute(request, _stdout, _stderr), Times.Once());
        _stderr.ToString().Should().BeEmpty();
        _stdout.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that main writes the help as UTF-8 without byte-order mark to standard output and returns zero (CL-12).
    /// </summary>
    [TestMethod]
    public void Main_Help_WritesUtf8ToStandardOutput_ReturnsZero()
    {
        using var output = new MemoryStream();
        using var error = new MemoryStream();
        var exitCode = Program.Main(new[] { "--Help" }, output, error);
        exitCode.Should().Be(0);
        var bytes = output.ToArray();
        bytes.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
        new UTF8Encoding(false, true).GetString(bytes).Should().Contain("Usage: WeatherParser --<command> [options]");
        error.Length.Should().Be(0);
    }

    /// <summary>
    /// Verifies that main with invalid arguments writes one UTF-8 log line to standard error and returns the usage code.
    /// </summary>
    [TestMethod]
    public void Main_InvalidArguments_WritesLogLine_ExitUsage()
    {
        using var output = new MemoryStream();
        using var error = new MemoryStream();
        var exitCode = Program.Main(new[] { "--ParseDWD", "--Langitude", "1" }, output, error);
        exitCode.Should().Be(2);
        new UTF8Encoding(false, true).GetString(error.ToArray()).Should().Contain(" ERROR CLI-ARGUMENT-INVALID command=ParseDWD Unknown option '--Langitude'.");
        output.Length.Should().Be(0);
    }

    /// <summary>
    /// Verifies that an unexpected exception ends with exit code 1 and the log line "… ERROR CLI-UNEXPECTED command=- The command failed unexpectedly. &lt;ExceptionType&gt;: &lt;message&gt;".
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-09")]
    public void Run_UnexpectedException_ExitFailureWithCliUnexpected()
    {
        var parsed = new ParsedCommandLine(CliCommand.ParseDwd, new Dictionary<string, string>(), new HashSet<string>());
        var request = new ParseDwdRequest(new CommonOptions(OutputFormat.Mqtt, null, null, false), "00044");
        _parser.Setup(p => p.Parse(It.IsAny<IReadOnlyList<string>>())).Returns(parsed);
        _validator.Setup(v => v.Validate(parsed)).Returns(new ValidatedCommand(request));
        _executor.Setup(e => e.Execute(request, _stdout, _stderr)).Throws(new InvalidOperationException("boom"));

        var exitCode = Run("--ParseDWD");

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Contain(" ERROR CLI-UNEXPECTED command=ParseDWD The command failed unexpectedly. InvalidOperationException: boom");
        _stderr.ToString().Should().NotContain(" at ");
        _stdout.ToString().Should().BeEmpty();
    }

    [TestMethod]
    [TestCategory("T-P3-09")]
    public void Run_ValidatorUsageError_NamesTheCommand()
    {
        var parsed = new ParsedCommandLine(CliCommand.SelectDwd, new Dictionary<string, string>(), new HashSet<string>());
        _parser.Setup(p => p.Parse(It.Is<IReadOnlyList<string>>(a => a.SequenceEqual(new[] { "--SelectDWD" }))))
            .Returns(parsed);
        _validator.Setup(v => v.Validate(parsed))
            .Returns(new UsageError("COORDINATE-INVALID", "Latitude out of range."));

        var exitCode = Run("--SelectDWD");
        exitCode.Should().Be(2);

        _stderr.ToString().Should().Contain(" ERROR COORDINATE-INVALID command=SelectDWD Latitude out of range.");
    }

    [TestMethod]
    [TestCategory("T-P3-09")]
    public void Run_ParserThrows_ExitFailureWithCliUnexpected()
    {
        _parser.Setup(p => p.Parse(It.IsAny<IReadOnlyList<string>>())).Throws(new InvalidOperationException("parser broke"));

        var exitCode = Run("--ParseDWD");
        exitCode.Should().Be(1);

        _stderr.ToString().Should().Contain(" ERROR CLI-UNEXPECTED command=- The command failed unexpectedly. InvalidOperationException: parser broke");
    }

    [TestMethod]
    [TestCategory("T-P3-09")]
    public void Run_ValidatorThrows_ExitFailureNamesTheCommand()
    {
        var parsed = new ParsedCommandLine(CliCommand.ParseMosmix, new Dictionary<string, string>(), new HashSet<string>());
        _parser.Setup(p => p.Parse(It.Is<IReadOnlyList<string>>(a => a.SequenceEqual(new[] { "--ParseMOSMIX" }))))
            .Returns(parsed);
        _validator.Setup(v => v.Validate(parsed))
            .Throws(new InvalidOperationException("validator broke"));

        var exitCode = Run("--ParseMOSMIX");
        exitCode.Should().Be(1);

        _stderr.ToString().Should().Contain(" ERROR CLI-UNEXPECTED command=ParseMOSMIX The command failed unexpectedly. InvalidOperationException: validator broke");
    }
}
