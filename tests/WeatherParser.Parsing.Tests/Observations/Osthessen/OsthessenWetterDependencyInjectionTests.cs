using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class OsthessenWetterDependencyInjectionTests
{
    private static ServiceProvider Build(IServiceCollection services) => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    private sealed class PageHandler(string html) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (Requests)
            {
                Requests.Add(request.RequestUri!);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            });
        }
    }

    private static string Page(params string[] names) => OsthessenWetterHtml.Page(OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(), names.Select(n => OsthessenWetterHtml.OverviewRow(n)).ToArray()), null);

    private static IServiceCollection Services(PageHandler handler, Uri pageUri, params string[] metadataNames)
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.Replace(ServiceDescriptor.Singleton(new HttpClient(handler)));
        services.Replace(ServiceDescriptor.Singleton(new OsthessenWetterEndpoints { PageUri = pageUri }));
        services.Replace(ServiceDescriptor.Singleton(new OsthessenWetterOptions
        {
            Stations = metadataNames.Select(n => new OsthessenWetterStationMetadata { Name = n, Latitude = 50.5m, Longitude = 9.7m }).ToList(),
            PageDeadline = TimeSpan.FromSeconds(60)
        }));
        return services;
    }

    [TestMethod]
    public void AddWeatherParsing_WithConsumerOptions_ResolvesParser()
    {
        using var provider = Build(new ServiceCollection().AddWeatherParsing().AddTestConsumerServices());
        var parser = provider.GetRequiredService<IOsthessenWetterParser>();
        parser.Should().BeOfType<OsthessenWetterParser>();
    }

    [TestMethod]
    public void AddWeatherParsing_MissingOsthessenOptions_FailsValidateOnBuild()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.RemoveAll<OsthessenWetterOptions>();
        FluentActions.Invoking(() => Build(services))
            .Should().Throw<AggregateException>()
            .Which.Message.Should().Contain(nameof(OsthessenWetterOptions));
    }

    [TestMethod]
    public void AddWeatherParsing_MissingOsthessenEndpoints_FailsValidateOnBuild()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.RemoveAll<OsthessenWetterEndpoints>();
        FluentActions.Invoking(() => Build(services))
            .Should().Throw<AggregateException>()
            .Which.Message.Should().Contain(nameof(OsthessenWetterEndpoints));
    }

    [TestMethod]
    public void AddWeatherParsing_RegistersNoOsthessenOptionsOrEndpoints()
    {
        var services = new ServiceCollection().AddWeatherParsing();
        services.Should().NotContain(s => s.ServiceType == typeof(OsthessenWetterOptions));
        services.Should().NotContain(s => s.ServiceType == typeof(OsthessenWetterEndpoints));
    }

    [TestMethod]
    public async Task AddWeatherParsing_InvalidDeadline_FailsAtResolution()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.Replace(ServiceDescriptor.Singleton(new OsthessenWetterOptions { PageDeadline = TimeSpan.Zero }));
        using var provider = Build(services);
        FluentActions.Invoking(() => provider.GetRequiredService<IOsthessenWetterParser>())
            .Should().Throw<OsthessenWetterException>()
            .Which.ErrorId.Should().Be(OsthessenWetterException.OptionsInvalidErrorId);
    }

    [TestMethod]
    public async Task AddWeatherParsing_InvalidMetadata_FailsAtResolutionWithoutRequest()
    {
        var handler = new PageHandler(Page("Bimbach"));
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.Replace(ServiceDescriptor.Singleton(new HttpClient(handler)));
        services.Replace(ServiceDescriptor.Singleton(new OsthessenWetterEndpoints { PageUri = new Uri("https://example.org/messwerte/") }));
        services.Replace(ServiceDescriptor.Singleton(new OsthessenWetterOptions
        {
            Stations = [new OsthessenWetterStationMetadata { Name = "Bimbach", Latitude = 91m, Longitude = 9.7m }],
            PageDeadline = TimeSpan.FromSeconds(60)
        }));
        using var provider = Build(services);
        FluentActions.Invoking(() => provider.GetRequiredService<IOsthessenWetterParser>())
            .Should().Throw<OsthessenWetterException>()
            .Which.ErrorId.Should().Be(OsthessenWetterException.OptionsInvalidErrorId);
        handler.Requests.Should().BeEmpty();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task AddWeatherParsing_TwoContainers_IndependentEndpointsAndMetadata()
    {
        var handlerA = new PageHandler(Page("Bimbach", "Schlitz"));
        var handlerB = new PageHandler(Page("Bimbach", "Schlitz"));

        using var providerA = Build(Services(handlerA, new Uri("https://a.example.invalid/messwerte/"), "Bimbach"));
        using var providerB = Build(Services(handlerB, new Uri("https://b.example.invalid/messwerte/"), "Schlitz"));

        var parserA = providerA.GetRequiredService<IOsthessenWetterParser>();
        var parserB = providerB.GetRequiredService<IOsthessenWetterParser>();

        var resultA = await parserA.ParseAsync("*", CancellationToken.None);
        var resultB = await parserB.ParseAsync("*", CancellationToken.None);

        resultA.Datasets.Select(d => d.Location.StationId).Should().Equal("bimbach");
        resultA.StationFailures.Select(f => (f.StationName, f.ErrorId)).Should().Equal(("Schlitz", OsthessenWetterNoticeIds.StationMetadataMissing));

        resultB.Datasets.Select(d => d.Location.StationId).Should().Equal("schlitz");
        resultB.StationFailures.Select(f => (f.StationName, f.ErrorId)).Should().Equal(("Bimbach", OsthessenWetterNoticeIds.StationMetadataMissing));

        handlerA.Requests.Select(u => u.AbsoluteUri).Should().Equal("https://a.example.invalid/messwerte/");
        handlerB.Requests.Select(u => u.AbsoluteUri).Should().Equal("https://b.example.invalid/messwerte/");
    }

    [TestMethod]
    public void AddWeatherParsing_ParserSingleton()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = Build(services);

        var parser1 = provider.GetRequiredService<IOsthessenWetterParser>();
        var parser2 = provider.GetRequiredService<IOsthessenWetterParser>();

        parser1.Should().BeSameAs(parser2);
    }
}
