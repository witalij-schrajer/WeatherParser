using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.RegularExpressions;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Tests.Configuration;
using WeatherParser.Cli.Commands;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Cli.Mqtt;
using System.Linq;

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
    private Mock<ICommandRegistrar> _commandRegistrar = null!;
    private Mock<IConfigurationValidator> _validator = null!;
    private Mock<ICommandHandler<ParseDwdRequest>> _handler = null!;
    private Mock<IMessagePublisher> _publisher = null!;
    private Mock<IMqttPublicationSettingsFactory> _publicationFactory = null!;
    private StringWriter _stdout = null!;
    private StringWriter _stderr = null!;
    private CliConfiguration _configuration = null!;
    private CommandRequest _request = null!;
    private static readonly MqttPublicationSettings Publication = new(new MqttConnectionSettings("broker.example.invalid", 8883, MqttProtocol.V500, "weatherparser0123456789", TimeSpan.FromSeconds(30), true, null, null), MqttPublicationLimits.Default);

    /// <summary>
    /// Initializes the test fixtures.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _loader = _mocks.Create<IConfigurationLoader>();
        _registrar = _mocks.Create<IConfigurationRegistrar>();
        _commandRegistrar = _mocks.Create<ICommandRegistrar>();
        _validator = _mocks.Create<IConfigurationValidator>();
        _handler = _mocks.Create<ICommandHandler<ParseDwdRequest>>();
        _publisher = _mocks.Create<IMessagePublisher>();
        _publicationFactory = _mocks.Create<IMqttPublicationSettingsFactory>();
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

    private ICommandExecutor CreateSubject() => new CommandExecutor(_loader.Object, _registrar.Object, _commandRegistrar.Object, _validator.Object, new ExitCodePolicy(), new LibraryErrors(), new LogLineFormatter());

    private void RegisterParseDwdHandler() => _commandRegistrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), _request)).Callback<IServiceCollection, CommandRequest>((services, _) => services.AddSingleton(_handler.Object));

    private void SetupValidConfiguration(CommandRequest request)
    {
        _loader.Setup(l => l.Load(request.Common.ConfigPath)).Returns(_configuration);
        _registrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), _configuration, request)).Callback<IServiceCollection, CliConfiguration, CommandRequest>((services, configuration, r) => new ConfigurationRegistrar().Register(services, configuration, r));
    }

    /// <summary>
    /// Verifies that a valid configuration is loaded, the container is built and validated, then disposed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-05")]
    public void Execute_ValidConfiguration_BuildsValidatesAndDisposesContainer()
    {
        SetupValidConfiguration(_request);
        _registrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), _configuration, _request)).Callback<IServiceCollection, CliConfiguration, CommandRequest>((services, configuration, r) => new ConfigurationRegistrar().Register(services, configuration, r));
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, _request)).Callback<IServiceProvider, CliConfiguration, CommandRequest>((provider, _, _) => { }).Returns(new ValidatedSettings("00044", null));
        RegisterParseDwdHandler();
        _handler.Setup(h => h.HandleAsync((ParseDwdRequest)_request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CommandOutcome(1, 0, false));
        var subject = CreateSubject();

        var exitCode = subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(0);
        _stderr.ToString().Should().NotContain(" ERROR ");
        _stdout.ToString().Should().BeEmpty();
        AssertEveryLineIsALogLine();
    }

    /// <summary>
    /// Verifies that an exception from the validator is written to stderr and returns one.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-05")]
    public void Execute_ValidatorFails_WritesErrorReturnsOne()
    {
        SetupValidConfiguration(_request);
        _registrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), _configuration, _request))
            .Callback<IServiceCollection, CliConfiguration, CommandRequest>((services, configuration, request) => new ConfigurationRegistrar().Register(services, configuration, request));
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, _request)).Throws(new CliException("CLI-CONFIG-INVALID", "ENDPOINT-INVALID: bad endpoint"));
        RegisterParseDwdHandler();
        var subject = CreateSubject();

        var exitCode = subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(2);
        _stderr.ToString().Should().Contain(" ERROR CLI-CONFIG-INVALID command=ParseDWD ENDPOINT-INVALID: bad endpoint");
        _stderr.ToString().TrimEnd().Should().NotContain("\n");
        _stdout.ToString().Should().BeEmpty();
        AssertEveryLineIsALogLine();
    }

    [TestMethod]
    [TestCategory("T-P3-08")]
    public void Execute_Verbose_LogsDebugLevel()
    {
        var request = new ParseDwdRequest(new CommonOptions(OutputFormat.Json, null, "c.json", true), "00044");
        SetupValidConfiguration(request);
        _registrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), _configuration, request))
            .Callback<IServiceCollection, CliConfiguration, CommandRequest>((services, configuration, request) => new ConfigurationRegistrar().Register(services, configuration, request));
        _commandRegistrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), request)).Callback<IServiceCollection, CommandRequest>((services, _) => services.AddSingleton(_handler.Object));
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, request))
            .Callback<IServiceProvider, CliConfiguration, CommandRequest>((provider, _, _) => provider.GetRequiredService<ILogger<CommandExecutorTests>>().LogDebug("debug entry"))
            .Returns(new ValidatedSettings("00044", null));
        _handler.Setup(h => h.HandleAsync((ParseDwdRequest)request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CommandOutcome(1, 0, false));
        var subject = CreateSubject();

        var exitCode = subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(0);
        _stderr.ToString().Should().Contain(" DEBUG - command=ParseDWD debug entry");
    }

    [TestMethod]
    [TestCategory("T-P3-09")]
    public void Execute_PartialOutcome_ExitPartial()
    {
        SetupValidConfiguration(_request);
        RegisterParseDwdHandler();
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, _request)).Returns(new ValidatedSettings("00044", null));
        _handler.Setup(h => h.HandleAsync((ParseDwdRequest)_request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CommandOutcome(1, 1, false));
        var subject = CreateSubject();

        var exitCode = subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(3);
    }

    [TestMethod]
    [TestCategory("T-P3-09")]
    public void Execute_FailedOutcome_ExitFailure()
    {
        SetupValidConfiguration(_request);
        RegisterParseDwdHandler();
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, _request)).Returns(new ValidatedSettings("00044", null));
        _handler.Setup(h => h.HandleAsync((ParseDwdRequest)_request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CommandOutcome(0, 1, false));
        var subject = CreateSubject();

        var exitCode = subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(1);
    }

    [TestMethod]
    [TestCategory("T-P3-09")]
    public void Execute_CommandLevelLibraryFailure_LogsIdExitOne()
    {
        SetupValidConfiguration(_request);
        RegisterParseDwdHandler();
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, _request)).Returns(new ValidatedSettings("00044", null));
        _handler.Setup(h => h.HandleAsync((ParseDwdRequest)_request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>())).ThrowsAsync(new OsthessenWetterException(OsthessenWetterException.PageUnavailableErrorId, "The page is unavailable."));
        var subject = CreateSubject();

        var exitCode = subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Contain(" ERROR OSTHESSEN-PAGE-UNAVAILABLE command=ParseDWD The page is unavailable.");
        _stdout.ToString().Should().BeEmpty();
        AssertEveryLineIsALogLine();
    }

    [TestMethod]
    [TestCategory("T-P3-09")]
    public void Execute_PreRequestLibraryFailure_ExitUsage()
    {
        SetupValidConfiguration(_request);
        RegisterParseDwdHandler();
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, _request)).Returns(new ValidatedSettings("00044", null));
        _handler.Setup(h => h.HandleAsync((ParseDwdRequest)_request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>())).ThrowsAsync(new DwdStationException(DwdStationException.StationIdInvalidErrorId, "The station ID is invalid."));
        var subject = CreateSubject();

        var exitCode = subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(2);
        _stderr.ToString().Should().Contain(" ERROR STATION-ID-INVALID command=ParseDWD ");
    }

    [TestMethod]
    [TestCategory("T-P3-09")]
    public void Execute_RuntimeCliFailure_ExitFailure()
    {
        SetupValidConfiguration(_request);
        RegisterParseDwdHandler();
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, _request)).Returns(new ValidatedSettings("00044", null));
        _handler.Setup(h => h.HandleAsync((ParseDwdRequest)_request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>())).ThrowsAsync(new CliException(CliErrorIds.OutputExists, "The destination exists."));
        var subject = CreateSubject();

        var exitCode = subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Contain(" ERROR CLI-OUTPUT-EXISTS command=ParseDWD The destination exists.");
    }

    [DataRow(CliCommand.SelectDwd)]
    [DataRow(CliCommand.SelectMosmix)]
    [DataRow(CliCommand.ParseDwd)]
    [DataRow(CliCommand.ParseOstHessen)]
    [DataRow(CliCommand.ParseMosmix)]
    [TestMethod]
    [TestCategory("T-P3-09")]
    public void Execute_EachCommand_RunsItsHandler(CliCommand command)
    {
        var request = TestConfigurations.Request(command, OutputFormat.Json);
        SetupValidConfiguration(request);
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, request)).Returns(new ValidatedSettings(null, null));
        RegisterHandler(request);
        var subject = CreateSubject();

        var exitCode = subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(0);
    }

    private void RegisterHandler(CommandRequest request)
    {
        switch (request)
        {
            case SelectDwdRequest selectDwd:
                var selectDwdHandler = _mocks.Create<ICommandHandler<SelectDwdRequest>>();
                selectDwdHandler.Setup(h => h.HandleAsync(selectDwd, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CommandOutcome(1, 0, false));
                _commandRegistrar.Setup(c => c.Register(It.IsAny<IServiceCollection>(), request)).Callback<IServiceCollection, CommandRequest>((services, _) => services.AddSingleton(selectDwdHandler.Object));
                break;
            case SelectMosmixRequest selectMosmix:
                var selectMosmixHandler = _mocks.Create<ICommandHandler<SelectMosmixRequest>>();
                selectMosmixHandler.Setup(h => h.HandleAsync(selectMosmix, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CommandOutcome(1, 0, false));
                _commandRegistrar.Setup(c => c.Register(It.IsAny<IServiceCollection>(), request)).Callback<IServiceCollection, CommandRequest>((services, _) => services.AddSingleton(selectMosmixHandler.Object));
                break;
            case ParseDwdRequest parseDwd:
                var parseDwdHandler = _mocks.Create<ICommandHandler<ParseDwdRequest>>();
                parseDwdHandler.Setup(h => h.HandleAsync(parseDwd, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CommandOutcome(1, 0, false));
                _commandRegistrar.Setup(c => c.Register(It.IsAny<IServiceCollection>(), request)).Callback<IServiceCollection, CommandRequest>((services, _) => services.AddSingleton(parseDwdHandler.Object));
                break;
            case ParseOstHessenRequest parseOsthessen:
                var parseOsthessenHandler = _mocks.Create<ICommandHandler<ParseOstHessenRequest>>();
                parseOsthessenHandler.Setup(h => h.HandleAsync(parseOsthessen, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CommandOutcome(1, 0, false));
                _commandRegistrar.Setup(c => c.Register(It.IsAny<IServiceCollection>(), request)).Callback<IServiceCollection, CommandRequest>((services, _) => services.AddSingleton(parseOsthessenHandler.Object));
                break;
            case ParseMosmixRequest parseMosmix:
                var parseMosmixHandler = _mocks.Create<ICommandHandler<ParseMosmixRequest>>();
                parseMosmixHandler.Setup(h => h.HandleAsync(parseMosmix, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CommandOutcome(1, 0, false));
                _commandRegistrar.Setup(c => c.Register(It.IsAny<IServiceCollection>(), request)).Callback<IServiceCollection, CommandRequest>((services, _) => services.AddSingleton(parseMosmixHandler.Object));
                break;
        }
    }

    private void AssertEveryLineIsALogLine()
    {
        foreach (var line in _stderr.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            line.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2} UTC [A-Z]+ \S+ command=\S+ ");
        }
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


    /// <summary>
    /// Verifies that an existing output file is rejected after the validation phase and before the command runs, with the usage code (CL-11, CL-12).
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-12")]
    public void Execute_OutputFileExists_RejectedBeforeTheHandler_ExitUsage()
    {
        var request = new ParseDwdRequest(new CommonOptions(OutputFormat.Json, "out.json", "c.json", false), "00044");
        var destination = _mocks.Create<IOutputDestination>();
        destination.Setup(d => d.Check(request.Common)).Throws(new CliException(CliErrorIds.OutputExists, "The output file already exists."));
        SetupValidConfiguration(request);
        _commandRegistrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), request)).Callback<IServiceCollection, CommandRequest>((services, _) =>
        {
            services.AddSingleton(_handler.Object);
            services.AddSingleton(destination.Object);
        });
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, request)).Returns(new ValidatedSettings("00044", null));
        var subject = CreateSubject();

        var exitCode = subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(2);
        _stderr.ToString().Should().Contain(" ERROR CLI-OUTPUT-EXISTS command=ParseDWD The output file already exists.");
        _stdout.ToString().Should().BeEmpty();
    }

    private (ParseDwdRequest Request, PublicationQueue Queue) SetupMqttRun(CommandOutcome handlerOutcome, int messages, BrokerCredentials? credentials = null)
    {
        var request = new ParseDwdRequest(new CommonOptions(OutputFormat.Mqtt, null, "c.json", false), "00044");
        var queue = new PublicationQueue();
        SetupValidConfiguration(request);
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, request)).Returns(new ValidatedSettings("00044", credentials));
        _commandRegistrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), request)).Callback<IServiceCollection, CommandRequest>((services, _) =>
        {
            services.AddSingleton(_handler.Object);
            services.AddSingleton<IPublicationQueue>(queue);
            services.AddSingleton(_publisher.Object);
            services.AddSingleton(_publicationFactory.Object);
        });
        _publicationFactory.Setup(f => f.Create(_configuration, credentials)).Returns(Publication);
        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                for (var i = 0; i < messages; i++)
                {
                    queue.Add(new MqttMessage($"t/{i}", new byte[] { 1 }, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation), "dwd:00044");
                }
            })
            .ReturnsAsync(handlerOutcome);
        return (request, queue);
    }

    [TestMethod]
    [TestCategory("T-P3-15")]
    public async Task Execute_MqttMode_PublishesTheQueueAfterTheHandler()
    {
        var (request, queue) = SetupMqttRun(new CommandOutcome(1, 0, false), 1);
        _publisher.Setup(p => p.PublishAllAsync(It.Is<IReadOnlyList<PublicationItem>>(m => m.Count == 1 && m[0].Message.Topic == "t/0"), Publication, It.IsAny<CancellationToken>())).ReturnsAsync(new PublicationReport(1, 0, 0));
        var subject = CreateSubject();

        var exitCode = subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(0);
    }

    [TestMethod]
    [TestCategory("T-P3-15")]
    public async Task Execute_MqttMode_OnePublicationFails_ExitPartial()
    {
        var (request, queue) = SetupMqttRun(new CommandOutcome(2, 0, false), 2);
        _publisher.Setup(p => p.PublishAllAsync(It.Is<IReadOnlyList<PublicationItem>>(m => m.Count == 2), Publication, It.IsAny<CancellationToken>())).ReturnsAsync(new PublicationReport(1, 1, 0));
        var subject = CreateSubject();

        var exitCode = subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(3);
    }

    [TestMethod]
    [TestCategory("T-P3-15")]
    public async Task Execute_MqttMode_NothingPublished_ExitFailure()
    {
        var (request, queue) = SetupMqttRun(new CommandOutcome(1, 0, false), 1);
        _publisher.Setup(p => p.PublishAllAsync(It.Is<IReadOnlyList<PublicationItem>>(m => m.Count == 1), Publication, It.IsAny<CancellationToken>())).ReturnsAsync(new PublicationReport(0, 0, 1));
        var subject = CreateSubject();

        var exitCode = subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(1);
    }

    [TestMethod]
    [TestCategory("T-P3-15")]
    public async Task Execute_MqttMode_ItemFailureAndPublishedItem_ExitPartial()
    {
        var (request, queue) = SetupMqttRun(new CommandOutcome(1, 1, false), 1);
        _publisher.Setup(p => p.PublishAllAsync(It.Is<IReadOnlyList<PublicationItem>>(m => m.Count == 1), Publication, It.IsAny<CancellationToken>())).ReturnsAsync(new PublicationReport(1, 0, 0));
        var subject = CreateSubject();

        var exitCode = subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(3);
    }

    [TestMethod]
    [TestCategory("T-P3-15")]
    public async Task Execute_MqttMode_ConnectFailure_LogsIdExitFailure()
    {
        var (request, queue) = SetupMqttRun(new CommandOutcome(1, 0, false), 1);
        _publisher.Setup(p => p.PublishAllAsync(It.IsAny<IReadOnlyList<PublicationItem>>(), Publication, It.IsAny<CancellationToken>())).ThrowsAsync(new CliException(CliErrorIds.MqttConnectFailed, "The broker is not reachable."));
        var subject = CreateSubject();

        var exitCode = subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Contain(" ERROR CLI-MQTT-CONNECT-FAILED command=ParseDWD The broker is not reachable.");
    }

    [TestMethod]
    [TestCategory("T-P3-15")]
    public void Execute_MqttMode_SettingsBuiltFromConfigurationAndCredentials()
    {
        var credentials = new BrokerCredentials("user", "secret");
        var (request, queue) = SetupMqttRun(new CommandOutcome(1, 0, false), 1, credentials);
        _publisher.Setup(p => p.PublishAllAsync(It.Is<IReadOnlyList<PublicationItem>>(m => m.Count == 1), Publication, It.IsAny<CancellationToken>())).ReturnsAsync(new PublicationReport(1, 0, 0));
        var subject = CreateSubject();

        var exitCode = subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(0);
    }
}
