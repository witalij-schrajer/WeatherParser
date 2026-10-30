using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Tests.Output;

[TestClass]
[TestCategory("T-P3-15")]
public sealed class PublicationQueueTests
{
    private PublicationQueue _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new PublicationQueue();
    }

    [TestMethod]
    public void Messages_Initially_Empty()
    {
        _subject.Messages.Should().BeEmpty();
    }

    [TestMethod]
    public void Add_KeepsResultOrder()
    {
        var message1 = new MqttMessage("t/1", new byte[] { 1 }, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation);
        var message2 = new MqttMessage("t/2", new byte[] { 1 }, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation);

        _subject.Add(message1);
        _subject.Add(message2);

        _subject.Messages.Select(m => m.Topic).Should().Equal("t/1", "t/2");
    }

    [TestMethod]
    public void Add_Null_Throws()
    {
        Action act = () => _subject.Add(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
