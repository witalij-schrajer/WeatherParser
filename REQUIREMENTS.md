# WeatherParser – Requirements

**Phase status (2026-10-15): P0 complete; P1 started with solution/project scaffolding. Domain implementation, tests and coverage acceptance remain pending.**

As of 2026-10-14. Status: requirements and architectural foundation; no implementation yet. “Must” indicates a binding requirement. Open decisions must be resolved before the relevant implementation step.

## 1. Purpose and scope

**REQ-001** WeatherParser must primarily provide a publishable C# NuGet library containing parsers, output entities, a generic MQTT serializer and DI integration. A separate CLI consumes the package as a usage example demonstrating retrieval, observations/forecasts and MQTT/JSON/XML output.

**REQ-002** Scope includes DWD observations, OsthessenWetter and DWD MOSMIX forecasts through library functionality demonstrated by five CLI commands: `SelectDWD`, `ParseDWD`, `ParseOstHessen`, `SelectMOSMIX`, `ParseMOSMIX`. A persistent background service and internal scheduling are outside the application. Scheduling and orchestration are external. History, additional forecast providers and MQTT error collection are future extensions. Shared observation types must support future room stations; device integration is outside this parser.

**REQ-003** Source code is intended for public GitHub publication under MIT. Initial development is local. GitHub repository creation, remotes and publication require a separate instruction.

### Target platform and workflow role

**REQ-004** Binding platform policy (2026-10-14): .NET version 10 or newer and Debian version 13 Trixie or newer. The initial library/example target framework is `net10.0`; the minimum architecture is ARM64 (`linux-arm64`), particularly NanoPi R76S. Add newer stable .NET/Debian releases to the verified compatibility matrix; previews are not automatically guaranteed. Visual Studio 2026 and SDK 10.0.303 are the verified local starting point; packages, extra ARM variants and deployment model remain undecided. Verify the library, actual package consumer and example on Debian 13/ARM64 with .NET 10. Distinguish platform requirements from verified compatibility; never claim runtime success without documented tests.

**REQ-005** Libraries, native dependencies, paths, encodings, timezone handling and serializers must support the Debian/ARM target. Document reproducible builds, packaging and runtime dependencies. A developer-machine build or cross-publish alone is not proof of ARM execution.

**REQ-006** The example CLI is a short-lived process: validate input, execute exactly one selected command, finish publication/export and logging, release resources and exit with an actionable status. No continuous polling, internal periodic timers, waiting for the next invocation or built-in scheduler. A command may process its multiple stations or forecast steps; these are not additional CLI commands. Bounded network timeouts and technical retries within the invocation are allowed.

**REQ-007** Scheduling of the example CLI must be external, such as n8n or cron. Regular invocations must not require interactive input. Configuration, stdout, stderr and exit status must work without an interactive terminal. Document success, partial failure and complete failure so the caller can distinguish them. Cancellation signals must permit controlled, bounded termination; an interrupted export must not appear successfully complete.

**REQ-008** WeatherParser is an independent component of the overall data/automation workflow. Its responsibility ends at retrieval, parsing, station selection/normalization and requested MQTT/JSON/XML output. The external workflow controls scheduling, retries across invocations, subsequent steps, storage, display and other actions. There must be no hard coupling to n8n: cron, shell scripts and other orchestrators must be able to invoke it. The application neither installs/starts an orchestrator nor controls devices. Example flows: external invocation → WeatherParser → MQTT → consumers, or external invocation → WeatherParser → export → downstream processing.

**REQ-009** All source-code comments must be in English. This includes inline/block comments, XML documentation comments, and comments in scripts and configuration examples. Technical identifiers, CLI commands and fields remain language-independent. All program-generated output, including help, logs, status and error messages, must be English.

## 2. Command-line interface

**REQ-010** Syntax must be `WeatherParser --<command> [options]`. Exactly one command is allowed per invocation. Missing/unknown commands, duplicate/conflicting options and invalid values must produce clear errors before network access. Help must explain commands, units and examples.

**REQ-011** Option names are case-insensitive. The required spelling is `Longitude`; the original `Langitude` was a typo. Documentation/output fields must use the correct spelling. A typo alias is not required.

### Library, package boundary and constructor parameters

**REQ-012** The NuGet package is the primary product. It contains DWD/OsthessenWetter/MOSMIX parsers, required retrieval/normalization, public output entities, a generic MQTT serializer and DI integration. It must work as a library without CLI, n8n, cron or a broker connection. The CLI is a separate usage example. Parser code does not own arguments, process exits, application configuration, file/console output or broker transport. Document this boundary in public contracts/examples.

**REQ-013** The example CLI must consume the library using a versioned `PackageReference`. A `ProjectReference` alone or copied parser files does not demonstrate this. Local development/CI builds a `.nupkg` and may use an isolated local feed without publication. Decide package ID, versioning, target frameworks and API before implementation; feed/publication are separately authorized. Test an independent consumer of the actual built package.

