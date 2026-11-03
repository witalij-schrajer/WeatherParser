using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Catalogs;

namespace WeatherParser.Parsing.Tests.Observations.Dwd;

[TestClass]
public sealed class DwdObservationDependencyInjectionTests
{
    private static ServiceProvider Build(IServiceCollection services) => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (Requests)
            {
                Requests.Add(request.RequestUri!);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class PendingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private static ServiceProvider BuildWithDeadline(ManualTimeProvider clock, TimeSpan deadline)
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.Replace(ServiceDescriptor.Singleton(new DwdObservationOptions { ObservationDeadline = deadline }));
        services.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));
        services.Replace(ServiceDescriptor.Singleton(new HttpClient(new PendingHandler()) { Timeout = Timeout.InfiniteTimeSpan }));
        return Build(services);
    }

    [TestMethod]
    public void AddWeatherParsing_WithConsumerOptions_ResolvesDwdObservationParser()
    {
        using var provider = Build(new ServiceCollection().AddWeatherParsing().AddTestConsumerServices());
        var parser = provider.GetRequiredService<IDwdObservationParser>();
        parser.Should().BeOfType<DwdObservationParser>();
    }

    [TestMethod]
    public void AddWeatherParsing_WithoutDwdObservationOptions_FailsAtBuildValidation()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.RemoveAll<DwdObservationOptions>();
        FluentActions.Invoking(() => Build(services))
            .Should().Throw<AggregateException>()
            .Which.Message.Should().Contain(nameof(DwdObservationOptions));
    }

    [TestMethod]
    public void AddWeatherParsing_RegistersNoHttpClientTimeProviderOrOptions()
    {
        var services = new ServiceCollection().AddWeatherParsing();
        services.Should().NotContain(s => s.ServiceType == typeof(HttpClient));
        services.Should().NotContain(s => s.ServiceType == typeof(TimeProvider));
        services.Should().NotContain(s => s.ServiceType == typeof(DwdObservationOptions));
        services.Should().NotContain(s => s.ServiceType == typeof(CdcCatalogEndpoints));
    }

    [TestMethod]
    public void AddWeatherParsing_ParserIsSingleton()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = Build(services);

        var parser1 = provider.GetRequiredService<IDwdObservationParser>();
        var parser2 = provider.GetRequiredService<IDwdObservationParser>();

        parser1.Should().BeSameAs(parser2);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task AddWeatherParsing_TwoContainersDifferentDeadlines_Independent()
    {
        var clockA = new ManualTimeProvider(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero));
        var clockB = new ManualTimeProvider(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero));
        using var providerA = BuildWithDeadline(clockA, TimeSpan.FromSeconds(10));
        using var providerB = BuildWithDeadline(clockB, TimeSpan.FromSeconds(20));
        var parseA = providerA.GetRequiredService<IDwdObservationParser>().ParseAsync("00044", CancellationToken.None);
        var parseB = providerB.GetRequiredService<IDwdObservationParser>().ParseAsync("00044", CancellationToken.None);

        clockA.Advance(TimeSpan.FromSeconds(15));
        clockB.Advance(TimeSpan.FromSeconds(15));
        await FluentActions.Awaiting(() => parseA).Should().ThrowAsync<ObservationLimitException>().Where(ex => ex.ErrorId == ObservationLimitException.DeadlineExceededErrorId);
        parseB.IsCompleted.Should().BeFalse(); // 15 s is below the second container's 20 s deadline

        clockB.Advance(TimeSpan.FromSeconds(10));
        await FluentActions.Awaiting(() => parseB).Should().ThrowAsync<ObservationLimitException>().Where(ex => ex.ErrorId == ObservationLimitException.DeadlineExceededErrorId);
    }

    [TestMethod]
    public async Task AddWeatherParsing_ParserUsesConsumerHttpClient()
    {
        var handler = new RecordingHandler();
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.Replace(ServiceDescriptor.Singleton(new HttpClient(handler)));

        using var provider = Build(services);
        var parser = provider.GetRequiredService<IDwdObservationParser>();

        await FluentActions.Invoking(async () => await parser.ParseAsync("00044", CancellationToken.None))
            .Should().ThrowAsync<CatalogRetrievalException>();

        handler.Requests.Should().ContainSingle().Which.AbsoluteUri.Should().Be("https://example.org/cdc/air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt");
    }
}
