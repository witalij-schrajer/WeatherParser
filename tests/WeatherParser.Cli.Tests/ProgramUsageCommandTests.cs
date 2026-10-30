using WeatherParser.Parsing.Geography;
using FluentAssertions;
using Moq;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Diagnostics;

namespace WeatherParser.Cli.Tests;

[TestClass]
[TestCategory("T-P3-08")]
public class ProgramUsageCommandTests
{
    private MockRepository _mocks = null!;
    private Mock<ICommandExecutor> _executor = null!;
    private StringWriter _stdout = null!;
    private StringWriter _stderr = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _executor = _mocks.Create<ICommandExecutor>();
        _stdout = new StringWriter();
        _stderr = new StringWriter();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
    }

    private int Run(string[] arguments) => Program.Run(arguments, _stdout, _stderr, new HelpWriter(), new CommandLineParser(), new CommandRequestValidator(new CoordinateParser(), new SearchParameterFactory()), _executor.Object, new ExitCodePolicy());

    [TestMethod]
    public void Run_ParserErrorWithKnownCommand_NamesTheCommand()
    {
        var exitCode = Run(new[] { "--ParseMOSMIX", "--MOSMIXID", "10637" });

        exitCode.Should().Be(2);
        _stderr.ToString().Should().Contain(" ERROR CLI-ARGUMENT-INVALID command=ParseMOSMIX Option '--Product' is required with '--ParseMOSMIX'.");

    }

    [TestMethod]
    [TestCategory("T-P3-08")]
    [DataRow("--SelectDWD", "SelectDWD")]
    [DataRow("--SelectMOSMIX", "SelectMOSMIX")]
    [DataRow("--ParseOstHessen", "ParseOstHessen")]
    [DataRow("--ParseMOSMIX", "ParseMOSMIX")]
    [DataRow("--ParseDWD --Output csv", "ParseDWD")]
    [DataRow("--ParseDWD --Help --Verbose", "ParseDWD")]
    public void Run_ParserErrorForEachCommand_NamesTheCommand(string arguments, string commandName)
    {
        var exitCode = Run(arguments.Split(' '));

        exitCode.Should().Be(2);
        _stderr.ToString().Should().Contain($" ERROR CLI-ARGUMENT-INVALID command={commandName} ");
    }
}
