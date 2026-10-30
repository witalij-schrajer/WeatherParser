using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MQTTnet;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using WeatherParser.Cli.Mqtt;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Tests.Mqtt;

[TestClass]
[TestCategory("T-P3-14")]
public sealed class MqttNetClientAdapterTests
{
    private MockRepository _mocks = null!;
    private Mock<IMqttClient> _client = null!;
    private Mock<IMqttNetOptionsBuilder> _builder = null!;
    private RecordingTimeProvider _time = null!;
    private MqttNetClientAdapter _subject = null!;

    private static readonly MqttConnectionSettings Settings = new("broker.example.invalid", 8883, MqttProtocol.V500, "weatherparser0123456789", TimeSpan.FromSeconds(30), true, null, null);
    private readonly MqttClientOptions _options = new();
    private readonly MqttApplicationMessage _applicationMessage = new();
    private static readonly MqttMessage Message = new("t/1", new byte[] { 1 }, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation);

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _client = _mocks.Create<IMqttClient>();
        _builder = _mocks.Create<IMqttNetOptionsBuilder>();
        _time = new RecordingTimeProvider();
        _subject = new MqttNetClientAdapter(_client.Object, _builder.Object, _time);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    [TestMethod]
    public async Task Connect_PassesOptionsAndToken()
    {
        using var cancellation = new CancellationTokenSource();

        _builder.Setup(b => b.BuildClientOptions(Settings)).Returns(_options);
        _client.Setup(c => c.ConnectAsync(_options, cancellation.Token))
            .ReturnsAsync(new MqttClientConnectResult { ResultCode = MqttClientConnectResultCode.Success });

        await _subject.ConnectAsync(Settings, cancellation.Token);
    }

