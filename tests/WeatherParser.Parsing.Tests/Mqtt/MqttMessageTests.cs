using FluentAssertions;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Parsing.Tests.Mqtt;

[TestClass]
public sealed class MqttMessageTests
{
    [TestMethod]
    public void MqttMessage_Constructor_KeepsTopicPayloadQosRetainAndType()
    {
        var payload = new byte[] { 0x7B, 0x7D };
        var message = new MqttMessage("haus/weather/x", payload, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Forecast);

        message.Topic.Should().Be("haus/weather/x");
        message.Payload.ToArray().Should().Equal(0x7B, 0x7D);
        message.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        message.Retain.Should().Be(true);
        message.MessageType.Should().Be(MqttMessageType.Forecast);
    }

    [TestMethod]
    public void MqttMessage_NullOrEmptyTopic_Throws()
    {
        var payload = new byte[] { };

        var act1 = () => new MqttMessage(null!, payload, MqttQualityOfService.AtMostOnce, false, MqttMessageType.Observation);
        act1.Should().Throw<ArgumentNullException>().WithParameterName("topic");

        var act2 = () => new MqttMessage("", payload, MqttQualityOfService.AtMostOnce, false, MqttMessageType.Observation);
        act2.Should().Throw<ArgumentException>().WithParameterName("topic");
    }

    [TestMethod]
    public void MqttMessage_UndefinedQosOrType_Throws()
    {
        var payload = new byte[] { };

        var act1 = () => new MqttMessage("test", payload, (MqttQualityOfService)3, false, MqttMessageType.Observation);
        act1.Should().Throw<ArgumentException>().WithParameterName("qualityOfService");

        var act2 = () => new MqttMessage("test", payload, (MqttQualityOfService)(-1), false, MqttMessageType.Observation);
        act2.Should().Throw<ArgumentException>().WithParameterName("qualityOfService");

        var act3 = () => new MqttMessage("test", payload, MqttQualityOfService.AtMostOnce, false, (MqttMessageType)4);
        act3.Should().Throw<ArgumentException>().WithParameterName("messageType");
    }

    [TestMethod]
    public void MqttMessage_PayloadIsACopy()
    {
        var payload = new byte[] { 0x7B, 0x7D };
        var message = new MqttMessage("test", payload, MqttQualityOfService.AtMostOnce, false, MqttMessageType.Observation);

        payload[0] = 0x00;

        message.Payload.ToArray().Should().Equal(0x7B, 0x7D);
    }

    [TestMethod]
    public void MqttQualityOfService_Values_AreZeroOneTwo()
    {
        var values = Enum.GetValues<MqttQualityOfService>();
        values.Should().HaveCount(3);
        ((int)MqttQualityOfService.AtMostOnce).Should().Be(0);
        ((int)MqttQualityOfService.AtLeastOnce).Should().Be(1);
        ((int)MqttQualityOfService.ExactlyOnce).Should().Be(2);
    }

    [TestMethod]
    public void MqttMessageType_Values_AreTheFourMessageTypes()
    {
        var values = Enum.GetValues<MqttMessageType>();
        values.Should().Equal(MqttMessageType.Observation, MqttMessageType.Forecast, MqttMessageType.CdcStationList, MqttMessageType.MosmixLocationList);
    }

    [TestMethod]
    public void MqttMessageException_KeepsErrorIdMessageTypeAndInner()
    {
        var inner = new Exception("inner");
        var ex1 = new MqttMessageException(MqttMessageErrorIds.MappingMissing, MqttMessageType.Forecast, "test message", inner);

        ex1.ErrorId.Should().Be(MqttMessageErrorIds.MappingMissing);
        ex1.MessageType.Should().Be(MqttMessageType.Forecast);
        ex1.Message.Should().Be("test message");
        ex1.InnerException.Should().Be(inner);

        var ex2 = new MqttMessageException(MqttMessageErrorIds.MappingMissing, null, "test message");

        ex2.ErrorId.Should().Be(MqttMessageErrorIds.MappingMissing);
        ex2.MessageType.Should().BeNull();
        ex2.Message.Should().Be("test message");
        ex2.InnerException.Should().BeNull();
    }

    [TestMethod]
    public void MqttMessageException_UnknownErrorId_Throws()
    {
        var act1 = () => new MqttMessageException("MQTT-UNKNOWN", MqttMessageType.Observation, "test");
        act1.Should().Throw<ArgumentException>().WithParameterName("errorId");

        var act2 = () => new MqttMessageException(MqttMessageErrorIds.MappingMissing, (MqttMessageType)9, "test");
        act2.Should().Throw<ArgumentException>().WithParameterName("messageType");
    }

    [TestMethod]
    public void MqttMessageErrorIds_HaveTheContractValues()
    {
        MqttMessageErrorIds.MessageTypeUnsupported.Should().Be("MQTT-MESSAGE-TYPE-UNSUPPORTED");
        MqttMessageErrorIds.MappingMissing.Should().Be("MQTT-MAPPING-MISSING");
        MqttMessageErrorIds.SerializationFailed.Should().Be("MQTT-SERIALIZATION-FAILED");
        MqttMessageErrorIds.PlaceholderValueInvalid.Should().Be("MQTT-PLACEHOLDER-VALUE-INVALID");
        MqttMessageErrorIds.TopicInvalid.Should().Be("MQTT-TOPIC-INVALID");
        MqttMessageErrorIds.PayloadTooLarge.Should().Be("MQTT-PAYLOAD-TOO-LARGE");
        MqttMessageErrorIds.OptionsInvalid.Should().Be("MQTT-OPTIONS-INVALID");
    }
}
