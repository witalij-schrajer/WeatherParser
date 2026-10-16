using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests;

[TestClass]
public sealed class ServiceCollectionExtensionsTests
{
    [TestMethod]
    public void GetRequiredService_WithoutAnyRegistration_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();

        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IParserEndpointValidator>();

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void GetRequiredService_WithOnlyParserEndpointValidatorRegistered_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IParserEndpointValidator, ParserEndpointValidator>();

        using var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IParserEndpointValidator>();

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void AddWeatherParsing_WithoutFurtherRegistration_ResolvesParserEndpointValidator()
    {
        var services = new ServiceCollection();
        services.AddWeatherParsing();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IParserEndpointValidator>();

        validator.Should().BeOfType<ParserEndpointValidator>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvedValidator_PerformsRealValidation()
    {
        var services = new ServiceCollection();
        services.AddWeatherParsing();

        using var provider = services.BuildServiceProvider();
        var validator = provider.GetRequiredService<IParserEndpointValidator>();

        var endpoint = new ParserEndpointDefinition("Mosmix", new Uri("https://example.org/mosmix"));
        var result = validator.ValidateAndMap([endpoint]);

        result.Should().ContainKey("Mosmix");
    }
}
