using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Verifies the composition root of the example CLI: the validated container with AddWeatherParsing (CL-07) and the HttpClient of CL-21 (T-P3-01).
/// </summary>
[TestClass]
public sealed class CompositionRootTests
{
    /// <summary>
    /// Verifies that the composition root builds a validated container.
    /// </summary>
    [TestMethod]
    public void CompositionRoot_BuildsValidatedContainer()
    {
        using var provider = CompositionRoot.Build(TestConfiguration.Register);
        _ = provider.GetRequiredService<HttpClient>();
        _ = provider.GetRequiredService<TimeProvider>();
        provider.GetRequiredService<TimeProvider>().Should().Be(TimeProvider.System);
    }

    /// <summary>
    /// Verifies that the composition root registers the library through AddWeatherParsing.
    /// </summary>
    [TestMethod]
    public void CompositionRoot_RegistersLibraryThroughAddWeatherParsing()
    {
        using var provider = CompositionRoot.Build(TestConfiguration.Register);
        provider.GetRequiredService<IJsonExportSerializer>().Should().NotBeNull();
        provider.GetRequiredService<IXmlExportSerializer>().Should().NotBeNull();
        provider.GetRequiredService<IMqttMessageSerializer>().Should().NotBeNull();
        provider.GetRequiredService<ICdcStationSelector>().Should().NotBeNull();
        provider.GetRequiredService<IMosmixLocationSelector>().Should().NotBeNull();
        provider.GetRequiredService<IDwdObservationParser>().Should().NotBeNull();
        provider.GetRequiredService<IOsthessenWetterParser>().Should().NotBeNull();
        provider.GetRequiredService<IMosmixLParser>().Should().NotBeNull();
        provider.GetRequiredService<IMosmixSParser>().Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the composition root fails at build when a consumer registration is missing.
    /// </summary>
    [TestMethod]
    public void CompositionRoot_MissingConsumerRegistration_FailsAtBuild()
    {
        Action act = () => CompositionRoot.Build(services => { });
        act.Should().Throw<AggregateException>();
    }

    /// <summary>
    /// Verifies that two builds result in independent containers.
    /// </summary>
    [TestMethod]
    public void CompositionRoot_TwoBuilds_IndependentContainers()
    {
        using var provider1 = CompositionRoot.Build(TestConfiguration.Register);
        using var provider2 = CompositionRoot.Build(TestConfiguration.Register);
        provider1.GetRequiredService<HttpClient>().Should().NotBeSameAs(provider2.GetRequiredService<HttpClient>());
    }

    /// <summary>
    /// Verifies that the HTTP handler does not allow redirects and decompression.
    /// </summary>
    [TestMethod]
    public void CompositionRoot_HttpHandler_NoRedirectNoDecompression()
    {
        using var handler = CompositionRoot.CreateHttpHandler();
        handler.AllowAutoRedirect.Should().BeFalse();
        handler.AutomaticDecompression.Should().Be(DecompressionMethods.None);
    }

    /// <summary>
    /// Verifies that the HTTP client has infinite timeout and a user agent.
    /// </summary>
    [TestMethod]
    public void CompositionRoot_HttpClient_InfiniteTimeoutAndUserAgent()
    {
        using var client = CompositionRoot.CreateHttpClient(null);
        client.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
        client.DefaultRequestHeaders.UserAgent.ToString().Should().StartWith("WeatherParser/");
        client.DefaultRequestHeaders.UserAgent.ToString().Length.Should().BeGreaterThan("WeatherParser/".Length);
    }
}
