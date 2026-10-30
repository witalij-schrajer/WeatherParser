using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
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

    private ICommandExecutor CreateSubject() => new CommandExecutor(_loader.Object, _registrar.Object, _validator.Object);

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
        _stderr.ToString().Should().Be("The command is not implemented yet." + Environment.NewLine);
        _stdout.ToString().Should().BeEmpty();
        captured.Should().NotBeNull();
        Action act = () => captured!.GetService(typeof(TimeProvider));
        act.Should().Throw<ObjectDisposedException>();
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

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Be("CLI-CONFIG-MISSING: No configuration file given." + Environment.NewLine);
        _stdout.ToString().Should().BeEmpty();
        _registrar.Verify(r => r.Register(It.IsAny<IServiceCollection>(), It.IsAny<CliConfiguration>(), It.IsAny<CommandRequest>()), Times.Never());
        _validator.Verify(v => v.Validate(It.IsAny<IServiceProvider>(), It.IsAny<CliConfiguration>(), It.IsAny<CommandRequest>()), Times.Never());
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

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Be("CLI-CONFIG-INVALID: ENDPOINT-INVALID: bad endpoint" + Environment.NewLine);
        _stdout.ToString().Should().BeEmpty();
    }
}
