using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Mqtt;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Tests.Mqtt;

[TestClass]
[TestCategory("T-P3-14")]
public sealed class MqttPublisherTests
{
    private static readonly byte[] PayloadBytes = { 0x7B, 0xC3, 0x84, 0x7D };
    private MockRepository _mocks = null!;
    private Mock<IMqttClientAdapter> _adapter = null!;
    private StringWriter _stderr = null!;
    private ILoggerFactory _loggerFactory = null!;
    private MqttPublicationSettings _settings = null!;
    private MqttPublisher _subject = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _adapter = _mocks.Create<IMqttClientAdapter>();
        _stderr = new StringWriter();
        _loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug).AddProvider(new StandardErrorLoggerProvider(_stderr, TimeProvider.System, new LogLineFormatter(), LogLevel.Debug, "ParseDWD")));
        _settings = Settings(useTls: true, credentials: null);
        _subject = new MqttPublisher(_adapter.Object, TimeProvider.System, _loggerFactory.CreateLogger<MqttPublisher>());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
        _loggerFactory.Dispose();
        _stderr.Dispose();
    }

    private static MqttPublicationSettings Settings(bool useTls, BrokerCredentials? credentials) =>
        new(new MqttConnectionSettings("broker.example.invalid", useTls ? 8883 : 1883, MqttProtocol.V500, "weatherparser0123456789", TimeSpan.FromSeconds(30), useTls, null, credentials),
            new MqttPublicationLimits(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), 1, TimeSpan.Zero));

    private static MqttMessage Message(string topic, MqttMessageType type = MqttMessageType.Observation, MqttQualityOfService qos = MqttQualityOfService.AtLeastOnce, bool? retain = null) =>
        new(topic, PayloadBytes, qos, retain ?? (type is MqttMessageType.Observation or MqttMessageType.Forecast), type);

    private void SetupConnect() => _adapter.Setup(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    private void SetupDisconnect() => _adapter.Setup(a => a.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    private void SetupAcknowledgment(MqttMessage message, bool success, string reason = "Success") =>
        _adapter.Setup(a => a.PublishAsync(It.Is<MqttMessage>(m => ReferenceEquals(m, message)), It.IsAny<CancellationToken>())).ReturnsAsync(new MqttPublishAcknowledgment(success, reason));

    [TestMethod]
    public async Task Publish_EmptyList_NeverContactsTheBroker()
    {
        (await _subject.PublishAllAsync(Array.Empty<MqttMessage>(), _settings, CancellationToken.None)).Should().Be(new PublicationReport(0, 0, 0));
    }

    [TestMethod]
    public async Task Publish_SendsTopicPayloadQosRetainUnchanged()
    {
        var message = Message("haus/weather/dwd/00044");
        SetupConnect();
        SetupDisconnect();
        MqttMessage? sent = null;
        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).Callback<MqttMessage, CancellationToken>((m, _) => sent = m).ReturnsAsync(new MqttPublishAcknowledgment(true, "Success"));
        var report = await _subject.PublishAllAsync(new[] { message }, _settings, CancellationToken.None);
        report.Should().Be(new PublicationReport(1, 0, 0));
        sent.Should().BeSameAs(message);
        sent!.Topic.Should().Be("haus/weather/dwd/00044");
        sent.Payload.ToArray().Should().Equal(PayloadBytes);
        sent.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        sent.Retain.Should().BeTrue();
    }

    [TestMethod]
    public async Task Publish_SuccessOnlyAfterAcknowledgment()
    {
        SetupConnect();
        SetupDisconnect();
        var acknowledgment = new TaskCompletionSource<MqttPublishAcknowledgment>(TaskCreationOptions.RunContinuationsAsynchronously);
        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>())).Returns(acknowledgment.Task);
        var publication = _subject.PublishAllAsync(new[] { Message("t/1") }, _settings, CancellationToken.None);
        await Task.Delay(50);
        publication.IsCompleted.Should().BeFalse();
        acknowledgment.SetResult(new MqttPublishAcknowledgment(true, "Success"));
        (await publication).Should().Be(new PublicationReport(1, 0, 0));
    }

    [TestMethod]
    public async Task Publish_NegativeAcknowledgment_FailsWithMqttMessageRefused()
    {
        SetupConnect();
        SetupDisconnect();
        var message = Message("t/1");
        SetupAcknowledgment(message, false, "NotAuthorized");
        var report = await _subject.PublishAllAsync(new[] { message }, _settings, CancellationToken.None);
        report.Should().Be(new PublicationReport(0, 1, 0));
        _stderr.ToString().Should().Contain(" ERROR CLI-MQTT-MESSAGE-REFUSED command=ParseDWD ");
        _stderr.ToString().Should().Contain("t/1");
        _stderr.ToString().Should().Contain("NotAuthorized");
    }

    [TestMethod]
    public async Task Publish_NegativeAcknowledgment_RemainingMessagesStillPublished()
    {
        SetupConnect();
        SetupDisconnect();
        var first = Message("t/1");
        var second = Message("t/2");
        SetupAcknowledgment(first, false, "QuotaExceeded");
        SetupAcknowledgment(second, true);
        var report = await _subject.PublishAllAsync(new[] { first, second }, _settings, CancellationToken.None);
        report.Should().Be(new PublicationReport(1, 1, 0));
    }

    [DataRow(MqttQualityOfService.AtMostOnce)]
    [DataRow(MqttQualityOfService.ExactlyOnce)]
    [TestMethod]
    public async Task Publish_MessageWithQosOtherThanOne_RefusedBeforeConnect(MqttQualityOfService qos)
    {
        var act = () => _subject.PublishAllAsync(new[] { Message("t/1", qos: qos) }, _settings, CancellationToken.None);
        (await act.Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-UNEXPECTED");
    }

    [DataRow(MqttMessageType.CdcStationList)]
    [DataRow(MqttMessageType.MosmixLocationList)]
    [TestMethod]
    public async Task Publish_StationListRetained_RefusedBeforeConnect(MqttMessageType type)
    {
        var act = () => _subject.PublishAllAsync(new[] { Message("t/1", type, retain: true) }, _settings, CancellationToken.None);
        (await act.Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-UNEXPECTED");
    }

    [DataRow(MqttMessageType.Observation)]
    [DataRow(MqttMessageType.Forecast)]
    [TestMethod]
    public async Task Publish_ObservationNotRetained_RefusedBeforeConnect(MqttMessageType type)
    {
        var act = () => _subject.PublishAllAsync(new[] { Message("t/1", type, retain: false) }, _settings, CancellationToken.None);
        (await act.Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-UNEXPECTED");
    }

    [TestMethod]
    public async Task Publish_InvalidMessageAfterValidOne_NothingConnected()
    {
        var act = () => _subject.PublishAllAsync(new[] { Message("t/1"), Message("t/2", MqttMessageType.CdcStationList, retain: true) }, _settings, CancellationToken.None);
        (await act.Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-UNEXPECTED");
    }

    [TestMethod]
    public async Task Connect_Unencrypted_LogsOneWarning()
    {
        _settings = Settings(useTls: false, credentials: null);
        SetupConnect();
        var first = Message("t/1");
        var second = Message("t/2");
        SetupAcknowledgment(first, true);
        SetupAcknowledgment(second, true);
        SetupDisconnect();
        var report = await _subject.PublishAllAsync(new[] { first, second }, _settings, CancellationToken.None);
        report.Should().Be(new PublicationReport(2, 0, 0));
        Regex.Matches(_stderr.ToString(), " WARNING ").Count.Should().Be(1);
        _stderr.ToString().Should().Contain("unencrypted");
    }

    [TestMethod]
    public async Task Connect_Tls_NoWarning()
    {
        SetupConnect();
        SetupDisconnect();
        var message = Message("t/1");
        SetupAcknowledgment(message, true);
        var report = await _subject.PublishAllAsync(new[] { message }, _settings, CancellationToken.None);
        report.Should().Be(new PublicationReport(1, 0, 0));
        _stderr.ToString().Should().NotContain(" WARNING ");
    }

    [TestMethod]
    public async Task Connect_Failure_FailsWithMqttConnectFailed_NothingPublished()
    {
        _adapter.Setup(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).ThrowsAsync(new MqttTransportException(MqttTransportFailure.ConnectionFailed, "No connection could be made."));
        var act = () => _subject.PublishAllAsync(new[] { Message("t/1") }, _settings, CancellationToken.None);
        (await act.Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-MQTT-CONNECT-FAILED");
    }

    [TestMethod]
    public async Task Connect_CredentialsNeverLogged()
    {
        _settings = Settings(useTls: true, credentials: new BrokerCredentials("marker-user-4711", "marker-pass-4711"));
        _adapter.Setup(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).ThrowsAsync(new MqttTransportException(MqttTransportFailure.ConnectionRefused, "Not authorized."));
        var act = () => _subject.PublishAllAsync(new[] { Message("t/1") }, _settings, CancellationToken.None);
        var exception = (await act.Should().ThrowAsync<CliException>()).Which;
        exception.ErrorId.Should().Be("CLI-MQTT-CONNECT-FAILED");
        exception.Message.Should().NotContain("marker-user-4711").And.NotContain("marker-pass-4711");
        _stderr.ToString().Should().NotContain("marker-user-4711").And.NotContain("marker-pass-4711");
        _settings.Connection.ToString().Should().NotContain("marker-user-4711").And.NotContain("marker-pass-4711");
    }

    [TestMethod]
    public async Task Disconnect_Failure_DoesNotChangeTheReport()
    {
        SetupConnect();
        var message = Message("t/1");
        SetupAcknowledgment(message, true);
        _adapter.Setup(a => a.DisconnectAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new MqttTransportException(MqttTransportFailure.ConnectionLost, "Lost."));
        var report = await _subject.PublishAllAsync(new[] { message }, _settings, CancellationToken.None);
        report.Should().Be(new PublicationReport(1, 0, 0));
    }

    [TestMethod]
    public void Constructor_NullArguments_Throw()
    {
        ((Action)(() => new MqttPublisher(null!, TimeProvider.System, _loggerFactory.CreateLogger<MqttPublisher>()))).Should().Throw<ArgumentNullException>().WithParameterName("adapter");
        ((Action)(() => new MqttPublisher(_adapter.Object, null!, _loggerFactory.CreateLogger<MqttPublisher>()))).Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
        ((Action)(() => new MqttPublisher(_adapter.Object, TimeProvider.System, null!))).Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [TestMethod]
    public async Task Publish_NullArguments_Throw()
    {
        await _subject.Invoking(s => s.PublishAllAsync(null!, _settings, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("messages");

        await _subject.Invoking(s => s.PublishAllAsync(Array.Empty<MqttMessage>(), null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("settings");
    }
}
