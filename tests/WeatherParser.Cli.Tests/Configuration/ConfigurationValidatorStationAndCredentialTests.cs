using FluentAssertions;
using Moq;
using WeatherParser.Cli;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;

namespace WeatherParser.Cli.Tests.Configuration;

/// <summary>
/// Verifies that the validation phase resolves the DWD reference station and reads the broker credentials only in MQTT mode (REQ-030, REQ-060, CL-05, CL-06).
/// </summary>
[TestClass]
public sealed class ConfigurationValidatorStationAndCredentialTests
{
    private MockRepository _mocks = null!;
    private Mock<IReferenceStationResolver> _resolver = null!;
    private Mock<ICredentialProvider> _credentials = null!;
    private ConfigurationValidator _subject = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _resolver = _mocks.Create<IReferenceStationResolver>();
        _credentials = _mocks.Create<ICredentialProvider>();
        _subject = new ConfigurationValidator(_resolver.Object, _credentials.Object);
    }

    [TestCleanup]
    public void Cleanup() => _mocks.VerifyAll();

    private Action Act(CliConfiguration configuration, CommandRequest request) => () =>
    {
        using var provider = TestConfigurations.Build(configuration, request);
        _subject.Validate(provider, configuration, request);
    };

    [TestMethod]
    [TestCategory("T-P3-06")]
    public void Validate_ParseDwd_ResolvesStation()
    {
        var configuration = TestConfigurations.Valid();
        var request = TestConfigurations.Request(CliCommand.ParseDwd, OutputFormat.Json);
        _resolver.Setup(r => r.Resolve("00044", configuration)).Returns("00044");
        Act(configuration, request).Should().NotThrow();
    }

    [TestMethod]
    [TestCategory("T-P3-06")]
    public void Validate_ParseDwdWithoutStation_DwdStationMissing()
    {
        var configuration = TestConfigurations.Valid();
        var request = TestConfigurations.Request(CliCommand.ParseDwd, OutputFormat.Json);
        _resolver.Setup(r => r.Resolve("00044", configuration)).Throws(new CliException("CLI-DWD-STATION-MISSING", "x"));
        Act(configuration, request).Should().Throw<CliException>().Which.ErrorId.Should().Be("CLI-DWD-STATION-MISSING");
    }

    [TestMethod]
    [TestCategory("T-P3-07")]
    public void Validate_MqttMode_ReadsCredentials()
    {
        var configuration = TestConfigurations.Valid();
        var request = TestConfigurations.Request(CliCommand.SelectDwd, OutputFormat.Mqtt);
        _credentials.Setup(c => c.GetCredentials(configuration.Mqtt!.Broker)).Returns((BrokerCredentials?)null);
        Act(configuration, request).Should().NotThrow();
    }

    [TestMethod]
    [TestCategory("T-P3-07")]
    public void Validate_MqttMode_InvalidCredentials_Propagate()
    {
        var configuration = TestConfigurations.Valid();
        var request = TestConfigurations.Request(CliCommand.SelectDwd, OutputFormat.Mqtt);
        _credentials.Setup(c => c.GetCredentials(configuration.Mqtt!.Broker)).Throws(new CliException("CLI-CREDENTIALS-INVALID", "x"));
        Act(configuration, request).Should().Throw<CliException>().Which.ErrorId.Should().Be("CLI-CREDENTIALS-INVALID");
    }

    [TestMethod]
    [TestCategory("T-P3-07")]
    public void Validate_JsonMode_DoesNotReadCredentials()
    {
        var configuration = TestConfigurations.Valid();
        var request = TestConfigurations.Request(CliCommand.SelectDwd, OutputFormat.Json);
        Act(configuration, request).Should().NotThrow();
    }
}
