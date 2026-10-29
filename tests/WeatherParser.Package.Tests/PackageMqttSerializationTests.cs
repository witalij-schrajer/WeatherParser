using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Package.Tests;

[TestClass]
public sealed class PackageMqttSerializationTests
{
    [TestMethod]
    public async Task Package_ResolvesMqttMessageSerializer()
    {
        using var host = ConsumerLibrary.Host();
        var serializer1 = host.Provider.GetRequiredService<IMqttMessageSerializer>();
        var serializer2 = host.Provider.GetRequiredService<IMqttMessageSerializer>();
        serializer1.Should().BeOfType<MqttMessageSerializer>();
        serializer1.Should().BeSameAs(serializer2);
    }

    [TestMethod]
    public async Task Package_MqttMessage_ForEveryRoot()
    {
        using var host = ConsumerLibrary.Host();
        var serializer = host.Provider.GetRequiredService<IMqttMessageSerializer>();

        var dataset = ConsumerTestData.ValidDataset();
        var message = await serializer.SerializeAsync(dataset, CancellationToken.None);
        message.Topic.Should().Be("haus/weather/measurements/dwd:00044/state");
        message.Retain.Should().BeTrue();
        message.MessageType.Should().Be(MqttMessageType.Observation);
        message.Payload.ToArray().Should().Equal(await host.JsonAsync(dataset));

        var run = ConsumerTestData.ValidRun();
        message = await serializer.SerializeAsync(run, CancellationToken.None);
        message.Topic.Should().Be("haus/weather/forecasts/" + run.Source + "/state");
        message.MessageType.Should().Be(MqttMessageType.Forecast);
        message.Retain.Should().BeTrue();
        message.Payload.ToArray().Should().Equal(await host.JsonAsync(run));

        var cdcList = ConsumerTestData.ValidCdcList();
        cdcList.Source = "dwd:cdc-10min-now";
        message = await serializer.SerializeAsync(cdcList, CancellationToken.None);
        message.Topic.Should().Be("haus/weather/stations/dwd/selection");
        message.Retain.Should().BeFalse();
        message.Payload.ToArray().Should().Equal(await host.JsonAsync(cdcList));

        var mosmixList = ConsumerTestData.ValidMosmixList();
        mosmixList.Source = "dwd:mosmix-stationskatalog";
        message = await serializer.SerializeAsync(mosmixList, CancellationToken.None);
        message.Topic.Should().Be("haus/weather/stations/mosmix/selection");
        message.Retain.Should().BeFalse();
        message.Payload.ToArray().Should().Equal(await host.JsonAsync(mosmixList));
    }

    [TestMethod]
    public async Task Package_MqttOptionsInvalid_FailsOnResolution()
    {
        using var host = ConsumerLibrary.Host(topics: new MqttTopicOptions { Mappings = new Dictionary<MqttMessageType, MqttTopicMapping> { [MqttMessageType.Observation] = new() { TopicTemplate = "o/{station}" } } });
        Action act = () => host.Provider.GetRequiredService<IMqttMessageSerializer>();
        act.Should().Throw<MqttMessageException>()
            .Where(e => e.ErrorId == MqttMessageErrorIds.OptionsInvalid);
    }

    [TestMethod]
    public async Task Package_EmptyMqttOptions_MappingMissing()
    {
        using var host = ConsumerLibrary.Host(topics: new MqttTopicOptions());
        var serializer = host.Provider.GetRequiredService<IMqttMessageSerializer>();
        Func<Task> act = () => serializer.SerializeAsync(ConsumerTestData.ValidDataset(), CancellationToken.None);
        await act.Should().ThrowAsync<MqttMessageException>()
            .Where(e => e.ErrorId == MqttMessageErrorIds.MappingMissing);
    }
}
