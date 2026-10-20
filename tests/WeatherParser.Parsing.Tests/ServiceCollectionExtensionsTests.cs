using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;
using WeatherParser.Parsing.Quantities;

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

    [TestMethod]
    public void AddWeatherParsing_WithoutFurtherRegistration_ResolvesTimeServices()
    {
        _services.AddWeatherParsing();

        using var provider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<ITimeZoneResolver>().Should().BeOfType<TimeZoneResolver>();
        provider.GetRequiredService<ISourceTimeNormalizer>().Should().BeOfType<SourceTimeNormalizer>();
    }

    [TestMethod]
    public void AddWeatherParsing_WithoutFurtherRegistration_ResolvesQuantityServices()
    {
        _services.AddWeatherParsing();

        using var provider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IOutputRounding>().Should().BeOfType<OutputRounding>();
        provider.GetRequiredService<IPressureConverter>().Should().BeOfType<PressureConverter>();
        provider.GetRequiredService<IQuantityValidator>().Should().BeOfType<QuantityValidator>();
        provider.GetRequiredService<IPeriodRateConverter>().Should().BeOfType<PeriodRateConverter>();
        provider.GetRequiredService<IWindSpeedConverter>().Should().BeOfType<WindSpeedConverter>();
    }

    [TestMethod]
    public void AddWeatherParsing_QuantityServices_AreSingletonsPerContainer()
    {
        _services.AddWeatherParsing();

        using var provider = _services.BuildServiceProvider();

        var rounding1 = provider.GetRequiredService<IOutputRounding>();
        var rounding2 = provider.GetRequiredService<IOutputRounding>();
        rounding1.Should().BeSameAs(rounding2);

        var pressure1 = provider.GetRequiredService<IPressureConverter>();
        var pressure2 = provider.GetRequiredService<IPressureConverter>();
        pressure1.Should().BeSameAs(pressure2);

        var validator1 = provider.GetRequiredService<IQuantityValidator>();
        var validator2 = provider.GetRequiredService<IQuantityValidator>();
        validator1.Should().BeSameAs(validator2);

        var period1 = provider.GetRequiredService<IPeriodRateConverter>();
        var period2 = provider.GetRequiredService<IPeriodRateConverter>();
        period1.Should().BeSameAs(period2);

        var wind1 = provider.GetRequiredService<IWindSpeedConverter>();
        var wind2 = provider.GetRequiredService<IWindSpeedConverter>();
        wind1.Should().BeSameAs(wind2);

        var converted = pressure1.FromHpa(1000.5m, PressureReference.SeaLevel);
        converted.Hpa.Should().Be(1001m);
        converted.MmHg.Should().Be(750m);
    }

    [TestMethod]
    public void AddWeatherParsing_WithoutFurtherRegistration_ResolvesObservationDatasetBuilder()
    {
        _services.AddWeatherParsing();

        using var provider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IObservationDatasetBuilder>().Should().BeOfType<ObservationDatasetBuilder>();
    }

    [TestMethod]
    public void AddWeatherParsing_ObservationDatasetBuilder_IsSingletonPerContainer()
    {
        _services.AddWeatherParsing();

        using var provider = _services.BuildServiceProvider();
        provider.GetRequiredService<IObservationDatasetBuilder>().Should().BeSameAs(provider.GetRequiredService<IObservationDatasetBuilder>());
    }
}