**REQ-014** Every parser receives all required source URLs as explicit constructor parameters, preferably `Uri`. Clearly separate catalog/data/base endpoints where multiple URLs are needed. No hardcoded complete source URLs, silent fallback endpoints or independent configuration discovery in parsers. Validate absent/invalid endpoints and documented permitted schemes without initiating retrieval. DI forwards configured URLs unchanged to each parser constructor. Other service dependencies remain interface-based constructor parameters.

**REQ-015** The package MQTT serializer must be generic, independent of providers and CLI. Serialize suitable output entities as JSON and return a message with configured topic, payload, QoS and retain. Consumers supply topics/templates and message-type mappings, including configurable station/source parts. No hardcoded `haus/weather/...` paths in library behavior. Missing mappings, unresolved placeholders and invalid publish topics fail without broker access. Source IDs must not inject unintended topic levels or publish wildcards. The serializer manages neither broker connections nor credentials; transport belongs to consumers. Preserve the example CLI's agreed QoS/retain semantics.

**REQ-016** Provide documented DI integration registering parser/serializer interfaces and wiring consumer URLs, topic settings and services. No global `Uri` registrations confusing different parsers' endpoints; no domain service locators. Test independent parser configurations, forwarded URLs, configured topics, packaged public API and CLI/library separation. Entities remain `[Serializable]`; preserve TDD, English comments, English documentation, ARM compatibility and coverage rules.

**REQ-017** Maintain an explicit .NET/Debian/ARM compatibility matrix separating requested support, official vendor support and actual project evidence. Before release verify the .NET 10 minimum on Debian 13/ARM64, including the built NuGet package, dependency resolution, serializers, timezones and DI. Without this evidence runtime acceptance remains open. Add newer stable versions through documented tests; the Debian minimum does not automatically follow LTS transitions.

### SelectDWD

**REQ-020** Required options:

| Option | Meaning | Input/unit |
|---|---|---|
| `Latitude` | Reference location latitude | Decimal degrees or degrees/minutes/seconds with N/S |
| `Longitude` | Reference location longitude | Decimal degrees or degrees/minutes/seconds with E/W |
| `Altitude` | Reference altitude above normal level | Meters |
| `MaxDistance` | Maximum horizontal distance | Kilometers, at least 0 |
| `MaxAltDifference` | Maximum absolute altitude difference | Meters, at least 0 |

**REQ-021** Coordinates must support at least `50.5549`, `9.6738`, `50° 33′ 18″ N` and `9° 40′ 26″ E`. DMS is a single, quoted argument when needed. Latitude: −90 to +90; longitude: −180 to +180; minutes/seconds: 0 inclusive to 60 exclusive. Reject nonfinite numbers, wrong hemispheres and conflicting signs. Interpretation must be independent of OS locale.

**REQ-022** Both inclusive limits must hold: distance ≤ `MaxDistance` and absolute altitude difference ≤ `MaxAltDifference`. Calculate horizontal distance from WGS84 coordinates using a documented geodetic method. Filter altitude separately; do not compute Euclidean distances in degrees with altitude mixed in. Negative reference altitudes are allowed.

**REQ-023** Output is a station list in the selected MQTT/JSON/XML mode. Each entry contains:

| Field | Meaning |
|---|---|
| `station_id` | DWD ID as a string, preserving leading zeros |
| `name` | Station name |
| `latitude`, `longitude` | Decimal degrees |
| `altitude_m` | Station altitude in meters |
| `distance_km` | Distance from the reference location |
| `altitude_difference_m` | Signed station altitude minus reference altitude |
| `available_measurements` | Verified station quantities/data types |

Also include `schema_version`, source, UTC retrieval time and search parameters. Sort by distance, then ID. No matches produce a valid empty list and a log notice, never an invented substitute station.

**REQ-024** Derive availability from the relevant DWD products/metadata. Catalog membership proves neither all quantities nor valid current readings. Inactive stations without current data must not silently appear suitable. Selection must not automatically change configuration.

### ParseDWD

**REQ-030** An explicit `StationID` takes precedence; otherwise use the configured reference station. If both are missing, log a stable error ID and exit nonzero, without retrieval, MQTT publication or automatic selection.

**REQ-031** Report invalid/unknown IDs clearly. Observations must belong exclusively to the resolved station. Do not fill a missing optional product with another station's data.

**REQ-032** Output observation datasets under the shared schema through MQTT or JSON/XML export. Initially investigate documented DWD 10-minute products and use available quantities. Handle missing markers such as `-999`, quality indicators, freshness, UTC and differing periods. Missing values must not appear as real temperature/pressure/wind readings.

### ParseOstHessen

**REQ-040** Require `StationNamePattern`. Use case-insensitive glob semantics: `*` matches any number of characters; `?` matches one. This is not arbitrary regex. Without wildcards, match a complete station name.

