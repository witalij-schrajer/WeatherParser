# WeatherParser rules

[Deutsch](AGENTS.md) | [English](AGENTS.en.md)

**Phase status (2026-10-15): P0 complete; P1 started with solution/project scaffolding. Domain implementation, tests and coverage acceptance remain pending.**

These rules apply throughout the repository. Current explicit user instructions take precedence. Binding requirements are in [REQUIREMENTS.en.md](REQUIREMENTS.en.md); project status and usage are in [README.en.md](README.en.md).

## Language rules

- Write all code comments in English: inline/block comments, XML documentation comments, and comments in scripts and configuration examples. All program-generated runtime messages must be English.
- Maintain complete German and English documentation. The pairs `README.md` / `README.en.md`, `REQUIREMENTS.md` / `REQUIREMENTS.en.md` and `AGENTS.md` / `AGENTS.en.md`, plus `DEVELOPMENT_PLAN.md` / `DEVELOPMENT_PLAN.en.md`, must have equivalent content and reciprocal links.
- Update both languages in the same working revision for every content change. Preserve requirement IDs, commands, fields, units and examples. Provide both languages for new documents as well.
- Preserve the English MIT text in `LICENSE` as the legal license. Include language parity and English comments in change reviews.

## Current scope and authorization

- The project currently contains a local Git repository, documentation and initial library/test project scaffolding. Generate application code, .NET projects and implementation scaffolding only upon explicit request.
- The primary product is a reusable NuGet library; the CLI is only a package usage example with `--SelectDWD`, `--ParseDWD`, `--ParseOstHessen`, `--SelectMOSMIX`, `--ParseMOSMIX`, MQTT output and JSON/XML export.
- Keep observations and forecasts separate. Current requirements cover observations, MOSMIX forecasts and related station selection; do not add door control, n8n workflows or Home Assistant installation.
- Do not silently turn open decisions into approved requirements. Document CLI/MQTT contract changes.

## Platform and workflow role

- WeatherParser is an independent workflow component responsible for retrieval, parsing, station selection, normalization and output. Storage, display, device control and subsequent workflow actions belong elsewhere.
- Binding platform policy (2026-10-14): .NET version 10 or newer and Debian version 13 Trixie or newer. The initial library/example target framework is `net10.0`; the minimum architecture is ARM64 (`linux-arm64`), particularly NanoPi R76S. Add newer stable .NET/Debian releases to the verified compatibility matrix; previews are not automatically guaranteed. Visual Studio 2026 and SDK 10.0.303 are the verified local starting point; packages, extra ARM variants and deployment model remain undecided. Verify the library, actual package consumer and example on Debian 13/ARM64 with .NET 10. Distinguish platform requirements from verified compatibility; never claim runtime success without documented tests.
- Execute exactly one CLI command per invocation, then exit with an actionable status. No internal scheduler, continuous polling or periodic background timers. Multiple results within one command are allowed; technical retries remain bounded.
- Keep scheduling, retries across invocations and orchestration external, such as n8n or cron. Support noninteractive execution, clean output channels and controlled cancellation. Do not impose n8n as a dependency or install an orchestrator.
- Distinguish successful build/publish from actual Debian/ARM execution. Document target-platform verification, including serializers and single-command behavior, before release. Explicitly identify missing hardware tests.

## Open source and Git

- Code is published under MIT. Preserve `LICENSE`; add third-party code and dependencies only with clarified, suitable licenses.
- Never commit passwords, tokens, private keys, broker credentials, private operational logs/configuration or personal test data to the public repository.
- Use placeholders in example configuration. Add secret files to `.gitignore` before creating them. Never log secrets or store them in test snapshots.
- Document source conditions and attribution separately. MIT for code does not automatically license weather data, websites or images.
- Set up GitHub repositories, remotes, pushes, releases and publication only within explicit authorization. The general open-source objective does not authorize automatic publication.
- Check the working tree before editing; preserve others' changes and avoid destructive resets/cleanup. Use meaningful small commits when requested, and do not claim a commit when only files were created.

## NuGet primary product and example CLI

The intended published product is a reusable NuGet library. It contains DWD/OsthessenWetter/MOSMIX parsers with required retrieval/normalization, public output entities, a generic MQTT serializer and dependency-injection integration. The CLI is only a usage example and consumes a versioned package reference; other applications can use the package without the CLI.

Each parser receives its required source URLs explicitly as constructor parameters, preferably typed as `Uri`. Name multiple endpoints separately. DI integration forwards consumer-supplied endpoints to the corresponding constructors, without invented built-in sources or global URI singletons. HTTP, time and other services remain constructor-injected. The library does not independently read CLI arguments or application-specific configuration files/environment variables.

The MQTT serializer is source-independent: it serializes suitable output entities and creates a message containing topic, JSON payload, QoS and retain setting. Topics/templates and their mapping to message types come from consumer configuration, not hardcoded weather paths. Validate resolved publication topics; missing mappings and unresolved placeholders produce defined failures. Existing `haus/weather/...` topics are configurable examples only. Serialization and broker transport are separate; serialization needs neither a broker connection nor credentials.

