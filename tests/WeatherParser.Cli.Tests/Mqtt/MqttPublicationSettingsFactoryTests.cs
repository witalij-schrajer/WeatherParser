using System;
using System.Text.RegularExpressions;
using FluentAssertions;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Mqtt;
using WeatherParser.Parsing.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Cli.Tests.Mqtt;

[TestClass]
[TestCategory("T-P3-14")]
public sealed class MqttPublicationSettingsFactoryTests
{
    private MqttPublicationSettingsFactory _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new MqttPublicationSettingsFactory();
    }

    [TestMethod]
    public void Create_Defaults_TlsPort8883V500KeepAlive30s()
    {
        var settings = _subject.Create(Configuration(new BrokerSection { Host = "broker.example.invalid" }), null);

        settings.Connection.Host.Should().Be("broker.example.invalid");
        settings.Connection.Port.Should().Be(8883);
        settings.Connection.Protocol.Should().Be(MqttProtocol.V500);
        settings.Connection.KeepAlive.Should().Be(TimeSpan.FromSeconds(30));
        settings.Connection.UseTls.Should().BeTrue();
        settings.Connection.CaFile.Should().BeNull();
        settings.Connection.Credentials.Should().BeNull();
        settings.Limits.Should().Be(MqttPublicationLimits.Default);
    }

    [TestMethod]
    public void Create_DefaultClientId_WeatherparserAnd10LowercaseHex()
    {
        var settings = _subject.Create(Configuration(new BrokerSection { Host = "broker.example.invalid" }), null);

        var clientId = settings.Connection.ClientId;
        clientId.Should().HaveLength(23);
        Regex.IsMatch(clientId, "^weatherparser[0-9a-f]{10}$").Should().BeTrue();
    }

    [TestMethod]
    public void Create_DefaultClientId_NewPerCall()
    {
        var config = Configuration(new BrokerSection { Host = "broker.example.invalid" });
        var settings1 = _subject.Create(config, null);
        var settings2 = _subject.Create(config, null);

        settings1.Connection.ClientId.Should().NotBe(settings2.Connection.ClientId);
    }

    [TestMethod]
    public void Create_AllowUnencrypted_Port1883WithoutTls()
    {
        var settings = _subject.Create(Configuration(new BrokerSection { Host = "broker.example.invalid", AllowUnencrypted = true }), null);

        settings.Connection.UseTls.Should().BeFalse();
        settings.Connection.Port.Should().Be(1883);
    }

    [TestMethod]
    public void Create_ConfiguredValues_Forwarded()
    {
        var credentials = new BrokerCredentials("user", "secret");
        var broker = new BrokerSection
        {
            Host = "broker.example.invalid",
            Port = 8884,
            ProtocolVersion = "3.1.1",
            ClientId = "abc123",
            KeepAlive = TimeSpan.FromSeconds(60),
            Tls = new TlsSection { CaFile = "ca.pem" }
        };
        var limits = new LimitsSection
        {
            ConnectTimeout = TimeSpan.FromSeconds(5),
            PublishTimeout = TimeSpan.FromSeconds(7),
            MaxPublishAttempts = 2,
            RetryDelay = TimeSpan.Zero
        };

        var settings = _subject.Create(Configuration(broker, limits), credentials);

        settings.Connection.Port.Should().Be(8884);
        settings.Connection.Protocol.Should().Be(MqttProtocol.V311);
        settings.Connection.ClientId.Should().Be("abc123");
        settings.Connection.KeepAlive.Should().Be(TimeSpan.FromSeconds(60));
        settings.Connection.UseTls.Should().BeTrue();
        settings.Connection.CaFile.Should().Be("ca.pem");
        settings.Connection.Credentials.Should().BeSameAs(credentials);
        settings.Limits.Should().Be(new MqttPublicationLimits(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), 2, TimeSpan.Zero));
    }

    [TestMethod]
    public void Create_ProtocolVersion50_V500()
    {
        var settings = _subject.Create(Configuration(new BrokerSection { Host = "broker.example.invalid", ProtocolVersion = "5.0" }), null);

        settings.Connection.Protocol.Should().Be(MqttProtocol.V500);
    }

    [TestMethod]
    public void Create_NullConfiguration_Throws()
    {
        _subject.Invoking(s => s.Create(null!, null)).Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    private static CliConfiguration Configuration(BrokerSection broker, LimitsSection? limits = null) =>
        new() { Mqtt = new MqttSection { Broker = broker }, Limits = limits };
}
