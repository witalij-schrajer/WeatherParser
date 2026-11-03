# Using the WeatherParser library

German translation: [de/LIBRARY_USAGE.md](de/LIBRARY_USAGE.md).

This guide takes you from the package file to a working .NET application that reads the current DWD 10-minute observations of one station and writes them as JSON. Then it shows the other sources and the MQTT message serializer. The detailed reference of every source (options, limits, exported quantities, error IDs, source conditions) is in the [README](../README.md#library-reference).

Status: version 0.1.0. It is not published on nuget.org, and the repository is private. You need access to the package file (see section 1).

## 1. What you need

- The .NET 10 SDK to build your application, and the .NET 10 runtime 10.0.14 or newer to run it ([CHANGELOG.md](../CHANGELOG.md), "Compatibility").
- The package file `WeatherParser.Parsing.0.1.0.nupkg`. Either:
  - take the `.nupkg` of a release build and check it against the release's `SHA256SUMS` file (`sha256sum -c --ignore-missing SHA256SUMS` on Linux), or
  - build it yourself from a checkout as described in [CONTRIBUTING.md section 4](../CONTRIBUTING.md#4-pack-and-package-consumer).
- Network access to the source servers when the application runs. The library makes plain HTTPS GET requests through the `HttpClient` you supply.

Your application needs neither the source checkout nor PowerShell, MSTest or the coverage tools. The verified platforms are listed in [docs/COMPATIBILITY.md](COMPATIBILITY.md). Debian 13 ARM64 (NanoPi R76S) is the verified target and Windows x64 is the tested development platform. The ARM64 deployment rules of [docs/OPERATIONS.md](OPERATIONS.md) apply to the example CLI, not to your application.

## 2. Create the application and add the package

Put the package file into a folder that serves as a local package source, then add it to a new console application:

```bash
dotnet new console --framework net10.0 --name MyWeatherApp
cd MyWeatherApp
mkdir -p ../local-nuget && cp <path>/WeatherParser.Parsing.0.1.0.nupkg ../local-nuget/
dotnet new nugetconfig
dotnet nuget add source ../local-nuget --name weatherparser-local --configfile nuget.config
dotnet add package WeatherParser.Parsing --version 0.1.0
dotnet add package Microsoft.Extensions.DependencyInjection --version 10.0.14
dotnet add package Microsoft.Extensions.Logging.Console --version 10.0.14
```

`dotnet new nugetconfig` creates a `nuget.config` with nuget.org; the next command adds the local folder next to it. Do not restrict a restore to the local folder (`--source`): the library depends on `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions` and `AngleSharp`, which NuGet restores from nuget.org. The container and the console logger above are your application's choice. Any `IServiceProvider` implementation that resolves `ILogger<T>` works.

If an earlier build of `0.1.0` is already in your NuGet cache, NuGet uses the cached copy. Clear the cache entry `weatherparser.parsing/0.1.0` first, as described in CONTRIBUTING.md section 4.

## 3. Register the library

`AddWeatherParsing()` registers every library service, for all sources, as a singleton. It registers no endpoint, option, `HttpClient`, `TimeProvider` or logger: your application supplies them. With container validation (`ValidateOnBuild`), every service's dependencies are checked, so all types below must be registered even if you use only one source. A missing one fails when the container is built, before any request.

Add this file to your application. It registers the public DWD endpoints:

```csharp
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;

namespace MyWeatherApp;

/// <summary>
/// Registers WeatherParser and every endpoint and option type it needs. AddWeatherParsing registers the services of all sources, and container validation checks all of them, so every type below is required even when the application uses only one source.
/// </summary>
public static class WeatherParserRegistration
{
    /// <summary>
    /// Registers WeatherParser with the public DWD endpoints.
    /// </summary>
    /// <param name="services">The service collection to register the services with.</param>
    /// <param name="httpClient">The HTTP client owned by the application; the container does not dispose it.</param>
    /// <param name="timeProvider">TimeProvider.System in production.</param>
    /// <param name="evidenceCacheDirectory">An existing, writable absolute directory used only by the MOSMIX_S evidence cache.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddMyWeatherParsing(this IServiceCollection services, HttpClient httpClient, TimeProvider timeProvider, string evidenceCacheDirectory)
    {
        services.AddWeatherParsing();

        // Application-owned infrastructure; the library registers neither. Register ILogger<T> with AddLogging in the host.
        services.AddSingleton(httpClient);
        services.AddSingleton(timeProvider);

        // DWD 10-minute observations and CDC station selection.
        const string cdc = "https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/";
        services.AddSingleton(new CdcCatalogEndpoints
        {
            AirTemperatureStationList = new Uri(cdc + "air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt"),
            AirTemperatureNowDirectory = new Uri(cdc + "air_temperature/now/"),
            WindStationList = new Uri(cdc + "wind/now/zehn_now_ff_Beschreibung_Stationen.txt"),
            WindNowDirectory = new Uri(cdc + "wind/now/"),
            PrecipitationStationList = new Uri(cdc + "precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt"),
            PrecipitationNowDirectory = new Uri(cdc + "precipitation/now/"),
            SolarStationList = new Uri(cdc + "solar/now/zehn_now_sd_Beschreibung_Stationen.txt"),
            SolarNowDirectory = new Uri(cdc + "solar/now/"),
        });
        services.AddSingleton(new DwdObservationOptions { ObservationDeadline = TimeSpan.FromMinutes(2) });

        // MOSMIX location selection.
        const string mosmix = "https://opendata.dwd.de/weather/local_forecasts/mos/";
        services.AddSingleton(new MosmixCatalogEndpoints
        {
            CatalogUri = new Uri("https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_stationskatalog.cfg?view=nasPublication&nn=16102"),
            MosmixLSingleStationsDirectory = new Uri(mosmix + "MOSMIX_L/single_stations/"),
            MosmixSAllStationsUri = new Uri(mosmix + "MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz"),
            EvidenceCacheDirectory = evidenceCacheDirectory,
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
        });

        // MOSMIX_L and MOSMIX_S forecasts; MaxRunAge has no default.
        services.AddSingleton(new MosmixLForecastEndpoints
        {
            SingleStationsDirectory = new Uri(mosmix + "MOSMIX_L/single_stations/"),
        });
        services.AddSingleton(new MosmixLForecastOptions
        {
            ParseDeadline = TimeSpan.FromSeconds(60),
            MaxRunAge = TimeSpan.FromHours(8),
        });
        services.AddSingleton(new MosmixSForecastEndpoints
        {
            AllStationsUri = new Uri(mosmix + "MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz"),
        });
        services.AddSingleton(new MosmixSForecastOptions
        {
            ParseDeadline = TimeSpan.FromMinutes(5),
            MaxRunAge = TimeSpan.FromHours(2),
        });

        // OsthessenWetter observations: the values must not be redistributed until the source conditions are clarified. Add one Stations entry per station you parse.
        services.AddSingleton(new OsthessenWetterEndpoints
        {
            PageUri = new Uri("https://osthessen-wetter.de/messwerte/"),
        });
        services.AddSingleton(new OsthessenWetterOptions());

        // MQTT messages: add one mapping per message type you publish; without mappings every serialization fails with MQTT-MAPPING-MISSING.
        services.AddSingleton(new MqttTopicOptions());

        return services;
    }
}
```

| Registered type | Used by | Notes |
| --- | --- | --- |
| `HttpClient` | every source | Owned by your application. The library bounds each request itself (30 s, and the parse deadline for `MOSMIX_S`) and does not retry. Handler settings such as proxy, redirects and decompression are yours. Set an identifying User-Agent. |
| `TimeProvider` | every source | `TimeProvider.System` in production. It sets `retrieved_at` and the deadlines. |
| `ILogger<T>` | every source | Register it with `AddLogging`. Notices and omissions are logged with stable event names. |
| `CdcCatalogEndpoints` | CDC station selection, DWD observations | List URIs are used unchanged. Directory URIs must end with `/`. |
| `DwdObservationOptions` | DWD observations | `ObservationDeadline`, default 2 minutes, at most 1 hour. |
| `MosmixCatalogEndpoints` | MOSMIX location selection | `EvidenceCacheDirectory` must be an existing, writable absolute directory used only by the MOSMIX_S evidence cache. `MosmixSEvidenceMaxAge` has no default. |
| `MosmixLForecastEndpoints`, `MosmixLForecastOptions` | `IMosmixLParser` | `MaxRunAge` has no default. |
| `MosmixSForecastEndpoints`, `MosmixSForecastOptions` | `IMosmixSParser` | `MaxRunAge` has no default. Every parse downloads the whole all-stations file (about 36 MB). |
| `OsthessenWetterEndpoints`, `OsthessenWetterOptions` | `IOsthessenWetterParser` | `Stations` supplies the coordinates of each station you parse. The values must not be redistributed (section 9). |
| `MqttTopicOptions` | `IMqttMessageSerializer` | Topic templates per message type. An empty instance is valid but makes every serialization fail. |

Container validation checks that a type is registered, not that its values are valid. Invalid option values surface when a service is first resolved: `MOSMIX-OPTIONS-INVALID`, `OSTHESSEN-OPTIONS-INVALID`, `MQTT-OPTIONS-INVALID`, `ENDPOINT-INVALID` and the limit errors. Resolve the parsers you use once at startup if you want these errors before the first parse. Building the container and resolving services make no request.

## 4. Use the parser in an application class

Application classes receive the library interfaces through their constructors. Only the composition root (`Program.cs`) resolves services from the container. The interface:

```csharp
namespace MyWeatherApp;

/// <summary>
/// Writes the current DWD observations of one station as one JSON document.
/// </summary>
public interface IDwdJsonExporter
{
    /// <summary>
    /// Parses the observations of the station and writes one complete JSON document to <paramref name="destination"/>.
    /// </summary>
    /// <param name="stationId">The five-digit CDC station ID, kept as a string with leading zeros, for example 00044.</param>
    /// <param name="destination">The stream that receives the JSON document; the caller owns and disposes it.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    Task ExportAsync(string stationId, Stream destination, CancellationToken cancellationToken);
}
```

The implementation parses one station and serializes the dataset:

```csharp
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Serialization;

namespace MyWeatherApp;

/// <summary>
/// Parses the DWD observations of one station and writes them as JSON. The parser and the serializer come from the container through constructor injection.
/// </summary>
public sealed class DwdJsonExporter : IDwdJsonExporter
{
    private readonly IDwdObservationParser _parser;
    private readonly IJsonExportSerializer _serializer;

    public DwdJsonExporter(IDwdObservationParser parser, IJsonExportSerializer serializer)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <inheritdoc />
    public async Task ExportAsync(string stationId, Stream destination, CancellationToken cancellationToken)
    {
        var dataset = await _parser.ParseAsync(stationId, cancellationToken).ConfigureAwait(false);
        await _serializer.SerializeAsync(dataset, destination, cancellationToken).ConfigureAwait(false);
    }
}
```

`IDwdObservationParser.ParseAsync` checks the station ID (five ASCII digits) before any request, reads the four CDC station lists and the station's `now` archives (at most eight requests), and returns an `ObservationDataset`. `IJsonExportSerializer.SerializeAsync` writes one complete UTF-8 JSON document into the caller's stream. `IXmlExportSerializer` writes the XML form of the same entities, valid against the schema `schemas/export/v1/weatherparser-export-v1.xsd` contained in the package.

## 5. The composition root

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyWeatherApp;

// Station IDs stay strings; leading zeros matter.
var stationId = args.Length > 0 ? args[0] : "00044";

// The MOSMIX_S evidence cache needs an existing writable directory.
var evidenceCacheDirectory = Path.Combine(AppContext.BaseDirectory, "mosmix-evidence");
Directory.CreateDirectory(evidenceCacheDirectory);

// The library bounds every request itself; the application owns and disposes the client.
using var httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
// Identify the application to the source servers.
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyWeatherApp/1.0");

var services = new ServiceCollection();
// Logs go to stderr, so stdout carries only the JSON document.
services.AddLogging(logging => logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace));
services.AddMyWeatherParsing(httpClient, TimeProvider.System, evidenceCacheDirectory);
services.AddSingleton<IDwdJsonExporter, DwdJsonExporter>();

// A missing registration fails here, before any request.
await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

// Ctrl+C and the overall time limit both cancel the parse.
using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };

