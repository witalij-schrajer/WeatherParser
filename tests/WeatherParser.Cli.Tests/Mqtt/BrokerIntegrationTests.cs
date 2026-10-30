using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using WeatherParser.Cli;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Mqtt;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Tests.Mqtt;

[TestClass]
[TestCategory("Broker")]
[TestCategory("T-P3-19")]
public sealed class BrokerIntegrationTests
{
    private const string Variable = "WEATHERPARSER_TEST_BROKER";
    private CliConfiguration _configuration = null!;
    private BrokerCredentials? _credentials;
    private string _prefix = null!;
    private readonly List<string> _retainedTopics = new();

    [TestInitialize]
    public void Initialize()
    {
        var path = Environment.GetEnvironmentVariable(Variable);
        if (string.IsNullOrEmpty(path))
        {
            Assert.Inconclusive($"Broker integration test skipped: set {Variable} to a test configuration file outside the repository that names an authorized test broker, never a production broker (CL-20, REQ-083).");
        }

        var environment = new EnvironmentReader();
        _configuration = new ConfigurationLoader(environment).Load(path);
        _credentials = new CredentialProvider(environment, new FileModeReader()).GetCredentials(_configuration.Mqtt?.Broker);
        _prefix = "weatherparser-test/" + Guid.NewGuid().ToString("N") + "/";
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_retainedTopics.Count == 0)
        {
            return;
        }

