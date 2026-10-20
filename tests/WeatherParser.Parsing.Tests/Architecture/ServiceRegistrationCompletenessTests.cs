using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Architecture;

[TestClass]
public sealed class ServiceRegistrationCompletenessTests
{
    private static readonly Assembly Library = typeof(ServiceCollectionExtensions).Assembly;

    private static ServiceProvider BuildValidated(IServiceCollection services) => 
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    [TestMethod]
    public void AddWeatherParsing_RegistersEveryVisibleServiceInterfaceAsSingletonOfItsOnlyImplementation()
    {
        var services = new ServiceCollection().AddWeatherParsing();
        using var provider = BuildValidated(services);

        var interfaces = Library.GetTypes()
            .Where(t => t.IsInterface && t.IsVisible && !t.ContainsGenericParameters)
            .ToList();

        interfaces.Count.Should().BeGreaterThanOrEqualTo(14);

        foreach (var iface in interfaces)
        {
            var implementations = Library.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsVisible && !t.ContainsGenericParameters && iface.IsAssignableFrom(t))
                .ToList();

            implementations.Should().ContainSingle(iface.Name);
            
            var descriptor = services.Should().ContainSingle(d => d.ServiceType == iface, iface.Name).Subject;
            descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton, iface.Name);
            descriptor.ImplementationType.Should().Be(implementations[0], iface.Name);
            provider.GetRequiredService(iface).Should().BeOfType(implementations[0]);
        }
    }

    [TestMethod]
    public void AddWeatherParsing_AddsOnlyClosedTypeBasedDescriptors()
    {
        var services = new ServiceCollection().AddWeatherParsing();
        services.Should().NotBeEmpty();

        foreach (var d in services)
        {
            d.ImplementationFactory.Should().BeNull();
            d.ImplementationInstance.Should().BeNull();
            d.ImplementationType.Should().NotBeNull();
            d.ServiceType.ContainsGenericParameters.Should().BeFalse();
            d.ImplementationType!.ContainsGenericParameters.Should().BeFalse();
        }
    }

    [TestMethod]
    public void NonVisibleInterfaces_AreOnlyTheEntityValidationHook()
    {
        Library.GetTypes()
            .Where(t => t.IsInterface && !t.IsVisible)
            .Select(t => t.Name)
            .Should()
            .Equal("IExportRootValidation");
    }

    [TestMethod]
    public void ServiceProvider_MissingDependency_FailsOnBuild()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IForecastRunBuilder, ForecastRunBuilder>();
        FluentActions.Invoking(() => BuildValidated(services))
            .Should()
            .Throw<AggregateException>()
            .WithInnerException<InvalidOperationException>();
    }
}
