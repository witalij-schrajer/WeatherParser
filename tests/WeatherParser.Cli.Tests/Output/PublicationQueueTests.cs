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

        _subject.Add(message1, "dwd:00044");
        _subject.Add(message2, "dwd:00044");

        _subject.Messages.Select(m => m.Topic).Should().Equal("t/1", "t/2");
    }

    [TestMethod]
    public void Add_Null_Throws()
    {
        Action act = () => _subject.Add(null!, "dwd:00044");
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Add_KeepsTheSourceWithTheMessage()
    {
        var message = new MqttMessage("t/1", new byte[] { 1 }, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation);
        _subject.Add(message, "dwd:00044");

        _subject.Items.Should().ContainSingle();
        _subject.Items[0].Source.Should().Be("dwd:00044");
        _subject.Items[0].Message.Should().BeSameAs(message);
    }

    [TestMethod]
    public void Add_NullSource_Throws()
    {
        var message = new MqttMessage("t/1", new byte[] { 1 }, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation);
        Action act = () => _subject.Add(message, null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
