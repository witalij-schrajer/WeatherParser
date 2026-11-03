using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Evidence.MosmixL;
using WeatherParser.Parsing.Tests.Evidence.MosmixS;
using WeatherParser.Parsing.Tests.Parsing;
using WeatherParser.Parsing.Tests.Selection;

namespace WeatherParser.Parsing.Tests.Live;

internal static class LiveCheckTestData
{
    public const string Base = "https://a.example.invalid/";
    public static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    public static LiveCheckSettings Settings(string directory, int cdcCandidates = 10, int mosmixCandidates = 10, bool refreshFirst = true)
    {
        var cdc = ConsumerBoundary.CdcOptions(Base);
        cdc.MaxEvidenceCandidates = cdcCandidates;
        var mosmix = ConsumerBoundary.MosmixOptions(Base, directory);
        mosmix.MaxEvidenceCandidates = mosmixCandidates;
        return new LiveCheckSettings(cdc, mosmix, new SearchParameters { Latitude = 50.1m, Longitude = 9.5m, AltitudeM = 200m, MaxDistanceKm = 50m, MaxAltitudeDifferenceM = 500m }, refreshFirst);
    }

    public static void Arrange(StaticHttpHandler handler, LiveCheckSettings settings)
    {
        var placemark = MosmixSKmzBuilder.Placemark("X123A", MosmixSKmzBuilder.Forecast("TTT", "281.15 - 280.95"), MosmixSKmzBuilder.Forecast("FF", "3.10 - -"));
        handler.Add(settings.Mosmix.MosmixSAllStationsUri!, MosmixSKmzBuilder.Kmz(MosmixSKmzBuilder.Kml(placemarks: new[] { placemark })));

        var catalogLine1 = MosmixCatalogBuilder.Line("X123A", name: "MUSTERHAUSEN", latitude: "50.06", longitude: "9.30", elevation: "250");
        var catalogLine2 = MosmixCatalogBuilder.Line("01234", name: "BEISPIELDORF", latitude: "50.12", longitude: "9.36", elevation: "300");
        handler.Add(settings.Mosmix.CatalogUri!, MosmixCatalogBuilder.Catalog(catalogLine1, catalogLine2));

        handler.Add(
            new Uri(settings.Mosmix.MosmixLSingleStationsDirectory!, "X123A/kml/MOSMIX_L_LATEST_X123A.kmz"),
            MosmixKmzBuilder.Kmz(MosmixKmzBuilder.Kml("X123A")));

        handler.AddStatus(
            new Uri(settings.Mosmix.MosmixLSingleStationsDirectory!, "01234/kml/MOSMIX_L_LATEST_01234.kmz"),
            System.Net.HttpStatusCode.NotFound);

        var line = CdcStationListBuilder.Line("00044", toDate: "20261021", altitude: "123", latitude: "50.1234", longitude: "9.5678", name: "Musterhausen");
        var stationList = CdcStationListBuilder.Stream(new[] { line }).ToArray();
        handler.Add(settings.Cdc.AirTemperatureStationList!, stationList);
        handler.Add(settings.Cdc.WindStationList!, stationList);
        handler.Add(settings.Cdc.PrecipitationStationList!, stationList);
        handler.Add(settings.Cdc.SolarStationList!, stationList);

        var zip = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(CdcNowArchiveBuilder.Header("tu"), new[] { CdcNowArchiveBuilder.Row("00044", "202610211150", "1001.2", "12.3", "10.1", "85.0", "8.9") })));
        handler.Add(new Uri(settings.Cdc.AirTemperatureNowDirectory!, "10minutenwerte_TU_00044_now.zip"), zip);

        handler.AddStatus(new Uri(settings.Cdc.WindNowDirectory!, "10minutenwerte_wind_00044_now.zip"), System.Net.HttpStatusCode.NotFound);
        handler.AddStatus(new Uri(settings.Cdc.PrecipitationNowDirectory!, "10minutenwerte_nieder_00044_now.zip"), System.Net.HttpStatusCode.NotFound);
        handler.AddStatus(new Uri(settings.Cdc.SolarNowDirectory!, "10minutenwerte_SOLAR_00044_now.zip"), System.Net.HttpStatusCode.NotFound);
    }

    public static string IndexPath(LiveCheckSettings settings) => Path.Combine(settings.Mosmix.EvidenceCacheDirectory!, "mosmix_s_evidence_" + MosmixEvidenceIndexTestData.KeyOf(settings.Mosmix.MosmixSAllStationsUri!.OriginalString)[..16] + ".json");

    public static Task WriteIndexAsync(LiveCheckSettings settings, DateTime downloadedAtUtc) => new FileSystemMosmixEvidenceCache(
        settings.Mosmix,
        new MosmixCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator())))
        .WriteAsync(
            MosmixEvidenceIndexTestData.Index(new Dictionary<string, IReadOnlyList<string>> { ["X123A"] = new[] { "FF", "TTT" } })
                with
                {
                    SourceUri = settings.Mosmix.MosmixSAllStationsUri!.AbsoluteUri,
                    DownloadedAtUtc = downloadedAtUtc
                },
            CancellationToken.None);
}
