using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
public sealed class MqttPublisherRetryTests
{
    private static readonly TimeSpan Ten = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan One = TimeSpan.FromSeconds(1);
    private MockRepository _mocks = null!;
    private Mock<IMqttClientAdapter> _adapter = null!;
    private RecordingTimeProvider _time = null!;
    private StringWriter _stderr = null!;
    private ILoggerFactory _loggerFactory = null!;
    private MqttPublicationSettings _settings = null!;
    private MqttPublisher _subject = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _adapter = _mocks.Create<IMqttClientAdapter>();
        _time = new RecordingTimeProvider();
        _stderr = new StringWriter();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new StandardErrorLoggerProvider(_stderr, TimeProvider.System, new LogLineFormatter(), LogLevel.Information, "ParseDWD")));
        _settings = new MqttPublicationSettings(
            new MqttConnectionSettings("broker.example.invalid", 8883, MqttProtocol.V500, "weatherparser0123456789", TimeSpan.FromSeconds(30), true, null, null),
            new MqttPublicationLimits(Ten, Ten, 3, One));
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
    private static Task Never() => new TaskCompletionSource().Task;
    private static Task<MqttPublishAcknowledgment> NeverAcknowledged() => new TaskCompletionSource<MqttPublishAcknowledgment>().Task;
    private static MqttPublishAcknowledgment Success() => new(true, "Success");
    private static MqttTransportException Failed() => new(MqttTransportFailure.ConnectionFailed, "No connection could be made.");
    private static MqttTransportException Lost() => new(MqttTransportFailure.ConnectionLost, "The connection was lost.");
    private static MqttTransportException Refused() => new(MqttTransportFailure.ConnectionRefused, "Not authorized.");
    private void SetupConnect() => _adapter.Setup(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    private void SetupDisconnect() => _adapter.Setup(a => a.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    private Task<PublicationReport> Publish(params MqttMessage[] messages) => _subject.PublishAllAsync(messages, _settings, CancellationToken.None);

    [TestMethod]
    [Timeout(10000)]
    public async Task Connect_Timeout_FailsWithMqttConnectFailed()
    {
        _adapter.Setup(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).Returns(Never);
        (await ((Func<Task>)(() => Publish(Message("t/1")))).Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-MQTT-CONNECT-FAILED");
        _adapter.Verify(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>()), Times.Exactly(3));
        _time.DueTimes.Should().Equal(Ten, One, Ten, One, Ten);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Connect_FailsTwiceThenSucceeds_Published()
    {
        _adapter.SetupSequence(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).ThrowsAsync(Failed()).ThrowsAsync(Failed()).Returns(Task.CompletedTask);
        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(Success());
        SetupDisconnect();
        var report = await Publish(Message("t/1"));
        report.Should().Be(new PublicationReport(1, 0, 0));
        _time.DueTimes.Count(d => d == One).Should().Be(2);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Connect_Refused_NotRetried()
    {
        _adapter.Setup(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).ThrowsAsync(Refused());
        (await ((Func<Task>)(() => Publish(Message("t/1")))).Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-MQTT-CONNECT-FAILED");
        _adapter.Verify(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Connect_AttemptsExhausted_FailsWithMqttConnectFailed()
    {
        _adapter.Setup(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).ThrowsAsync(Failed());
        (await ((Func<Task>)(() => Publish(Message("t/1")))).Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-MQTT-CONNECT-FAILED");
        _adapter.Verify(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Publish_Timeout_RetriedWithinBound()
    {
        SetupConnect();
        SetupDisconnect();
        _adapter.Setup(a => a.IsConnected).Returns(true);
        _adapter.SetupSequence(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).Returns(NeverAcknowledged()).ReturnsAsync(Success());
        var report = await Publish(Message("t/1"));
        report.Should().Be(new PublicationReport(1, 0, 0));
        _adapter.Verify(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _time.DueTimes.Should().Contain(One);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Publish_AttemptsExhausted_FailsWithMqttPublishTimeout()
    {
        SetupConnect();
        SetupDisconnect();
        _adapter.Setup(a => a.IsConnected).Returns(true);
        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).Returns(NeverAcknowledged());
        var report = await Publish(Message("t/1"));
        report.Should().Be(new PublicationReport(0, 1, 0));
        _adapter.Verify(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _stderr.ToString().Should().Contain(" ERROR CLI-MQTT-PUBLISH-TIMEOUT command=ParseDWD ").And.Contain("t/1");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Publish_AcknowledgedMessage_NeverRepublished()
    {
        var first = Message("t/1");
        var second = Message("t/2");
        var third = Message("t/3");
        _adapter.Setup(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _adapter.Setup(a => a.IsConnected).Returns(false);
        _adapter.Setup(a => a.PublishAsync(first, It.IsAny<CancellationToken>())).ReturnsAsync(Success());
        _adapter.SetupSequence(a => a.PublishAsync(second, It.IsAny<CancellationToken>())).ThrowsAsync(Lost()).ReturnsAsync(Success());
        _adapter.Setup(a => a.PublishAsync(third, It.IsAny<CancellationToken>())).ReturnsAsync(Success());
        SetupDisconnect();
        var report = await Publish(first, second, third);
        report.Should().Be(new PublicationReport(3, 0, 0));
        _adapter.Verify(a => a.PublishAsync(first, It.IsAny<CancellationToken>()), Times.Once);
        _adapter.Verify(a => a.PublishAsync(second, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _adapter.Verify(a => a.PublishAsync(third, It.IsAny<CancellationToken>()), Times.Once);
        _adapter.Verify(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Publish_ConnectionLostForGood_RemainingNotAttempted()
    {
        _adapter.SetupSequence(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).ThrowsAsync(Failed()).ThrowsAsync(Failed());
        _adapter.Setup(a => a.IsConnected).Returns(false);
        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(Lost());
        SetupDisconnect();
        var report = await Publish(Message("t/1"), Message("t/2"), Message("t/3"));
        report.Should().Be(new PublicationReport(0, 1, 2));
        _adapter.Verify(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _stderr.ToString().Should().Contain(" ERROR CLI-MQTT-PUBLISH-FAILED command=ParseDWD ");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Publish_ReconnectRefused_RemainingNotAttempted()
    {
        _adapter.SetupSequence(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).ThrowsAsync(Refused());
        _adapter.Setup(a => a.IsConnected).Returns(false);
        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(Lost());
        SetupDisconnect();
        var report = await Publish(Message("t/1"), Message("t/2"));
        report.Should().Be(new PublicationReport(0, 1, 1));
        _adapter.Verify(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Publish_AuthorizationFailure_NotRetried()
    {
        SetupConnect();
        SetupDisconnect();
        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(new MqttPublishAcknowledgment(false, "NotAuthorized"));
        var report = await Publish(Message("t/1"));
        report.Should().Be(new PublicationReport(0, 1, 0));
        _adapter.Verify(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Publish_V5FailureReasonCode_NotRetried_MessageRefused()
    {
        SetupConnect();
        SetupDisconnect();
        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(new MqttPublishAcknowledgment(false, "QuotaExceeded"));
        var report = await Publish(Message("t/1"));
        report.Should().Be(new PublicationReport(0, 1, 0));
        _adapter.Verify(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _stderr.ToString().Should().Contain(" ERROR CLI-MQTT-MESSAGE-REFUSED command=ParseDWD ").And.Contain("QuotaExceeded");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Delays_UseInjectedTimeProvider()
    {
        _settings = _settings with { Limits = new MqttPublicationLimits(Ten, Ten, 3, TimeSpan.FromSeconds(30)) };
        _adapter.SetupSequence(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).ThrowsAsync(Failed()).ThrowsAsync(Failed()).Returns(Task.CompletedTask);
        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(Success());
        SetupDisconnect();
        var watch = Stopwatch.StartNew();
        var report = await Publish(Message("t/1"));
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
        report.Should().Be(new PublicationReport(1, 0, 0));
        _time.DueTimes.Count(d => d == TimeSpan.FromSeconds(30)).Should().Be(2);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Cancellation_External_Propagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await ((Func<Task>)(() => _subject.PublishAllAsync(new[] { Message("t/1") }, _settings, cancellation.Token))).Should().ThrowAsync<OperationCanceledException>();
    }
}
