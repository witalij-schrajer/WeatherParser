using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;

namespace WeatherParser.Parsing.Tests.Selection;

[TestClass]
public sealed class SelectionDependencyInjectionTests
{
    private const string Base = "https://a.example.invalid/";
    private string _directory = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "wp-di-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    private IServiceCollection Complete() => new ServiceCollection()
        .AddWeatherParsing()
        .AddConsumerBoundary(
            ConsumerBoundary.CdcOptions(Base),
            ConsumerBoundary.MosmixOptions(Base, _directory),
            new StaticHttpHandler(),
            TimeProvider.System);

    private static AggregateException ValidationFailure(IServiceCollection services)
    {
        try
        {
            ConsumerBoundary.Build(services).Dispose();
        }
        catch (AggregateException exception)
        {
            return exception;
        }

        throw new AssertFailedException("The container validated although a consumer registration is missing.");
    }

    [TestMethod]
    public void AddWeatherParsing_RegistersNoGlobalUriEndpointOptionsHttpClientOrTimeProvider()
    {
        var services = new ServiceCollection().AddWeatherParsing();
        var serviceTypes = services.Select(d => d.ServiceType).ToArray();

        serviceTypes.Should().NotContain(typeof(Uri));
        serviceTypes.Should().NotContain(typeof(CdcCatalogEndpoints));
        serviceTypes.Should().NotContain(typeof(MosmixCatalogEndpoints));
        serviceTypes.Should().NotContain(typeof(HttpClient));
        serviceTypes.Should().NotContain(typeof(TimeProvider));
        serviceTypes.Should().NotContain(typeof(ILogger<>));
    }

    [TestMethod]
    public void ConsumerRegistration_CompleteBoundary_BuildsValidatedContainer()
    {
        using var provider = ConsumerBoundary.Build(Complete());
        provider.GetRequiredService<ICdcStationSelector>().Should().NotBeNull();
        provider.GetRequiredService<IMosmixLocationSelector>().Should().NotBeNull();
    }

    [TestMethod]
    public void AllP4ServicesResolveFromOneValidatedContainer()
    {
        using var provider = ConsumerBoundary.Build(Complete());
        var types = new[]
        {
            typeof(ICoordinateParser),
            typeof(ISearchParameterFactory),
            typeof(IDistanceCalculator),
            typeof(ISearchAreaFilter),
            typeof(ICdcStationCatalogReader),
            typeof(ICdcNowEvidenceReader),
            typeof(ICdcAvailabilityDeriver),
            typeof(IMosmixLocationCatalogReader),
            typeof(IMosmixCatalogEndpointsValidator),
            typeof(IMosmixLEvidenceReader),
            typeof(IMosmixEvidenceCache),
            typeof(IMosmixAvailabilityResolver),
            typeof(ICatalogDownloader),
            typeof(ICdcCatalogEndpointsValidator),
            typeof(ICdcCatalogSource),
            typeof(IMosmixCatalogSource),
            typeof(IMosmixSEvidenceRefresher),
            typeof(ICdcStationSelector),
            typeof(IMosmixLocationSelector),
            typeof(IListEntryOrdering)
        };

        foreach (var type in types)
        {
            var first = provider.GetRequiredService(type);
            first.Should().NotBeNull();
            var second = provider.GetRequiredService(type);
            second.Should().BeSameAs(first);
        }
    }

    [TestMethod]
    public void MosmixSEvidenceRefresherAndCache_ResolveFromSameContainer()
    {
        using var provider = ConsumerBoundary.Build(Complete());
        var refresher = provider.GetRequiredService<IMosmixSEvidenceRefresher>();
        var cache = provider.GetRequiredService<IMosmixEvidenceCache>();
        var selector = provider.GetRequiredService<IMosmixLocationSelector>();
        refresher.Should().NotBeNull();
        cache.Should().NotBeNull();
        selector.Should().NotBeNull();
    }

    [DataRow("cdc")]
    [DataRow("mosmix")]
    [TestMethod]
    public void MissingEndpointOptions_FailsAtValidationWithActionableMessage(string missing)
    {
        var services = new ServiceCollection()
            .AddWeatherParsing()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton(new HttpClient())
            .AddSingleton(TimeProvider.System);

        if (missing == "cdc")
        {
            services.AddSingleton(ConsumerBoundary.MosmixOptions(Base, _directory));
        }
        else
        {
            services.AddSingleton(ConsumerBoundary.CdcOptions(Base));
        }

        var exception = ValidationFailure(services);
        exception.ToString().Should().Contain(missing == "cdc" ? "CdcCatalogEndpoints" : "MosmixCatalogEndpoints");
    }

