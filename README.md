# WeatherParser

**Phase status (2026-10-19): P0 complete; P1.01–P1.07 are recorded as complete. SDK 10.0.402 is pinned; remaining P1 acceptance, including clean-checkout reproducibility, is pending.**

WeatherParser is primarily a planned C# NuGet library for DWD/OsthessenWetter observations and MOSMIX forecasts. The published product is the package containing parsers, output entities, a generic MQTT serializer and DI integration. The CLI is only a usage example consuming that package.

## NuGet primary product and example CLI

The intended published product is a reusable NuGet library. It contains DWD/OsthessenWetter/MOSMIX parsers with required retrieval/normalization, public output entities, a generic MQTT serializer and dependency-injection integration. The CLI is only a usage example and consumes a versioned package reference; other applications can use the package without the CLI.

Each parser receives its required source URLs explicitly as constructor parameters, preferably typed as `Uri`. Name multiple endpoints separately. DI integration forwards consumer-supplied endpoints to the corresponding constructors, without invented built-in sources or global URI singletons. HTTP, time and other services remain constructor-injected. The library does not independently read CLI arguments or application-specific configuration files/environment variables.

The MQTT serializer is source-independent: it serializes suitable output entities and creates a message containing topic, JSON payload, QoS and retain setting. Topics/templates and their mapping to message types come from consumer configuration, not hardcoded weather paths. Validate resolved publication topics; missing mappings and unresolved placeholders produce defined failures. Existing `haus/weather/...` topics are configurable examples only. Serialization and broker transport are separate; serialization needs neither a broker connection nor credentials.

Provide documented DI registration for library parsers, serializer and required services. Consumers supply endpoints, topic configuration and external dependencies. Every doing class has an interface; output entities remain dedicated `[Serializable]` types. Preserve testability, UTC and JSON/XML compatibility.

The example CLI owns arguments, application configuration, single-command process execution, exit codes, file/stdout export and optional MQTT transmission. It accesses parsing only through the package, never copied implementations. Acceptance prioritizes the public API, package versioning and independent consumption. The local test package is `WeatherParser.Parsing` version `0.1.0-initial`, built and consumed in [P1.04](docs/p1/P1_04_PACKAGE_CONSUMER.md). Public package identity, feed and publication procedure remain release decisions; no public package publication is claimed.
## Languages

English is the sole maintained language for project documentation and instructions. Use one canonical English file at each standard unsuffixed path; do not maintain German or `.en.md` counterparts. Preserve requirement IDs, commands, data fields, units, examples and original source names. This decision dated 2026-10-19 supersedes the earlier bilingual policy.

**All code comments must be written in English**, including inline/block comments, XML documentation comments, and comments in scripts and configuration examples. All program-generated runtime messages must be English. The binding MIT license text in `LICENSE` remains in English.

## Project status

As of 2026-10-19, the solution contains the library, source-reference tests, isolated package-consumer tests and coverage tooling. Parser-endpoint validation and the `AddWeatherParsing` DI entry point exist; the unused export-entity scaffolding was removed on 2026-10-19 and will be reintroduced test-first in P2. P1.05/P1.06 record 6 and then 8 passing unit tests; P1.04 records 2 passing package-consumer tests, and P1.07 records coverage collection. These are historical results, not a new test run. **Weather-source parsers and executable CLI commands are not implemented yet.** The commands below describe the planned interface and cannot be run yet. Full P1 acceptance remains open; no GitHub or public package publication is claimed.

## Workflow role and execution

The example CLI is an independently callable component of the overall weather-data and automation workflow. Its responsibilities are retrieval, parsing, station selection, normalization and the requested output. Downstream storage, display and other actions belong to other components.

**Each invocation executes exactly one command and then exits.** Scheduling and retries across invocations are external responsibilities, for example in n8n or cron. WeatherParser has no built-in scheduler and does not run as a persistent service. One command may return results for multiple stations. The CLI must support noninteractive execution, meaningful exit codes and separate data and log output.

Example workflows:

- n8n or cron → WeatherParser → MQTT → dashboard or other consumers.
- External workflow → WeatherParser → JSON/XML file → downstream processing.

There is no hard dependency on n8n; other programs and shell scripts can invoke the component.