**REQ-041** Process all matching stations with individual source IDs and datasets, and individual messages in MQTT mode. No matches must fail. Do not silently reduce multiple matches to the first. Preserve original names/umlauts in datasets; generate stable, collision-free topic IDs.

**REQ-042** If only time of day is provided, deriving the date from retrieval time in `Europe/Berlin` is allowed. The date is an assumption, not verified observation time. Convert to UTC and mark `timestamp_quality: "estimated_date"` and `source_timezone: "Europe/Berlin"`. For an unambiguous complete source timestamp use `timestamp_quality: "source"`.

**REQ-043** Read measurement times as displayed by the source; retain complete timestamps and convert them to UTC. Do not substitute retrieval time or add freshness filtering. Consumers evaluate the times. If only time of day is supplied, apply the explicitly approved REQ-042 date inference with its estimate marker and midnight handling; never arbitrarily resolve ambiguous/nonexistent DST times.

**REQ-044** Detect/log format changes, offline stations, missing time and unreadable numbers. Do not substitute retrieval time for observation time. A gust is not mean wind; accumulated rainfall is not intensity. Do not mislabel them.

**REQ-045** Use the OsthessenWetter page structure as is, as encountered at implementation time. During implementation, document the implementation date, source URL and structural assumptions relevant to parsing. Support applies to this documented revision; later structural changes may require parser updates. Log detectable format deviations according to REQ-044; timestamp-quality and source-condition requirements remain applicable.

**REQ-046** Supply the OsthessenWetter source URL through the parser constructor, as for DWD. If the page cannot be retrieved, throw an exception with an actionable English diagnostic. Otherwise extract values and measurement times from the documented as-is page structure; apply the shared unit/UTC/rounding contract without fabricated replacements.

### SelectMOSMIX

**REQ-100** `--SelectMOSMIX` must select forecast locations from the official DWD MOSMIX catalog. Required parameters match `SelectDWD`: `Latitude`, `Longitude`, `Altitude`, `MaxDistance`, `MaxAltDifference`. Apply REQ-020 through REQ-022 unchanged, including formats, validation, distance calculation and inclusive limits. Forecast locations are not equivalent to CDC observation stations or their instrumentation.

**REQ-101** Output a MOSMIX list in the selected MQTT/JSON/XML mode. Each entry contains `mosmix_id`, `name`, `latitude`, `longitude`, `altitude_m`, `distance_km`, `altitude_difference_m`, `available_forecast_elements`. Preserve independent MOSMIX IDs as strings, including leading zeros and alphanumeric IDs. Never assume CDC IDs are MOSMIX IDs. Include `schema_version`, source, UTC retrieval time and search parameters. Sort by distance then MOSMIX ID; no matches yield a valid empty list and log notice.

**REQ-102** Normalize catalog coordinates/altitudes according to the actual documented format. Never replace missing altitude with zero; exclude unfilterable locations with a notice. Verify available elements for the selected product. If location-specific availability is unknown, mark it unknown rather than claiming all elements. Do not automatically change configuration or fall back to the CDC catalog.

### ParseMOSMIX

**REQ-103** `--ParseMOSMIX` requires `--MOSMIXID <id>` and `--Product MOSMIX_S|MOSMIX_L`. Report missing, invalid or unknown IDs with a stable error and nonzero status. Reject missing/syntactically invalid IDs before retrieval. Do not inherit the DWD reference station, silently choose a neighbor or equate ID systems. `ParseMOSMIX` is the binding spelling; `PasreMOSMIX` is a typo, not another command.

**REQ-104** Both MOSMIX_S and MOSMIX_L must have their own parsers under REQ-109. Each retrieves the latest available run for the specified ID from its constructor-supplied endpoints and reads KML/KMZ. Document source paths, element mapping and product-specific update/freshness behavior before implementation; never switch products silently. Validate namespaces, location, issue time and forecast parameter/time alignment. Do not present old runs as current without a marker.

**REQ-105** Output a separate forecast dataset with at least `schema_version`, `dataset_type: "forecast"`, `source`, `mosmix_id`, `location`, `product`, `issued_at`, `retrieved_at` and chronologically sorted `forecasts`. Each entry has `valid_at` and `values` containing available normalized quantities. Interval quantities additionally identify period start/end. `issued_at` is source issue time, `valid_at` the forecast time and `retrieved_at` retrieval time. Never use `measured_at` or publish forecasts as observations. All times follow REQ-051: UTC without `Z` or offset.

**REQ-106** Use REQ-052 field names/units where available and semantically appropriate. Verify/normalize source units and pressure references using MOSMIX element definitions. Omit unavailable quantities; never fill absent humidity or illuminance with zero. Probabilities, rain totals, gusts and energy are not direct substitutes for humidity, rain intensity, mean wind or irradiance. Derived quantities need documented formulas and periods.

**REQ-107** Align each parameter value to its correct forecast time/period. Detect inconsistent lengths, corrupted archives, wrong IDs, missing issue time and unknown required structures. Omit missing markers without shifting later values to other times. Runs with no usable forecast values fail. Never merge multiple runs' metadata into one apparent run.