    [TestMethod]
    public void MissingHttpDependency_FailsAtValidation()
    {
        var services = new ServiceCollection()
            .AddWeatherParsing()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton(TimeProvider.System)
            .AddSingleton(ConsumerBoundary.CdcOptions(Base))
            .AddSingleton(ConsumerBoundary.MosmixOptions(Base, _directory));

        var exception = ValidationFailure(services);
        exception.ToString().Should().Contain("HttpClient");
    }

    [DataRow("time")]
    [DataRow("logger")]
    [TestMethod]
    public void MissingTimeSourceOrLogger_FailsAtValidation(string missing)
    {
        var services = new ServiceCollection()
            .AddWeatherParsing()
            .AddSingleton(new HttpClient())
            .AddSingleton(ConsumerBoundary.CdcOptions(Base))
            .AddSingleton(ConsumerBoundary.MosmixOptions(Base, _directory));

        if (missing == "time")
        {
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        }
        else
        {
            services.AddSingleton(TimeProvider.System);
        }

        var exception = ValidationFailure(services);
        exception.ToString().Should().Contain(missing == "time" ? "TimeProvider" : "ILogger");
    }

    [TestMethod]
    public void MissingMosmixSEvidenceMaxAge_FailsAtConstructionWithEVIDENCE_CACHE_SETTINGS_INVALID()
    {
        var services = new ServiceCollection()
            .AddWeatherParsing()
            .AddConsumerBoundary(
                ConsumerBoundary.CdcOptions(Base),
                new MosmixCatalogEndpoints
                {
                    CatalogUri = new Uri(Base + "mosmix/mosmix_stationskatalog.cfg?view=nasPublication"),
                    MosmixLSingleStationsDirectory = new Uri(Base + "mosmix/single_stations/"),
                    MosmixSAllStationsUri = new Uri(Base + "mosmix/all_stations/kml/MOSMIX_S_LATEST_240.kmz"),
                    EvidenceCacheDirectory = _directory,
                    MosmixSEvidenceMaxAge = null,
                    MosmixSRefreshDeadline = TimeSpan.FromMinutes(10),
                    MaxEvidenceCandidates = 100,
                    SelectionDeadline = TimeSpan.FromSeconds(120)
                },
                new StaticHttpHandler(),
                TimeProvider.System);

        using var provider = ConsumerBoundary.Build(services);
        FluentActions.Invoking(() => provider.GetRequiredService<IMosmixLocationSelector>())
            .Should().Throw<EvidenceCacheException>()
            .Which.ErrorId.Should().Be(EvidenceCacheException.SettingsInvalidErrorId);
    }

    [DataRow(0, 120)]
    [DataRow(10001, 120)]
    [DataRow(100, 0)]
    [TestMethod]
    public void InvalidSelectionLimit_FailsAtConstructionWithSELECTION_LIMIT_INVALID(int maxEvidenceCandidates, int deadlineSeconds)
    {
        var cdc = ConsumerBoundary.CdcOptions(Base);
        cdc.MaxEvidenceCandidates = maxEvidenceCandidates;
        cdc.SelectionDeadline = TimeSpan.FromSeconds(deadlineSeconds);

        var services = new ServiceCollection()
            .AddWeatherParsing()
            .AddConsumerBoundary(
                cdc,
                ConsumerBoundary.MosmixOptions(Base, _directory),
                new StaticHttpHandler(),
                TimeProvider.System);

        using var provider = ConsumerBoundary.Build(services);
        FluentActions.Invoking(() => provider.GetRequiredService<ICdcStationSelector>())
            .Should().Throw<SelectionLimitException>()
            .Which.ErrorId.Should().Be(SelectionLimitException.LimitInvalidErrorId);
    }

    [TestMethod]
    public void InvalidDirectoryUri_FailsAtConstructionWithENDPOINT_INVALID()
    {
        var cdc = ConsumerBoundary.CdcOptions(Base);
        cdc.AirTemperatureNowDirectory = new Uri(Base + "cdc/air_temperature/now");

        var services = new ServiceCollection()
            .AddWeatherParsing()
            .AddConsumerBoundary(
                cdc,
                ConsumerBoundary.MosmixOptions(Base, _directory),
                new StaticHttpHandler(),
                TimeProvider.System);

        using var provider = ConsumerBoundary.Build(services);
        FluentActions.Invoking(() => provider.GetRequiredService<ICdcStationSelector>())
            .Should().Throw<ParserEndpointValidationException>()
            .Which.ErrorId.Should().Be(ParserEndpointValidationException.EndpointInvalidErrorId);
    }
}
