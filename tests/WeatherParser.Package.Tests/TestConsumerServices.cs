using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherParser.Parsing.Evidence.MosmixS;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Registers what a consumer must register next to AddWeatherParsing (SC-08, SC-12): logging (a no-op logger here) and valid MOSMIX catalog endpoint options with placeholder URIs.
/// Tests that observe log calls use strict mocks instead.
/// </summary>
internal static class TestConsumerServices
{
    public static IServiceCollection AddTestConsumerServices(this IServiceCollection services)
    {
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(new MosmixCatalogEndpoints
        {
            CatalogUri = new Uri("https://example.org/mosmix/stations.cfg"),
            MosmixLSingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/"),
            MosmixSAllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz"),
            EvidenceCacheDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-test-evidence"),
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
        });
        return services;
    }
}
