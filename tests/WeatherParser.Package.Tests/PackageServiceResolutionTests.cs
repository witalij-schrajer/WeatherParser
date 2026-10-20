using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Package.Tests;

[TestClass]
public sealed class PackageServiceResolutionTests
{
    private ServiceProvider _provider = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _provider = new ServiceCollection().AddWeatherParsing().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _provider.Dispose();
    }

    [TestMethod]
    public void Package_AddWeatherParsing_ResolvesSerializers()
    {
        _provider.GetRequiredService<IJsonExportSerializer>().Should().NotBeNull();
        _provider.GetRequiredService<IXmlExportSerializer>().Should().NotBeNull();
    }

    [TestMethod]
    public void Package_AddWeatherParsing_ResolvesBuilders()
    {
        _provider.GetRequiredService<IObservationDatasetBuilder>().Should().NotBeNull();
        _provider.GetRequiredService<IForecastRunBuilder>().Should().NotBeNull();
    }

    [TestMethod]
    public void Package_AddWeatherParsing_ServicesComeFromThePackageAssembly()
    {
        _provider.GetRequiredService<IJsonExportSerializer>().GetType().Assembly.Should().BeSameAs(typeof(ParserEndpointDefinition).Assembly);
    }
}
