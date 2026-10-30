using System;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Tests.Output;

[TestClass]
[TestCategory("T-P3-14")]
public sealed class UnavailableMessagePublisherTests
{
    private UnavailableMessagePublisher _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new UnavailableMessagePublisher();
    }

    [TestMethod]
    public async Task PublishAllAsync_EmptyList_EmptyReportWithoutContact()
    {
        (await _subject.PublishAllAsync(Array.Empty<MqttMessage>(), CancellationToken.None))
            .Should().Be(new PublicationReport(0, 0, 0));
    }

    [TestMethod]
    public async Task PublishAllAsync_Messages_FailWithConnectFailed()
    {
        var act = () => _subject.PublishAllAsync(new[] { new MqttMessage("t/1", Array.Empty<byte>(), MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation) }, CancellationToken.None);
        (await act.Should().ThrowAsync<CliException>())
            .Which.ErrorId.Should().Be("CLI-MQTT-CONNECT-FAILED");
    }
}
