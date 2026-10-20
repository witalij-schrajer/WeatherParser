using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests;

[TestClass]
public sealed class ServiceCollectionExtensionsTests
{
    private ServiceCollection _services = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _services = new ServiceCollection();
    }

    [TestMethod]
    public void GetRequiredService_WithoutAnyRegistration_ThrowsInvalidOperationException()
    {
        using var provider = _services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IParserEndpointValidator>();

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void GetRequiredService_WithOnlyParserEndpointValidatorRegistered_ThrowsInvalidOperationException()
    {
        _services.AddSingleton<IParserEndpointValidator, ParserEndpointValidator>();

        using var provider = _services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IParserEndpointValidator>();

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void AddWeatherParsing_WithoutFurtherRegistration_ResolvesParserEndpointValidator()
    {
        _services.AddWeatherParsing();

        using var provider = _services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IParserEndpointValidator>();

        validator.Should().BeOfType<ParserEndpointValidator>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvedValidator_PerformsRealValidation()
    {
        _services.AddWeatherParsing();

        using var provider = _services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IParserEndpointValidator>();

        var endpoint = new ParserEndpointDefinition("Mosmix", new Uri("https://example.org/mosmix"));
        var result = validator.ValidateAndMap([endpoint]);

        result.Should().ContainKey("Mosmix");
    }

    [TestMethod]
    public void AddWeatherParsing_WithoutFurtherRegistration_ResolvesExportSerializers()
    {
        _services.AddWeatherParsing();

        using var provider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        
        provider.GetRequiredService<IJsonExportSerializer>().Should().BeOfType<JsonExportSerializer>();
        provider.GetRequiredService<IXmlExportSerializer>().Should().BeOfType<XmlExportSerializer>();
    }

    [TestMethod]
    public void AddWeatherParsing_ExportSerializers_AreSingletonsPerContainer()
    {
        _services.AddWeatherParsing();
        
        using var provider1 = _services.BuildServiceProvider();
        var xmlSerializer1 = provider1.GetRequiredService<IXmlExportSerializer>();
        var xmlSerializer2 = provider1.GetRequiredService<IXmlExportSerializer>();
        
        xmlSerializer1.Should().BeSameAs(xmlSerializer2);
        
        _services.AddWeatherParsing();
        using var provider2 = _services.BuildServiceProvider();
        var xmlSerializer3 = provider2.GetRequiredService<IXmlExportSerializer>();
        
        xmlSerializer1.Should().NotBeSameAs(xmlSerializer3);
    }

    [TestMethod]
    public void AddWeatherParsing_WithoutFurtherRegistration_ResolvesListEntryOrdering()
    {
        _services.AddWeatherParsing();

        using var provider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IListEntryOrdering>().Should().BeOfType<ListEntryOrdering>();
    }
}
