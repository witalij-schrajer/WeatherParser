# WeatherParser

**Phase status (2026-10-25): P0 and P1 complete; P1 accepted by the user on 2026-10-19 ([acceptance record](docs/p1/P1_12_ACCEPTANCE.md#p1-acceptance-2026-10-19)); P2 (export contract) accepted by the user on 2026-10-20 ([P2 acceptance record](docs/p2/P2_12_ACCEPTANCE.md)), complete (pull request #3 merged into `develop` on 2026-10-20, merge commit `74bf625`); P4 started on 2026-10-20 ([P4 handoff](docs/p2/P4_HANDOFF.md)) and was accepted by the user on 2026-10-24 on candidate `b6bc47f` ([P4 acceptance record](docs/p4/P4_12_ACCEPTANCE.md)); it is complete: pull request #5 merged into `develop` on 2026-10-24 (merge commit `95bcb1e`); P5 (DWD observations) started on 2026-10-24 on the user's instruction ([P5 handoff](docs/p4/P5_HANDOFF.md), [P5 plan](PHASE_P5_PLAN.md)); P5 accepted by the user on 2026-10-25 on candidate `fd461c8` ([P5 acceptance record](docs/p5/P5_11_ACCEPTANCE.md), [P6 handoff](docs/p5/P6_HANDOFF.md)); it is complete: pull request #7 merged into `develop` on 2026-10-25 (merge commit `29fd553`); P6 starts only on a separate instruction; P6 (OsthessenWetter observations) started on 2026-10-25 on the user's instruction on branch `features/develop/P6` from `develop` `8d8079a` ([P6 handoff](docs/p5/P6_HANDOFF.md), [P6 plan](PHASE_P6_PLAN.md)); P6.01 (foundation check) complete with outcome READY ([record](docs/p6/P6_01_FOUNDATION_CHECK.md)); P6.02: OsthessenWetter contract OW-01 to OW-16 approved by the user on 2026-10-25 ([contract](docs/p6/P6_OSTHESSEN_CONTRACT.md)) and the P6 plans reconciled with it. The SDK is pinned ([DEPENDENCIES.md](DEPENDENCIES.md)); P1.09 is accepted and clean-checkout reproducibility is verified on Windows x64. P1.10 (Debian 13 ARM64) is verified on the NanoPi R76S and accepted; P1.11 developer instructions ([CONTRIBUTING.md](CONTRIBUTING.md)) are accepted.**

WeatherParser is primarily a planned C# NuGet library for DWD/OsthessenWetter observations and MOSMIX forecasts. The published product is the package containing parsers, output entities, a generic MQTT serializer and DI integration. The CLI is only a usage example consuming that package.

## NuGet primary product and example CLI

The intended published product is a reusable NuGet library. It contains DWD/OsthessenWetter/MOSMIX parsers with required retrieval/normalization, public output entities, a generic MQTT serializer and dependency-injection integration. The CLI is only a usage example and consumes a versioned package reference; other applications can use the package without the CLI.

Each parser receives its required source URLs explicitly as constructor parameters, preferably typed as `Uri`. Name multiple endpoints separately. DI integration forwards consumer-supplied endpoints to the corresponding constructors, without invented built-in sources or global URI singletons. HTTP, time and other services remain constructor-injected. The library does not independently read CLI arguments or application-specific configuration files/environment variables.

The MQTT serializer is source-independent: it serializes suitable output entities and creates a message containing topic, JSON payload, QoS and retain setting. Topics/templates and their mapping to message types come from consumer configuration, not hardcoded weather paths. Validate resolved publication topics; missing mappings and unresolved placeholders produce defined failures. Existing `haus/weather/...` topics are configurable examples only. Serialization and broker transport are separate; serialization needs neither a broker connection nor credentials.

Provide documented DI registration for library parsers, serializer and required services. Consumers supply endpoints, topic configuration and external dependencies. Every doing class has an interface; output entities remain dedicated `[Serializable]` types. Preserve testability, UTC and JSON/XML compatibility.

The example CLI owns arguments, application configuration, single-command process execution, exit codes, file/stdout export and optional MQTT transmission. It accesses parsing only through the package, never copied implementations. Acceptance prioritizes the public API, package versioning and independent consumption. The local test package `WeatherParser.Parsing` (version in `Directory.Packages.props`) was first built and consumed in [P1.04](docs/p1/P1_04_PACKAGE_CONSUMER.md). Public package identity, feed and publication procedure remain release decisions; no public package publication is claimed.
## Languages

English is the sole maintained language for project documentation and instructions. Use one canonical English file at each standard unsuffixed path; do not maintain German or `.en.md` counterparts. Preserve requirement IDs, commands, data fields, units, examples and original source names. This decision dated 2026-10-19 supersedes the earlier bilingual policy.

**All code comments must be written in English**, including inline/block comments, XML documentation comments, and comments in scripts and configuration examples. All program-generated runtime messages must be English. The binding MIT license text in `LICENSE` remains in English.

## Project status

As of 2026-10-20, the solution contains the library, source-reference tests, isolated package-consumer tests, the coverage tooling and the export-schema generator. The library implements parser-endpoint validation, the `AddWeatherParsing` DI entry point, the export entities of the five export roots (envelope, CDC station list, MOSMIX location list, observation dataset, forecast run), the JSON and XML serializers, the observation-dataset and forecast-run builders, UTC source-time normalization, unit conversion and output rounding, and the output schema `schemas/export/v1/weatherparser-export-v1.xsd`, which the package contains. The contract is recorded in [P2_EXPORT_CONTRACT.md](docs/p2/P2_EXPORT_CONTRACT.md); its version 1 is frozen since the P2 acceptance on 2026-10-20 (only additive optional changes, C-03). Test and coverage evidence is kept in the [P1 records](docs/p1/) and the [P2 verification record](docs/p2/P2_VERIFICATION.md). Since P4.09 (2026-10-23) it also selects CDC stations and MOSMIX forecast locations around a reference point (see [Station and location selection](#station-and-location-selection-library)). Since P5.07 (2026-10-25) it also parses the DWD 10-minute observations of one CDC station into an observation dataset (see [DWD observations](#dwd-observations-library)). **The OsthessenWetter and MOSMIX parsers, MQTT and executable CLI commands are not implemented yet.** The commands below describe the planned interface and cannot be run yet. No public package publication is claimed.

## Workflow role and execution

The example CLI is an independently callable component of the overall weather-data and automation workflow. Its responsibilities are retrieval, parsing, station selection, normalization and the requested output. Downstream storage, display and other actions belong to other components.

**Each invocation executes exactly one command and then exits.** Scheduling and retries across invocations are external responsibilities, for example in n8n or cron. WeatherParser has no built-in scheduler and does not run as a persistent service. One command may return results for multiple stations. The CLI must support noninteractive execution, meaningful exit codes and separate data and log output.

Example workflows:

- n8n or cron → WeatherParser → MQTT → dashboard or other consumers.
- External workflow → WeatherParser → JSON/XML file → downstream processing.

There is no hard dependency on n8n; other programs and shell scripts can invoke the component.

## Target platform

Binding platform policy (2026-10-14): .NET version 10 or newer and Debian version 13 Trixie or newer. The initial library/example target framework is `net10.0`; the minimum architecture is ARM64 (`linux-arm64`), particularly NanoPi R76S. Add newer stable .NET/Debian releases to the verified compatibility matrix; previews are not automatically guaranteed. The pinned SDK and the verified toolchain are listed in [DEPENDENCIES.md](DEPENDENCIES.md), the developer setup in [CONTRIBUTING.md](CONTRIBUTING.md) and the verified platforms in [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md); extra ARM variants and the deployment model remain undecided. Verify the library, actual package consumer and example on Debian 13/ARM64 with .NET 10. Distinguish platform requirements from verified compatibility; never claim runtime success without documented tests.

Sources (checked 2026-10-14): [.NET support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [.NET 10 OS matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md), [Debian releases](https://www.debian.org/releases/).

## Planned commands of the example CLI

```text
WeatherParser --<command> [options]
```

| Command | Purpose | Functional options |
|---|---|---|
| `--SelectDWD` | Find suitable DWD stations and publish an MQTT station list | `--Latitude`, `--Longitude`, `--Altitude`, `--MaxDistance`, `--MaxAltDifference` |
| `--ParseDWD` | Retrieve and publish observations for a DWD station | `--StationID`; otherwise the configured reference station |
| `--ParseOstHessen` | Retrieve and publish observations for matching OsthessenWetter stations | `--StationNamePattern` |
| `--SelectMOSMIX` | Find suitable MOSMIX forecast locations | `--Latitude`, `--Longitude`, `--Altitude`, `--MaxDistance`, `--MaxAltDifference` |
| `--ParseMOSMIX` | Retrieve a forecast for a MOSMIX location | `--MOSMIXID`, `--Product` (`MOSMIX_S`/`MOSMIX_L`) |

Planned usage examples:

```text
WeatherParser --SelectDWD --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200
WeatherParser --SelectDWD --Latitude "50° 33′ 18″ N" --Longitude "9° 40′ 26″ E" --Altitude 300 --MaxDistance 50 --MaxAltDifference 200
WeatherParser --ParseDWD --StationID <DWD-STATION-ID>
WeatherParser --ParseDWD
WeatherParser --ParseOstHessen --StationNamePattern "Fulda*"
WeatherParser --SelectMOSMIX --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200
WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID>
WeatherParser --ParseMOSMIX --Product MOSMIX_S --MOSMIXID <MOSMIX-ID>
```

The example coordinates demonstrate two input formats and are not exactly equivalent. The altitude is an example value. The station pattern is illustrative and does not guarantee a match. `Longitude` and `AltitudeDifference` are the required spellings; `Langitude` and `AltitudeDiffeference` in the original description are typos.

If `ParseDWD` has neither a `StationID` argument nor a configured reference station, the application must log an error and exit unsuccessfully. It must never select a station silently. An explicit CLI value overrides configuration.

## JSON and XML export (library implemented, CLI planned)

All five commands support `--Output mqtt|json|xml`, with MQTT as the default. JSON and XML export require no MQTT broker. `--OutputFile <path>` writes a UTF-8 file; without a path, the complete document goes to standard output. Logs remain on standard error. Existing files are not silently overwritten.

```text
WeatherParser --ParseDWD --StationID <DWD-STATION-ID> --Output json --OutputFile weather.json
WeatherParser --ParseOstHessen --StationNamePattern "Fulda*" --Output xml --OutputFile weather.xml
WeatherParser --SelectDWD --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200 --Output json
```

These commands are not implemented yet. Both export formats contain the same observation or forecast data and UTC timestamps as MQTT, including estimated-date markers. Multiple station results, station lists and forecast steps are grouped into one complete document. The library serializer MQTT payloads remain JSON.

### Export entities and serialization

Each functional export object type has its own strongly typed C# entity marked `[Serializable]`, including nested export objects. This includes station lists and entries, observation datasets, forecast runs and steps, and quality/period metadata. Reusable value entities may be shared.

JSON uses `System.Text.Json.JsonSerializer`; XML uses `System.Xml.Serialization.XmlSerializer`. Both formats and MQTT JSON use the same prepared export entities. Serializer-specific rules control field names, omitted values and UTC without a timezone suffix. Serializer services use constructor injection. The library implements these entities and both serializers (P2.02 to P2.08); the XML output schema is generated from the same mappings and ships in the package at `schemas/export/v1/weatherparser-export-v1.xsd` (P2.09).

## Shared observation data

All sources use the same data contract. Timestamps are always **UTC in `YYYY-MM-DDTHH:mm:ss` format, without `Z` or an offset**. Consumers must explicitly interpret this convention; local-time conversion happens only for display.

Quantities include temperature in °C, pressure in hPa and mmHg, relative humidity in %, wind direction in degrees, wind speed in km/h, illuminance in lux, solar irradiance in W/m² and rain intensity in mm/h. Missing values are omitted. `0` represents an actual measurement.

hPa and mmHg refer to the same original value and pressure reference. Lux and W/m² are separate quantities. Averaged values retain their measurement periods. `--ParseMOSMIX` produces forecasts separately, never as observations.

The inspected OsthessenWetter overview provides only a time of day per station. The project permits deriving the date from retrieval time and the source timezone `Europe/Berlin`, then converting to UTC and **marking the date as estimated**. Daylight-saving transitions and ambiguous times require explicit handling; the retrieval date does not prove that the observation is current.

## Planned MOSMIX forecasts

`--SelectMOSMIX` uses the same location/filter parameters as `--SelectDWD` but searches the DWD MOSMIX station catalog. Results include MOSMIX ID, name, coordinates, altitude, distance, altitude difference and verified forecast elements. MOSMIX IDs are independent, potentially alphanumeric identifiers and are not derived from CDC observation-station IDs.

`--ParseMOSMIX --Product MOSMIX_S|MOSMIX_L --MOSMIXID <id>` retrieves the latest available forecast run for the location. Issue time (`issued_at`), retrieval time (`retrieved_at`) and forecast times (`forecasts[].valid_at`) remain separate and are output as UTC without a timezone suffix. Interval quantities retain their reference periods. Both product parsers are required; document their parameter mappings and freshness limits before implementation.

MQTT forecasts use `haus/weather/forecasts/<source>/state`; MOSMIX station lists use `haus/weather/stations/mosmix/selection`. The prefix is configurable. The example CLI configures the library serializer for one retained message per complete run, location and product; separate broker transport sends it. JSON and XML exports use the same normalized forecast contract.

```text
WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID> --Output json --OutputFile forecast.json
WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID> --Output xml --OutputFile forecast.xml
```

### Separate MOSMIX parsers and update frequency

Provide two distinct parsers with separate interfaces: `MosmixSParser` / `IMosmixSParser` for `MOSMIX_S`, and `MosmixLParser` / `IMosmixLParser` for `MOSMIX_L`. Both receive their own source URLs as constructor parameters and have independent DI configuration. Shared KML/KMZ helpers may be reused, but one switchable parser cannot replace the two public contracts. Missing data never triggers silent fallback to the other product.

| Product | DWD output frequency | Run times |
|---|---|---|
| `MOSMIX_S` | Hourly | A new product run each hour |
| `MOSMIX_L` | Four times daily | 03, 09, 15 and 21 UTC |

These describe provider cycles, not an internal scheduler or guaranteed file availability exactly at run time. Keep issue, retrieval and validity times separate. Read the actual forecast horizon from the file timeline; a new file does not prove all contained steps were recalculated. Define freshness limits and external retrieval plans per product.

The example keeps `--ParseMOSMIX` and additionally requires `--Product MOSMIX_S|MOSMIX_L` to select the library parser. Reject missing/unknown products before retrieval. `--SelectMOSMIX` keeps the existing geographic/altitude options and distinguishes known availability/elements per product; explicitly mark unknown availability. Output metadata, source identifiers and configured MQTT topics must distinguish S/L to prevent accidental retained-state replacement.

Source, checked 2026-10-14: [DWD procedure description, section 2.1.3/table 2.1](https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_verfahrenbeschreibung_gesamt.pdf?__blob=publicationFile&v=6). Recheck cycles before implementation/release; no parser is implemented yet.

## Architecture and tests

- C# and .NET 10 or newer (initial TFM `net10.0`); pin SDK patch and package versions before implementation.
- Each class with executable logic (a “doing class”) implements a suitable interface. Dependencies are constructor-injected through these interfaces and wired centrally in the composition root. Pure data/export entities remain separate.
- Separate responsibilities for CLI, configuration, retrieval, parsing, station selection, normalization, serialization and MQTT.
- TDD follows Red → Green → Refactor, including bug fixes. Use MSTest, Moq and FluentAssertions; construct test objects in `[TestInitialize]` and call `VerifyAll()` for mocks in `[TestCleanup]`.
- Automated tests use synthetic fixtures and mocks, without mandatory live websites or production MQTT brokers.
- Code-block coverage (as defined above) must reach at least 80%. During initial bootstrap without a measurable target-branch baseline, only this minimum applies; target-branch comparison is not applicable. An unexpectedly missing, stale or incomparable baseline in subsequent operation remains an error. Documentation-only PRs may retain the same coverage for the block metric as the target branch; decreases are forbidden. Other PRs must strictly improve the block metric below a 100% baseline; an exact 100% baseline accepts an exact 100% result. Use current commit IDs, comparable collection conditions and unrounded counter ratios. Missing PR measurements, invalid counts and zero denominators cannot pass. Microsoft.CodeCoverage reports, including the code-block breakdown, are the basis for verification; Cobertura line-rate, branch-rate or any Cobertura-derived value must never be used as or substituted for the block metric; missing or unreadable native block counters fail. Verify metric mapping and report format against real reports in P1.

See the [development plan](DEVELOPMENT_PLAN.md) for phases, dependencies and acceptance gates. The [P1 detailed plan](PHASE_P1_PLAN.md) breaks the first implementation phase into work packages and tests. See [REQUIREMENTS.md](REQUIREMENTS.md) for detailed requirements and acceptance criteria, and [AGENTS.md](AGENTS.md) for contributor rules. Developer procedures (restore, build, tests, pack, coverage, local CI, Debian/ARM64) are in [CONTRIBUTING.md](CONTRIBUTING.md).

## Station and location selection (library)

The library selects CDC 10-minute stations (`ICdcStationSelector`) and MOSMIX forecast locations (`IMosmixLocationSelector`) around a reference point. `SelectAsync(SearchParameters parameters, CancellationToken cancellationToken)` returns a `CdcStationList` or `MosmixLocationList` (export roots of contract v1, serializable with `IJsonExportSerializer` / `IXmlExportSerializer`). Search parameters come from `ISearchParameterFactory.Create(latitude, longitude, altitudeM, maxDistanceKm, maxAltitudeDifferenceM)`; coordinate strings (decimal degrees or degrees, minutes and seconds with hemisphere) are parsed by `ICoordinateParser`.

### Consumer registration

```csharp
services.AddWeatherParsing();
services.AddLogging();                       // or any ILogger<T> registration
services.AddSingleton(new HttpClient { Timeout = Timeout.InfiniteTimeSpan });
services.AddSingleton(TimeProvider.System);
services.AddSingleton(new CdcCatalogEndpoints
{
    AirTemperatureStationList = new Uri("https://example.invalid/cdc/air_temperature/now/stations.txt"),
    AirTemperatureNowDirectory = new Uri("https://example.invalid/cdc/air_temperature/now/"),
    WindStationList = new Uri("https://example.invalid/cdc/wind/now/stations.txt"),
    WindNowDirectory = new Uri("https://example.invalid/cdc/wind/now/"),
    PrecipitationStationList = new Uri("https://example.invalid/cdc/precipitation/now/stations.txt"),
    PrecipitationNowDirectory = new Uri("https://example.invalid/cdc/precipitation/now/"),
    SolarStationList = new Uri("https://example.invalid/cdc/solar/now/stations.txt"),
    SolarNowDirectory = new Uri("https://example.invalid/cdc/solar/now/"),
});
services.AddSingleton(new MosmixCatalogEndpoints
{
    CatalogUri = new Uri("https://example.invalid/mosmix/stations.cfg"),
    MosmixLSingleStationsDirectory = new Uri("https://example.invalid/mosmix/single_stations/"),
    MosmixSAllStationsUri = new Uri("https://example.invalid/mosmix/MOSMIX_S_LATEST_240.kmz"),
    EvidenceCacheDirectory = "/var/lib/weatherparser/evidence",
    MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
});
```

- `AddWeatherParsing` registers every library service as a singleton but no `Uri`, endpoint options, `HttpClient`, `TimeProvider` or logger; the consumer supplies them and a missing registration fails at container validation (`ValidateOnBuild`).
- `services.AddLogging()` needs the `Microsoft.Extensions.Logging` package in the consuming application; the library itself depends only on `Microsoft.Extensions.Logging.Abstractions`.
- List and catalog endpoints are absolute HTTP(S) URIs used unchanged (a query is allowed); directory endpoints must end with `/` and have no query or fragment, otherwise `ENDPOINT-INVALID` at construction; per-station file names are appended to the directory.
- Limits per catalog: `MaxEvidenceCandidates` (default 100, 1 to 10000) and `SelectionDeadline` (default 120 s, greater than zero and at most 1 hour), validated at construction (`SELECTION-LIMIT-INVALID`); more candidates inside the search area than `MaxEvidenceCandidates` fail with `SELECTION-TOO-MANY-CANDIDATES` before any per-station request.

### HTTP transport

- The library makes one GET per file, no retries, bounded to 30 s per request and to 8 MiB per list or catalog, 1 MiB per per-station file and 64 MiB for the MOSMIX_S all-stations file.
- Handler settings (redirects, decompression, proxy, header limits) are the consumer's responsibility.
- Set `HttpClient.Timeout` to `Timeout.InfiniteTimeSpan` or above 30 s — a shorter client timeout is permitted and its expiry is reported as `CATALOG-TIMEOUT`.

### MOSMIX_S evidence cache

- MOSMIX_S availability comes from a stored index that `IMosmixSEvidenceRefresher.RefreshAsync(CancellationToken)` builds from the all-stations file.
- The consumer calls the refresh on its own external schedule (for example a cron job or an n8n workflow; neither is a dependency, the library has no scheduler).
- Settings `MosmixSAllStationsUri` (absolute HTTP(S) file URI), `EvidenceCacheDirectory` (absolute local path of a directory used only by the cache), `MosmixSEvidenceMaxAge` (required, no default, 1 minute to 365 days) and `MosmixSRefreshDeadline` (default 10 minutes, greater than zero and at most 1 hour); a missing or out-of-range setting fails at construction with `EVIDENCE-CACHE-SETTINGS-INVALID`, without file-system access.
- The directory must exist and be writable: a missing or non-writable directory is detected only when used, by the refresh as `EVIDENCE-CACHE-FAILED` and by a selection as a missing index (`MOSMIX-S-EVIDENCE-MISSING`).
- A selection without a usable index fails with `MOSMIX-S-EVIDENCE-MISSING` before any request.
- An index older than `MosmixSEvidenceMaxAge` is still used and one `MOSMIX-S-EVIDENCE-STALE` warning names its age and the limit.
- MOSMIX_L files are fetched per location inside the search area, HTTP 404 meaning `unavailable`.

### Error and notice IDs

Exceptions (`ErrorId`):
- `CoordinateFormatException` `COORDINATE-INVALID`
- `SearchParameterException` `SEARCH-PARAMETER-INVALID`
- `CatalogFormatException` `CATALOG-FORMAT-INVALID`, `CDC-EVIDENCE-INVALID`, `MOSMIX-EVIDENCE-INVALID`
- `CatalogRetrievalException` `CATALOG-RETRIEVAL-FAILED`, `CATALOG-TIMEOUT`, `CATALOG-TOO-LARGE`
- `SelectionLimitException` `SELECTION-TOO-MANY-CANDIDATES`, `SELECTION-DEADLINE-EXCEEDED`, `SELECTION-LIMIT-INVALID`
- `EvidenceCacheException` `EVIDENCE-CACHE-SETTINGS-INVALID`, `EVIDENCE-CACHE-FAILED`, `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED`, `MOSMIX-S-EVIDENCE-MISSING`
- `ParserEndpointValidationException` `ENDPOINT-INVALID`

Notices (log event names):
- `SELECTION-EMPTY` (Information)
- `MOSMIX-LOCATION-NO-ALTITUDE`, `CDC-STATION-NO-ALTITUDE`, `CATALOG-RECORD-SKIPPED`, `CDC-STATION-CONFLICT`, `MOSMIX-S-EVIDENCE-STALE` (Warning)

A caller cancellation surfaces as `OperationCanceledException` and wins over the selection deadline.

### Source conditions and retrieval guidance

- The data come from the Deutscher Wetterdienst (DWD) open-data server [opendata.dwd.de](https://opendata.dwd.de/) under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) per the [DWD legal notice](https://www.dwd.de/EN/service/legal_notice/legal_notice_node.html).
- Consumers who republish selection results or other DWD-derived data (for example via MQTT or file export) must keep the attribution "Deutscher Wetterdienst (DWD)".
- DWD stores the requesting IP address for at most 7 days.
- A selection is a configuration-time operation — run it when a station or location is chosen or reviewed, not on a schedule, and if repeated at most once per day per search.
- The per-station evidence requests are part of that run and not intended for polling (project recommendation, not a provider guarantee).

## DWD observations (library)

The library parses the DWD 10-minute `now` observations of one explicitly given CDC station (`IDwdObservationParser`). `ParseAsync(string stationId, CancellationToken cancellationToken)` returns an `ObservationDataset` (export root of contract v1, serializable with `IJsonExportSerializer` / `IXmlExportSerializer`). The station ID has exactly five ASCII digits with leading zeros, for example `00044`; it is checked before any request, and the library never substitutes another or the nearest station. One call is one parse: the library reads the four CDC station lists, resolves the station and reads the station's `now` archive of every product whose list contains it. There is no scheduler and no retry; scheduling stays external. The example CLI command `--ParseDWD` is still planned (P3).

### Consumer registration

The registration of [Station and location selection](#station-and-location-selection-library) is extended by the observation options:

```csharp
services.AddSingleton(new DwdObservationOptions
{
    ObservationDeadline = TimeSpan.FromSeconds(120),
});
```

- `DwdObservationOptions` must be registered by every consumer of `AddWeatherParsing`, also one that only selects stations, because the container validation (`ValidateOnBuild`) checks the parser's dependencies; `new DwdObservationOptions()` gives the default.
- `ObservationDeadline` (default 120 s) bounds one parse: it must be greater than zero and at most 1 hour, otherwise construction fails with `OBSERVATION-LIMIT-INVALID` (zero, negative values, `Timeout.InfiniteTimeSpan` and values above 1 hour are rejected); its expiry fails the parse with `OBSERVATION-DEADLINE-EXCEEDED`.
- The parser reuses the eight URIs of `CdcCatalogEndpoints`; its `SelectionDeadline` and `MaxEvidenceCandidates` do not apply to observations.
- At most eight requests per parse: the four station lists and at most four archives (`10minutenwerte_TU_<id>_now.zip`, `..._wind_...`, `..._nieder_...`, `..._SOLAR_...` below the `now` directories), at most four in parallel, with the bounds of [HTTP transport](#http-transport). An archive answering HTTP 404 is a missing product (`OBSERVATION-PRODUCT-MISSING`), and the other products are still used; every other retrieval or format failure fails the parse.

### Exported quantities

| DWD column | Export field | Conversion and period |
| --- | --- | --- |
| `TT_10` (°C, 2 m, instantaneous) | `temperature_c` | none; no period |
| `RF_10` (%) | `humidity_percent` | none; no period |
| `PP_10` (hPa at station altitude) | `pressure_hpa`, `pressure_mmhg` | `pressure_reference` `sensor`; both from one value; no period |
| `FF_10` (m/s, mean of the previous 10 minutes) | `wind_speed_kmh` | × 3.6; period of the previous 10 minutes |
| `DD_10` (degrees, mean of the previous 10 minutes) | `wind_direction_deg` | 360 exported as 0 (north); period of the previous 10 minutes |
| `RWS_10` (mm, sum of the previous 10 minutes) | `rain_rate_mm_h` | mm per 10 minutes × 6; period of the previous 10 minutes |
| `GS_10` (J/cm², sum of the previous 10 minutes) | `solar_irradiance_w_m2` | × 10000 to J/m², ÷ 600 s; period of the previous 10 minutes |

- Every quantity takes the newest row with a usable value and keeps its own `measured_at` (UTC, from `MESS_DATUM`); the dataset `measured_at` is the newest of them. There is no freshness filter: the archive of a station that stopped reporting still yields its old values, so consumers evaluate `measured_at`.
- `-999` in any spelling is a missing value and the quantity is omitted (`OBSERVATION-READING-OMITTED`); `0` is a real value.
- Wind direction is omitted when the wind speed of the same row is 0 (calm) or the direction is 0, because the DWD data write north as 360 and document no calm marker (a project assumption).
- `quality` is the DWD quality level `QN` of the row, unchanged: `1` only formal control, `2` controlled with individually defined criteria, `3` automatic control and correction ([DWD dataset descriptions](#data-sources)); data with `QN` 1 may contain significant errors.
- Not exported: `TM5_10`, `TD_10`, `RWS_DAU_10`, `RWS_IND_10`, `DS_10`, `SD_10`, `LS_10` (no export field) and `illuminance_lux` (no DWD 10-minute source).
- Limitations: the quality control of the `now` data is not yet completed, so values may still change; the time stamp of `RWS_10` is instrument dependent (with a Pluvio Ott gauge the physical interval ends about 5 minutes earlier) and is not corrected — the exported period is the nominal one.

### Error and notice IDs

Exceptions (`ErrorId`):
- `DwdStationException` `STATION-ID-INVALID` (before any request), `STATION-UNKNOWN` (the station is in no list; no archive request), `STATION-METADATA-CONFLICT` (the station's records in the lists differ, so its location cannot be stated)
- `ObservationLimitException` `OBSERVATION-LIMIT-INVALID`, `OBSERVATION-DEADLINE-EXCEEDED`
- reused from selection: `CatalogRetrievalException` `CATALOG-RETRIEVAL-FAILED`, `CATALOG-TIMEOUT`, `CATALOG-TOO-LARGE`; `CatalogFormatException` `CATALOG-FORMAT-INVALID`, `CDC-EVIDENCE-INVALID`; `ParserEndpointValidationException` `ENDPOINT-INVALID`
- `ObservationDatasetRejectedException` when no reading is usable (no `ErrorId`; `OmittedReadings` lists the omitted quantities with their reasons)

Notices (log event names, Warning): `OBSERVATION-PRODUCT-MISSING`, `OBSERVATION-READING-OMITTED` (quantity and reason, including `CalmWind` and `ZeroDirection`), `CDC-STATION-CONFLICT`, `CATALOG-RECORD-SKIPPED`. Log messages carry the station ID and the retrieval time in UTC, never measurement values.

A caller cancellation surfaces as `OperationCanceledException` and wins over the observation deadline, which wins over every other failure.

### Source conditions and retrieval guidance

- The attribution and privacy notes of [Source conditions and retrieval guidance](#source-conditions-and-retrieval-guidance) apply: data of the Deutscher Wetterdienst (DWD) under CC BY 4.0, republished observations keep the attribution "Deutscher Wetterdienst (DWD)", and DWD stores the requesting IP address for at most 7 days.
- The values are 10-minute values; DWD updates the `now` data at a frequency of less than one hour (DWD dataset descriptions).
- Project recommendation, not a provider rule (DWD documents no request-rate limit): schedule parses externally (for example cron or an n8n workflow; neither is a dependency) and parse a station at most once every 10 minutes.

## Configuration and publication

Station selection, source settings, MQTT connection and topics are configurable. Credentials belong in environment variables or local configuration excluded from Git. The public repository contains only examples without secrets or private operational data.

The example CLI separate broker transport uses dedicated credentials and scoped topic permissions. Sources have separate observation or forecast messages; observations, forecasts and both station-list types use separate topics. Topic behavior is defined in the requirements.

## Data sources

- [DWD Climate Data Center: 10-minute observations](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/)
- [DWD: temperature, humidity and pressure dataset description](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/air_temperature/DESCRIPTION_obsgermany_climate_10min_air_temperature_en.pdf)
- [DWD: wind dataset description](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/wind/DESCRIPTION_obsgermany_climate_10min_wind_en.pdf)
- [DWD: precipitation dataset description](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/precipitation/DESCRIPTION_obsgermany-climate-10min-precipitation_en.pdf)
- [DWD: solar dataset description](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/solar/DESCRIPTION_obsgermany_climate_10min_solar_en.pdf)
- [OsthessenWetter observations](https://osthessen-wetter.de/messwerte/)
- [DWD MOSMIX: catalog, formats and element definitions](https://www.dwd.de/DE/leistungen/opendata/hilfe.html)
- [DWD MOSMIX_L: individual forecast locations](https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/)

Code is licensed under [MIT](LICENSE). This does not grant rights to third-party weather data or websites. Source conditions, attribution and permitted retrieval frequency must be documented separately for each adapter and checked before production use. A publicly available OsthessenWetter API is not assumed.

## Binding MOSMIX schema source

[DWD pointforecast_dwd_extension_V1_0.xsd](https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd) is the binding basis for both parsers' MOSMIX source/XML data structures. Derive types, namespaces, elements and cardinalities from this schema; verify additional KML structures before implementation. Record schema revision and checksum for reproducible type generation/derivation. Map source types to separate public output entities; they do not replace the normalized forecast contract. Follow TDD for schema/mapping tests. No C# data structures have been generated yet.


## Decisions dated 2026-10-14

The additional decisions and P0 clarifications dated 2026-10-14 (development environment, English output, measurement timestamps, rounding, output XSDs, test frameworks, coverage exception and metric, deferred CLI questions, dependencies, coverage bootstrap, sources, OsthessenWetter behaviour) are maintained in [AGENTS.md](AGENTS.md#additional-decisions-dated-2026-10-14) and [REQUIREMENTS.md](REQUIREMENTS.md). The current toolchain is listed in [DEPENDENCIES.md](DEPENDENCIES.md).


## Binding phase status and sequence

**2026-10-15: P0 complete; P1 solution/project scaffolding created. P1 is not yet accepted.**

**2026-10-19: P1 accepted by the user (see the [P1 acceptance record](docs/p1/P1_12_ACCEPTANCE.md#p1-acceptance-2026-10-19)). P2 is next and starts only on a separate instruction.**

**2026-10-20: P2 accepted by the user (see the [P2 acceptance record](docs/p2/P2_12_ACCEPTANCE.md)); complete: pull request #3 merged into `develop` on 2026-10-20 (merge commit `74bf625`). P4 started on 2026-10-20 on the user's instruction ([P4 handoff](docs/p2/P4_HANDOFF.md)).**

**2026-10-24: P4 accepted by the user on candidate `b6bc47f` ([P4 acceptance record](docs/p4/P4_12_ACCEPTANCE.md)); it is complete: pull request #5 merged into `develop` on 2026-10-24 (merge commit `95bcb1e`); P5 starts only on a separate instruction ([P5 handoff](docs/p4/P5_HANDOFF.md)).**

**2026-10-24: P5 (DWD observations) started on the user's instruction on branch `features/develop/P5` from `develop` `1e604dc` ([P5 handoff](docs/p4/P5_HANDOFF.md), [P5 plan](PHASE_P5_PLAN.md)); P5.01 (foundation check) complete with outcome READY ([record](docs/p5/P5_01_FOUNDATION_CHECK.md)); P5.02: observation contract OC-01 to OC-14 approved by the user on 2026-10-24 ([contract](docs/p5/P5_OBSERVATION_CONTRACT.md)) and the P5 plans reconciled with it.**

**2026-10-25: P5 (DWD observations) accepted by the user on candidate `fd461c8` ([P5 acceptance record](docs/p5/P5_11_ACCEPTANCE.md)) after the integrated verification READY on Windows, hosted CI and the NanoPi R76S ([P5.10 record](docs/p5/P5_10_INTEGRATED_VERIFICATION.md)); it is complete: pull request #7 merged into `develop` on 2026-10-25 (merge commit `29fd553`); P6 starts only on a separate instruction ([P6 handoff](docs/p5/P6_HANDOFF.md)).**

**2026-10-25: P6 (OsthessenWetter observations) started on 2026-10-25 on the user's instruction on branch `features/develop/P6` from `develop` `8d8079a` ([P6 handoff](docs/p5/P6_HANDOFF.md), [P6 plan](PHASE_P6_PLAN.md)); P6.01 (foundation check) complete with outcome READY ([record](docs/p6/P6_01_FOUNDATION_CHECK.md)); P6.02: OsthessenWetter contract OW-01 to OW-16 approved by the user on 2026-10-25 ([contract](docs/p6/P6_OSTHESSEN_CONTRACT.md)) and the P6 plans reconciled with it.**

Sequence with unchanged phase IDs: **P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9**. Complete and accept the entire library first, including station selection, DWD/OsthessenWetter/MOSMIX_S/MOSMIX_L parsers, output entities, JSON/XML, XSDs, MQTT serializer and DI. Then begin P3 with the CLI usage example, configuration and example broker transport. Public release remains P9.

P1 contains only a technical package consumer as an integration test, not a CLI/application example. Before P3, accept source functionality directly through the library and its tests; CLI commands do not gate library phases. This sequence supersedes earlier numeric ordering or dependencies on P3. [P1 detailed plan](PHASE_P1_PLAN.md). The current task adds project scaffolding; domain implementation remains deferred.


## Development
The solution [WeatherParser.slnx](WeatherParser.slnx) contains nine projects:

```text
src/
  WeatherParser.Parsing/                # Packable net10.0 library (local test package)
tests/
  WeatherParser.Parsing.Tests/          # MSTest project with source reference
  WeatherParser.Package.Tests/          # Isolated NuGet PackageReference consumer tests
  WeatherParser.Coverage.Tests/         # Coverage evaluator tests
  WeatherParser.Coverage.Runner.Tests/  # Coverage runner tests
  WeatherParser.SchemaGeneration.Tests/ # Export schema generator, golden-file and refinement tests
tools/
  WeatherParser.Coverage/               # Non-packable coverage evaluator library
  WeatherParser.Coverage.Runner/        # Non-packable console runner for the coverage gate
  WeatherParser.SchemaGeneration/       # Non-packable export schema (XSD) generator library
```

Quickstart (Windows, PowerShell 7, from the repository root):

```powershell
New-Item -ItemType Directory -Force artifacts/local-nuget
dotnet build WeatherParser.Ci.slnf --configuration Release
pwsh ./scripts/ci-local.ps1 -TargetRef <comparison branch> -RunSettings coverage.runsettings
```

Every developer procedure, its platform and its verification status are in [CONTRIBUTING.md](CONTRIBUTING.md); contributor rules are in [AGENTS.md](AGENTS.md), versions and licenses in [DEPENDENCIES.md](DEPENDENCIES.md), and the verified platform matrix in [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md). Phase history and acceptance records: [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md), [PHASE_P1_PLAN.md](PHASE_P1_PLAN.md) and [docs/p1](docs/p1/).