## Target platform

Binding platform policy (2026-10-14): .NET version 10 or newer and Debian version 13 Trixie or newer. The initial library/example target framework is `net10.0`; the minimum architecture is ARM64 (`linux-arm64`), particularly NanoPi R76S. Add newer stable .NET/Debian releases to the verified compatibility matrix; previews are not automatically guaranteed. Visual Studio 2026 and SDK 10.0.303 are the verified local starting point; packages, extra ARM variants and deployment model remain undecided. Verify the library, actual package consumer and example on Debian 13/ARM64 with .NET 10. Distinguish platform requirements from verified compatibility; never claim runtime success without documented tests.

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

## Planned JSON and XML export

All five commands support `--Output mqtt|json|xml`, with MQTT as the default. JSON and XML export require no MQTT broker. `--OutputFile <path>` writes a UTF-8 file; without a path, the complete document goes to standard output. Logs remain on standard error. Existing files are not silently overwritten.

```text
WeatherParser --ParseDWD --StationID <DWD-STATION-ID> --Output json --OutputFile weather.json
WeatherParser --ParseOstHessen --StationNamePattern "Fulda*" --Output xml --OutputFile weather.xml
WeatherParser --SelectDWD --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200 --Output json
```

These commands are not implemented yet. Both export formats contain the same observation or forecast data and UTC timestamps as MQTT, including estimated-date markers. Multiple station results, station lists and forecast steps are grouped into one complete document. The library serializer MQTT payloads remain JSON.

### Export entities and serialization

Each functional export object type has its own strongly typed C# entity marked `[Serializable]`, including nested export objects. This includes station lists and entries, observation datasets, forecast runs and steps, and quality/period metadata. Reusable value entities may be shared.

JSON uses `System.Text.Json.JsonSerializer`; XML uses `System.Xml.Serialization.XmlSerializer`. Both formats and MQTT JSON use the same prepared export entities. Serializer-specific rules control field names, omitted values and UTC without a timezone suffix. Serializer services use constructor injection. The earlier, untested export-entity scaffolding was removed on 2026-10-19; P2 introduces each entity test-first together with its serializer contracts.

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

See the [development plan](DEVELOPMENT_PLAN.md) for phases, dependencies and acceptance gates. The [P1 detailed plan](PHASE_P1_PLAN.md) breaks the first implementation phase into work packages and tests. See [REQUIREMENTS.md](REQUIREMENTS.md) for detailed requirements and acceptance criteria, and [AGENTS.md](AGENTS.md) for contributor rules. Restore/build commands are documented below; test methods have not been implemented yet.

## Configuration and publication

Station selection, source settings, MQTT connection and topics are configurable. Credentials belong in environment variables or local configuration excluded from Git. The public repository contains only examples without secrets or private operational data.

The example CLI separate broker transport uses dedicated credentials and scoped topic permissions. Sources have separate observation or forecast messages; observations, forecasts and both station-list types use separate topics. Topic behavior is defined in the requirements.

## Data sources

