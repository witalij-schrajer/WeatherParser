using FluentAssertions;
using Moq;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;

namespace WeatherParser.Cli.Tests.Configuration;

/// <summary>
/// Verifies that the configuration validator returns the resolved station and credentials (CL-04 to CL-06).
/// </summary>
[TestClass]
[TestCategory("T-P3-06")]
public sealed class ConfigurationValidatorResultTests
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

    [TestMethod]
    public void Validate_ParseDwd_ReturnsResolvedStation()
    {
        var configuration = TestConfigurations.Valid();
        var request = TestConfigurations.Request(CliCommand.ParseDwd, OutputFormat.Json);
        _resolver.Setup(r => r.Resolve("00044", configuration)).Returns("01234");
        using var provider = TestConfigurations.Build(configuration, request);
        var settings = _subject.Validate(provider, configuration, request);
        settings.StationId.Should().Be("01234");
        settings.Credentials.Should().BeNull();
    }

    [TestMethod]
    public void Validate_MqttMode_ReturnsCredentials()
    {
        var configuration = TestConfigurations.Valid();
        var request = TestConfigurations.Request(CliCommand.SelectDwd, OutputFormat.Mqtt);
        var credentials = new BrokerCredentials("user", "placeholder-password");
        _credentials.Setup(c => c.GetCredentials(configuration.Mqtt!.Broker)).Returns(credentials);
        using var provider = TestConfigurations.Build(configuration, request);
        var settings = _subject.Validate(provider, configuration, request);
        settings.Credentials.Should().BeSameAs(credentials);
        settings.StationId.Should().BeNull();
    }

    [TestMethod]
    public void Validate_JsonSelection_NoStationNoCredentials()
    {
        var configuration = TestConfigurations.Valid();
        var request = TestConfigurations.Request(CliCommand.SelectDwd, OutputFormat.Json);
        using var provider = TestConfigurations.Build(configuration, request);
        var settings = _subject.Validate(provider, configuration, request);
        settings.Should().Be(new ValidatedSettings(null, null));
    }
}