Provide documented DI registration for library parsers, serializer and required services. Consumers supply endpoints, topic configuration and external dependencies. Every doing class has an interface; output entities remain dedicated `[Serializable]` types. Preserve testability, UTC and JSON/XML compatibility.

The example CLI owns arguments, application configuration, single-command process execution, exit codes, file/stdout export and optional MQTT transmission. It accesses parsing only through the package, never copied implementations. Acceptance prioritizes the public API, package versioning and independent consumption. Decide package ID, feed and publication procedure before publishing; no built/published package exists yet.

## Architecture

- Each class with executable logic (a “doing class”) must implement a suitable interface. Inject dependencies on these classes through their interfaces. Pure data/export entities without executable logic need no artificial interface.
- Provide all service dependencies through constructor injection. Domain classes must neither construct concrete services nor retrieve them from an `IServiceProvider`.
- Centralize registration/wiring in the composition root. Framework builders, immutable data objects and values are not hidden service dependencies.
- Keep HTTP, MQTT, filesystem and time behind injectable boundaries. Parsers and normalization must be testable with local inputs without network access.
- Separate CLI, configuration, adapters, station selection, data contracts, JSON/XML serialization, export destinations and MQTT. No global service singletons or hidden global state.
- Use asynchronous I/O, CancellationToken and bounded timeouts. Do not hide failures with fabricated values or silent suppression.
- Check .NET/package compatibility, maintenance and licensing against official sources before implementation. Pin reproducible versions and avoid unnecessary frameworks.

## Data contract

- Output every timestamp as UTC in `YYYY-MM-DDTHH:mm:ss` format without `Z` or an offset. Retain UTC internally; never serialize unchecked local system time.
- Resolve DWD station from CLI or configuration. If both are missing: log an error, fail, and perform no HTTP or MQTT. Never substitute the nearest station automatically.
- Support decimal degrees and DMS with hemisphere; validate ranges independently of culture.
- Select MOSMIX locations with the same geographic/altitude filters but its own catalog. Preserve IDs as strings, never equate them to CDC IDs. `ParseMOSMIX` requires `MOSMIXID`.
- Normalize forecasts separately, preserving issue, retrieval, validity times and intervals. Never emit forecasts as observations or with `measured_at`. Use separate topics and a complete retained forecast run.
- Provide separate MOSMIX_S/MOSMIX_L parsers/interfaces with independent constructor URLs and DI registrations; document elements/freshness per product. Read KML/KMZ namespaces and align parameter/time series correctly. Missing values must not shift timestamps. Disable external XML entities/DTDs and bound archive sizes.
- Omit missing/invalid readings; `0` is real. Do not publish DWD missing-value markers as observations.
- Convert pressure units from the same original value and preserve pressure reference. No general conversion between lux and W/m².
- Distinguish rain amount/intensity, gust/mean wind and radiation energy/power. Convert only with a verified reference period.
- Never merge different timestamps misleadingly. Preserve periods and quality metadata according to REQUIREMENTS.
- OsthessenWetter date inference is permitted only with `estimated_date`. Apply `Europe/Berlin` and convert to UTC; test midnight and DST. Do not claim certainty for ambiguous times.
- Define a strongly typed entity for each export object type, explicitly marking all custom export entities, including nested types, `[Serializable]`. Do not replace them with anonymous objects, `dynamic` or untyped export dictionaries.
- Generate JSON with `System.Text.Json.JsonSerializer` and XML with `System.Xml.Serialization.XmlSerializer`, using the same entities for both and MQTT JSON. No manually concatenated documents. Entities must support both serializers and contain no services or secrets.
- Put serializers behind injectable services or centrally wired factories. `[Serializable]` does not replace JSON/XML field, null or timestamp rules.
- Derive JSON and XML from the same normalized model. Preserve UTC, missing values, actual zeros, source and quality metadata; escape XML correctly.
- Do not require or contact MQTT in JSON/XML mode. Produce one complete document per invocation, log to stderr, and avoid silent overwrites or misleading partial files.

### Separate MOSMIX parsers and update frequency

Provide two distinct parsers with separate interfaces: `MosmixSParser` / `IMosmixSParser` for `MOSMIX_S`, and `MosmixLParser` / `IMosmixLParser` for `MOSMIX_L`. Both receive their own source URLs as constructor parameters and have independent DI configuration. Shared KML/KMZ helpers may be reused, but one switchable parser cannot replace the two public contracts. Missing data never triggers silent fallback to the other product.

| Product | DWD output frequency | Run times |
|---|---|---|
| `MOSMIX_S` | Hourly | A new product run each hour |
| `MOSMIX_L` | Four times daily | 03, 09, 15 and 21 UTC |

These describe provider cycles, not an internal scheduler or guaranteed file availability exactly at run time. Keep issue, retrieval and validity times separate. Read the actual forecast horizon from the file timeline; a new file does not prove all contained steps were recalculated. Define freshness limits and external retrieval plans per product.

