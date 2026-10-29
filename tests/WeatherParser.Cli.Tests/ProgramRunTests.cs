using FluentAssertions;
using Moq;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Tests the <see cref="Program.Run"/> method.
/// </summary>
[TestClass]
public sealed class ProgramRunTests
{
    private MockRepository _mocks = null!;
    private Mock<IHelpWriter> _help = null!;
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

    /// <summary>
    /// Verifies that help is written to stdout and returns zero.
    /// </summary>
    [TestMethod]
    public void Run_Help_WritesHelpToStdout_ReturnsZero()
    {
        _help.Setup(h => h.WriteHelp(_stdout));

        var exitCode = Program.Run(new[] { "--Help" }, _stdout, _stderr, _help.Object);
        exitCode.Should().Be(0);

        _stderr.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that help in other case is accepted.
    /// </summary>
    [TestMethod]
    public void Run_HelpInOtherCase_Accepted()
    {
        _help.Setup(h => h.WriteHelp(_stdout));

        var exitCode = Program.Run(new[] { "--help" }, _stdout, _stderr, _help.Object);
        exitCode.Should().Be(0);

        _stderr.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that no arguments writes help to stderr and returns one.
    /// </summary>
    [TestMethod]
    public void Run_NoArguments_WritesHelpToStderr_ReturnsOne()
    {
        _help.Setup(h => h.WriteHelp(_stderr));

        var exitCode = Program.Run(Array.Empty<string>(), _stdout, _stderr, _help.Object);
        exitCode.Should().Be(1);

        _stdout.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that other arguments result in not implemented and returns one.
    /// </summary>
    [TestMethod]
    public void Run_OtherArguments_NotImplemented_ReturnsOne()
    {
        var exitCode = Program.Run(new[] { "--ParseDWD" }, _stdout, _stderr, _help.Object);
        exitCode.Should().Be(1);

        _stderr.ToString().Should().Be("The command is not implemented yet." + Environment.NewLine);
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
}
