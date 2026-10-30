using FluentAssertions;
using Moq;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;

namespace WeatherParser.Cli.Tests.Configuration;

/// <summary>
/// Verifies that the configuration validator enforces the OW-14 restriction for OST Hessen publication.
/// </summary>
[TestClass]
[TestCategory("T-P3-16")]
public sealed class ConfigurationValidatorOsthessenPublicationTests
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

    private static ParseOstHessenRequest Request(OutputFormat output) => new(new CommonOptions(output, null, null, false), "Test*");

    [TestMethod]
    public void Validate_ParseOstHessenMqtt_WithoutPermission_NotPermitted()
    {
        var configuration = TestConfigurations.Valid();
        configuration.OsthessenWetter!.PublicationPermitted = false;
        var request = Request(OutputFormat.Mqtt);
        _credentials.Setup(c => c.GetCredentials(configuration.Mqtt!.Broker)).Returns((BrokerCredentials?)null);
        using var provider = TestConfigurations.Build(configuration, request);
        _subject.Invoking(s => s.Validate(provider, configuration, request))
            .Should().Throw<CliException>()
            .Which.ErrorId.Should().Be("CLI-OSTHESSEN-PUBLICATION-NOT-PERMITTED");
    }

    [TestMethod]
    public void Validate_ParseOstHessenMqtt_WithPermission_Accepted()
    {
        var configuration = TestConfigurations.Valid();
        configuration.OsthessenWetter!.PublicationPermitted = true;
        var request = Request(OutputFormat.Mqtt);
        _credentials.Setup(c => c.GetCredentials(configuration.Mqtt!.Broker)).Returns((BrokerCredentials?)null);
        using var provider = TestConfigurations.Build(configuration, request);
        _subject.Invoking(s => s.Validate(provider, configuration, request))
            .Should().NotThrow<CliException>();
    }

    [TestMethod]
    public void Validate_ParseOstHessenJson_WithoutPermission_Accepted()
    {
        var configuration = TestConfigurations.Valid();
        configuration.OsthessenWetter!.PublicationPermitted = false;
        var request = Request(OutputFormat.Json);
        using var provider = TestConfigurations.Build(configuration, request);
        _subject.Invoking(s => s.Validate(provider, configuration, request))
            .Should().NotThrow<CliException>();
    }
}
