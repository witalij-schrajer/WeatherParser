using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Evidence.MosmixL;
using WeatherParser.Parsing.Tests.Evidence.MosmixS;
using WeatherParser.Parsing.Tests.Parsing;

namespace WeatherParser.Parsing.Tests.Selection;

[TestClass]
public sealed class SelectionDependencyInjectionRoutingTests
{
    private readonly List<string> _directories = new();

    [TestCleanup]
    public void Cleanup()
    {
        foreach (var directory in _directories)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private string NewDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "wp-di-route-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        _directories.Add(directory);
        return directory;
    }

    private static void ArrangeCdc(StaticHttpHandler handler, CdcCatalogEndpoints options)
    {
        var line = CdcStationListBuilder.Line("00044", toDate: "20261021", altitude: "123", latitude: "50.1234", longitude: "9.5678", name: "Musterhausen");
        var stationListTu = CdcStationListBuilder.Stream(new[] { line }).ToArray();
        var stationListWind = CdcStationListBuilder.Stream(new[] { line }).ToArray();
        var stationListPrec = CdcStationListBuilder.Stream(new[] { line }).ToArray();
        var stationListSolar = CdcStationListBuilder.Stream(new[] { line }).ToArray();

        handler.Add(options.AirTemperatureStationList!, stationListTu);
        handler.Add(options.WindStationList!, stationListWind);
        handler.Add(options.PrecipitationStationList!, stationListPrec);
        handler.Add(options.SolarStationList!, stationListSolar);

        var row = CdcNowArchiveBuilder.Row("00044", "202610211150", "1001.2", "12.3", "10.1", "85.0", "8.9");
        var zip = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(CdcNowArchiveBuilder.Header("tu"), new[] { row })));
        handler.Add(new Uri(options.AirTemperatureNowDirectory!, "10minutenwerte_TU_00044_now.zip"), zip);
    }

    private static (ServiceProvider Provider, StaticHttpHandler Handler, CdcCatalogEndpoints Cdc, MosmixCatalogEndpoints Mosmix) Container(string baseUri, string directory)
    {
        var handler = new StaticHttpHandler();
        var cdc = ConsumerBoundary.CdcOptions(baseUri);
        var mosmix = ConsumerBoundary.MosmixOptions(baseUri, directory);
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero));
        var provider = ConsumerBoundary.Build(new ServiceCollection().AddWeatherParsing().AddConsumerBoundary(cdc, mosmix, handler, clock));
        return (provider, handler, cdc, mosmix);
    }

    [TestMethod]
    public async Task TwoContainers_DifferentEndpoints_DoNotShareEndpointsOrState()
    {
        var parameters = new SearchParameters { Latitude = 50.1m, Longitude = 9.5m, AltitudeM = 200m, MaxDistanceKm = 50m, MaxAltitudeDifferenceM = 500m };

        var dirA = NewDirectory();
        var (providerA, handlerA, cdcA, _) = Container("https://a.example.invalid/", dirA);
        ArrangeCdc(handlerA, cdcA);
        var resultA = await providerA.GetRequiredService<ICdcStationSelector>().SelectAsync(parameters, CancellationToken.None);

        var dirB = NewDirectory();
        var (providerB, handlerB, cdcB, _) = Container("https://b.example.invalid/", dirB);
        ArrangeCdc(handlerB, cdcB);
        var resultB = await providerB.GetRequiredService<ICdcStationSelector>().SelectAsync(parameters, CancellationToken.None);

        using (providerA)
        using (providerB)
        {
            resultA.Stations.Should().HaveCount(1);
            resultA.Stations[0].StationId.Should().Be("00044");
            resultB.Stations.Should().HaveCount(1);
            resultB.Stations[0].StationId.Should().Be("00044");

            handlerA.Requests.Should().OnlyContain(uri => uri.StartsWith("https://a.example.invalid/"));
            handlerB.Requests.Should().OnlyContain(uri => uri.StartsWith("https://b.example.invalid/"));

            handlerA.Requests.Should().HaveCount(8);
            handlerB.Requests.Should().HaveCount(8);

            var selectorA = providerA.GetRequiredService<ICdcStationSelector>();
            var selectorB = providerB.GetRequiredService<ICdcStationSelector>();
            selectorA.Should().NotBeSameAs(selectorB);

            var downloaderA = providerA.GetRequiredService<ICatalogDownloader>();
            var downloaderB = providerB.GetRequiredService<ICatalogDownloader>();
            downloaderA.Should().NotBeSameAs(downloaderB);
        }
    }

    [TestMethod]
    public async Task Endpoints_ReachSourcesUnchanged()
    {
        var baseUri = "https://a.example.invalid/data/";
        var dir = NewDirectory();
        var (provider, handler, cdc, _) = Container(baseUri, dir);
        ArrangeCdc(handler, cdc);

        var parameters = new SearchParameters { Latitude = 50.1m, Longitude = 9.5m, AltitudeM = 200m, MaxDistanceKm = 50m, MaxAltitudeDifferenceM = 500m };
        await provider.GetRequiredService<ICdcStationSelector>().SelectAsync(parameters, CancellationToken.None);

        using (provider)
        {
            var expectedRequests = new[]
            {
                cdc.AirTemperatureStationList!.AbsoluteUri,
                cdc.WindStationList!.AbsoluteUri,
                cdc.PrecipitationStationList!.AbsoluteUri,
                cdc.SolarStationList!.AbsoluteUri,
                new Uri(cdc.AirTemperatureNowDirectory!, "10minutenwerte_TU_00044_now.zip").AbsoluteUri
            };

            handler.Requests.Should().ContainInOrder(expectedRequests);
            foreach (var request in handler.Requests)
            {
                request.Should().Contain("/data/");
            }
        }
    }

    [TestMethod]
    public async Task MosmixCatalogEndpoint_ReachesSourceUnchanged()
    {
        var baseUri = "https://a.example.invalid/";
        var dir = NewDirectory();
        var (provider, handler, _, mosmix) = Container(baseUri, dir);

        await provider.GetRequiredService<IMosmixEvidenceCache>().WriteAsync(
            MosmixEvidenceIndexTestData.Index() with { SourceUri = "https://a.example.invalid/mosmix/all_stations/kml/MOSMIX_S_LATEST_240.kmz", DownloadedAtUtc = new DateTime(2026, 10, 21, 10, 0, 0, DateTimeKind.Utc) },
            CancellationToken.None);

        var catalogLine = MosmixCatalogBuilder.Line("X123A", name: "MUSTERHAUSEN", latitude: "50.06", longitude: "9.30", elevation: "250");
        handler.Add(mosmix.CatalogUri!, MosmixCatalogBuilder.Catalog(catalogLine));
        handler.Add(new Uri(mosmix.MosmixLSingleStationsDirectory!, "X123A/kml/MOSMIX_L_LATEST_X123A.kmz"), MosmixKmzBuilder.Kmz(MosmixKmzBuilder.Kml("X123A")));

        var parameters = new SearchParameters { Latitude = 50.1m, Longitude = 9.5m, AltitudeM = 200m, MaxDistanceKm = 50m, MaxAltitudeDifferenceM = 500m };
        var result = await provider.GetRequiredService<IMosmixLocationSelector>().SelectAsync(parameters, CancellationToken.None);

        using (provider)
        {
            result.Locations.Should().HaveCount(1);
            result.Locations[0].MosmixId.Should().Be("X123A");

            var expectedRequests = new[]
            {
                mosmix.CatalogUri!.AbsoluteUri,
                new Uri(mosmix.MosmixLSingleStationsDirectory!, "X123A/kml/MOSMIX_L_LATEST_X123A.kmz").AbsoluteUri
            };

            handler.Requests.Should().ContainInOrder(expectedRequests);
        }
    }

    [TestMethod]
    public async Task TwoContainers_DifferentEvidenceCacheDirectories_KeepSeparateIndexes()
    {
        var baseUri = "https://a.example.invalid/";
        var dirA = NewDirectory();
        var dirB = NewDirectory();

        var (providerA, _, _, mosmixA) = Container(baseUri, dirA);
        var (providerB, _, _, mosmixB) = Container(baseUri, dirB);

        await providerA.GetRequiredService<IMosmixEvidenceCache>().WriteAsync(
            MosmixEvidenceIndexTestData.Index() with { SourceUri = "https://a.example.invalid/mosmix/all_stations/kml/MOSMIX_S_LATEST_240.kmz", DownloadedAtUtc = new DateTime(2026, 10, 21, 10, 0, 0, DateTimeKind.Utc) },
            CancellationToken.None);

        var indexA = await providerA.GetRequiredService<IMosmixEvidenceCache>().ReadAsync(CancellationToken.None);
        var indexB = await providerB.GetRequiredService<IMosmixEvidenceCache>().ReadAsync(CancellationToken.None);

        using (providerA)
        using (providerB)
        {
            indexA.Should().NotBeNull();
            indexB.Should().BeNull();

            Directory.GetFiles(dirA).Should().HaveCount(1);
            Directory.GetFiles(dirB).Should().BeEmpty();
        }
    }
}
