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
    [TestMethod]
    public void Run_ParserErrorWithKnownCommand_NamesTheCommand()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var executor = new Mock<ICommandExecutor>(MockBehavior.Strict);

        var exitCode = Program.Run(
            new[] { "--ParseMOSMIX", "--MOSMIXID", "10637" },
            stdout,
            stderr,
            new HelpWriter(),
            new CommandLineParser(),
            new CommandRequestValidator(new CoordinateParser(), new SearchParameterFactory()),
            executor.Object,
            new ExitCodePolicy());

        exitCode.Should().Be(2);
        stderr.ToString().Should().Contain(" ERROR CLI-ARGUMENT-INVALID command=ParseMOSMIX Option '--Product' is required with '--ParseMOSMIX'.");

        executor.VerifyAll();
    }

    [TestMethod]
    [TestCategory("T-P3-08")]
    [DataRow("--SelectDWD", "SelectDWD")]
    [DataRow("--SelectMOSMIX", "SelectMOSMIX")]
    [DataRow("--ParseOstHessen", "ParseOstHessen")]
    [DataRow("--ParseMOSMIX", "ParseMOSMIX")]
    [DataRow("--ParseDWD --Output csv", "ParseDWD")]
    public void Run_ParserErrorForEachCommand_NamesTheCommand(string arguments, string commandName)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var executor = new Mock<ICommandExecutor>(MockBehavior.Strict);

        var exitCode = Program.Run(
            arguments.Split(' '),
            stdout,
            stderr,
            new HelpWriter(),
            new CommandLineParser(),
            new CommandRequestValidator(new CoordinateParser(), new SearchParameterFactory()),
            executor.Object,
            new ExitCodePolicy());

        exitCode.Should().Be(2);
        stderr.ToString().Should().Contain($" ERROR CLI-ARGUMENT-INVALID command={commandName} ");

        executor.VerifyAll();
    }
}
