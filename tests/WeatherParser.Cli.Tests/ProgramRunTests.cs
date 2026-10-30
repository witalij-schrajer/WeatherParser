using FluentAssertions;
using Moq;
using WeatherParser.Cli.Arguments;

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

    private int Run(params string[] args) => Program.Run(args, _stdout, _stderr, _help.Object, _parser.Object, _validator.Object, _executor.Object);

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
    public void Run_ParserUsageError_WritesErrorLineAndHelpToStderr_ReturnsOne()
    {
        _parser.Setup(p => p.Parse(It.Is<IReadOnlyList<string>>(a => a.SequenceEqual(new[] { "--ParseDWD", "--Bogus" }))))
            .Returns(new UsageError("CLI-ARGUMENT-INVALID", "Unknown option '--Bogus'."));
        _help.Setup(h => h.WriteHelp(_stderr));

        var exitCode = Run("--ParseDWD", "--Bogus");
        exitCode.Should().Be(1);

        _stderr.ToString().Should().StartWith("CLI-ARGUMENT-INVALID: Unknown option '--Bogus'." + Environment.NewLine);
        _stdout.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a validator usage error is written to stderr and help is written to stderr, then returns one.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-04")]
    public void Run_ValidatorUsageError_WritesErrorLineAndHelpToStderr_ReturnsOne()
    {
        var parsed = new ParsedCommandLine(CliCommand.SelectDwd, new Dictionary<string, string>(), new HashSet<string>());
        _parser.Setup(p => p.Parse(It.Is<IReadOnlyList<string>>(a => a.SequenceEqual(new[] { "--SelectDWD" }))))
            .Returns(parsed);
        _validator.Setup(v => v.Validate(parsed))
            .Returns(new UsageError("COORDINATE-INVALID", "Latitude out of range."));
        _help.Setup(h => h.WriteHelp(_stderr));

        var exitCode = Run("--SelectDWD");
        exitCode.Should().Be(1);

        _stderr.ToString().Should().StartWith("COORDINATE-INVALID: Latitude out of range." + Environment.NewLine);
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
    /// Verifies that main with help writes to console out and returns zero.
    /// </summary>
    [TestMethod]
    public void Main_Help_WritesToConsoleOut_ReturnsZero()
    {
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(_stdout);
            var exitCode = Program.Main(new[] { "--Help" });
            exitCode.Should().Be(0);
            _stdout.ToString().Should().Contain("Usage: WeatherParser --<command> [options]");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Verifies that main with invalid arguments writes error to console error and returns one.
    /// </summary>
    [TestMethod]
    public void Main_InvalidArguments_WritesErrorToConsoleError_ReturnsOne()
    {
        var originalErr = Console.Error;
        try
        {
            Console.SetError(_stderr);
            var exitCode = Program.Main(new[] { "--ParseDWD", "--Langitude", "1" });
            exitCode.Should().Be(1);
            _stderr.ToString().Should().StartWith("CLI-ARGUMENT-INVALID: Unknown option '--Langitude'." + Environment.NewLine);
        }
        finally
        {
            Console.SetError(originalErr);
        }
    }
}
