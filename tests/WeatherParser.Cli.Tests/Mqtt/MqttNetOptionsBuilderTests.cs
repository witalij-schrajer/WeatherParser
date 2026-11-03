using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Buffers;
using FluentAssertions;
using Moq;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Mqtt;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Cli.Tests.Mqtt;

[TestClass]
[TestCategory("T-P3-14")]
public sealed class MqttNetOptionsBuilderTests
{
    private MockRepository _mocks = null!;
    private Mock<IFileOperations> _files = null!;
    private MqttNetOptionsBuilder _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _files = _mocks.Create<IFileOperations>();
        _subject = new(_files.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private static MqttConnectionSettings Settings(bool useTls = true, string? caFile = null, BrokerCredentials? credentials = null, MqttProtocol protocol = MqttProtocol.V500) =>
        new("broker.example.invalid", 8883, protocol, "weatherparser0123456789", TimeSpan.FromSeconds(45), useTls, caFile, credentials);

    private static (string Pem, string Thumbprint) TestCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=WeatherParser Test CA", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return (certificate.ExportCertificatePem(), certificate.Thumbprint);
    }

    private static MqttClientTcpOptions Tcp(MqttClientOptions options) => (MqttClientTcpOptions)options.ChannelOptions;

    [TestMethod]
    public void Adapter_MapsConnectOptions_ToClientOptions()
    {
        var options = _subject.BuildClientOptions(Settings(credentials: new BrokerCredentials("user", "secret")));
        Tcp(options).RemoteEndpoint.Should().BeOfType<DnsEndPoint>().Which.Host.Should().Be("broker.example.invalid");
        ((DnsEndPoint)Tcp(options).RemoteEndpoint).Port.Should().Be(8883);
        options.ProtocolVersion.Should().Be(MqttProtocolVersion.V500);
        options.ClientId.Should().Be("weatherparser0123456789");
        options.KeepAlivePeriod.Should().Be(TimeSpan.FromSeconds(45));
        options.CleanSession.Should().BeTrue();
        options.Credentials!.GetUserName(options).Should().Be("user");
        Encoding.UTF8.GetString(options.Credentials.GetPassword(options)).Should().Be("secret");
        Tcp(options).TlsOptions.UseTls.Should().BeTrue();
    }

    [TestMethod]
    public void Connect_Protocol311_Mapped()
    {
        var options = _subject.BuildClientOptions(Settings(protocol: MqttProtocol.V311));
        options.ProtocolVersion.Should().Be(MqttProtocolVersion.V311);
    }

    [TestMethod]
    public void Connect_WithoutCredentials_NoCredentials()
    {
        var options = _subject.BuildClientOptions(Settings(credentials: null));
        options.Credentials.Should().BeNull();
    }

    [TestMethod]
    public void Connect_TlsByDefault_SystemTrustStore()
    {
        var options = _subject.BuildClientOptions(Settings());
        Tcp(options).TlsOptions.UseTls.Should().BeTrue();
        (Tcp(options).TlsOptions.TrustChain?.Count ?? 0).Should().Be(0);
    }

    [TestMethod]
    public void Connect_Unencrypted_NoTls()
    {
        var options = _subject.BuildClientOptions(Settings(useTls: false));
        Tcp(options).TlsOptions.UseTls.Should().BeFalse();
    }

    [TestMethod]
    public void Connect_CustomCaFile_Used()
    {
        var (pem, thumbprint) = TestCertificate();
        _files.Setup(f => f.ReadAllText("ca.pem")).Returns(pem);
        var options = _subject.BuildClientOptions(Settings(caFile: "ca.pem"));
        Tcp(options).TlsOptions.TrustChain!.Count.Should().Be(1);
        Tcp(options).TlsOptions.TrustChain![0].Thumbprint.Should().Be(thumbprint);
    }

    [TestMethod]
    public void Connect_CertificateValidationNeverDisabled()
    {
        var settings = Settings();
        var options = _subject.BuildClientOptions(settings);
        Tcp(options).TlsOptions.CertificateValidationHandler.Should().BeNull();
        Tcp(options).TlsOptions.AllowUntrustedCertificates.Should().BeFalse();
        Tcp(options).TlsOptions.IgnoreCertificateChainErrors.Should().BeFalse();
        Tcp(options).TlsOptions.IgnoreCertificateRevocationErrors.Should().BeFalse();
    }

    [TestMethod]
    public void Connect_CertificateValidationNeverDisabled_WithCaFile()
    {
        var settings = Settings(caFile: "ca.pem");
        var (pem, _) = TestCertificate();
        _files.Setup(f => f.ReadAllText("ca.pem")).Returns(pem);
        var options = _subject.BuildClientOptions(settings);
        Tcp(options).TlsOptions.CertificateValidationHandler.Should().BeNull();
        Tcp(options).TlsOptions.AllowUntrustedCertificates.Should().BeFalse();
        Tcp(options).TlsOptions.IgnoreCertificateChainErrors.Should().BeFalse();
        Tcp(options).TlsOptions.IgnoreCertificateRevocationErrors.Should().BeFalse();
    }

    [TestMethod]
    public void Connect_CaFileWithoutCertificate_Fails()
    {
        _files.Setup(f => f.ReadAllText("ca.pem")).Returns("not a certificate");
        var exception = _subject.Invoking(s => s.BuildClientOptions(Settings(caFile: "ca.pem"))).Should().Throw<MqttTransportException>().Which;
        exception.Failure.Should().Be(MqttTransportFailure.ConnectionFailed);
        exception.Message.Should().Contain("Mqtt.Broker.Tls.CaFile");
    }

    [TestMethod]
    public void Connect_CaFileUnreadable_Fails()
    {
        _files.Setup(f => f.ReadAllText("ca.pem")).Throws(new IOException("denied"));
        var exception = _subject.Invoking(s => s.BuildClientOptions(Settings(caFile: "ca.pem"))).Should().Throw<MqttTransportException>().Which;
        exception.Failure.Should().Be(MqttTransportFailure.ConnectionFailed);
        exception.Message.Should().Contain("Mqtt.Broker.Tls.CaFile");
    }

    [TestMethod]
    public void BuildMessage_Unchanged()
    {
        var message = new MqttMessage("haus/weather/dwd/00044", new byte[] { 0x7B, 0xC3, 0x84, 0x7D }, MqttQualityOfService.AtLeastOnce, true, MqttMessageType.Observation);
        var applicationMessage = _subject.BuildMessage(message);
        applicationMessage.Topic.Should().Be("haus/weather/dwd/00044");
        applicationMessage.Payload.ToArray().Should().Equal(new byte[] { 0x7B, 0xC3, 0x84, 0x7D });
        applicationMessage.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
        applicationMessage.Retain.Should().BeTrue();

        var message2 = new MqttMessage("haus/weather/dwd/00044", new byte[] { 0x7B, 0xC3, 0x84, 0x7D }, MqttQualityOfService.AtLeastOnce, false, MqttMessageType.CdcStationList);
        var applicationMessage2 = _subject.BuildMessage(message2);
        applicationMessage2.Retain.Should().BeFalse();
    }

    [TestMethod]
    public void Constructor_NullFiles_Throws()
    {
        Action act = () => new MqttNetOptionsBuilder(null!);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("files");
    }

    [TestMethod]
    public void BuildClientOptions_NullSettings_Throws()
    {
        Action act = () => _subject.BuildClientOptions(null!);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("settings");
    }
}