**REQ-108** Forecast parsing/retrieval must be constructor-injectable and offline-testable like observation adapters. Bound download/unpacked sizes; XML must not load external entities, DTDs or referenced resources. Export the normalized forecast model, not the raw KML file.

**REQ-109** Publish separate `MosmixSParser` / `IMosmixSParser` and `MosmixLParser` / `IMosmixLParser` contracts in the NuGet library. Configure each through its own constructor URLs and DI registration. Shared low-level parsing may be reused, but do not replace both parsers with a single product switch or silent fallback. Test each product independently, simultaneous configuration, correct endpoint use and absence of cross-product state leakage.

**REQ-110** Document provider update frequency in English: `MOSMIX_S` hourly; `MOSMIX_L` four daily runs at 03, 09, 15 and 21 UTC. Distinguish run cycles from actual publication availability, retrieval scheduling and forecast validity. Derive horizon from actual file timestamps rather than an assumed fixed length. Review against the cited official DWD description before release; keep scheduling external and freshness checks product-specific.

**REQ-111** The example `--ParseMOSMIX` requires `--Product MOSMIX_S|MOSMIX_L`; reject missing/unknown values before retrieval. `--SelectMOSMIX` retains existing geographic options and distinguishes verified elements/availability per product, including unknown status. Do not mix S/L runs in metadata or datasets. Consumer-configured source/topic mappings distinguish products. Verify both products through MQTT, JSON and XML without changing the shared forecast contract or command count.

### Separate MOSMIX parsers and update frequency

Provide two distinct parsers with separate interfaces: `MosmixSParser` / `IMosmixSParser` for `MOSMIX_S`, and `MosmixLParser` / `IMosmixLParser` for `MOSMIX_L`. Both receive their own source URLs as constructor parameters and have independent DI configuration. Shared KML/KMZ helpers may be reused, but one switchable parser cannot replace the two public contracts. Missing data never triggers silent fallback to the other product.

| Product | DWD output frequency | Run times |
|---|---|---|
| `MOSMIX_S` | Hourly | A new product run each hour |
| `MOSMIX_L` | Four times daily | 03, 09, 15 and 21 UTC |

These describe provider cycles, not an internal scheduler or guaranteed file availability exactly at run time. Keep issue, retrieval and validity times separate. Read the actual forecast horizon from the file timeline; a new file does not prove all contained steps were recalculated. Define freshness limits and external retrieval plans per product.

The example keeps `--ParseMOSMIX` and additionally requires `--Product MOSMIX_S|MOSMIX_L` to select the library parser. Reject missing/unknown products before retrieval. `--SelectMOSMIX` keeps the existing geographic/altitude options and distinguishes known availability/elements per product; explicitly mark unknown availability. Output metadata, source identifiers and configured MQTT topics must distinguish S/L to prevent accidental retained-state replacement.

Source, checked 2026-10-14: [DWD procedure description, section 2.1.3/table 2.1](https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_verfahrenbeschreibung_gesamt.pdf?__blob=publicationFile&v=6). Recheck cycles before implementation/release; no parser is implemented yet.

**REQ-112** Use [pointforecast_dwd_extension_V1_0.xsd](https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd) as the binding basis for MOSMIX source/XML types used by both MOSMIX_S and MOSMIX_L parsers. Derive structures from the schema, preserve namespaces/cardinalities and verify additional KML types. Record the schema revision/checksum and reproducible generation or manual derivation process before implementation. Keep source types separate from public `[Serializable]` output entities and verify their mapping through TDD. This instruction records the schema source and does not authorize code generation.

## 3. Shared observation contract

**REQ-050** MQTT observation/forecast messages must be JSON. JSON/XML exports share the same functional contract. Observation datasets contain at least `schema_version`, `source`, `location`, `measured_at`, `retrieved_at`, `timestamp_quality`, `measurements`. Forecasts follow REQ-105. Source identifies provider/station uniquely. Never publish an observation dataset without usable readings as successful.

**REQ-051** All timestamps must be UTC in `YYYY-MM-DDTHH:mm:ss` format, without `Z` or offset. Retain unambiguous UTC internally and convert local source times before serialization. JSON/XML numbers are culture-independent and contain no unit suffix.

**REQ-052** Required observation fields; use the same names/units for corresponding forecast quantities:

| Field | Unit | Meaning |
|---|---|---|
| `temperature_c` | °C | Air temperature |
| `pressure_hpa` | hPa | Air pressure with an identified reference |
| `pressure_mmhg` | mmHg | The same pressure in another unit |
| `humidity_percent` | % | Relative humidity, 0–100 |
| `wind_direction_deg` | Degrees | Meteorological origin direction: 0 = north, 90 = east |
| `wind_speed_kmh` | km/h | Wind speed, not an unmarked gust |
| `illuminance_lux` | Lux | Illuminance |
| `solar_irradiance_w_m2` | W/m² | Solar power per area |
| `rain_rate_mm_h` | mm/h | Rain intensity |

