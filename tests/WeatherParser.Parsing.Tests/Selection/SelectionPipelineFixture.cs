using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Evidence.MosmixL;
using WeatherParser.Parsing.Tests.Evidence.MosmixS;
using WeatherParser.Parsing.Tests.Parsing;

namespace WeatherParser.Parsing.Tests.Selection;

internal sealed class SelectionPipelineFixture : IDisposable
{
    public const string CdcName = "Mühlheim & <Süd>";
    public const string MosmixName = "MÜHLHEIM & <SUED>";

    public ManualTimeProvider Clock { get; }
    public StaticHttpHandler Handler { get; }
    public CapturingLogger<CdcStationSelector> CdcLogger { get; }
    public CapturingLogger<MosmixLocationSelector> MosmixLogger { get; }
    public CapturingLogger<MosmixLocationCatalogReader> ReaderLogger { get; }
    public string Directory { get; }
    public ServiceProvider Provider { get; }
    public CdcCatalogEndpoints CdcOptions { get; }
    public MosmixCatalogEndpoints MosmixOptions { get; }

    public SelectionPipelineFixture(DateTimeOffset start, MqttTopicOptions? mqttOptions = null)
    {
        Directory = Path.Combine(Path.GetTempPath(), "wp-selection-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Directory);

        Clock = new ManualTimeProvider(start);
        Handler = new StaticHttpHandler();
        CdcLogger = new CapturingLogger<CdcStationSelector>();
        MosmixLogger = new CapturingLogger<MosmixLocationSelector>();
        ReaderLogger = new CapturingLogger<MosmixLocationCatalogReader>();

        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.AddSingleton(new HttpClient(Handler));
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<ILogger<CdcStationSelector>>(CdcLogger);
        services.AddSingleton<ILogger<MosmixLocationSelector>>(MosmixLogger);
        services.AddSingleton<ILogger<MosmixLocationCatalogReader>>(ReaderLogger);
        services.AddSingleton(MosmixEvidenceIndexTestData.Options(Directory));
        if (mqttOptions is not null)
        {
            services.AddSingleton(mqttOptions);
        }

        Provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        CdcOptions = Provider.GetRequiredService<CdcCatalogEndpoints>();
        MosmixOptions = Provider.GetRequiredService<MosmixCatalogEndpoints>();
    }

    public static SearchParameters Parameters(decimal latitude = 50.1m) => new()
    {
        Latitude = latitude,
        Longitude = 9.5m,
        AltitudeM = 200m,
        MaxDistanceKm = 50m,
        MaxAltitudeDifferenceM = 500m
    };

    public void ArrangeCdc(string toDate = "20261021", string messDatum = "202610211150")
    {
        var line = CdcStationListBuilder.Line("00044", toDate: toDate, altitude: "123", latitude: "50.1234", longitude: "9.5678", name: CdcName);
        var stationListTu = CdcStationListBuilder.Stream(new[] { line }).ToArray();
        var stationListWind = CdcStationListBuilder.Stream(new[] { line }).ToArray();
        var stationListPrec = CdcStationListBuilder.Stream(new[] { line }).ToArray();
        var stationListSolar = CdcStationListBuilder.Stream(new[] { line }).ToArray();

        Handler.Add(CdcOptions.AirTemperatureStationList!, stationListTu);
        Handler.Add(CdcOptions.WindStationList!, stationListWind);
        Handler.Add(CdcOptions.PrecipitationStationList!, stationListPrec);
        Handler.Add(CdcOptions.SolarStationList!, stationListSolar);

        var row = CdcNowArchiveBuilder.Row("00044", messDatum, "1001.2", "12.3", "10.1", "85.0", "8.9");
        var zip = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(CdcNowArchiveBuilder.Header("tu"), new[] { row })));
        Handler.Add(new Uri(CdcOptions.AirTemperatureNowDirectory!, "10minutenwerte_TU_00044_now.zip"), zip);
    }

    public async Task ArrangeMosmixAsync(params string[] catalogLines)
    {
        if (catalogLines.Length == 0)
        {
            catalogLines = new[]
            {
                MosmixCatalogBuilder.Line("X123A", name: MosmixName, latitude: "50.06", longitude: "9.30", elevation: "250"),
                MosmixCatalogBuilder.Line("01234", name: "BEISPIELDORF", latitude: "50.12", longitude: "9.36", elevation: "300")
            };
        }

        Handler.Add(MosmixOptions.CatalogUri!, MosmixCatalogBuilder.Catalog(catalogLines));
        Handler.Add(
            new Uri(MosmixOptions.MosmixLSingleStationsDirectory!, "X123A/kml/MOSMIX_L_LATEST_X123A.kmz"),
            MosmixKmzBuilder.Kmz(MosmixKmzBuilder.Kml("X123A")));

        await Provider.GetRequiredService<IMosmixEvidenceCache>().WriteAsync(
            MosmixEvidenceIndexTestData.Index(new Dictionary<string, IReadOnlyList<string>> { ["X123A"] = new[] { "FF", "TTT" } })
                with { DownloadedAtUtc = new DateTime(2026, 10, 21, 10, 0, 0, DateTimeKind.Utc) },
            CancellationToken.None);
    }

    public Task<CdcStationList> SelectCdcAsync(SearchParameters parameters) => Provider.GetRequiredService<ICdcStationSelector>().SelectAsync(parameters, CancellationToken.None);

    public Task<MosmixLocationList> SelectMosmixAsync(SearchParameters parameters) => Provider.GetRequiredService<IMosmixLocationSelector>().SelectAsync(parameters, CancellationToken.None);

    public async Task<byte[]> JsonAsync<TRoot>(TRoot root) where TRoot : class
    {
        using var ms = new MemoryStream();
        await Provider.GetRequiredService<IJsonExportSerializer>().SerializeAsync(root, ms, CancellationToken.None);
        return ms.ToArray();
    }

    public async Task<byte[]> XmlAsync<TRoot>(TRoot root) where TRoot : class
    {
        using var ms = new MemoryStream();
        await Provider.GetRequiredService<IXmlExportSerializer>().SerializeAsync(root, ms, CancellationToken.None);
        return ms.ToArray();
    }

    public void Dispose()
    {
        Provider.Dispose();

        if (System.IO.Directory.Exists(Directory))
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }
    }
}