// Resolve only at the composition root; application classes get their dependencies through their constructors.
var exporter = provider.GetRequiredService<IDwdJsonExporter>();
await using var stdout = Console.OpenStandardOutput();
await exporter.ExportAsync(stationId, stdout, cancellation.Token);
```

Run it with a station ID, for example `dotnet run -- 00044`. This contacts the DWD open-data server: see the conditions in section 9.

## 6. The result

For the offline test fixture of section 10 (synthetic values for station `00044`, retrieved at 2026-10-21 12:00 UTC), the serializer writes the document below. It is shown indented here; the serializer itself writes it without indentation. These are not real measurements.

```json
{
  "schema_version": "1",
  "source": "dwd:00044",
  "location": {
    "station_id": "00044",
    "name": "Grossenkneten",
    "latitude": 52.9336,
    "longitude": 8.2370,
    "altitude_m": 44
  },
  "measured_at": "2026-10-21T11:50:00",
  "retrieved_at": "2026-10-21T12:00:00",
  "timestamp_quality": "source",
  "measurements": {
    "temperature_c": {
      "value": 12.3,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "quality": "3"
    },
    "pressure_hpa": {
      "value": 1001,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "quality": "3",
      "pressure_reference": "sensor"
    },
    "pressure_mmhg": {
      "value": 751,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "quality": "3",
      "pressure_reference": "sensor"
    },
    "humidity_percent": {
      "value": 85,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "quality": "3"
    },
    "wind_direction_deg": {
      "value": 270,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "period": {
        "start": "2026-10-21T11:40:00",
        "end": "2026-10-21T11:50:00"
      },
      "quality": "3"
    },
    "wind_speed_kmh": {
      "value": 12,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "period": {
        "start": "2026-10-21T11:40:00",
        "end": "2026-10-21T11:50:00"
      },
      "quality": "3"
    },
    "solar_irradiance_w_m2": {
      "value": 600,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "period": {
        "start": "2026-10-21T11:40:00",
        "end": "2026-10-21T11:50:00"
      },
      "quality": "3"
    },
    "rain_rate_mm_h": {
      "value": 0,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "period": {
        "start": "2026-10-21T11:40:00",
        "end": "2026-10-21T11:50:00"
      },
      "quality": "3"
    }
  }
}
```

What the document tells you:

- **Times** are UTC in the form `YYYY-MM-DDTHH:mm:ss` without `Z` or an offset. Every quantity has its own `measured_at`. The dataset `measured_at` is the newest of them, and `retrieved_at` is the time the library read the data. Neither is a freshness guarantee: a station that stopped reporting still yields its old values, so check `measured_at` yourself.
- **Missing versus zero**: a quantity the source does not deliver, or marks as missing, is omitted. In this example the fixture has no illuminance (no DWD 10-minute source) and therefore no `illuminance_lux`. `0` is a real value, as `rain_rate_mm_h` shows.
- **Periods**: averaged and summed quantities carry their `period`. Here the rate is the mm of the previous 10 minutes times 6, and the irradiance is the 10-minute energy sum divided by 600 s.
- **Pressure**: `pressure_hpa` and `pressure_mmhg` come from the same original value and name their `pressure_reference` (`sensor`: station altitude, not reduced to sea level).
- **Quality**: `quality` is the DWD quality level of the row, unchanged.
- **Rounding** is applied after conversion: 0.1 °C, 1 hPa and 1 mmHg, 1 %, 1 km/h, with exact ties rounded away from zero.
- **Schema stability**: export format version 1 (`schema_version` `"1"`) changes only by optional additions. Validate XML with the schema of the package version that produced it.

## 7. Errors, cancellation and retries

| Situation | What you get |
| --- | --- |
| Invalid station ID | `DwdStationException`, `ErrorId` `STATION-ID-INVALID`, before any request |
| Station not in any list | `DwdStationException` `STATION-UNKNOWN` |
| Server error, timeout, oversized file | `CatalogRetrievalException` `CATALOG-RETRIEVAL-FAILED`, `CATALOG-TIMEOUT`, `CATALOG-TOO-LARGE` |
| Unreadable list or archive | `CatalogFormatException` `CATALOG-FORMAT-INVALID`, `CDC-EVIDENCE-INVALID` |
| The parse took longer than `ObservationDeadline` | `ObservationLimitException` `OBSERVATION-DEADLINE-EXCEEDED` |
| No usable reading at all | `ObservationDatasetRejectedException`; `OmittedReadings` lists the reasons |
| One product archive missing (HTTP 404) | No exception. The other products are used, and an `OBSERVATION-PRODUCT-MISSING` warning is logged. |
| Your token is cancelled | `OperationCanceledException`. It wins over the deadline, which wins over every other failure. |

The library exceptions have no common base class. `DwdStationException`, `CatalogRetrievalException`, `CatalogFormatException`, `ObservationLimitException` and the other source exceptions carry a stable `ErrorId` (the full lists are in the README's library reference); `ObservationDatasetRejectedException` has no `ErrorId` and lists the reasons in `OmittedReadings`, and `OperationCanceledException` is the framework's cancellation. The library never retries and has no scheduler. One call is one retrieval. Whether and when to retry, and how often to parse, is your application's decision: for DWD 10-minute data the project recommends at most one parse per station every 10 minutes. Treat `STATION-ID-INVALID` and the option errors as configuration errors, not as reasons to retry.

## 8. Lifetimes and threads

All library services are singletons without per-call state. The package tests run the parsers of every source, a CDC station selection and the JSON, XML and MQTT serializers in parallel and compare the results with sequential runs. Your registered `HttpClient`, `TimeProvider` and loggers must therefore be safe for concurrent use, as the framework implementations are. Register your own application classes with any lifetime that fits; the example uses a singleton.

## 9. The other sources and MQTT

Each source has its own interface, signature and failure model. They are registered by the same `AddMyWeatherParsing` call above.

| Interface | Call | Result | Partial results |
| --- | --- | --- | --- |
| `ICdcStationSelector`, `IMosmixLocationSelector` | `SelectAsync(SearchParameters, CancellationToken)` | `CdcStationList`, `MosmixLocationList` | CDC entries list their available measurements; each MOSMIX entry states the availability per product as `available`, `unavailable` or `unknown` |
| `IDwdObservationParser` | `ParseAsync(string stationId, CancellationToken)` | `ObservationDataset` | A missing product is skipped with a warning |
| `IOsthessenWetterParser` | `ParseAsync(string stationNamePattern, CancellationToken)` | `OsthessenWetterParseResult` with `Datasets` and `StationFailures` | Per station; the call fails only when no matched station is usable |
| `IMosmixLParser`, `IMosmixSParser` | `ParseAsync(string mosmixId, CancellationToken)` | `MosmixParseResult` with `Run`, `Freshness`, `RunAge` and the omitted and unmapped elements | A stale run is returned with `Freshness` `Stale`; never a fallback to the other product |
| `IMqttMessageSerializer` | `SerializeAsync<TRoot>(TRoot root, CancellationToken)` | `MqttMessage` with topic, JSON payload, QoS and retain | None; it does not connect to a broker |

- **Station and location selection** needs `SearchParameters` from `ISearchParameterFactory.Create(latitude, longitude, altitudeM, maxDistanceKm, maxAltitudeDifferenceM)`; `ICoordinateParser` reads decimal degrees and degrees, minutes and seconds. MOSMIX_S availability comes from a local index that your application refreshes on its own schedule with `IMosmixSEvidenceRefresher.RefreshAsync`. Run a selection when a station is chosen, not on a schedule. Reference: [README, Station and location selection](../README.md#station-and-location-selection-library).
- **OsthessenWetter** matches the pattern (`*` and `?`) against the station names on the measurement page; add one `OsthessenWetterStationMetadata` entry with the coordinates of each station to `OsthessenWetterOptions.Stations`. Dates are inferred and marked `estimated_date`. The source publishes no licence: production use and redistribution of its values, for example over MQTT, are not accepted until the source conditions are clarified. Reference: [README, OsthessenWetter observations](../README.md#osthessenwetter-observations-library).
- **MOSMIX forecasts** are forecast runs with `issued_at`, `retrieved_at` and one `valid_at` per step, never observations. The MOSMIX ID is a catalog ID of four or five capital letters or digits, such as `10637`, and is unrelated to CDC station IDs. Reference: [README, MOSMIX forecasts](../README.md#mosmix-forecasts-library).
- **Serializing the results**: the JSON and XML serializers accept exactly the export roots `ObservationDataset`, `ForecastRun`, `CdcStationList`, `MosmixLocationList` and the multi-result envelope `ExportDocument`; any other type fails with `ExportSerializationException`. Serialize each dataset in `OsthessenWetterParseResult.Datasets` and the `Run` of a `MosmixParseResult`, and handle `StationFailures`, `Freshness` and the omitted elements in your application. The MQTT serializer accepts the four roots other than `ExportDocument`.
- **MQTT**: configure one topic template per message type in `MqttTopicOptions`, for example `haus/weather/measurements/{source}/state` for observations; publishing the resulting `MqttMessage` with an MQTT client of your choice is your application's job. Reference: [README, MQTT serializer](../README.md#mqtt-serializer-library).

Source conditions: DWD data are open data under CC BY 4.0. Name "Deutscher Wetterdienst (DWD)" wherever you show or republish them. DWD stores the requesting IP address for at most 7 days. The MIT licence of this library covers the code only, not the data.

## 10. How this guide is verified

- `tests/WeatherParser.Package.Tests/LibraryUsageExampleTests.cs` compiles the three files of sections 3 and 4 against the packed library and builds the container with `ValidateOnBuild` and `ValidateScopes`. It resolves every public entry point without a request, runs the export offline against a fake HTTP handler that answers for the DWD URIs of section 3, and checks the result. It also checks that this guide shows the three files verbatim and that the JSON of section 6 equals the serializer output. The tests run in every CI run that builds code.
- `Program.cs` of section 5 and the package commands of section 2 were checked on 2026-11-02 in a new console application outside the repository, with an empty package cache: it restored the local `0.1.0` package and its dependencies and built without warnings. That application was not run, because running it contacts the DWD server; the live behavior of the parsers is covered by the authorized live checks recorded in the phase records ([PHASE_STATUS.md](development/PHASE_STATUS.md)), not by this guide.