**REQ-053** Omit absent/invalid values. `0` is not missing. Nonfinite values are forbidden. Normalization/plausibility checks are field-specific; negative temperatures are allowed. Do not invent north for calm conditions without a reliable direction.

**REQ-054** Convert using `mmHg = hPa × 0.7500617` and `hPa = mmHg × 1.3332239`. One original value is authoritative; round only for output with documented precision. Both outputs must be consistent. `pressure_reference` is `sensor`, `sea_level` or `unknown` if unverified. Never silently change the pressure reference.

**REQ-055** Do not generically convert lux and W/m². Radiation energy may become mean W/m² only with a known period; rain accumulation may become mean mm/h only with a known period. Preserve averaging semantics/reference periods in the contract.

**REQ-056** Include the measurement timestamp for each exported observation quantity. Preserve differing source times per value; consumers handle alignment and evaluation. Preserve UTC formatting, source/quality metadata and inferred-time markers. Forecasts retain their separate issue/validity contract.

## 4. MQTT and configuration

**REQ-060** Configure broker, port, TLS, credentials and topic prefix. Never hardcode/log credentials or require password arguments visible in process lists. Exclude local secret configuration from Git.

**REQ-061** Proposed consumer-configured example topics: `haus/weather/measurements/<source>/state` for observations, `haus/weather/forecasts/<source>/state` for MOSMIX forecasts, `haus/weather/stations/dwd/selection` for CDC lists and `haus/weather/stations/mosmix/selection` for MOSMIX lists. Topics/templates and prefix are configurable; these examples are not hardcoded library defaults. Forecast source IDs uniquely identify provider/product/MOSMIX ID. Observation commands do not publish forecasts; `ParseMOSMIX` does not publish observations. Each source needs scoped write permissions and must not overwrite others' topics.

**REQ-062** Publish observations and MOSMIX forecasts with QoS 1 and retained. A complete run is one message per MOSMIX location/product, preventing individual steps from replacing each other's retained state. Consumers must be able to handle QoS-1 duplicates using the source together with the measurement timestamp or forecast run. Station lists use QoS 1 without retained so old search responses are not mistaken for current selection. Never generally disable TLS certificate validation. Unencrypted operation requires explicit configuration/documentation.

**REQ-063** MQTT success requires acknowledgment. Connection, retrieval and publication need bounded timeouts and controlled cancellation. Bound retries and load. Summarize partial station failures; exit nonzero if any requested station failed, even if others published successfully.

**REQ-064** Decide configuration format/location and environment-variable names during CLI development in P3; they do not gate library work. Validate the resolved station before every `ParseDWD` retrieval. Public-repository configuration must not require private paths, internal addresses or local usernames.

### JSON and XML export

**REQ-065** All five commands support `--Output mqtt|json|xml`, default `mqtt`. `json` and `xml` modes require no MQTT connection, configuration or availability. Reject unknown formats before retrieval. Functional validation, including DWD resolution and mandatory MOSMIX ID, remains unchanged in every mode.

**REQ-066** In JSON/XML mode, optional `--OutputFile <path>` selects a file; otherwise output to stdout. `OutputFile` is invalid for MQTT. Use UTF-8, validate paths and fail on write errors. Do not silently overwrite existing files; reject existing destinations before retrieval. Failed creation must not leave a partial target file appearing successful.

**REQ-067** Both formats preserve the same normalized data, schema version, sources, observation/forecast quantities, units, UTC issue/validity times, timestamp-quality markers and quality/period metadata. Omit missing quantities; preserve zeros, leading-zero IDs and umlauts. Document a versionable XML field/collection mapping; missing numbers must not become empty numeric elements.

**REQ-068** Each export produces one syntactically complete JSON/XML document. Embed station lists, multiple results and forecast steps as collections, not concatenated documents. Ordering/metadata must be traceable and deterministic for identical input. Partial failure may yield documented successful results plus identifiable errors, but exit remains nonzero. Errors are not observations. Decide the envelope before implementation.

**REQ-069** Export success requires complete successful serialization/output. Inject serializer and file/stdout access through constructors for offline testing. Different serializations of the same result must not require another retrieval. The library MQTT serializer remains JSON; XML export must not silently change the MQTT contract.

## 5. Architecture and logging

**REQ-070** Inject all application-class dependencies through constructors. No service locators, static service singletons or direct service construction inside domain classes. Register centrally in the composition root. Values, immutable data objects and framework builders are distinct from service wiring and may be constructed normally.

**REQ-071** Separate CLI, configuration, HTTP, parsing, normalization, selection, time, JSON/XML serialization, export destinations and MQTT behind interfaces. Time/external I/O must be injectable. Classes receive their specific dependencies, not a general `IServiceProvider`.

