using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Linq;
using System.Text;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Mqtt;
using WeatherParser.Cli.Output;
using WeatherParser.Cli.Tests.Configuration;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Verifies the command executor's MQTT publication behavior (CL-11, CL-15, REQ-063).
/// </summary>
[TestClass]
[TestCategory("T-P3-15")]
public sealed class CommandExecutorPublicationTests
{
    private MockRepository _mocks = null!;
    private Mock<IConfigurationLoader> _loader = null!;
    private Mock<IConfigurationRegistrar> _registrar = null!;
    private Mock<ICommandRegistrar> _commandRegistrar = null!;
    private Mock<IConfigurationValidator> _validator = null!;
    private Mock<ICommandHandler<ParseDwdRequest>> _handler = null!;
    private Mock<IMqttClientAdapter> _adapter = null!;
    private StringWriter _stdout = null!;
    private StringWriter _stderr = null!;
    private CliConfiguration _configuration = null!;
    private ParseDwdRequest _request = null!;
    private CommandExecutor _subject = null!;

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
        _adapter = _mocks.Create<IMqttClientAdapter>();
        _stdout = new StringWriter();
        _stderr = new StringWriter();
        _configuration = TestConfigurations.Valid();
        _configuration.Limits = new LimitsSection { MaxPublishAttempts = 1, RetryDelay = TimeSpan.Zero };
        _request = new ParseDwdRequest(new CommonOptions(OutputFormat.Mqtt, null, "c.json", false), "00044");
        _subject = new CommandExecutor(_loader.Object, _registrar.Object, _commandRegistrar.Object, _validator.Object, new ExitCodePolicy(), new LibraryErrors(), new LogLineFormatter());
    }

    /// <summary>
    /// Cleans up the test fixtures.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
        _stdout.Dispose();
        _stderr.Dispose();
    }

    // The handler succeeds for every message it queues; the real publisher then publishes the queue over the adapter mock.
    private void SetupRun(int messages)
    {
        var queue = new PublicationQueue();
        _loader.Setup(l => l.Load(_request.Common.ConfigPath)).Returns(_configuration);
        _registrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), _configuration, _request)).Callback<IServiceCollection, CliConfiguration, CommandRequest>((services, configuration, request) => new ConfigurationRegistrar().Register(services, configuration, request));
        _commandRegistrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), _request)).Callback<IServiceCollection, CommandRequest>((services, _) =>
        {
            services.AddSingleton(_handler.Object);
            services.AddSingleton<IPublicationQueue>(queue);
            services.AddSingleton(_adapter.Object);
            services.AddSingleton<IMessagePublisher, MqttPublisher>();
            services.AddSingleton<IMqttPublicationSettingsFactory, MqttPublicationSettingsFactory>();
        });
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, _request)).Returns(new ValidatedSettings("00044", null));
        _handler.Setup(h => h.HandleAsync(_request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                for (var i = 0; i < messages; i++)
                {
                    queue.Add(new MqttMessage($"t/{i}", new byte[] { 1 }, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation), "dwd:00044");
                }
            })
            .ReturnsAsync(new CommandOutcome(messages, 0, false));
    }

    private void SetupConnectFailure(MqttTransportFailure failure, string text) =>
        _adapter.Setup(a => a.ConnectAsync(It.IsAny<MqttConnectionSettings>(), It.IsAny<CancellationToken>())).ThrowsAsync(new MqttTransportException(failure, text));

    /// <summary>
    /// Verifies that one failed publication results in exit code 3 and logs the summary.
    /// </summary>
    [TestMethod]
    public void Command_OneOfThreePublicationsFails_SummaryAndExitPartial()
    {
        SetupRun(3);
        _adapter.Setup(a => a.ConnectAsync(It.IsAny<MqttConnectionSettings>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _adapter.Setup(a => a.PublishAsync(It.Is<MqttMessage>(m => m.Topic == "t/1"), It.IsAny<CancellationToken>())).ReturnsAsync(new MqttPublishAcknowledgment(false, "QuotaExceeded"));
        _adapter.Setup(a => a.PublishAsync(It.Is<MqttMessage>(m => m.Topic == "t/0"), It.IsAny<CancellationToken>())).ReturnsAsync(new MqttPublishAcknowledgment(true, "Success"));
        _adapter.Setup(a => a.PublishAsync(It.Is<MqttMessage>(m => m.Topic == "t/2"), It.IsAny<CancellationToken>())).ReturnsAsync(new MqttPublishAcknowledgment(true, "Success"));
        _adapter.Setup(a => a.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var exitCode = _subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(3);
        _stderr.ToString().Should().Contain(" ERROR CLI-MQTT-MESSAGE-REFUSED command=ParseDWD ");
        _stderr.ToString().Should().Contain("Publication summary: published=2 failed=1 not_attempted=0.");
        _stdout.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a connection failure results in exit code 1 and logs the summary.
    /// </summary>
    [TestMethod]
    public void Command_ConnectionFails_NoPublication_ExitFailure()
    {
        SetupRun(1);
        SetupConnectFailure(MqttTransportFailure.ConnectionFailed, "No connection could be made.");
        var exitCode = _subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Contain(" ERROR CLI-MQTT-CONNECT-FAILED command=ParseDWD ");
        _stderr.ToString().Should().Contain("not_attempted=1");
    }

    /// <summary>
    /// Verifies that an authentication failure results in exit code 1 and logs the summary.
    /// </summary>
    [TestMethod]
    public void Command_AuthenticationFails_ReportedWithId_ExitFailure()
    {
        SetupRun(1);
        SetupConnectFailure(MqttTransportFailure.ConnectionRefused, "Not authorized.");
        var exitCode = _subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Contain(" ERROR CLI-MQTT-CONNECT-FAILED command=ParseDWD ");
        _stderr.ToString().Should().Contain("ConnectionRefused");
        _stderr.ToString().Should().Contain("Not authorized.");
    }

    /// <summary>
    /// Verifies that a TLS failure results in exit code 1 and logs the summary.
    /// </summary>
    [TestMethod]
    public void Command_TlsFailure_ReportedWithId_ExitFailure()
    {
        SetupRun(1);
        SetupConnectFailure(MqttTransportFailure.ConnectionFailed, "The remote certificate is invalid.");
        var exitCode = _subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Contain(" ERROR CLI-MQTT-CONNECT-FAILED command=ParseDWD ");
        _stderr.ToString().Should().Contain("The remote certificate is invalid.");
    }

    /// <summary>
    /// Verifies that nothing to publish results in no connection and exit code 0.
    /// </summary>
    [TestMethod]
    public void Command_NothingToPublish_NeverConnects_ExitSuccess()
    {
        SetupRun(0);
        var exitCode = _subject.Execute(_request, _stdout, _stderr);

        exitCode.Should().Be(0);
        _stderr.ToString().Should().Contain("published=0 failed=0 not_attempted=0");
    }
}
