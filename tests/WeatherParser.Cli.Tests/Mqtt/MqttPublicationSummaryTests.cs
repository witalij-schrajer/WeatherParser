using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
[TestCategory("T-P3-15")]
public sealed class MqttPublicationSummaryTests
{
    private MockRepository _mocks = null!;
    private Mock<IMqttClientAdapter> _adapter = null!;
    private StringWriter _stderr = null!;
    private ILoggerFactory _loggerFactory = null!;
    private MqttPublicationSettings _settings = null!;
    private MqttPublisher _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _adapter = _mocks.Create<IMqttClientAdapter>();
        _stderr = new StringWriter();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new StandardErrorLoggerProvider(_stderr, TimeProvider.System, new LogLineFormatter(), LogLevel.Information, "ParseDWD")));
        _settings = new MqttPublicationSettings(
            new MqttConnectionSettings("broker.example.invalid", 8883, MqttProtocol.V500, "weatherparser0123456789", TimeSpan.FromSeconds(30), true, null, null),
            new MqttPublicationLimits(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), 1, TimeSpan.Zero));
        _subject = new MqttPublisher(_adapter.Object, TimeProvider.System, _loggerFactory.CreateLogger<MqttPublisher>());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
        _loggerFactory.Dispose();
        _stderr.Dispose();
    }

    private static MqttMessage Message(string topic) => new(topic, new byte[] { 1 }, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation);

    private int SummaryLines() => Regex.Matches(_stderr.ToString(), " INFORMATION CLI-PUBLICATION-SUMMARY command=ParseDWD ").Count;

    [TestMethod]
    public async Task Summary_LogLineCountsPublishedAndFailed()
    {
        var first = Message("t/1");
        var second = Message("t/2");

        _adapter.Setup(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _adapter.Setup(a => a.PublishAsync(It.Is<MqttMessage>(m => ReferenceEquals(m, first)), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new MqttPublishAcknowledgment(true, "Success")));
        _adapter.Setup(a => a.PublishAsync(It.Is<MqttMessage>(m => ReferenceEquals(m, second)), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new MqttPublishAcknowledgment(false, "NotAuthorized")));
        _adapter.Setup(a => a.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var report = await _subject.PublishAllAsync(new[] { first, second }, _settings, CancellationToken.None);

        report.Should().BeEquivalentTo(new PublicationReport(1, 1, 0));
        SummaryLines().Should().Be(1);
        _stderr.ToString().Should().Contain("Publication summary: published=1 failed=1 not_attempted=0.");
    }

    [TestMethod]
    public async Task Summary_EmptyList_AllZeroWithoutContact()
    {
        var report = await _subject.PublishAllAsync(Array.Empty<MqttMessage>(), _settings, CancellationToken.None);

        report.Should().BeEquivalentTo(new PublicationReport(0, 0, 0));
        SummaryLines().Should().Be(1);
        _stderr.ToString().Should().Contain("Publication summary: published=0 failed=0 not_attempted=0.");
    }

    [TestMethod]
    public async Task Summary_ConnectionFailure_CountsEveryMessageNotAttempted()
    {
        var messages = new[] { Message("t/1"), Message("t/2") };

        _adapter.Setup(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MqttTransportException(MqttTransportFailure.ConnectionFailed, "No connection could be made."));

        Func<Task> act = async () => await _subject.PublishAllAsync(messages, _settings, CancellationToken.None);

        (await act.Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-MQTT-CONNECT-FAILED");
        SummaryLines().Should().Be(1);
        _stderr.ToString().Should().Contain("Publication summary: published=0 failed=0 not_attempted=2.");
    }

    [TestMethod]
    public async Task Summary_MessageCheckFailure_NoSummary()
    {
        var message = new MqttMessage("t/1", new byte[] { 1 }, MqttQualityOfService.AtMostOnce, true, MqttMessageType.Observation);

        Func<Task> act = async () => await _subject.PublishAllAsync(new[] { message }, _settings, CancellationToken.None);

        (await act.Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-UNEXPECTED");
        SummaryLines().Should().Be(0);
    }

    [TestMethod]
    public async Task Summary_ConnectionLostForGood_CountsNotAttempted()
    {
        var messages = new[] { Message("t/1"), Message("t/2"), Message("t/3") };

        _settings = _settings with { Limits = new MqttPublicationLimits(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), 2, TimeSpan.Zero) };

        _adapter.SetupSequence(a => a.ConnectAsync(_settings.Connection, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .ThrowsAsync(new MqttTransportException(MqttTransportFailure.ConnectionRefused, "Not authorized."));
        _adapter.Setup(a => a.IsConnected).Returns(false);
        _adapter.Setup(a => a.PublishAsync(It.IsAny<MqttMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MqttTransportException(MqttTransportFailure.ConnectionLost, "Lost."));
        _adapter.Setup(a => a.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var report = await _subject.PublishAllAsync(messages, _settings, CancellationToken.None);

        report.Should().BeEquivalentTo(new PublicationReport(0, 1, 2));
        SummaryLines().Should().Be(1);
        _stderr.ToString().Should().Contain("Publication summary: published=0 failed=1 not_attempted=2.");
    }
}