**REQ-072** Error logs contain UTC, severity, stable error ID, command and source/station where applicable. Never log secrets or unchecked secret configuration. MQTT error collection is a future feature and cannot be required to report broker failure.

**REQ-073** Write CLI errors/logs to stderr. In JSON/XML mode without a file, stdout contains only the complete export document. File output must not contain logs or success messages either. MQTT is the default; export requires no broker.

### Export entities and serializers

**REQ-074** Every functional export object type must have an explicitly defined, strongly typed C# entity. This includes envelopes, CDC/MOSMIX list entries, search parameters, observation datasets/values, forecast runs/steps/values, and exported quality/period/error objects. Explicitly mark all custom entities in the export graph, including nested types, `[Serializable]`. A separate entity means one type per functional object, not a new type per runtime instance. Reusable value entities may be shared; observation/forecast dataset types remain separate.

**REQ-075** Use `System.Text.Json.JsonSerializer` for JSON and `System.Xml.Serialization.XmlSerializer` for XML. No manual document concatenation or separately maintained second data assembly. Both receive the same prepared functional object graph; The NuGet generic MQTT serializer uses the relevant entities and JSON serializer too. Anonymous objects, `dynamic` and untyped object/dictionary constructions must not replace defined entities.

**REQ-076** Entities contain only export data, never services, connections, loggers or secret configuration. Support both serializers with public types/properties, XML-compatible typed collections, and public parameterless constructors/writable properties where XmlSerializer requires them. Serialization attributes/central configuration control fields, roots, collections and omission. `[Serializable]` is an additional project rule, not a replacement for serializer-specific behavior. Explicitly preserve UTC without suffix, culture-independent numbers and schema versioning.

**REQ-077** Expose serializers through constructor-injected export services; use a centrally wired factory for type-specific creation if needed. Domain classes must not construct serializer services. Verify `[Serializable]` on all entities and representative complete graphs using real JSON/XML serializers: both station lists, observations, forecasts, omitted values, zeros, leading zeros, special characters, UTC and semantic equivalence. Do not completely mock serializer calls in contract tests; external I/O and wiring remain separately mockable.

**REQ-078** Every “doing class”, meaning a class with executable application/domain logic, must implement a corresponding purpose-specific interface. This includes command handlers, CLI processing, retrieval, parsers, selection, normalization, serializer/export services and MQTT publication. Constructor-inject these dependencies as interfaces and wire them in the composition root. Interfaces represent the needed contract. Pure data/export entities require no artificial interface unless they take on executable logic.

## 6. Tests and acceptance

**REQ-080** Use MSTest, Moq and FluentAssertions. Versions must fit the chosen .NET target and MIT project; check licensing before adding them. Collect coverage using Microsoft.CodeCoverage.

**REQ-081** Build the test subject/shared mocks in `[TestInitialize]`. `[TestCleanup]` calls `VerifyAll()` for every mock, for example through a shared `MockRepository`. Do not rewire production services in each test. Individual tests may vary data and configure explicit expected interactions.

**REQ-082** Required tests:

- Decimal/DMS coordinates, S/W hemispheres, limits, invalid input and culture independence.
- Inclusive geographic/altitude filters, stable sorting, empty lists and product-specific quantities.
- DWD CLI precedence, configuration fallback, missing/invalid IDs, and no retrieval/publication on configuration failure.
- DWD CSV/ZIP, missing products/markers, station identity and differing observation times.
- MOSMIX catalog filters, alphanumeric/leading-zero IDs, missing altitudes, sorting and empty results.
- MOSMIX KML/KMZ, namespaces, station/product checks, issue/validity times, UTC, series alignment, missing values and mismatched lengths.
- Forecast unit/period normalization, separation from observations, complete retained runs, export without MQTT, rejected XML external entities and oversized archives.
- OsthessenWetter one/many/no pattern matches, umlauts, format changes and offline stations.
- UTC without offset, midnight, DST gaps/ambiguity and estimated dates.
- Conversions, real zeros, missing quantities, pressure references, accumulation/gust versus intensity/mean wind.
- MQTT topics, QoS/retained, authentication/TLS failure, timeouts, cancellation, partial failure and exit codes.
- Secret-free logs and publication success only after acknowledgment.
- Independent NuGet consumption, example CLI package reference, constructor URLs, isolated DI configuration and generic configurable MQTT topics under REQ-012 through REQ-016.
- `[Serializable]` entities, complete graphs and real serializers under REQ-074 through REQ-077.
- Both exports for all five commands: semantic equivalence, UTC, omission, zeros, leading-zero IDs, umlauts and XML escaping.
- Complete multi-station/empty-list documents, file/stdout output, separate logs, no MQTT for export, invalid format/destination combinations, existing files, write errors and partial failures.

**REQ-083** Standard automated tests must run offline without production brokers. Prefer small synthetic fixtures. Keep live/broker integration tests separate, explicitly enabled and documented. Do not add unchecked third-party page copies to Git.

