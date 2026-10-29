using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;

namespace WeatherParser.Package.Tests;

internal sealed class ConsumerClock : TimeProvider
{
    public DateTimeOffset Now { get; set; }

    public ConsumerClock(DateTimeOffset now)
    {
        Now = now;
    }

    public override DateTimeOffset GetUtcNow() => Now;
}

internal sealed class ConsumerLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string? EventName, string Message)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (Entries)
        {
            Entries.Add((logLevel, eventId.Name, formatter(state, exception)));
        }
    }

    public int Count(string eventName) => Entries.Count(entry => entry.EventName == eventName);
}

internal sealed class ConsumerSelectionHost : IDisposable
{
    private ServiceProvider? _provider;

    public string Directory { get; }
    public ConsumerHttpHandler Handler { get; }
    public ConsumerClock Clock { get; }
    public ConsumerLogger<CdcStationSelector> CdcLogger { get; }
    public ConsumerLogger<MosmixLocationSelector> MosmixLogger { get; }
    public CdcCatalogEndpoints Cdc { get; }
    public MosmixCatalogEndpoints Mosmix { get; }
    public IServiceCollection Services { get; }

    public ServiceProvider Provider => _provider ??= Services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    public ConsumerSelectionHost(string baseUri, DateTimeOffset now)
    {
        Directory = Path.Combine(Path.GetTempPath(), "wp-consumer-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(Directory);
        Handler = new ConsumerHttpHandler();
        Clock = new ConsumerClock(now);
        CdcLogger = new ConsumerLogger<CdcStationSelector>();
        MosmixLogger = new ConsumerLogger<MosmixLocationSelector>();

        var cdcBase = baseUri + "cdc/";
        Cdc = new CdcCatalogEndpoints
        {
            AirTemperatureStationList = new Uri(cdcBase + "air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt"),
            AirTemperatureNowDirectory = new Uri(cdcBase + "air_temperature/now/"),
            WindStationList = new Uri(cdcBase + "wind/now/zehn_now_ff_Beschreibung_Stationen.txt"),
            WindNowDirectory = new Uri(cdcBase + "wind/now/"),
            PrecipitationStationList = new Uri(cdcBase + "precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt"),
            PrecipitationNowDirectory = new Uri(cdcBase + "precipitation/now/"),
            SolarStationList = new Uri(cdcBase + "solar/now/zehn_now_sd_Beschreibung_Stationen.txt"),
            SolarNowDirectory = new Uri(cdcBase + "solar/now/"),
        };

        var mosmixBase = baseUri + "mosmix/";
        Mosmix = new MosmixCatalogEndpoints
        {
            CatalogUri = new Uri(mosmixBase + "mosmix_stationskatalog.cfg"),
            MosmixLSingleStationsDirectory = new Uri(mosmixBase + "single_stations/"),
            MosmixSAllStationsUri = new Uri(mosmixBase + "all_stations/kml/MOSMIX_S_LATEST_240.kmz"),
            EvidenceCacheDirectory = Directory,
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
        };

        Services = new ServiceCollection()
            .AddWeatherParsing()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton<ILogger<CdcStationSelector>>(CdcLogger)
            .AddSingleton<ILogger<MosmixLocationSelector>>(MosmixLogger)
            .AddSingleton(new HttpClient(Handler))
            .AddSingleton<TimeProvider>(Clock)
            .AddSingleton(Cdc)
            .AddSingleton(Mosmix)
            .AddSingleton(new DwdObservationOptions())
            .AddSingleton(new OsthessenWetterOptions { Stations = { new OsthessenWetterStationMetadata { Name = "Teststation", Latitude = 50.5m, Longitude = 9.7m } } })
            .AddSingleton(new OsthessenWetterEndpoints { PageUri = new Uri("https://osthessen.example/messwerte/") })
            .AddSingleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://mosmix.example/single_stations/") })
            .AddSingleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://mosmix.example/MOSMIX_S_LATEST_240.kmz") })
            .AddSingleton(new MosmixLForecastOptions { MaxRunAge = TimeSpan.FromHours(8) })
            .AddSingleton(new MosmixSForecastOptions { MaxRunAge = TimeSpan.FromHours(2) })
            .AddSingleton(new MqttTopicOptions());
    }

    public void ArrangeCdc()
    {
        var stationLine = ConsumerFixtures.CdcLine("00044", "123", "50.1234", "9.5678", "Mühlheim & <Süd>");
        var cdcListBytes = ConsumerFixtures.CdcList(stationLine);
        Handler.Add(Cdc.AirTemperatureStationList!, cdcListBytes);
        Handler.Add(Cdc.WindStationList!, cdcListBytes);
        Handler.Add(Cdc.PrecipitationStationList!, cdcListBytes);
        Handler.Add(Cdc.SolarStationList!, cdcListBytes);

        var nowFile = ConsumerFixtures.CdcNowZip("tu", "00044", ConsumerFixtures.CdcRow("00044", "202610211150", "1001.2", "12.3", "10.1", "85.0", "8.9"));
        Handler.Add(new Uri(Cdc.AirTemperatureNowDirectory!, "10minutenwerte_TU_00044_now.zip"), nowFile);
    }

    public void ArrangeCdcFullStation()
    {
        ArrangeCdc();
        Handler.Add(new Uri(Cdc.WindNowDirectory!, "10minutenwerte_wind_00044_now.zip"), ConsumerFixtures.CdcNowZip("ff", "00044", ConsumerFixtures.CdcRow("00044", "202610211150", "3.47", "270")));
        Handler.Add(new Uri(Cdc.PrecipitationNowDirectory!, "10minutenwerte_nieder_00044_now.zip"), ConsumerFixtures.CdcNowZip("rr", "00044", ConsumerFixtures.CdcRow("00044", "202610211150", "10", "0.5", "1")));
        Handler.Add(new Uri(Cdc.SolarNowDirectory!, "10minutenwerte_SOLAR_00044_now.zip"), ConsumerFixtures.CdcNowZip("sd", "00044", ConsumerFixtures.CdcRow("00044", "202610211150", "0.0", "36.0", "0.1", "-999")));
    }

    public void ArrangeMosmix()
    {
        var catalogLine1 = ConsumerFixtures.MosmixLine("X123A", "MÜHLHEIM & <SUED>", "50.06", "9.30", "250");
        var catalogLine2 = ConsumerFixtures.MosmixLine("01234", "BEISPIELDORF", "50.12", "9.36", "300");
        var catalogBytes = ConsumerFixtures.MosmixCatalog(catalogLine1, catalogLine2);
        Handler.Add(Mosmix.CatalogUri!, catalogBytes);

        var lKmz = ConsumerFixtures.MosmixLKmz("X123A");
        Handler.Add(new Uri(Mosmix.MosmixLSingleStationsDirectory!, "X123A/kml/MOSMIX_L_LATEST_X123A.kmz"), lKmz);

        var sKmz = ConsumerFixtures.MosmixSKmz(("X123A", new[] { "FF", "TTT" }));
        Handler.Add(Mosmix.MosmixSAllStationsUri!, sKmz);
    }

    public static SearchParameters Parameters(decimal latitude = 50.1m) => new()
    {
        Latitude = latitude,
        Longitude = 9.5m,
        AltitudeM = 200m,
        MaxDistanceKm = 50m,
        MaxAltitudeDifferenceM = 500m
    };

    public async Task<byte[]> JsonAsync<TRoot>(TRoot root) where TRoot : class
    {
        using var stream = new MemoryStream();
        var serializer = Provider.GetRequiredService<IJsonExportSerializer>();
        await serializer.SerializeAsync(root, stream, CancellationToken.None);
        return stream.ToArray();
    }

    public async Task<byte[]> XmlAsync<TRoot>(TRoot root) where TRoot : class
    {
        using var stream = new MemoryStream();
        var serializer = Provider.GetRequiredService<IXmlExportSerializer>();
        await serializer.SerializeAsync(root, stream, CancellationToken.None);
        return stream.ToArray();
    }

    public static IReadOnlyList<string> ValidateAgainstPackagedSchema(byte[] document)
    {
        var events = new List<string>();
        var schemas = new XmlSchemaSet { XmlResolver = null };
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Schemas", "weatherparser-export-v1.xsd");
        schemas.ValidationEventHandler += (_, e) => events.Add(e.Severity + ": " + e.Message);
        using (var reader = XmlReader.Create(schemaPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
        {
            schemas.Add(null, reader);
        }
        schemas.Compile();
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            ValidationType = ValidationType.Schema,
            ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings,
            Schemas = schemas
        };
        settings.ValidationEventHandler += (_, e) => events.Add(e.Severity + ": " + e.Message);
        using (var documentReader = XmlReader.Create(new MemoryStream(document), settings))
        {
            while (documentReader.Read())
            {
            }
        }
        return events;
    }

    public void Dispose()
    {
        _provider?.Dispose();
        if (System.IO.Directory.Exists(Directory))
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }
    }
}