The example keeps `--ParseMOSMIX` and additionally requires `--Product MOSMIX_S|MOSMIX_L` to select the library parser. Reject missing/unknown products before retrieval. `--SelectMOSMIX` keeps the existing geographic/altitude options and distinguishes known availability/elements per product; explicitly mark unknown availability. Output metadata, source identifiers and configured MQTT topics must distinguish S/L to prevent accidental retained-state replacement.

Source, checked 2026-10-14: [DWD procedure description, section 2.1.3/table 2.1](https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_verfahrenbeschreibung_gesamt.pdf?__blob=publicationFile&v=6). Recheck cycles before implementation/release; no parser is implemented yet.

## Tests

- Follow TDD: Red → Green → Refactor. Demonstrate the expected failure first, implement minimally, then refactor with green tests. Bug fixes begin with a reproducing regression test.
- Use MSTest, Moq and FluentAssertions.
- Build the test subject and shared dependencies in `[TestInitialize]`. Prefer a `MockRepository` and explicitly configure expected interactions.
- Call `VerifyAll()` for all mocks in `[TestCleanup]`. Individual tests may add their own inputs and expectations.
- Maintain meaningful behavior, failure and boundary tests from REQUIREMENTS. Avoid implementation-mirroring tests and trivial getter-only tests.
- Explicitly test MOSMIX catalog filters, IDs, KML/KMZ, units, missing values, forecast times, topics and export equivalence.
- Check `[Serializable]` on all export entities and verify complete graphs with the real JSON/XML serializers, including equivalence, UTC, omitted values and special characters. Do not completely mock the serializer in contract tests.
- Line and branch coverage must each reach at least 80%. During initial bootstrap without a measurable target-branch baseline, only this minimum applies; target-branch comparison is not applicable. An unexpectedly missing, stale or incomparable baseline in subsequent operation remains an error. Documentation-only PRs may retain the same coverage for each metric as the target branch; decreases are forbidden. Other PRs must strictly improve every metric below a 100% baseline; an exact 100% baseline accepts an exact 100% result. Use current commit IDs, comparable collection conditions and unrounded counter ratios. Missing PR measurements, invalid counts and zero denominators cannot pass. Microsoft.CodeCoverage reports, including the code-block breakdown, are the basis for verification; block coverage is not interchangeable with branch coverage. Verify metric mapping and report format against real reports in P1.
- Run standard tests without internet or production brokers. Mock HTTP, time and MQTT; prefer synthetic fixtures.
- Keep integration tests separate and explicitly enabled. Normal tests must not publish production messages or alter target systems.
- Run relevant builds/tests after implementation changes. Report omitted or failed checks honestly; mocks are not evidence of hardware/live success.

## Source retrieval and logging

- Verify DWD formats/units against official documentation and OsthessenWetter against its actual published structure. Do not guess unknown fields or time references.
- Do not bypass access protection or make unbounded requests. Document frequency, retries and errors.
- Log stable error IDs, UTC, source/station and command. Never expose secrets or unchecked configuration objects.
- Write logs to stderr and reserve JSON/XML stdout for the document. Report MQTT success only after acknowledgment and export success only after complete output.
- MQTT error collection is a future feature. Broker failures must remain observable locally without it.

## Documentation

- Keep `DEVELOPMENT_PLAN.md` / `DEVELOPMENT_PLAN.en.md` aligned with requirements and actual progress. Phases remain planned until acceptance is evidenced; never bypass unresolved coverage bootstrap/boundary decisions unilaterally.

- Keep README, REQUIREMENTS and actual status consistent. Mark commands as planned until implemented and verified.
- Keep public examples portable and free of private paths, addresses and secrets.
- Document changed assumptions, configuration precedence, fields, topics, units and source limitations.
- Do not implement extra features merely because they are mentioned as future extensions.

## Binding MOSMIX schema source

[DWD pointforecast_dwd_extension_V1_0.xsd](https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd) is the binding basis for both parsers' MOSMIX source/XML data structures. Derive types, namespaces, elements and cardinalities from this schema; verify additional KML structures before implementation. Record schema revision and checksum for reproducible type generation/derivation. Map source types to separate public output entities; they do not replace the normalized forecast contract. Follow TDD for schema/mapping tests. No C# data structures have been generated yet.


## Additional decisions dated 2026-10-14

- Development environment: Visual Studio 2026. Locally verified: Community 18.8.3 and .NET SDK 10.0.303; use this SDK revision as the reproducible starting point for the planned `global.json` and CI, retaining target framework `net10.0`. Check servicing status before implementation and explicitly document any required SDK update. Visual Studio is not a Debian runtime dependency. [Microsoft SDK/Visual Studio mapping](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs).
- All program-generated messages are English: help, validation, errors, logs and status. Data field names remain language-independent; preserve original source names. Project documentation remains German/English.
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

P1 contains only a technical package consumer as an integration test, not a CLI/application example. Before P3, accept source functionality directly through the library and its tests; CLI commands do not gate library phases. This sequence supersedes earlier numeric ordering or dependencies on P3. [P1 detailed plan](PHASE_P1_PLAN.en.md). The current task adds project scaffolding; domain implementation remains deferred.