- [DWD Climate Data Center: 10-minute observations](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/)
- [DWD: temperature, humidity and pressure dataset description](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/air_temperature/DESCRIPTION_obsgermany_climate_10min_air_temperature_en.pdf)
- [OsthessenWetter observations](https://osthessen-wetter.de/messwerte/)
- [DWD MOSMIX: catalog, formats and element definitions](https://www.dwd.de/DE/leistungen/opendata/hilfe.html)
- [DWD MOSMIX_L: individual forecast locations](https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/)

Code is licensed under [MIT](LICENSE). This does not grant rights to third-party weather data or websites. Source conditions, attribution and permitted retrieval frequency must be documented separately for each adapter and checked before production use. A publicly available OsthessenWetter API is not assumed.

## Binding MOSMIX schema source

[DWD pointforecast_dwd_extension_V1_0.xsd](https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd) is the binding basis for both parsers' MOSMIX source/XML data structures. Derive types, namespaces, elements and cardinalities from this schema; verify additional KML structures before implementation. Record schema revision and checksum for reproducible type generation/derivation. Map source types to separate public output entities; they do not replace the normalized forecast contract. Follow TDD for schema/mapping tests. No C# data structures have been generated yet.


## Additional decisions dated 2026-10-14

- Development environment: Visual Studio 2026. Locally verified: Community 18.8.3 and .NET SDK 10.0.303; use this SDK revision as the reproducible starting point for the planned `global.json` and CI, retaining target framework `net10.0`. Check servicing status before implementation and explicitly document any required SDK update. Visual Studio is not a Debian runtime dependency. [Microsoft SDK/Visual Studio mapping](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs).
- All program-generated messages are English: help, validation, errors, logs and status. Data field names remain language-independent; preserve original source names. Project documentation is English only (language-policy update dated 2026-10-19).
- Observation output includes the measurement timestamp for every exported quantity in UTC without a timezone suffix. Preserve different times per value; consumers handle temporal alignment, selection and evaluation. Keep inferred source times marked accordingly. Forecasts retain their separate issue/validity times.
- Round only for output, after normalization/conversion: temperature to 0.1 °C, pressure to 1 hPa or 1 mmHg, relative humidity to 1 percentage point and wind speed to 1 km/h. Compute both pressure outputs from the same unrounded original; account for rounding tolerance. Round exact midpoint ties away from zero (20.25 → 20.3; −20.25 → −20.3). No new rounding policy is imposed on other quantities.
- Generate reproducible XSD files for public XML export entities. XML serializer contract tests validate exports against these schemas. Keep output schemas separate from the DWD MOSMIX source XSD; implement generation/versioning in P2.
- Tests use MSTest, Moq and FluentAssertions; collect coverage using Microsoft.CodeCoverage. Record exact versions, test-runner integration and platform evidence in P1.
- Coverage exception: block coverage at an exact 100% target-branch baseline may remain at an exact 100% in the PR. Except for documentation-only PRs, block coverage below 100% requires strict improvement; initial bootstrap without a baseline skips comparison. Block coverage must reach at least 80%. A displayed, rounded 100% is insufficient.
- Coverage metric (decision dated 2026-10-19): Changed from line and branch coverage to code-block coverage as reported natively by Microsoft.CodeCoverage. This change is due to Microsoft.CodeCoverage not measuring branches and the ambiguity in line counts for partially covered lines. The earlier line and branch wording is superseded.
- Decide remaining CLI questions during CLI development in P3: configuration format/path, environment variables, secret handling, concrete exit codes and CLI cancellation/timeout bounds. These do not gate P0/P1 library work. Keep public library error/cancellation contracts separate; P1 may document minimal process conventions for tests without preempting the final CLI contract.

These decisions update documentation only; no application code or XSD files have been generated.


## P0 clarifications dated 2026-10-14

- Dependencies: On adoption or an explicitly requested update, choose the latest available stable versions, check .NET/platform compatibility and licensing, and pin the resolved versions reproducibly. Do not use unchecked floating versions on every build; document conflicts rather than silently selecting an older version.
- Coverage bootstrap: At least 80% block coverage, without target-branch comparison when no measurable baseline exists yet. Documentation-only PRs may retain equal coverage. The existing exact-100% exception remains valid. Use Microsoft.CodeCoverage reports with their native block counters for verification.
- Rounding: Round to the nearest agreed increment; round exact midpoint ties away from zero (`MidpointRounding.AwayFromZero`), for example 20.25 → 20.3 and −20.25 → −20.3. Round after conversion, not before.
- Sources: Consumers supply concrete DWD and OsthessenWetter endpoints through the respective parser constructor parameters. Do not embed source paths or treat them as an unresolved global P0 configuration decision. Document actual source formats and parameter mappings during the corresponding adapter implementation.
- OsthessenWetter: If the configured page cannot be retrieved, the parser throws an exception with an actionable English diagnostic; no replacement data or simulated success. Parse an available page using the documented as-is structure, including its displayed measurement timestamps. Do not add freshness filtering or artificially correct source data; consumers evaluate measurement timestamps. Shared UTC/unit/rounding rules still apply. When only a time of day is available, retain the explicitly permitted date inference and estimated marker under REQ-042; never replace a complete source timestamp. Decide CLI error presentation and exit codes in P3.

These are documentation decisions; they do not generate application code yet.


## Binding phase status and sequence

**2026-10-15: P0 complete; P1 solution/project scaffolding created. P1 is not yet accepted.**

Sequence with unchanged phase IDs: **P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9**. Complete and accept the entire library first, including station selection, DWD/OsthessenWetter/MOSMIX_S/MOSMIX_L parsers, output entities, JSON/XML, XSDs, MQTT serializer and DI. Then begin P3 with the CLI usage example, configuration and example broker transport. Public release remains P9.

P1 contains only a technical package consumer as an integration test, not a CLI/application example. Before P3, accept source functionality directly through the library and its tests; CLI commands do not gate library phases. This sequence supersedes earlier numeric ordering or dependencies on P3. [P1 detailed plan](PHASE_P1_PLAN.md). The current task adds project scaffolding; domain implementation remains deferred.


## Project structure and local build

As of 2026-10-19, [WeatherParser.slnx](WeatherParser.slnx) contains four projects:

```text
WeatherParser.slnx
src/
  WeatherParser.Parsing/           # Packable net10.0 library
tests/
  WeatherParser.Parsing.Tests/     # MSTest project with source reference
  WeatherParser.Package.Tests/    # Isolated NuGet PackageReference consumer tests
tools/
  WeatherParser.Coverage/         # Non-packable coverage-tooling library
```

`global.json` pins SDK 10.0.402 with roll-forward disabled. Visual Studio 2026 can open `.slnx` directly. `Directory.Build.props` enables `net10.0`, nullable analysis, implicit usings and deterministic builds. `Directory.Packages.props` pins package versions, including the local `WeatherParser.Parsing` test package; see [dependencies and licenses](DEPENDENCIES.md). `NuGet.Config` configures nuget.org and the project-local `artifacts/local-nuget` source. Build the local package before the first solution restore so that the package-consumer dependency is available.

From the repository directory, the intended restore/build sequence is:

```powershell
dotnet restore src/WeatherParser.Parsing/WeatherParser.Parsing.csproj --locked-mode
dotnet pack src/WeatherParser.Parsing/WeatherParser.Parsing.csproj --configuration Release --no-restore --output artifacts/local-nuget
dotnet restore WeatherParser.slnx --locked-mode
dotnet build WeatherParser.slnx --configuration Release --no-restore
```

Track lock files. For requested package updates, resolve once without `--locked-mode` and review changes. Build outputs, test reports and local secrets are excluded from Git.

**Current reproducibility limitation (2026-10-19):** the package-consumer lock file records an earlier artifact of `WeatherParser.Parsing` version `0.1.0-initial`. A fresh-cache locked restore against the rebuilt package fails content-hash validation (`NU1403`). The sequence above therefore does not yet establish clean-cache reproducibility. Package version/content and the consumer lock file must be reconciled in a reviewed packaging change; the documentation migration preserves the existing lock files.

The library contains tested endpoint-validation/DI behavior (export entities follow in P2); weather-source parsers remain pending. The package-consumer project exists in the solution and uses a versioned `PackageReference`, without a source reference. See the [package-consumer guide](tests/WeatherParser.Package.Tests/README.md). Full P1 acceptance remains pending. Build the CLI example only after library completion in P3.

Historical initial-scaffolding verification on 2026-10-15: solution listing and restore succeeded; Release build completed with 0 warnings and 0 errors. Both projects present at that time had lock files. That initial empty-scaffolding result was not test/coverage evidence; later evidence is recorded in P1.04–P1.07.


P1.01 entry audit completed on 2026-10-15: [record and starting inventory](docs/p1/P1_01_BASELINE.md). Existing scaffolding is preserved; no new domain code or test/coverage result is claimed. P1.02 review has since completed; current follow-up is recorded below.


**P1.02 review (2026-10-15) complete:** [tooling/license/compatibility report](docs/p1/P1_02_TOOLING_REVIEW.md). All 16 test packages were reviewed; six direct versions were current, with no reported NuGet vulnerability/deprecation findings at that review. SDK 10.0.303 was outside servicing; the review selected 10.0.402 for an update before P1.03 acceptance. No SDK/package changes or new tests were made in P1.02. [P1.03](docs/p1/P1_03_BUILD_SETUP.md) subsequently pinned 10.0.402 and verified CLI/IDE builds; clean-checkout reproducibility remains pending.
