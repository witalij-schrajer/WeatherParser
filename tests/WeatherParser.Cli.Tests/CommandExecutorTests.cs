using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.RegularExpressions;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Tests.Configuration;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Verifies the command executor: configuration, the one validated container, the validation phase and the disposal of the container (CL-04, CL-05, CL-07).
/// </summary>
[TestClass]
public sealed class CommandExecutorTests
{
    private MockRepository _mocks = null!;
    private Mock<IConfigurationLoader> _loader = null!;
    private Mock<IConfigurationRegistrar> _registrar = null!;
    private Mock<IConfigurationValidator> _validator = null!;
    private StringWriter _stdout = null!;
    private StringWriter _stderr = null!;
    private CliConfiguration _configuration = null!;
    private CommandRequest _request = null!;

    /// <summary>
    /// Initializes the test fixtures.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _loader = _mocks.Create<IConfigurationLoader>();
        _registrar = _mocks.Create<IConfigurationRegistrar>();
        _validator = _mocks.Create<IConfigurationValidator>();
        _stdout = new StringWriter();
        _stderr = new StringWriter();
        _configuration = TestConfigurations.Valid();
        _request = new ParseDwdRequest(new CommonOptions(OutputFormat.Json, null, "c.json", false), "00044");
    }

    /// <summary>
    /// Cleans up the test fixtures.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
    }

    private ICommandExecutor CreateSubject() => new CommandExecutor(_loader.Object, _registrar.Object, _validator.Object, new ExitCodePolicy(), new LogLineFormatter());

    /// <summary>
    /// Verifies that a valid configuration is loaded, the container is built and validated, then disposed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-05")]
    public void Execute_ValidConfiguration_BuildsValidatesAndDisposesContainer()
    {
        _loader.Setup(l => l.Load("c.json")).Returns(_configuration);
        _registrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), _configuration, _request)).Callback<IServiceCollection, CliConfiguration, CommandRequest>((services, configuration, request) => new ConfigurationRegistrar().Register(services, configuration, request));
        IServiceProvider? captured = null;
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, _request)).Callback<IServiceProvider, CliConfiguration, CommandRequest>((provider, _, _) => captured = provider);
        var subject = CreateSubject();

        var exitCode = subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Contain(" ERROR - command=ParseDWD The command is not implemented yet.");
        _stdout.ToString().Should().BeEmpty();
        captured.Should().NotBeNull();
        Action act = () => captured!.GetService(typeof(TimeProvider));
        act.Should().Throw<ObjectDisposedException>();
        AssertEveryLineIsALogLine();
    }

    /// <summary>
    /// Verifies that an exception from the loader is written to stderr and returns one.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-05")]
    public void Execute_LoaderFails_WritesErrorReturnsOne_NoContainer()
    {
        _loader.Setup(l => l.Load("c.json")).Throws(new CliException("CLI-CONFIG-MISSING", "No configuration file given."));
        var subject = CreateSubject();

        var exitCode = subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(2);
        _stderr.ToString().Should().Contain(" ERROR CLI-CONFIG-MISSING command=ParseDWD No configuration file given.");
        _stderr.ToString().TrimEnd().Should().NotContain("\n");
        _stdout.ToString().Should().BeEmpty();
        _registrar.Verify(r => r.Register(It.IsAny<IServiceCollection>(), It.IsAny<CliConfiguration>(), It.IsAny<CommandRequest>()), Times.Never());
        _validator.Verify(v => v.Validate(It.IsAny<IServiceProvider>(), It.IsAny<CliConfiguration>(), It.IsAny<CommandRequest>()), Times.Never());
        AssertEveryLineIsALogLine();
    }

    /// <summary>
    /// Verifies that an exception from the validator is written to stderr and returns one.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-05")]
    public void Execute_ValidatorFails_WritesErrorReturnsOne()
    {
        _loader.Setup(l => l.Load("c.json")).Returns(_configuration);
        _registrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), _configuration, _request))
            .Callback<IServiceCollection, CliConfiguration, CommandRequest>((services, configuration, request) => new ConfigurationRegistrar().Register(services, configuration, request));
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, _request)).Throws(new CliException("CLI-CONFIG-INVALID", "ENDPOINT-INVALID: bad endpoint"));
        var subject = CreateSubject();

        var exitCode = subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(2);
        _stderr.ToString().Should().Contain(" ERROR CLI-CONFIG-INVALID command=ParseDWD ENDPOINT-INVALID: bad endpoint");
        _stderr.ToString().TrimEnd().Should().NotContain("\n");
        _stdout.ToString().Should().BeEmpty();
        AssertEveryLineIsALogLine();
    }

    [DataRow(CliCommand.SelectDwd, "SelectDWD")]
    [DataRow(CliCommand.SelectMosmix, "SelectMOSMIX")]
    [DataRow(CliCommand.ParseDwd, "ParseDWD")]
    [DataRow(CliCommand.ParseOstHessen, "ParseOstHessen")]
    [DataRow(CliCommand.ParseMosmix, "ParseMOSMIX")]
    [TestMethod]
    [TestCategory("T-P3-08")]
    public void Execute_LogLineNamesTheCommand(CliCommand command, string name)
    {
        var request = TestConfigurations.Request(command, OutputFormat.Json);
        _loader.Setup(l => l.Load(null)).Throws(new CliException("CLI-CONFIG-MISSING", "missing"));
        var subject = CreateSubject();

        var exitCode = subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(2);
        _stderr.ToString().Should().Contain($" ERROR CLI-CONFIG-MISSING command={name} missing");
    }

    [TestMethod]
    [TestCategory("T-P3-08")]
    public void Execute_Verbose_LogsDebugLevel()
    {
        var request = new ParseDwdRequest(new CommonOptions(OutputFormat.Json, null, "c.json", true), "00044");
        _loader.Setup(l => l.Load("c.json")).Returns(_configuration);
        _registrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), _configuration, request))
            .Callback<IServiceCollection, CliConfiguration, CommandRequest>((services, configuration, request) => new ConfigurationRegistrar().Register(services, configuration, request));
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, request))
            .Callback<IServiceProvider, CliConfiguration, CommandRequest>((provider, _, _) => provider.GetRequiredService<ILogger<CommandExecutorTests>>().LogDebug("debug entry"));
        var subject = CreateSubject();

        var exitCode = subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Contain(" DEBUG - command=ParseDWD debug entry");
    }

    private void AssertEveryLineIsALogLine()
    {
        foreach (var line in _stderr.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            line.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2} UTC [A-Z]+ \S+ command=\S+ ");
        }
    }
}