**REQ-084** Before release, execute/verify on the documented Debian/ARM64 target. Check noninteractive single-command runs, process termination, exit codes/channels, real JSON/XML serializers, UTC/timezones and resource cleanup. Document example scheduling/chaining without assuming a persistent process. Report hardware/ARM checks separately from offline unit tests; unperformed platform tests remain explicitly open.

**REQ-085** Follow TDD: first write a meaningful behavior/failure test and demonstrate its expected failure (Red), implement minimally until passing (Green), then refactor with green tests (Refactor). Bug fixes start with a reproducing regression test. MSTest, Moq, FluentAssertions, TestInitialize and TestCleanup/VerifyAll remain mandatory. Test behavior rather than implementation details. Apply this sequence once implementation is explicitly authorized; the current instruction covers project scaffolding; domain implementation remains deferred.

**REQ-086** Line and branch coverage must each reach at least 80%. During initial bootstrap without a measurable target-branch baseline, only this minimum applies; target-branch comparison is not applicable. An unexpectedly missing, stale or incomparable baseline in subsequent operation remains an error. Documentation-only PRs may retain the same coverage for each metric as the target branch; decreases are forbidden. Other PRs must strictly improve every metric below a 100% baseline; an exact 100% baseline accepts an exact 100% result. Use current commit IDs, comparable collection conditions and unrounded counter ratios. Missing PR measurements, invalid counts and zero denominators cannot pass. Microsoft.CodeCoverage reports, including the code-block breakdown, are the basis for verification; block coverage is not interchangeable with branch coverage. Verify metric mapping and report format against real reports in P1. CI uses Microsoft.CodeCoverage and produces machine-readable results and readable report artifacts. Keep exclusions narrow and justified; never hide domain logic. Coverage supplements behavior/regression tests. This requirement authorizes neither publication nor code generation.

## 7. Open source and next steps

**REQ-090** Include README, requirements, agent rules and MIT license. README distinguishes planned and implemented features. Dependencies/data retain their own conditions; the project's MIT license does not replace them.

**REQ-091** Before each relevant implementation step (CLI decisions only in P3), apply the confirmed .NET/Debian minimums to the test matrix; decide SDK patch, packages, deployment, configuration, MQTT/JSON/XML contracts including periods and error/exit codes. Also define both MOSMIX parsers, product mappings, catalog format, mapping and forecast contract. Also decide package API, constructor URL contracts, topic configuration and DI integration. Then implement/verify package/example structure and tests, DWD/MOSMIX selection, DWD observations, MOSMIX forecasts, OsthessenWetter and output integration incrementally. Generate application code only upon explicit instruction.

**REQ-092** Before production retrieval check access, attribution, frequency and source conditions. No approved machine-readable OsthessenWetter API is established; HTML parsing remains format-dependent. Never bypass access controls or request restrictions.

**REQ-093** English is the sole maintained language for project documentation and contributor instructions. Maintain one canonical English document at each standard unsuffixed path, including `README.md`, `REQUIREMENTS.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md` and `PHASE_P1_PLAN.md`; do not maintain German or `.en.md` counterparts. Before consolidation, reconcile all information unique to either version. Preserve requirement IDs, commands, fields, units, examples, source identities and technical contracts. Keep documentation and implementation status consistent in the same revision. Preserve the unchanged English MIT legal text in `LICENSE`. Review English-language consistency and local links with every change. This decision dated 2026-10-19 supersedes bilingual maintenance.

Planned implementation and outstanding decisions are documented in [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md).

