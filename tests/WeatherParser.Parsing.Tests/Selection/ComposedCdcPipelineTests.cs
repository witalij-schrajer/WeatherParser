using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Parsing;
using FluentAssertions;

namespace WeatherParser.Parsing.Tests.Selection;

[TestClass]
public sealed class ComposedCdcPipelineTests
{
    [TestMethod]
    public async Task ComposedCdcPipeline_RealSourceReadersDeriverAndSelector()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 21, 12, 0, 0, 500, TimeSpan.Zero));
        var handler = new StaticHttpHandler();
        var logger = new CapturingLogger<CdcStationSelector>();

        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.AddSingleton(new HttpClient(handler));
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton<ILogger<CdcStationSelector>>(logger);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var options = provider.GetRequiredService<CdcCatalogEndpoints>();

        // Station lists
        var stationListTu = CdcStationListBuilder.Stream(new[]
        {
            CdcStationListBuilder.Line("00044", toDate: "20261021", altitude: "123", latitude: "50.1234", longitude: "9.5678", name: "Musterhausen"),
            CdcStationListBuilder.Line("00055", toDate: "20250101", altitude: "150", latitude: "50.2000", longitude: "9.6000", name: "Altdorf"),
            CdcStationListBuilder.Line("00077", toDate: "20261021", altitude: "abc", latitude: "50.3000", longitude: "9.7000", name: "Kaputt"),
            CdcStationListBuilder.Line("00066", toDate: "20261021", altitude: "200", latitude: "50.1500", longitude: "9.5500", name: "Zwist")
        }).ToArray();

        var stationListWind = CdcStationListBuilder.Stream(new[]
        {
            CdcStationListBuilder.Line("00044", toDate: "20261021", altitude: "123", latitude: "50.1234", longitude: "9.5678", name: "Musterhausen"),
            CdcStationListBuilder.Line("00066", toDate: "20261021", altitude: "210", latitude: "50.1500", longitude: "9.5500", name: "Zwist")
        }).ToArray();

        var stationListPrec = CdcStationListBuilder.Stream(new[]
        {
            CdcStationListBuilder.Line("00044", toDate: "20261021", altitude: "123", latitude: "50.1234", longitude: "9.5678", name: "Musterhausen")
        }).ToArray();

        var stationListSolar = CdcStationListBuilder.Stream(new[]
        {
            CdcStationListBuilder.Line("00044", toDate: "20261021", altitude: "123", latitude: "50.1234", longitude: "9.5678", name: "Musterhausen")
        }).ToArray();

        handler.Add(options.AirTemperatureStationList!, stationListTu);
        handler.Add(options.WindStationList!, stationListWind);
        handler.Add(options.PrecipitationStationList!, stationListPrec);
        handler.Add(options.SolarStationList!, stationListSolar);

        // Evidence
        var row = CdcNowArchiveBuilder.Row("00044", "202610211150", "1001.2", "12.3", "10.1", "85.0", "8.9");
        var zip = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(CdcNowArchiveBuilder.Header("tu"), new[] { row })));
        handler.Add(new Uri(options.AirTemperatureNowDirectory!, "10minutenwerte_TU_00044_now.zip"), zip);

        // Parameters
        var parameters = new SearchParameters
        {
            Latitude = 50.1m,
            Longitude = 9.5m,
            AltitudeM = 100m,
            MaxDistanceKm = 50m,
            MaxAltitudeDifferenceM = 500m
        };

        var result = await provider.GetRequiredService<ICdcStationSelector>().SelectAsync(parameters, CancellationToken.None);

        result.Stations.Should().ContainSingle();
        var station = result.Stations[0];
        station.StationId.Should().Be("00044");
        station.Name.Should().Be("Musterhausen");
        station.AltitudeM.Should().Be(123m);
        station.AvailableMeasurements.Should().Equal(
            "humidity_percent",
            "pressure_hpa",
            "pressure_mmhg",
            "temperature_c");

        result.Source.Should().Be("dwd:cdc-10min-now");
        result.RetrievedAtWire.Should().Be("2026-10-21T12:00:00");

        logger.Count("CATALOG-RECORD-SKIPPED").Should().Be(1);
        logger.Count("CDC-STATION-CONFLICT").Should().Be(1);
        logger.Count("SELECTION-EMPTY").Should().Be(0);

        var expectedRequests = new List<string>
        {
            options.AirTemperatureStationList!.AbsoluteUri,
            options.WindStationList!.AbsoluteUri,
            options.PrecipitationStationList!.AbsoluteUri,
            options.SolarStationList!.AbsoluteUri,
            new Uri(options.AirTemperatureNowDirectory!, "10minutenwerte_TU_00044_now.zip").AbsoluteUri,
            new Uri(options.WindNowDirectory!, "10minutenwerte_wind_00044_now.zip").AbsoluteUri,
            new Uri(options.PrecipitationNowDirectory!, "10minutenwerte_nieder_00044_now.zip").AbsoluteUri,
            new Uri(options.SolarNowDirectory!, "10minutenwerte_SOLAR_00044_now.zip").AbsoluteUri
        };

        handler.Requests.Should().BeEquivalentTo(expectedRequests);
        handler.Requests.Should().NotContain(uri => uri.Contains("00055") || uri.Contains("00066") || uri.Contains("00077"));
    }
}
