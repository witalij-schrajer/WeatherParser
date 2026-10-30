using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Cli;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Mqtt;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Tests.Mqtt;

[TestClass]
[TestCategory("T-P3-14")]
public sealed class MqttPublisherDeadlineTests
{
    private static readonly TimeSpan Ten = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan One = TimeSpan.FromSeconds(1);
    private MockRepository _mocks = null!;
    private Mock<IMqttClientAdapter> _adapter = null!;
    private ManualTimeProvider _time = null!;
    private StringWriter _stderr = null!;
    private ILoggerFactory _loggerFactory = null!;
    private MqttPublicationSettings _settings = null!;
    private MqttPublisher _subject = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _adapter = _mocks.Create<IMqttClientAdapter>();
        _time = new ManualTimeProvider();
        _stderr = new StringWriter();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new StandardErrorLoggerProvider(_stderr, TimeProvider.System, new LogLineFormatter(), LogLevel.Information, "ParseDWD")));
        _settings = new MqttPublicationSettings(
            new MqttConnectionSettings("broker.example.invalid", 8883, MqttProtocol.V500, "weatherparser0123456789", TimeSpan.FromSeconds(30), true, null, null),
            new MqttPublicationLimits(Ten, Ten, 1, One));
        _subject = new MqttPublisher(_adapter.Object, _time, _loggerFactory.CreateLogger<MqttPublisher>());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
        _loggerFactory.Dispose();
        _stderr.Dispose();
    }

    private static MqttMessage Message(string topic) => new(topic, new byte[] { 1 }, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation);
    private static MqttPublishAcknowledgment Success() => new(true, "Success");
    private void SetupConnect() => _adapter.Setup(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    private void SetupDisconnect() => _adapter.Setup(a => a.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    private Task<PublicationReport> Publish(params MqttMessage[] messages) => _subject.PublishAllAsync(messages, _settings, CancellationToken.None);

    [TestMethod]
    [Timeout(10000)]
    public async Task PublishTimeout_AcknowledgedJustBeforeTheDeadline_Published()
    {
        SetupConnect();
        SetupDisconnect();
        var acknowledgment = new TaskCompletionSource<MqttPublishAcknowledgment>(TaskCreationOptions.RunContinuationsAsynchronously);
        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).Returns(acknowledgment.Task);
        var publication = Publish(Message("t/1"));
        await _time.WaitForTimersAsync(1);
        _time.Advance(Ten - TimeSpan.FromMilliseconds(1));
        await Task.Delay(50);
        publication.IsCompleted.Should().BeFalse();
        acknowledgment.SetResult(Success());
        (await publication).Should().Be(new PublicationReport(1, 0, 0));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task PublishTimeout_DeadlineReached_FailsWithPublishTimeout()
    {
        SetupConnect();
        SetupDisconnect();
        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).Returns(new TaskCompletionSource<MqttPublishAcknowledgment>().Task);
        var publication = Publish(Message("t/1"));
        await _time.WaitForTimersAsync(1);
        _time.Advance(Ten);
        (await publication).Should().Be(new PublicationReport(0, 1, 0));
        _stderr.ToString().Should().Contain(" ERROR CLI-MQTT-PUBLISH-TIMEOUT command=ParseDWD ");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ConnectTimeout_NotReached_StillWaiting_ThenFailsAtTheDeadline()
    {
        _adapter.Setup(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).Returns(new TaskCompletionSource().Task);
        var publication = Publish(Message("t/1"));
        await _time.WaitForTimersAsync(1);
        _time.Advance(Ten - TimeSpan.FromMilliseconds(1));
        await Task.Delay(50);
        publication.IsCompleted.Should().BeFalse();
        _time.Advance(TimeSpan.FromMilliseconds(1));
        (await ((Func<Task>)(() => publication)).Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-MQTT-CONNECT-FAILED");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RetryDelay_NextAttemptOnlyAfterTheDelay()
    {
        _settings = _settings with { Limits = new MqttPublicationLimits(Ten, Ten, 2, One) };
        SetupConnect();
        SetupDisconnect();
        _adapter.Setup(a => a.IsConnected).Returns(true);
        _adapter.SetupSequence(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new MqttTransportException(MqttTransportFailure.ConnectionLost, "Lost.")).ReturnsAsync(Success());
        var publication = Publish(Message("t/1"));
        await _time.WaitForTimersAsync(1);
        _time.Advance(One - TimeSpan.FromMilliseconds(1));
        await Task.Delay(50);
        _adapter.Verify(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _time.Advance(TimeSpan.FromMilliseconds(1));
        (await publication).Should().Be(new PublicationReport(1, 0, 0));
        _adapter.Verify(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
