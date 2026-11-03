using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Mqtt;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Tests.Mqtt;

/// <summary>
/// Tests the cancellation behavior of <see cref="MqttPublisher.PublishAllAsync"/>.
/// </summary>
[TestClass]
[TestCategory("T-P3-18")]
public sealed class MqttPublisherCancellationTests
{
    private MockRepository _mocks = null!;
    private Mock<IMqttClientAdapter> _adapter = null!;
    private StringWriter _stderr = null!;
    private ILoggerFactory _loggerFactory = null!;
    private MqttPublicationSettings _settings = null!;
    private CancellationTokenSource _cancellation = null!;
    private MqttPublisher _subject = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _adapter = _mocks.Create<IMqttClientAdapter>();
        _stderr = new StringWriter();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new StandardErrorLoggerProvider(_stderr, TimeProvider.System, new LogLineFormatter(), LogLevel.Information, "ParseDWD")));
        _settings = new MqttPublicationSettings(
            new MqttConnectionSettings("broker.example.invalid", 8883, MqttProtocol.V500, "weatherparser0123456789", TimeSpan.FromSeconds(30), true, null, null),
            new MqttPublicationLimits(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), 1, TimeSpan.Zero));
        _cancellation = new CancellationTokenSource();
        _subject = new MqttPublisher(_adapter.Object, TimeProvider.System, _loggerFactory.CreateLogger<MqttPublisher>());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
        _cancellation.Dispose();
        _loggerFactory.Dispose();
        _stderr.Dispose();
    }

    private static PublicationItem Item(string topic) => new(new MqttMessage(topic, new byte[] { 1 }, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation), "dwd:00044");

    // The publication of the topic cancels the run and then waits for the cancellation.
    private void CancelWhilePublishing(string topic) =>
        _adapter.Setup(a => a.PublishAsync(It.Is<MqttMessage>(m => m.Topic == topic), It.IsAny<CancellationToken>()))
            .Returns<MqttMessage, CancellationToken>(async (_, token) =>
            {
                _cancellation.Cancel();
                await Task.Delay(Timeout.Infinite, token);
                return new MqttPublishAcknowledgment(true, "Success");
            });

    /// <summary>
    /// Tests that when cancellation occurs after one message is published successfully,
    /// the exception thrown contains the correct counts.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task PublishAllAsync_CancelledAfterOnePublished_ThrowsWithReport()
    {
        _adapter.Setup(a => a.ConnectAsync(It.IsAny<MqttConnectionSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _adapter.Setup(a => a.PublishAsync(It.Is<MqttMessage>(m => m.Topic == "t/0"), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new MqttPublishAcknowledgment(true, "Success")));
        CancelWhilePublishing("t/1");
        _adapter.Setup(a => a.DisconnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = () => _subject.PublishAllAsync(new[] { Item("t/0"), Item("t/1"), Item("t/2") }, _settings, _cancellation.Token);
        var exception = (await act.Should().ThrowAsync<PublicationCancelledException>()).Which;

        exception.Report.Should().Be(new PublicationReport(1, 0, 2));
        exception.CancellationToken.Should().Be(_cancellation.Token);
        _stderr.ToString().Should().Contain("Publication summary: published=1 failed=0 not_attempted=2.");
    }

    /// <summary>
    /// Tests that when cancellation occurs after one message is refused,
    /// the exception thrown contains the correct counts.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task PublishAllAsync_CancelledAfterARefusal_ReportCountsTheFailure()
    {
        _adapter.Setup(a => a.ConnectAsync(It.IsAny<MqttConnectionSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _adapter.Setup(a => a.PublishAsync(It.Is<MqttMessage>(m => m.Topic == "t/0"), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new MqttPublishAcknowledgment(false, "QuotaExceeded")));
        CancelWhilePublishing("t/1");
        _adapter.Setup(a => a.DisconnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = () => _subject.PublishAllAsync(new[] { Item("t/0"), Item("t/1") }, _settings, _cancellation.Token);
        var exception = (await act.Should().ThrowAsync<PublicationCancelledException>()).Which;

        exception.Report.Should().Be(new PublicationReport(0, 1, 1));
    }

    /// <summary>
    /// Tests that when cancellation occurs before the run starts,
    /// all items are reported as not attempted.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task PublishAllAsync_CancelledBeforeTheRun_ReportAllNotAttempted()
    {
        _cancellation.Cancel();

        Func<Task> act = () => _subject.PublishAllAsync(new[] { Item("t/0"), Item("t/1") }, _settings, _cancellation.Token);
        var exception = (await act.Should().ThrowAsync<PublicationCancelledException>()).Which;

        exception.Report.Should().Be(new PublicationReport(0, 0, 2));
        _stderr.ToString().Should().Contain("Publication summary: published=0 failed=0 not_attempted=2.");
    }

    /// <summary>
    /// Tests that when cancellation occurs while connecting,
    /// all items are reported as not attempted.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task PublishAllAsync_CancelledWhileConnecting_ReportAllNotAttempted()
    {
        _adapter.Setup(a => a.ConnectAsync(It.IsAny<MqttConnectionSettings>(), It.IsAny<CancellationToken>()))
            .Returns<MqttConnectionSettings, CancellationToken>(async (_, token) =>
            {
                _cancellation.Cancel();
                await Task.Delay(Timeout.Infinite, token);
            });

        Func<Task> act = () => _subject.PublishAllAsync(new[] { Item("t/0") }, _settings, _cancellation.Token);
        var exception = (await act.Should().ThrowAsync<PublicationCancelledException>()).Which;

        exception.Report.Should().Be(new PublicationReport(0, 0, 1));
    }
}