    [DataRow(MqttClientConnectResultCode.NotAuthorized)]
    [DataRow(MqttClientConnectResultCode.BadUserNameOrPassword)]
    [DataRow(MqttClientConnectResultCode.Banned)]
    [DataRow(MqttClientConnectResultCode.BadAuthenticationMethod)]
    [DataRow(MqttClientConnectResultCode.ClientIdentifierNotValid)]
    [TestMethod]
    public async Task Connect_RefusedResultCode_ConnectionRefused(MqttClientConnectResultCode code)
    {
        var act = async () => await _subject.ConnectAsync(Settings, CancellationToken.None);

        _builder.Setup(b => b.BuildClientOptions(Settings)).Returns(_options);
        _client.Setup(c => c.ConnectAsync(_options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult { ResultCode = code });

        (await act.Should().ThrowAsync<MqttTransportException>())
            .Which.Failure.Should().Be(MqttTransportFailure.ConnectionRefused);
    }

    [TestMethod]
    public async Task Connect_OtherResultCode_ConnectionFailed()
    {
        var act = async () => await _subject.ConnectAsync(Settings, CancellationToken.None);

        _builder.Setup(b => b.BuildClientOptions(Settings)).Returns(_options);
        _client.Setup(c => c.ConnectAsync(_options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientConnectResult { ResultCode = MqttClientConnectResultCode.ServerUnavailable });

        (await act.Should().ThrowAsync<MqttTransportException>())
            .Which.Failure.Should().Be(MqttTransportFailure.ConnectionFailed);
    }

    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [TestMethod]
    public async Task Connect_TransportException_ConnectionFailed(int kind)
    {
        Exception exception = kind switch
        {
            0 => new MqttCommunicationException("broken", new IOException("broken")),
            1 => new SocketException(10061),
            2 => new IOException("broken"),
            _ => new AuthenticationException("The remote certificate is invalid.")
        };

        var act = async () => await _subject.ConnectAsync(Settings, CancellationToken.None);

        _builder.Setup(b => b.BuildClientOptions(Settings)).Returns(_options);
        _client.Setup(c => c.ConnectAsync(_options, It.IsAny<CancellationToken>()))
            .Returns(Task.FromException<MqttClientConnectResult>(exception));

        var transport = (await act.Should().ThrowAsync<MqttTransportException>()).Which;
        transport.Failure.Should().Be(MqttTransportFailure.ConnectionFailed);
        transport.Message.Should().Contain(exception.Message);
    }

    [TestMethod]
    public async Task Connect_Cancelled_Propagates()
    {
        var act = async () => await _subject.ConnectAsync(Settings, CancellationToken.None);

        _builder.Setup(b => b.BuildClientOptions(Settings)).Returns(_options);
        _client.Setup(c => c.ConnectAsync(_options, It.IsAny<CancellationToken>()))
            .Returns(Task.FromException<MqttClientConnectResult>(new OperationCanceledException()));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task Connect_PendingDisconnect_WaitsAndConnects()
    {
        _builder.Setup(b => b.BuildClientOptions(Settings)).Returns(_options);
        _client.SetupSequence(c => c.ConnectAsync(_options, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Not allowed to connect while connect/disconnect is pending."))
            .ReturnsAsync(new MqttClientConnectResult { ResultCode = MqttClientConnectResultCode.Success });

        await _subject.ConnectAsync(Settings, CancellationToken.None);

        _client.Verify(c => c.ConnectAsync(_options, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _time.DueTimes.Should().Contain(TimeSpan.FromMilliseconds(50));
    }

    [TestMethod]
    public async Task Connect_PendingDisconnectUntilCancelled_Propagates()
    {
        using var cancellation = new CancellationTokenSource();
        var calls = 0;

        _builder.Setup(b => b.BuildClientOptions(Settings)).Returns(_options);
        _client.Setup(c => c.ConnectAsync(_options, It.IsAny<CancellationToken>()))
            .Callback(() => { if (++calls == 3) { cancellation.Cancel(); } })
            .ThrowsAsync(new InvalidOperationException("Not allowed to connect while connect/disconnect is pending."));

        await ((Func<Task>)(() => _subject.ConnectAsync(Settings, cancellation.Token)))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task Publish_FailureAfterCancellation_IsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        _builder.Setup(b => b.BuildMessage(Message)).Returns(_applicationMessage);
        _client.Setup(c => c.PublishAsync(_applicationMessage, cancellation.Token))
            .ThrowsAsync(new MqttCommunicationTimedOutException());

        await ((Func<Task>)(() => _subject.PublishAsync(Message, cancellation.Token)))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task Publish_TimedOutWithoutCancellation_ConnectionLost()
    {
        _builder.Setup(b => b.BuildMessage(Message)).Returns(_applicationMessage);
        _client.Setup(c => c.PublishAsync(_applicationMessage, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MqttCommunicationTimedOutException());

        (await ((Func<Task>)(() => _subject.PublishAsync(Message, CancellationToken.None))).Should().ThrowAsync<MqttTransportException>())
            .Which.Failure.Should().Be(MqttTransportFailure.ConnectionLost);
    }

    [TestMethod]
    public async Task Connect_FailureAfterCancellation_IsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        _builder.Setup(b => b.BuildClientOptions(Settings)).Returns(_options);
        _client.Setup(c => c.ConnectAsync(_options, cancellation.Token))
            .ThrowsAsync(new MqttCommunicationException("broken", new IOException("broken")));

        await ((Func<Task>)(() => _subject.ConnectAsync(Settings, cancellation.Token)))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task Disconnect_FailureAfterCancellation_IsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        _client.Setup(c => c.IsConnected).Returns(true);
        _client.Setup(c => c.DisconnectAsync(
            It.IsAny<MqttClientDisconnectOptions>(),
            cancellation.Token))
            .ThrowsAsync(new MqttCommunicationException("broken", new IOException("broken")));

        await ((Func<Task>)(() => _subject.DisconnectAsync(cancellation.Token)))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task Adapter_PublishAwaitsAcknowledgment()
    {
        var acknowledgment = new TaskCompletionSource<MqttClientPublishResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        _builder.Setup(b => b.BuildMessage(Message)).Returns(_applicationMessage);
        _client.Setup(c => c.PublishAsync(_applicationMessage, It.IsAny<CancellationToken>())).Returns(acknowledgment.Task);

        var publication = _subject.PublishAsync(Message, CancellationToken.None);

        await Task.Delay(50);
        publication.IsCompleted.Should().BeFalse();

        acknowledgment.SetResult(Published(MqttClientPublishReasonCode.Success));

        (await publication).Should().Be(new MqttPublishAcknowledgment(true, "Success"));
    }

    [DataRow(MqttClientPublishReasonCode.NotAuthorized, false)]
    [DataRow(MqttClientPublishReasonCode.QuotaExceeded, false)]
    [DataRow(MqttClientPublishReasonCode.NoMatchingSubscribers, true)]
    [TestMethod]
    public async Task Publish_ReasonCode_Mapped(MqttClientPublishReasonCode code, bool success)
    {
        _builder.Setup(b => b.BuildMessage(Message)).Returns(_applicationMessage);
        _client.Setup(c => c.PublishAsync(_applicationMessage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Published(code));

        var result = await _subject.PublishAsync(Message, CancellationToken.None);

        result.Should().Be(new MqttPublishAcknowledgment(success, code.ToString()));
    }

    [TestMethod]
    public async Task Adapter_CancellationTokenPassedToClient()
    {
        using var cancellation = new CancellationTokenSource();

        _builder.Setup(b => b.BuildMessage(Message)).Returns(_applicationMessage);
        _client.Setup(c => c.PublishAsync(_applicationMessage, cancellation.Token))
            .ReturnsAsync(Published(MqttClientPublishReasonCode.Success));

        (await _subject.PublishAsync(Message, cancellation.Token))
            .Should().Be(new MqttPublishAcknowledgment(true, "Success"));
    }

    [TestMethod]
    public async Task Publish_NotConnected_ConnectionLost()
    {
        var act = async () => await _subject.PublishAsync(Message, CancellationToken.None);

        _builder.Setup(b => b.BuildMessage(Message)).Returns(_applicationMessage);
        _client.Setup(c => c.PublishAsync(_applicationMessage, It.IsAny<CancellationToken>()))
            .Returns(Task.FromException<MqttClientPublishResult>(new MqttClientNotConnectedException()));

        (await act.Should().ThrowAsync<MqttTransportException>())
            .Which.Failure.Should().Be(MqttTransportFailure.ConnectionLost);
    }

    [TestMethod]
    public async Task Publish_SocketFailure_ConnectionLost()
    {
        var act = async () => await _subject.PublishAsync(Message, CancellationToken.None);

        _builder.Setup(b => b.BuildMessage(Message)).Returns(_applicationMessage);
        _client.Setup(c => c.PublishAsync(_applicationMessage, It.IsAny<CancellationToken>()))
            .Returns(Task.FromException<MqttClientPublishResult>(new SocketException(10054)));

        (await act.Should().ThrowAsync<MqttTransportException>())
            .Which.Failure.Should().Be(MqttTransportFailure.ConnectionLost);
    }

    [TestMethod]
    public async Task Disconnect_Connected_NormalDisconnection()
    {
        using var cancellation = new CancellationTokenSource();

        _client.Setup(c => c.IsConnected).Returns(true);
        _client.Setup(c => c.DisconnectAsync(
            It.Is<MqttClientDisconnectOptions>(o => o.Reason == MqttClientDisconnectOptionsReason.NormalDisconnection),
            cancellation.Token))
            .Returns(Task.CompletedTask);

        await _subject.DisconnectAsync(cancellation.Token);
    }

    [TestMethod]
    public async Task Disconnect_NotConnected_NoCall()
    {
        _client.Setup(c => c.IsConnected).Returns(false);

        await _subject.DisconnectAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task Disconnect_Failure_ConnectionLost()
    {
        var act = async () => await _subject.DisconnectAsync(CancellationToken.None);

        _client.Setup(c => c.IsConnected).Returns(true);
        _client.Setup(c => c.DisconnectAsync(
            It.IsAny<MqttClientDisconnectOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.FromException<Exception>(new MqttCommunicationException("broken", new IOException("broken"))));

        (await act.Should().ThrowAsync<MqttTransportException>())
            .Which.Failure.Should().Be(MqttTransportFailure.ConnectionLost);
    }

    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public void IsConnected_ForwardsTheClientState(bool connected)
    {
        _client.Setup(c => c.IsConnected).Returns(connected);

        _subject.IsConnected.Should().Be(connected);
    }

    [TestMethod]
    public void Dispose_DoesNotDisposeTheClient()
    {
        _subject.Dispose();
    }

    [TestMethod]
    public void Constructor_NullArguments_Throw()
    {
        Action act1 = () => new MqttNetClientAdapter(null!, _builder.Object, _time);
        Action act2 = () => new MqttNetClientAdapter(_client.Object, null!, _time);
        Action act3 = () => new MqttNetClientAdapter(_client.Object, _builder.Object, null!);

        act1.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("client");
        act2.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("optionsBuilder");
        act3.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("timeProvider");
    }

    private static MqttClientPublishResult Published(MqttClientPublishReasonCode code) => new(1, code, null, Array.Empty<MqttUserProperty>());
}
