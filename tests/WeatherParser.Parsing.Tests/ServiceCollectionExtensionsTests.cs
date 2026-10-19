using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Parsing;

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
}
