using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Forecasts.Mosmix;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public sealed class MosmixDependencyInjectionTests
{
    private MockRepository _mocks = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private static IServiceCollection Services() => new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();

    private static ServiceProvider Build(IServiceCollection services) => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    // Serves the given KMZ for every request through a strict handler, records the requested URIs, and uses a fixed clock.
    private ServiceProvider ProviderWithFile(byte[] kmz, List<Uri> requests, MosmixLForecastOptions lOptions, MosmixSForecastOptions sOptions, Uri? lDirectory = null, Uri? sUri = null)
    {
        var handler = _mocks.Create<HttpMessageHandler>();
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback((HttpRequestMessage request, CancellationToken _) => requests.Add(request.RequestUri!))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(kmz) });
        var services = Services();
        services.Replace(ServiceDescriptor.Singleton(new HttpClient(handler.Object, disposeHandler: false)));
        services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new ManualTimeProvider(new DateTimeOffset(2026, 10, 26, 16, 16, 30, TimeSpan.Zero))));
        services.Replace(ServiceDescriptor.Singleton(lOptions));
        services.Replace(ServiceDescriptor.Singleton(sOptions));
        if (lDirectory is not null) { services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastEndpoints { SingleStationsDirectory = lDirectory })); }
        if (sUri is not null) { services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastEndpoints { AllStationsUri = sUri })); }
        return Build(services);
    }

    [TestMethod]
    public void AddWeatherParsing_BothParsersResolveWithConsumerOptions()
    {
        using var provider = Build(Services());
        var parserL = provider.GetRequiredService<IMosmixLParser>();
        var parserS = provider.GetRequiredService<IMosmixSParser>();
        var pipeline = provider.GetRequiredService<IMosmixParsePipeline>();

        parserL.Should().BeOfType<MosmixLParser>();
        parserS.Should().BeOfType<MosmixSParser>();
        parserL.Should().BeSameAs(provider.GetRequiredService<IMosmixLParser>());
        parserS.Should().BeSameAs(provider.GetRequiredService<IMosmixSParser>());
        pipeline.Should().BeSameAs(provider.GetRequiredService<IMosmixParsePipeline>());
    }

    [TestMethod]
    public async Task AddWeatherParsing_IndependentContainers_IndependentOptions()
    {
        var lOptionsA = new MosmixLForecastOptions { MaxRunAge = TimeSpan.FromHours(8) };
        var sOptionsA = new MosmixSForecastOptions { MaxRunAge = TimeSpan.FromHours(2) };

        var lOptionsB = new MosmixLForecastOptions { MaxRunAge = TimeSpan.FromHours(1) };
        var sOptionsB = new MosmixSForecastOptions { MaxRunAge = TimeSpan.FromHours(1) };

        var kmzL = MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15")), memberName: MosmixForecastKmzBuilder.SingleMemberName);
        var kmzS = MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(issueTime: "2026-10-26T15:00:00.000Z", timeSteps: 3), MosmixForecastKmzBuilder.Placemark("10637", "BEISPIELSTADT", "9.6,50.5,250.0", MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15"))), memberName: MosmixForecastKmzBuilder.AllStationsMemberName);

        var requestsL = new List<Uri>();
        var requestsS = new List<Uri>();

        using (var providerA = ProviderWithFile(kmzL, requestsL, lOptionsA, sOptionsA))
        using (var providerB = ProviderWithFile(kmzL, requestsL, lOptionsB, sOptionsB))
        {
            var parserA = providerA.GetRequiredService<IMosmixLParser>();
            var parserB = providerB.GetRequiredService<IMosmixLParser>();

            parserA.Should().NotBeSameAs(parserB);

            var resultA = await parserA.ParseAsync("10637", CancellationToken.None);
            resultA.Run.RunAgeExceeded.Should().BeFalse();
            resultA.Freshness.Should().Be(MosmixRunFreshness.Fresh);

            var resultB = await parserB.ParseAsync("10637", CancellationToken.None);
            resultB.Run.RunAgeExceeded.Should().BeTrue();
            resultB.Freshness.Should().Be(MosmixRunFreshness.Stale);

            // Test S product
            using (var providerA_S = ProviderWithFile(kmzS, requestsS, lOptionsA, sOptionsA))
            using (var providerB_S = ProviderWithFile(kmzS, requestsS, lOptionsB, sOptionsB))
            {
                var parserA_S = providerA_S.GetRequiredService<IMosmixSParser>();
                var parserB_S = providerB_S.GetRequiredService<IMosmixSParser>();

                var resultA_S = await parserA_S.ParseAsync("10637", CancellationToken.None);
                resultA_S.Run.RunAgeExceeded.Should().BeFalse();
                resultA_S.Freshness.Should().Be(MosmixRunFreshness.Fresh);

                var resultB_S = await parserB_S.ParseAsync("10637", CancellationToken.None);
                resultB_S.Run.RunAgeExceeded.Should().BeTrue();
                resultB_S.Freshness.Should().Be(MosmixRunFreshness.Stale);

                // Ensure parsers are different instances
                parserA_S.Should().NotBeSameAs(parserB_S);
            }
        }
    }

    [TestMethod]
    public async Task AddWeatherParsing_EndpointsForwardedUnchanged()
    {
        var lDirectory = new Uri("https://l.example.org:8443/x/single_stations/");
        var sUri = new Uri("https://s.example.org/y/MOSMIX_S_LATEST_240.kmz?v=1");

        var services = Services();
        services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastEndpoints { SingleStationsDirectory = lDirectory }));
        services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastEndpoints { AllStationsUri = sUri }));

        var kmzL = MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15")), memberName: MosmixForecastKmzBuilder.SingleMemberName);
        var kmzS = MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(issueTime: "2026-10-26T15:00:00.000Z", timeSteps: 3), MosmixForecastKmzBuilder.Placemark("10637", "BEISPIELSTADT", "9.6,50.5,250.0", MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15"))), memberName: MosmixForecastKmzBuilder.AllStationsMemberName);

        var requestsL = new List<Uri>();
        var requestsS = new List<Uri>();

        using (var providerL = ProviderWithFile(kmzL, requestsL, new MosmixLForecastOptions { MaxRunAge = TimeSpan.FromHours(8) }, new MosmixSForecastOptions { MaxRunAge = TimeSpan.FromHours(2) }, lDirectory, null))
        using (var providerS = ProviderWithFile(kmzS, requestsS, new MosmixLForecastOptions { MaxRunAge = TimeSpan.FromHours(8) }, new MosmixSForecastOptions { MaxRunAge = TimeSpan.FromHours(2) }, null, sUri))
        {
            var sourceL = providerL.GetRequiredService<IMosmixLForecastSource>();
            var sourceS = providerS.GetRequiredService<IMosmixSForecastSource>();

            sourceL.SanitizedEndpoint.Should().Be("https://l.example.org:8443/x/single_stations/");
            sourceS.SanitizedEndpoint.Should().Be("https://s.example.org/y/MOSMIX_S_LATEST_240.kmz");

            // Parse to trigger requests
            var parserL = providerL.GetRequiredService<IMosmixLParser>();
            var parserS = providerS.GetRequiredService<IMosmixSParser>();

            await parserL.ParseAsync("10637", CancellationToken.None);
            await parserS.ParseAsync("10637", CancellationToken.None);

            requestsL.Should().ContainSingle();
            requestsL[0].Should().Be(new Uri("https://l.example.org:8443/x/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz"));

            requestsS.Should().ContainSingle();
            requestsS[0].Should().Be(new Uri("https://s.example.org/y/MOSMIX_S_LATEST_240.kmz?v=1"));
        }
    }

    [TestMethod]
    public void AddWeatherParsing_ValidateOnBuild_Succeeds()
    {
        using var provider = Build(Services());
        provider.GetRequiredService<IMosmixLParser>();
        provider.GetRequiredService<IMosmixSParser>();
        provider.GetRequiredService<IMosmixLForecastSource>();
        provider.GetRequiredService<IMosmixSForecastSource>();
        provider.GetRequiredService<IMosmixParsePipeline>();
        provider.GetRequiredService<IMosmixForecastDocumentReader>();
        provider.GetRequiredService<IMosmixElementMapper>();
        provider.GetRequiredService<IMosmixUnitScaler>();
    }

    [DataRow("MosmixLForecastEndpoints")]
    [DataRow("MosmixSForecastEndpoints")]
    [DataRow("MosmixLForecastOptions")]
    [DataRow("MosmixSForecastOptions")]
    [DataTestMethod]
    public void AddWeatherParsing_MissingMosmixOptions_FailsOnBuild(string missing)
    {
        var services = Services();
        switch (missing)
        {
            case "MosmixLForecastEndpoints":
                services.RemoveAll<MosmixLForecastEndpoints>();
                break;
            case "MosmixSForecastEndpoints":
                services.RemoveAll<MosmixSForecastEndpoints>();
                break;
            case "MosmixLForecastOptions":
                services.RemoveAll<MosmixLForecastOptions>();
                break;
            case "MosmixSForecastOptions":
                services.RemoveAll<MosmixSForecastOptions>();
                break;
        }

        FluentActions.Invoking(() => Build(services))
            .Should().Throw<AggregateException>()
            .Which.Message.Should().Contain(missing);
    }

    [DataRow(false)]
    [DataRow(true)]
    [DataTestMethod]
    public void AddWeatherParsing_UnsetMaxRunAge_FailsWithOptionsInvalid(bool mosmixS)
    {
        var services = Services();
        if (mosmixS)
        {
            services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastOptions()));
        }
        else
        {
            services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastOptions()));
        }

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = true });
        Action resolveUnderTest = mosmixS ? () => provider.GetRequiredService<IMosmixSParser>() : () => provider.GetRequiredService<IMosmixLParser>();
        Action resolveOther = mosmixS ? () => provider.GetRequiredService<IMosmixLParser>() : () => provider.GetRequiredService<IMosmixSParser>();
        var exception = resolveUnderTest.Should().Throw<MosmixForecastException>().Which;
        exception.ErrorId.Should().Be("MOSMIX-OPTIONS-INVALID");
        exception.Product.Should().Be(mosmixS ? MosmixProduct.MosmixS : MosmixProduct.MosmixL);
        resolveOther.Should().NotThrow();
    }

    [TestMethod]
    public void AddWeatherParsing_DefaultDeadlines()
    {
        new MosmixLForecastOptions().ParseDeadline.Should().Be(TimeSpan.FromSeconds(60));
        new MosmixSForecastOptions().ParseDeadline.Should().Be(TimeSpan.FromMinutes(5));
        new MosmixLForecastOptions().MaxRunAge.Should().BeNull();
        new MosmixSForecastOptions().MaxRunAge.Should().BeNull();
    }
}