References: [DWD CDC](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/), [DWD dataset description](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/air_temperature/DESCRIPTION_obsgermany_climate_10min_air_temperature_en.pdf), [OsthessenWetter observations](https://osthessen-wetter.de/messwerte/).

MOSMIX references: [DWD Open Data formats, element definitions and station catalog](https://www.dwd.de/DE/leistungen/opendata/hilfe.html), [MOSMIX_L individual locations](https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/), [MOSMIX_S forecasts](https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_S/).

Serializer references: [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview), [XmlSerializer and XML entities](https://learn.microsoft.com/en-us/dotnet/standard/serialization/introducing-xml-serialization), [SerializableAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.serializableattribute).

Sources (checked 2026-10-14): [.NET support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [.NET 10 OS matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md), [Debian releases](https://www.debian.org/releases/).

## Binding MOSMIX schema source

[DWD pointforecast_dwd_extension_V1_0.xsd](https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd) is the binding basis for both parsers' MOSMIX source/XML data structures. Derive types, namespaces, elements and cardinalities from this schema; verify additional KML structures before implementation. Record schema revision and checksum for reproducible type generation/derivation. Map source types to separate public output entities; they do not replace the normalized forecast contract. Follow TDD for schema/mapping tests. No C# data structures have been generated yet.


**REQ-057** Output precision: temperature 0.1 °C; pressure 1 hPa and 1 mmHg; humidity 1 percentage point; wind speed 1 km/h. Convert from unrounded values before rounding; round exact midpoint ties away from zero (20.25 → 20.3; −20.25 → −20.3).

**REQ-079** Generate XSD files from public XML export entities reproducibly and validate real XML exports against them. Keep these schemas separate from the MOSMIX input schema.


## Additional decisions dated 2026-10-14

- Development environment: Visual Studio 2026. Locally verified: Community 18.8.3 and .NET SDK 10.0.303; use this SDK revision as the reproducible starting point for the planned `global.json` and CI, retaining target framework `net10.0`. Check servicing status before implementation and explicitly document any required SDK update. Visual Studio is not a Debian runtime dependency. [Microsoft SDK/Visual Studio mapping](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs).
- All program-generated messages are English: help, validation, errors, logs and status. Data field names remain language-independent; preserve original source names. Project documentation is English only (language-policy update dated 2026-10-19).
- Observation output includes the measurement timestamp for every exported quantity in UTC without a timezone suffix. Preserve different times per value; consumers handle temporal alignment, selection and evaluation. Keep inferred source times marked accordingly. Forecasts retain their separate issue/validity times.
- Round only for output, after normalization/conversion: temperature to 0.1 °C, pressure to 1 hPa or 1 mmHg, relative humidity to 1 percentage point and wind speed to 1 km/h. Compute both pressure outputs from the same unrounded original; account for rounding tolerance. Round exact midpoint ties away from zero (20.25 → 20.3; −20.25 → −20.3). No new rounding policy is imposed on other quantities.
- Generate reproducible XSD files for public XML export entities. XML serializer contract tests validate exports against these schemas. Keep output schemas separate from the DWD MOSMIX source XSD; implement generation/versioning in P2.
- Tests use MSTest, Moq and FluentAssertions; collect coverage using Microsoft.CodeCoverage. Record exact versions, test-runner integration and platform evidence in P1.
- Coverage exception: each metric at an exact 100% target-branch baseline may remain at an exact 100% in the PR. Except for documentation-only PRs, metrics below 100% require strict improvement; initial bootstrap without a baseline skips comparison. Each must reach at least 80%. A displayed, rounded 100% is insufficient.
- Decide remaining CLI questions during CLI development in P3: configuration format/path, environment variables, secret handling, concrete exit codes and CLI cancellation/timeout bounds. These do not gate P0/P1 library work. Keep public library error/cancellation contracts separate; P1 may document minimal process conventions for tests without preempting the final CLI contract.

These decisions update documentation only; no application code or XSD files have been generated.


## P0 clarifications dated 2026-10-14

- Dependencies: On adoption or an explicitly requested update, choose the latest available stable versions, check .NET/platform compatibility and licensing, and pin the resolved versions reproducibly. Do not use unchecked floating versions on every build; document conflicts rather than silently selecting an older version.
- Coverage bootstrap: At least 80% per metric, without target-branch comparison when no measurable baseline exists yet. Documentation-only PRs may retain equal coverage. The existing exact-100% exception remains valid. Use Microsoft.CodeCoverage reports with their code-block breakdown for verification.
- Rounding: Round to the nearest agreed increment; round exact midpoint ties away from zero (`MidpointRounding.AwayFromZero`), for example 20.25 → 20.3 and −20.25 → −20.3. Round after conversion, not before.
- Sources: Consumers supply concrete DWD and OsthessenWetter endpoints through the respective parser constructor parameters. Do not embed source paths or treat them as an unresolved global P0 configuration decision. Document actual source formats and parameter mappings during the corresponding adapter implementation.
- OsthessenWetter: If the configured page cannot be retrieved, the parser throws an exception with an actionable English diagnostic; no replacement data or simulated success. Parse an available page using the documented as-is structure, including its displayed measurement timestamps. Do not add freshness filtering or artificially correct source data; consumers evaluate measurement timestamps. Shared UTC/unit/rounding rules still apply. When only a time of day is available, retain the explicitly permitted date inference and estimated marker under REQ-042; never replace a complete source timestamp. Decide CLI error presentation and exit codes in P3.

These are documentation decisions; they do not generate application code yet.


## Binding phase status and sequence

**2026-10-15: P0 complete; P1 solution/project scaffolding created. P1 is not yet accepted.**

Sequence with unchanged phase IDs: **P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9**. Complete and accept the entire library first, including station selection, DWD/OsthessenWetter/MOSMIX_S/MOSMIX_L parsers, output entities, JSON/XML, XSDs, MQTT serializer and DI. Then begin P3 with the CLI usage example, configuration and example broker transport. Public release remains P9.

P1 contains only a technical package consumer as an integration test, not a CLI/application example. Before P3, accept source functionality directly through the library and its tests; CLI commands do not gate library phases. This sequence supersedes earlier numeric ordering or dependencies on P3. [P1 detailed plan](PHASE_P1_PLAN.md). The current task adds project scaffolding; domain implementation remains deferred.
