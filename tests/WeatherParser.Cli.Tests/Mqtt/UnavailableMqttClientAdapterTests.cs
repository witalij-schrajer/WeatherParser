using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using WeatherParser.Cli.Mqtt;
using WeatherParser.Parsing.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Cli.Tests.Mqtt;

[TestClass]
[TestCategory("T-P3-14")]
public sealed class UnavailableMqttClientAdapterTests
{
    private UnavailableMqttClientAdapter _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new UnavailableMqttClientAdapter();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _subject.Dispose();
    }

    [TestMethod]
    public async Task Connect_FailsWithConnectionFailed()
    {
        var act = () => _subject.ConnectAsync(new MqttConnectionSettings("broker.example.invalid", 8883, MqttProtocol.V500, "weatherparser0123456789", TimeSpan.FromSeconds(30), true, null, null), CancellationToken.None);

        (await act.Should().ThrowAsync<MqttTransportException>()).Which.Failure.Should().Be(MqttTransportFailure.ConnectionFailed);
    }

    [TestMethod]
    public async Task Publish_FailsWithConnectionFailed()
    {
        var act = () => _subject.PublishAsync(new MqttMessage("t/1", new byte[] { 1 }, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation), CancellationToken.None);

        (await act.Should().ThrowAsync<MqttTransportException>()).Which.Failure.Should().Be(MqttTransportFailure.ConnectionFailed);
    }

    [TestMethod]
    public async Task Disconnect_Completes()
    {
        await _subject.DisconnectAsync(CancellationToken.None);
    }
}