        // Retained test messages are removed with an empty retained message.
        using var client = await ConnectSubscriberAsync();
        foreach (var topic in _retainedTopics)
        {
            await client.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(Array.Empty<byte>()).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).WithRetainFlag(true).Build());
        }

        await client.DisconnectAsync();
    }

    // The publisher as the CLI composes it for an MQTT command.
    private async Task<PublicationReport> PublishAsync(params MqttMessage[] messages)
    {
        var request = new ParseDwdRequest(new CommonOptions(OutputFormat.Mqtt, null, null, false), null);
        using var provider = CompositionRoot.Build(services =>
        {
            new ConfigurationRegistrar().Register(services, _configuration, request);
            new CommandRegistrar().Register(services, request);
            services.AddLogging();
        });
        var settings = provider.GetRequiredService<IMqttPublicationSettingsFactory>().Create(_configuration, _credentials);
        return await provider.GetRequiredService<IMessagePublisher>().PublishAllAsync(Items(messages), settings, CancellationToken.None);
    }

    private static PublicationItem[] Items(params MqttMessage[] messages) => messages.Select(message => new PublicationItem(message, "dwd:00044")).ToArray();

    private async Task<IMqttClient> ConnectSubscriberAsync(BrokerCredentials? credentials = null)
    {
        var settings = new MqttPublicationSettingsFactory().Create(_configuration, credentials ?? _credentials);
        var client = new MqttClientFactory().CreateMqttClient();
        // Auxiliary clients never reuse the publisher's client ID, which may be configured (MQTT takes over a session with the same ID).
        var options = new MqttNetOptionsBuilder(new FileOperations()).BuildClientOptions(settings.Connection with { ClientId = "wptestsub" + RandomNumberGenerator.GetHexString(10, lowercase: true) });
        await client.ConnectAsync(options);
        return client;
    }

    // Subscribes after the publication and collects what the broker delivers within the wait time.
    private async Task<IReadOnlyList<MqttApplicationMessage>> ReceiveAsync(string topic, TimeSpan wait)
    {
        var received = new ConcurrentQueue<MqttApplicationMessage>();
        using var client = await ConnectSubscriberAsync();
        client.ApplicationMessageReceivedAsync += e =>
        {
            received.Enqueue(e.ApplicationMessage);
            return Task.CompletedTask;
        };
        var subscription = await client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topic, MqttQualityOfServiceLevel.AtLeastOnce).Build());
        subscription.Items.Should().OnlyContain(item => item.ResultCode == MqttClientSubscribeResultCode.GrantedQoS0 || item.ResultCode == MqttClientSubscribeResultCode.GrantedQoS1 || item.ResultCode == MqttClientSubscribeResultCode.GrantedQoS2);
        await Task.Delay(wait);
        await client.DisconnectAsync();
        return received.ToArray();
    }

    // Subscribes before the publication, so that a delivered (live) message is observed; the caller disposes the returned client.
    private async Task<(IMqttClient Client, ConcurrentQueue<MqttApplicationMessage> Received)> SubscribeLiveAsync(string topic)
    {
        var received = new ConcurrentQueue<MqttApplicationMessage>();
        var client = await ConnectSubscriberAsync();
        client.ApplicationMessageReceivedAsync += e =>
        {
            received.Enqueue(e.ApplicationMessage);
            return Task.CompletedTask;
        };
        var subscription = await client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topic, MqttQualityOfServiceLevel.AtLeastOnce).Build());
        subscription.Items.Should().OnlyContain(item => item.ResultCode == MqttClientSubscribeResultCode.GrantedQoS0 || item.ResultCode == MqttClientSubscribeResultCode.GrantedQoS1 || item.ResultCode == MqttClientSubscribeResultCode.GrantedQoS2);
        return (client, received);
    }

    private MqttMessage Message(string name, string payload, MqttMessageType type)
    {
        var retain = type is MqttMessageType.Observation or MqttMessageType.Forecast;
        var topic = _prefix + name;
        if (retain)
        {
            _retainedTopics.Add(topic);
        }

        return new MqttMessage(topic, Encoding.UTF8.GetBytes(payload), MqttQualityOfService.AtLeastOnce, retain, type);
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task Broker_PublishAndSubscribe_RetainedObservationReceivedOnce()
    {
        var message = Message("observation", "{\"t\":\"Ä\"}", MqttMessageType.Observation);
        (await PublishAsync(message)).Should().Be(new PublicationReport(1, 0, 0));
        var received = await ReceiveAsync(message.Topic, TimeSpan.FromSeconds(2));
        received.Should().ContainSingle();
        received[0].Retain.Should().BeTrue();
        received[0].Payload.ToArray().Should().Equal(message.Payload.ToArray());
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task Broker_StationList_NotRetained()
    {
        var message = Message("stations", "[]", MqttMessageType.CdcStationList);
        var (live, delivered) = await SubscribeLiveAsync(message.Topic);
        using (live)
        {
            (await PublishAsync(message)).Should().Be(new PublicationReport(1, 0, 0));
            await Task.Delay(TimeSpan.FromSeconds(2));
            await live.DisconnectAsync();
        }

        // The live subscriber received the message, not retained; a later subscriber receives nothing.
        delivered.Should().ContainSingle();
        delivered.Single().Retain.Should().BeFalse();
        delivered.Single().Payload.ToArray().Should().Equal(message.Payload.ToArray());
        (await ReceiveAsync(message.Topic, TimeSpan.FromSeconds(2))).Should().BeEmpty();
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task Broker_SecondRunSameSource_ReplacesRetainedMessage()
    {
        var first = Message("observation", "{\"run\":1}", MqttMessageType.Observation);
        var second = new MqttMessage(first.Topic, Encoding.UTF8.GetBytes("{\"run\":2}"), MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation);
        (await PublishAsync(first)).Should().Be(new PublicationReport(1, 0, 0));
        (await PublishAsync(second)).Should().Be(new PublicationReport(1, 0, 0));
        var received = await ReceiveAsync(second.Topic, TimeSpan.FromSeconds(2));
        received.Should().ContainSingle();
        Encoding.UTF8.GetString(received[0].Payload).Should().Be("{\"run\":2}");
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task Broker_WrongCredentials_Rejected()
    {
        if (_credentials is null)
        {
            Assert.Inconclusive("The authorized test broker does not require credentials; a wrong-credential check needs a broker that does.");
        }

        // The valid credentials connect, so a later refusal is caused by the altered password.
        var valid = _credentials;
        using (var client = await ConnectSubscriberAsync(valid))
        {
            client.IsConnected.Should().BeTrue();
            await client.DisconnectAsync();
        }

        _credentials = new BrokerCredentials(valid.Username, valid.Password + "-wrong");
        try
        {
            var exception = (await ((Func<Task>)(() => PublishAsync(Message("observation", "{}", MqttMessageType.Observation)))).Should().ThrowAsync<CliException>()).Which;
            exception.ErrorId.Should().Be("CLI-MQTT-CONNECT-FAILED");
            exception.Message.Should().Contain("ConnectionRefused");
        }
        finally
        {
            // The cleanup uses the valid credentials and removes any message published if the broker accepted the altered password (an empty retained message to an unused topic is harmless).
            _credentials = valid;
        }
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task Broker_TlsWithCustomCa()
    {
        if (_configuration.Mqtt!.Broker!.AllowUnencrypted || _configuration.Mqtt.Broker.Tls?.CaFile is null)
        {
            Assert.Inconclusive("The authorized test broker does not offer TLS with a custom CA.");
        }

        var message = Message("observation", "{\"t\":\"Ä\"}", MqttMessageType.Observation);
        (await PublishAsync(message)).Should().Be(new PublicationReport(1, 0, 0));
        var received = await ReceiveAsync(message.Topic, TimeSpan.FromSeconds(2));
        received.Should().ContainSingle();
        received[0].Retain.Should().BeTrue();
        received[0].Payload.ToArray().Should().Equal(message.Payload.ToArray());
    }
}
