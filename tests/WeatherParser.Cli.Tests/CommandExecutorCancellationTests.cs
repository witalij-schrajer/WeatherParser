using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Lifetime;
using WeatherParser.Cli.Mqtt;
using WeatherParser.Cli.Output;
using WeatherParser.Cli.Tests.Configuration;
using WeatherParser.Cli.Tests.Lifetime;
using WeatherParser.Cli.Tests.Mqtt;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Tests the cancellation behavior of CommandExecutor.
/// </summary>
[TestClass]
[TestCategory("T-P3-18")]
public sealed class CommandExecutorCancellationTests
{
    private static readonly TimeSpan Deadline = TimeSpan.FromMinutes(2);
    private MockRepository _mocks = null!;
    private Mock<IConfigurationLoader> _loader = null!;
    private Mock<IConfigurationRegistrar> _registrar = null!;
    private Mock<ICommandRegistrar> _commandRegistrar = null!;
    private Mock<IConfigurationValidator> _validator = null!;
    private Mock<ILibraryDeadlineResolver> _deadlineResolver = null!;
    private Mock<ICommandHandler<ParseDwdRequest>> _handler = null!;
    private Mock<IMqttClientAdapter> _adapter = null!;
    private TestShutdownSignal _signal = null!;
    private ManualTimeProvider _time = null!;
    private PublicationQueue _queue = null!;
    private StringWriter _stdout = null!;
    private StringWriter _stderr = null!;
    private CliConfiguration _configuration = null!;
    private CommandExecutor _subject = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _loader = _mocks.Create<IConfigurationLoader>();
        _registrar = _mocks.Create<IConfigurationRegistrar>();
        _commandRegistrar = _mocks.Create<ICommandRegistrar>();
        _validator = _mocks.Create<IConfigurationValidator>();
        _deadlineResolver = _mocks.Create<ILibraryDeadlineResolver>();
        _handler = _mocks.Create<ICommandHandler<ParseDwdRequest>>();
        _adapter = _mocks.Create<IMqttClientAdapter>();
        _signal = new TestShutdownSignal();
        _time = new ManualTimeProvider();
        _queue = new PublicationQueue();
        _stdout = new StringWriter();
        _stderr = new StringWriter();
        _configuration = TestConfigurations.Valid();
        _configuration.Limits = new LimitsSection { CommandDeadline = Deadline, MaxPublishAttempts = 1, RetryDelay = TimeSpan.Zero };
        _subject = new CommandExecutor(_loader.Object, _registrar.Object, _commandRegistrar.Object, _validator.Object, new ExitCodePolicy(), new LibraryErrors(), new LogLineFormatter(), _signal, _deadlineResolver.Object, _time);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
        _signal.Dispose();
        _stdout.Dispose();
        _stderr.Dispose();
    }

    private static ParseDwdRequest Request(OutputFormat output) => new(new CommonOptions(output, null, "c.json", false), "00044");

    // Everything up to the handler succeeds; the library deadline of the command is one minute unless a test overrides it.
    private void SetupValidRun(ParseDwdRequest request, TimeSpan? libraryDeadline = null)
    {
        _loader.Setup(l => l.Load(request.Common.ConfigPath)).Returns(_configuration);
        _registrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), _configuration, request)).Callback<IServiceCollection, CliConfiguration, CommandRequest>((services, configuration, r) => new ConfigurationRegistrar().Register(services, configuration, r));
        _commandRegistrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), request)).Callback<IServiceCollection, CommandRequest>((services, _) =>
        {
            services.AddSingleton(_handler.Object);
            services.AddSingleton<IPublicationQueue>(_queue);
            services.AddSingleton(_adapter.Object);
            services.AddSingleton<IMessagePublisher, MqttPublisher>();
            services.AddSingleton<IMqttPublicationSettingsFactory, MqttPublicationSettingsFactory>();
        });
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, request)).Returns(new ValidatedSettings("00044", null));
        _deadlineResolver.Setup(d => d.Resolve(It.IsAny<IServiceProvider>(), request)).Returns(libraryDeadline ?? TimeSpan.FromMinutes(1));
    }

    private void Queue(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _queue.Add(new MqttMessage($"t/{i}", new byte[] { 1 }, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation), "dwd:00044");
        }
    }

    private void SetupConnectAndDisconnect()
    {
        _adapter.Setup(a => a.ConnectAsync(It.IsAny<MqttConnectionSettings>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _adapter.Setup(a => a.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Tests that the constructor throws when any dependency is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullSignalResolverOrTimeProvider_Throws()
    {
        Action nullSignal = () => new CommandExecutor(_loader.Object, _registrar.Object, _commandRegistrar.Object, _validator.Object, new ExitCodePolicy(), new LibraryErrors(), new LogLineFormatter(), null!, _deadlineResolver.Object, _time);
        Action nullDeadlineResolver = () => new CommandExecutor(_loader.Object, _registrar.Object, _commandRegistrar.Object, _validator.Object, new ExitCodePolicy(), new LibraryErrors(), new LogLineFormatter(), _signal, null!, _time);
        Action nullTimeProvider = () => new CommandExecutor(_loader.Object, _registrar.Object, _commandRegistrar.Object, _validator.Object, new ExitCodePolicy(), new LibraryErrors(), new LogLineFormatter(), _signal, _deadlineResolver.Object, null!);

        nullSignal.Should().Throw<ArgumentNullException>().WithParameterName("shutdownSignal");
        nullDeadlineResolver.Should().Throw<ArgumentNullException>().WithParameterName("deadlineResolver");
        nullTimeProvider.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    /// <summary>
    /// Tests that cancellation during retrieval results in exit code 4 and CLI-CANCELLED error.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public void Cancel_DuringRetrieval_ExitCancelled_NoDocument()
    {
        var request = Request(OutputFormat.Json);
        SetupValidRun(request);

        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .Returns<ParseDwdRequest, CommandContext, CancellationToken>(async (_, _, token) =>
            {
                _signal.Raise();
                await Task.Delay(Timeout.Infinite, token);
                return new CommandOutcome(1, 0, false);
            });

        var exitCode = _subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(4);
        _stderr.ToString().Should().Contain(" ERROR CLI-CANCELLED command=ParseDWD ");
        _stderr.ToString().Should().NotContain("CLI-DEADLINE-EXCEEDED");
        _stdout.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that cancellation during publication with no success claims results in exit code 4 and summary logged.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public void Cancel_DuringPublication_NoSuccessClaim_SummaryLogged()
    {
        var request = Request(OutputFormat.Mqtt);
        SetupValidRun(request);

        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => Queue(1))
            .ReturnsAsync(new CommandOutcome(1, 0, false));

        SetupConnectAndDisconnect();

        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>()))
            .Returns<MqttMessage, CancellationToken>(async (_, token) =>
            {
                _signal.Raise();
                await Task.Delay(Timeout.Infinite, token);
                return new MqttPublishAcknowledgment(true, "Success");
            });

        var exitCode = _subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(4);
        _stderr.ToString().Should().Contain("Publication summary: published=0 failed=0 not_attempted=1.");
        _stderr.ToString().Should().Contain(" ERROR CLI-CANCELLED command=ParseDWD ");
    }

    /// <summary>
    /// Tests that deadline exceeded results in exit code 1 and CLI-DEADLINE-EXCEEDED error.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public void Deadline_Exceeded_ExitPerCl11_WithDeadlineId()
    {
        var request = Request(OutputFormat.Json);
        SetupValidRun(request);

        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .Returns<ParseDwdRequest, CommandContext, CancellationToken>(async (_, _, token) =>
            {
                _time.Advance(Deadline - TimeSpan.FromTicks(1));
                token.IsCancellationRequested.Should().BeFalse();
                _time.Advance(TimeSpan.FromTicks(1));
                await Task.Delay(Timeout.Infinite, token);
                return new CommandOutcome(1, 0, false);
            });

        var exitCode = _subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Contain(" ERROR CLI-DEADLINE-EXCEEDED command=ParseDWD ");
        _stderr.ToString().Should().NotContain("CLI-CANCELLED");
        _stdout.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that deadline exceeded during publication with one published item results in exit code 3.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public void Deadline_DuringPublication_AfterOnePublished_ExitPartial()
    {
        var request = Request(OutputFormat.Mqtt);
        SetupValidRun(request);

        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => Queue(2))
            .ReturnsAsync(new CommandOutcome(2, 0, false));

        SetupConnectAndDisconnect();

        _adapter.Setup(a => a.PublishAsync(It.Is<MqttMessage>(m => m.Topic == "t/0"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttPublishAcknowledgment(true, "Success"));

        _adapter.Setup(a => a.PublishAsync(It.Is<MqttMessage>(m => m.Topic == "t/1"), It.IsAny<CancellationToken>()))
            .Returns<MqttMessage, CancellationToken>(async (_, token) =>
            {
                _time.Advance(Deadline);
                await Task.Delay(Timeout.Infinite, token);
                return new MqttPublishAcknowledgment(true, "Success");
            });

        var exitCode = _subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(3);
        _stderr.ToString().Should().Contain("Publication summary: published=1 failed=0 not_attempted=1.");
        _stderr.ToString().Should().Contain(" ERROR CLI-DEADLINE-EXCEEDED command=ParseDWD ");
    }

    /// <summary>
    /// Tests that deadline exceeded during publication with no published items results in exit code 1.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public void Deadline_DuringPublication_NothingPublished_ExitFailure()
    {
        var request = Request(OutputFormat.Mqtt);
        SetupValidRun(request);

        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => Queue(1))
            .ReturnsAsync(new CommandOutcome(1, 0, false));

        SetupConnectAndDisconnect();

        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>()))
            .Returns<MqttMessage, CancellationToken>(async (_, token) =>
            {
                _time.Advance(Deadline);
                await Task.Delay(Timeout.Infinite, token);
                return new MqttPublishAcknowledgment(true, "Success");
            });

        var exitCode = _subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Contain("Publication summary: published=0 failed=0 not_attempted=1.");
    }

    /// <summary>
    /// Tests that signal cancellation wins over deadline exceeded.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public void SignalAndDeadline_SignalWins_ExitCancelled()
    {
        var request = Request(OutputFormat.Json);
        SetupValidRun(request);

        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .Returns<ParseDwdRequest, CommandContext, CancellationToken>(async (_, _, token) =>
            {
                _signal.Raise();
                _time.Advance(Deadline);
                await Task.Delay(Timeout.Infinite, token);
                return new CommandOutcome(1, 0, false);
            });

        var exitCode = _subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(4);
        _stderr.ToString().Should().Contain(" ERROR CLI-CANCELLED command=ParseDWD ");
        _stderr.ToString().Should().NotContain("CLI-DEADLINE-EXCEEDED");
    }

    /// <summary>
    /// Tests that the cancellation token is passed to handler and publisher.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public void Token_PassedToHandlerAndPublisher_LinkedToSignal()
    {
        var request = Request(OutputFormat.Mqtt);
        SetupValidRun(request);

        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .Returns<ParseDwdRequest, CommandContext, CancellationToken>((_, _, token) =>
            {
                token.CanBeCanceled.Should().BeTrue();
                token.IsCancellationRequested.Should().BeFalse();
                Queue(1);
                _signal.Raise();
                token.IsCancellationRequested.Should().BeTrue();
                return Task.FromResult(new CommandOutcome(1, 0, false));
            });

        var exitCode = _subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(4);
        _stderr.ToString().Should().Contain("Publication summary: published=0 failed=0 not_attempted=1.");
    }

    /// <summary>
    /// Tests that cancellation after output completion still reports the completed outcome.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public void Cancel_AfterOutputCompleted_StillReportsCompletedOutcome()
    {
        var request = Request(OutputFormat.Json);
        SetupValidRun(request);

        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => _signal.Raise())
            .ReturnsAsync(new CommandOutcome(1, 0, false));

        var exitCode = _subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(0);
        _stderr.ToString().Should().NotContain("CLI-CANCELLED");
    }

    /// <summary>
    /// Tests that the shutdown grace period is configured from limits.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public void Execute_ConfiguresGracePeriodFromLimits()
    {
        _configuration.Limits!.ShutdownGracePeriod = TimeSpan.FromSeconds(7);
        var request = Request(OutputFormat.Json);
        SetupValidRun(request);

        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandOutcome(1, 0, false));

        var exitCode = _subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(0);
        _signal.GracePeriod.Should().Be(TimeSpan.FromSeconds(7));
        _signal.Logger.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the default shutdown grace period is 5 seconds.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public void Execute_NoGracePeriodConfigured_DefaultFiveSeconds()
    {
        var request = Request(OutputFormat.Json);
        SetupValidRun(request);

        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandOutcome(1, 0, false));

        var exitCode = _subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(0);
        _signal.GracePeriod.Should().Be(ShutdownSignal.DefaultGracePeriod);
    }

    /// <summary>
    /// Tests that a warning is logged when the command deadline is shorter than the library deadline.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public void Deadline_ShorterThanLibraryDeadline_WarningLogged()
    {
        var request = Request(OutputFormat.Json);
        SetupValidRun(request, TimeSpan.FromMinutes(3));

        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandOutcome(1, 0, false));

        var exitCode = _subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(0);
        _stderr.ToString().Should().Contain(" WARNING CLI-DEADLINE-EXCEEDED command=ParseDWD ");
        _stderr.ToString().Should().Contain("00:02:00");
        _stderr.ToString().Should().Contain("00:03:00");
    }

    /// <summary>
    /// Tests that no warning is logged when the command deadline is not shorter than the library deadline.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public void Deadline_NotShorterThanLibraryDeadline_NoWarning()
    {
        var request = Request(OutputFormat.Json);
        SetupValidRun(request, Deadline);

        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandOutcome(1, 0, false));

        var exitCode = _subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(0);
        _stderr.ToString().Should().NotContain("CLI-DEADLINE-EXCEEDED");
    }

    /// <summary>
    /// Tests that the default command deadline is 10 minutes.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public void Deadline_NotConfigured_DefaultTenMinutes()
    {
        _configuration.Limits!.CommandDeadline = null;
        var request = Request(OutputFormat.Json);
        SetupValidRun(request, TimeSpan.FromMinutes(11));

        _handler.Setup(h => h.HandleAsync(request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .Returns<ParseDwdRequest, CommandContext, CancellationToken>(async (_, _, token) =>
            {
                _time.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromTicks(1));
                token.IsCancellationRequested.Should().BeFalse();
                _time.Advance(TimeSpan.FromTicks(1));
                await Task.Delay(Timeout.Infinite, token);
                return new CommandOutcome(1, 0, false);
            });

        var exitCode = _subject.Execute(request, _stdout, _stderr);

        exitCode.Should().Be(1);
        _stderr.ToString().Should().Contain(" WARNING CLI-DEADLINE-EXCEEDED command=ParseDWD ");
        _stderr.ToString().Should().Contain(" ERROR CLI-DEADLINE-EXCEEDED command=ParseDWD ");
    }
}
