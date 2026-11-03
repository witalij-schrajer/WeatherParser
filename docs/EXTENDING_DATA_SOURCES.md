# Adding a data source

German translation: [de/EXTENDING_DATA_SOURCES.md](de/EXTENDING_DATA_SOURCES.md).

This guide is for developers who want WeatherParser to read another weather data source. It names what has to be decided before any code is written, how the existing sources are built, which tests and evidence are required, and what has to be documented. It describes requirements and existing patterns. It does not approve a new source, and no source beyond DWD CDC observations, OsthessenWetter and DWD MOSMIX exists in the library.

## 1. Two ways to add a source

| Way | Where the code lives | Registration | When to choose it |
| --- | --- | --- | --- |
| **Contribution to the library** | `src/WeatherParser.Parsing`, through a pull request into `develop` | Centrally in `AddWeatherParsing` | The source is useful to every consumer, its conditions allow it, and you follow the repository workflow ([CONTRIBUTING.md](../CONTRIBUTING.md), [AGENTS.md](../AGENTS.md)) |
| **Adapter in your application** | Your own project, consuming the `WeatherParser.Parsing` package | Your composition root, next to the [library registration](LIBRARY_USAGE.md#3-register-the-library) | You need the source now or only for your own use |

The library has no plugin API, no universal source interface and no discovery: nothing scans assemblies or loads adapters automatically. An application-owned adapter is an ordinary class of your application that uses the library's public building blocks (section 4) and produces the library's export entities, so the library's serializers accept its results.

## 2. Which kind of change is it?

| Change | What it needs |
| --- | --- |
| A new **observation** provider whose quantities already exist in the export contract (temperature, humidity, pressure, wind speed and direction, rain rate, solar irradiance, illuminance) | A source contract (section 3) and an implementation that maps to `ObservationDataset` through `IObservationDatasetBuilder` |
| A new **quantity**, a new member of an export entity, or a new value of an existing enumeration | A change of the export contract with its own decision and review: version 1 accepts only optional additions ([export contract](p2/P2_EXPORT_CONTRACT.md)), the XML schema is regenerated, and consumers with exhaustive switches are affected |
| A new **forecast** provider | A contract change as well: `ForecastRun.Product` is the enumeration `MosmixProduct` and `IForecastRunBuilder` forms `source` only as `dwd-mosmix-l:<id>` or `dwd-mosmix-s:<id>`, so a forecast of another provider cannot be expressed without extending the contract |
| A new **station list** type | A new export root, which is a contract change, and a new MQTT message type |

Never reuse an existing export member for a different physical quantity. For example, illuminance in lux is not solar irradiance in W/m², and a gust is not a mean wind speed.

## 3. Record the source contract first

Before implementation, write down the facts of the source and the decisions that follow from them, as the existing contracts do ([DWD observations](p5/P5_OBSERVATION_CONTRACT.md), [OsthessenWetter](p6/P6_OSTHESSEN_CONTRACT.md), [MOSMIX forecasts](p7/P7_FORECAST_CONTRACT.md)). Every fact comes from the provider's official documentation or from the actual published files; record the URL, the retrieval date and, for sample files, the size and SHA-256. Write "not documented" where the source says nothing, and never fill the gap with an assumption presented as a fact.

- **Identity and scope**: provider name, observation or forecast, the station or location identifier (kept as a string, with leading zeros), and the `source` value your results will carry. Observation datasets carry `<provider>:<station_id>`, for example `dwd:00044` or `osthessenwetter:<station ID>`.
- **Access and conditions** (REQ-092): licence, attribution text, redistribution rules, whether automated retrieval is allowed, documented rate limits or a project recommendation clearly marked as such, and what the provider logs about requesters. A source without clarified conditions may be implemented and tested with synthetic data, but its production use stays open, as for OsthessenWetter (OW-14).
- **Endpoints and authentication**: each URL the parser needs, as its own named option; who supplies it (always the consuming application, never a built-in default); credentials, if any, and how diagnostics avoid revealing them or sensitive query strings.
- **Format**: media type, encoding, schema or format version, archive structure and member names, maximum sizes observed, and how a format change is detected.
- **Values**: unit, precision, missing-value markers, quality flags, and whether each value is instantaneous, a mean, a maximum or a sum, over which period.
- **Times**: time zone, precision, whether the date is shown, how measurement, issue, retrieval and validity times relate. Local times are converted to UTC; an inferred date is marked `estimated_date` (REQ-042).
- **Mapping and failures**: the export member per source value, conversions and their reference periods, which values are omitted and why, what fails the whole call, what is a partial failure, and the stable error and notice IDs.

In this repository, a source contract is a decision record that the maintainer approves before implementation, after its review rounds.

## 4. Build it from the existing patterns

Every class with logic implements its own purpose-specific interface and receives its dependencies through the constructor (REQ-070, REQ-071, REQ-078). Retrieval, format reading, mapping and orchestration are separate where their responsibilities differ. Split a class only where a responsibility justifies it, not to match a diagram. The existing sources show three shapes:

| Source | Entry point | Retrieval | Reading and mapping | Result |
| --- | --- | --- | --- | --- |
| DWD 10-minute observations | `IDwdObservationParser.ParseAsync(stationId, ct)` | `IDwdObservationSource` (four station lists and up to four archives) | `IDwdStationResolver`, `IDwdObservationMapper` | `ObservationDataset` |
| OsthessenWetter | `IOsthessenWetterParser.ParseAsync(stationNamePattern, ct)` | `IOsthessenWetterPageSource` (one page) | `IOsthessenWetterPageReader`, `IOsthessenWetterValueReader`, `IOsthessenWetterTimeReader`, `IOsthessenWetterStationIdentity`, `IOsthessenWetterObservationMapper` | `OsthessenWetterParseResult` (datasets and per-station failures) |
| MOSMIX_L and MOSMIX_S | `IMosmixLParser`, `IMosmixSParser` (separate, no product switch) | `IMosmixLForecastSource`, `IMosmixSForecastSource` | `IMosmixForecastDocumentReader`, `IMosmixElementMapper`, `IMosmixUnitScaler` behind the shared `IMosmixParsePipeline` | `MosmixParseResult` |

Each parser interface has its own signature and result model. A new source gets its own interface that fits its identifiers and its failure model; it does not have to look like the others.

Reuse these public building blocks instead of copying their rules:

| Building block | Purpose |
| --- | --- |
| `ICatalogDownloader` with `CatalogRequest` | One bounded GET: maximum bytes, request timeout, deadline token, optional "404 means absent", stable retrieval errors, no retries |
| `IParserEndpointValidator`, `IEndpointUriValidator` | Validation of endpoint URIs at construction |
| `ISourceTimeNormalizer`, `ITimeZoneResolver` | Conversion of complete or local source times to UTC, and date inference with the estimated marker |
| `IQuantityValidator`, `IPressureConverter`, `IPeriodRateConverter`, `IWindSpeedConverter`, `IOutputRounding` | Plausibility, pressure units from one original value, amount-to-rate conversion with its period, wind units, output rounding with ties away from zero |
| `IObservationDatasetBuilder` with `ObservationInput` and `ObservationReading` | Builds the `ObservationDataset` (source, location, per-quantity UTC times, periods, quality, omissions) and reports `OmittedReadings` |
| `IJsonExportSerializer`, `IXmlExportSerializer`, `IMqttMessageSerializer` | Source-independent output; a new source never needs its own serializer |

HTTP, time, file system and logging are injected (`HttpClient`, `TimeProvider`, `ILogger<T>`). The library reads no configuration files, environment variables or command-line arguments; endpoints and options come from the consumer as registered option objects.

### Mapping rules

- Timestamps are UTC internally and on the wire (`YYYY-MM-DDTHH:mm:ss`, no suffix). Every quantity keeps its own measurement time; values with different times are never merged into one misleading timestamp.
- A missing or invalid value is omitted with a reason; `0` is a real value. A provider's missing-value marker is never exported.
- Convert from the original value and round only for output. Pressure in hPa and mmHg comes from the same unrounded value with its reference. Convert rates and energies only with a verified reference period.
- Keep quality information as the source states it, and keep identifiers as strings.

### Retrieval and resource bounds

- Every request is bounded in size and time and observes the caller's `CancellationToken`. A parse has an overall deadline option, validated at construction. The caller's cancellation wins over the deadline, which wins over every other failure.
- No retries inside the library, no scheduler, no background timer. One call is one retrieval; scheduling and retries belong to the consuming application.
- XML is read without DTD processing and without external entities. Archives are read with bounded member count, size and expansion, and member names are never used as file paths. Apply equivalent bounds to other formats, for example HTML parsed without scripts or resource loading, as for OsthessenWetter.
- Log stable event names with the source, the identifier and UTC times, never measurement values, credentials, payloads or query strings.

## 5. Registration

- **Library contribution**: register every new service in `AddWeatherParsing`. Because `AddWeatherParsing` registers all sources and container validation checks all of them, every new option or endpoint type becomes a required registration for every consumer, also for consumers that never use the new source. List it in the [library usage guide](LIBRARY_USAGE.md#3-register-the-library) and in its example registration (which the package tests check), in the README reference and in the changelog, as a breaking change for consumers that validate their container.
- **Application-owned adapter**: register your classes in your composition root after `AddMyWeatherParsing` (or your equivalent), resolving the library building blocks you use through their interfaces.

The library does not support registering only some sources; do not document or build on partial registration.

## 6. Output, MQTT and the example CLI

The serializers are source-independent: the JSON and XML serializers accept the export roots `ObservationDataset`, `ForecastRun`, `CdcStationList`, `MosmixLocationList` and the envelope `ExportDocument`, and the MQTT serializer the same roots without `ExportDocument` (an envelope fails with `MQTT-MESSAGE-TYPE-UNSUPPORTED`; publish its datasets and runs one by one). Whatever its provider, a valid dataset is accepted, so a new observation provider needs no serializer change. The MQTT `{provider}` placeholder is taken from the text of `source` before the first `:`, so consumers can give the new provider its own topic. A new CLI command or an MQTT broker client is not part of adding a source; they are separate changes of the example CLI with their own contract decisions.

## 7. Tests and evidence

In this repository every change is test-first (Red, Green, Refactor) with MSTest, Moq and FluentAssertions, and standard tests run without network access ([CONTRIBUTING.md](../CONTRIBUTING.md)). For a new source:

- **Offline behavior tests** with a fake HTTP handler, a fixed `TimeProvider` and synthetic or appropriately licensed, redacted fixtures: normal data, missing fields and markers, malformed data, unexpected units and time zones, boundary values on both sides, overflow where relevant, cancellation, deadlines, HTTP errors and timeouts, oversized responses, and partial failures.
- **Mapping assertions**: converted values, rounding, timestamps and their provenance, periods, missing versus zero, and the logged IDs.
- **Contract tests with the real serializers**: JSON and XML of the same result are equivalent, the XML validates against the packaged schema, and MQTT messages resolve their topics.
- **Package and container tests**: the new services resolve from the packed library with `ValidateOnBuild`, the [library usage example](LIBRARY_USAGE.md#10-how-this-guide-is-verified) still builds and validates, and the tests of the existing sources still pass.
- **Coverage and CI**: the native block coverage gates and the CI jobs of [CONTRIBUTING.md](../CONTRIBUTING.md) section 6 apply. Platform evidence for Debian 13 ARM64 comes from runs on the target, not from mocks.
- **Live checks** against the real source are separate, explicitly authorized and recorded; they never run in the standard tests. Mocked tests do not show that the real source is reachable or unchanged.

## 8. Documentation

Document in the same change: the source in the README library reference (call, options, limits, exported quantities, error and notice IDs, source conditions and retrieval guidance), the new registrations in the [library usage guide](LIBRARY_USAGE.md), the data source and its attribution in the README "Data sources" section, the change in [CHANGELOG.md](../CHANGELOG.md), and any new or changed requirement in [REQUIREMENTS.md](../REQUIREMENTS.md). An open decision stays marked as open. The English documents are authoritative; their German translations in [docs/de](de/README.md) are brought up to date before the next version.

## 9. Proposing a source

If you want a source added to the library but do not implement it yourself, open a data source request as described in [REPORTING_ISSUES.md](REPORTING_ISSUES.md#request-a-new-data-source). A request collects the facts of section 3; it is not an approval to implement the source.
