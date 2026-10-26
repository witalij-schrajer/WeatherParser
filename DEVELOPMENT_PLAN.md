# WeatherParser – Development plan

**Phase status (2026-10-25): P0 complete; P1.01-P1.12 complete and P1 accepted by the user on 2026-10-19 (P1.08 accepted with its documented Red-step deviation; Visual Studio build limitation resolved; P1.09 accepted and clean-checkout reproducibility verified on Windows x64); P1.10 Debian 13 ARM64 verification complete and accepted; P1.11 developer instructions complete and accepted; P2 accepted by the user on 2026-10-20 ([P2 acceptance record](docs/p2/P2_12_ACCEPTANCE.md)), complete (pull request #3 merged into `develop` on 2026-10-20, merge commit `74bf625`); P4 started on 2026-10-20 ([P4 handoff](docs/p2/P4_HANDOFF.md)) and was accepted by the user on 2026-10-24 on candidate `b6bc47f` ([P4 acceptance record](docs/p4/P4_12_ACCEPTANCE.md)); it is complete: pull request #5 merged into `develop` on 2026-10-24 (merge commit `95bcb1e`); P5 (DWD observations) started on 2026-10-24 on the user's instruction ([P5 handoff](docs/p4/P5_HANDOFF.md), [P5 plan](PHASE_P5_PLAN.md)); P5 accepted by the user on 2026-10-25 on candidate `fd461c8` ([P5 acceptance record](docs/p5/P5_11_ACCEPTANCE.md), [P6 handoff](docs/p5/P6_HANDOFF.md)); it is complete: pull request #7 merged into `develop` on 2026-10-25 (merge commit `29fd553`); P6 starts only on a separate instruction; P6 (OsthessenWetter observations) started on 2026-10-25 on the user's instruction on branch `features/develop/P6` from `develop` `8d8079a` ([P6 handoff](docs/p5/P6_HANDOFF.md), [P6 plan](PHASE_P6_PLAN.md)); P6.01 (foundation check) complete with outcome READY ([record](docs/p6/P6_01_FOUNDATION_CHECK.md)); P6.02: OsthessenWetter contract OW-01 to OW-16 approved by the user on 2026-10-25 ([contract](docs/p6/P6_OSTHESSEN_CONTRACT.md)) and the P6 plans reconciled with it; P6.03 to P6.09 implemented test-first on 2026-10-25 and 2026-10-26 ([P6 verification record](docs/p6/P6_VERIFICATION.md)): the OsthessenWetter parser is available in the library; P6.10 (integrated verification) READY on candidate `4373399` on Windows, hosted CI and the NanoPi R76S, with one authorized live check, and after the PR #9 review fixes again on candidate `12e7205` ([P6.10 record](docs/p6/P6_10_INTEGRATED_VERIFICATION.md)); P6.11: evidence audit ACCEPTED ([P6.11 acceptance record](docs/p6/P6_11_ACCEPTANCE.md), [P7 handoff](docs/p6/P7_HANDOFF.md)), P6 accepted by the user on 2026-10-26 on candidate `12e7205` (the library; production use and redistribution of OsthessenWetter values stay open until the source conditions are clarified, OW-14); it is complete: pull request #9 merged into `develop` on 2026-10-26 (merge commit `de9d4ba`); P7 starts only on a separate instruction ([P7 handoff](docs/p6/P7_HANDOFF.md)).**

As of 2026-10-14. Based on [REQUIREMENTS.md](REQUIREMENTS.md) and [AGENTS.md](AGENTS.md). P0 is complete; P1 has started with project scaffolding only. The repository contains documentation, a solution and library/test project scaffolding; no domain implementation yet. This plan does not authorize application-code generation, GitHub publication or installation on R76S.

## Objective and deliverables

WeatherParser will primarily be an independently usable C# NuGet library for Debian/ARM64. The example CLI consumes this package. Each invocation executes one of five commands and exits after successful output or a meaningful error. n8n, cron or other callers control scheduling and workflow chaining.

Deliverables:

- DWD observation-station selection and current observations.
- OsthessenWetter pattern selection and observations with marked date inference.
- MOSMIX location selection and separate weather forecasts.
- MQTT JSON or standalone JSON/XML export.
- Dedicated `[Serializable]` export entities, interfaces for every doing class, and constructor DI.
- TDD with MSTest, Moq and FluentAssertions, English code comments and complete English documentation.

## NuGet primary product and example CLI

The intended published product is a reusable NuGet library. It contains DWD/OsthessenWetter/MOSMIX parsers with required retrieval/normalization, public output entities, a generic MQTT serializer and dependency-injection integration. The CLI is only a usage example and consumes a versioned package reference; other applications can use the package without the CLI.

Each parser receives its required source URLs explicitly as constructor parameters, preferably typed as `Uri`. Name multiple endpoints separately. DI integration forwards consumer-supplied endpoints to the corresponding constructors, without invented built-in sources or global URI singletons. HTTP, time and other services remain constructor-injected. The library does not independently read CLI arguments or application-specific configuration files/environment variables.

The MQTT serializer is source-independent: it serializes suitable output entities and creates a message containing topic, JSON payload, QoS and retain setting. Topics/templates and their mapping to message types come from consumer configuration, not hardcoded weather paths. Validate resolved publication topics; missing mappings and unresolved placeholders produce defined failures. Existing `haus/weather/...` topics are configurable examples only. Serialization and broker transport are separate; serialization needs neither a broker connection nor credentials.

Provide documented DI registration for library parsers, serializer and required services. Consumers supply endpoints, topic configuration and external dependencies. Every doing class has an interface; output entities remain dedicated `[Serializable]` types. Preserve testability, UTC and JSON/XML compatibility.

The example CLI owns arguments, application configuration, single-command process execution, exit codes, file/stdout export and optional MQTT transmission. It accesses parsing only through the package, never copied implementations. Acceptance prioritizes the public API, package versioning and independent consumption. Decide package ID, feed and publication procedure before publishing; no built/published package exists yet.

## Approach and acceptance rules

Develop each functional change as a small, reviewable Red → Green → Refactor step: write a test, demonstrate its expected failure, implement minimally, then refactor with green tests. Bug fixes start with regression tests. Set up tests in `[TestInitialize]` and call `VerifyAll()` in `[TestCleanup]`.

Update the canonical English documentation and requirement traceability with every step. Keep observations, forecasts and ID systems separate. Mocks verify service interactions; real serializer contract tests verify actual JSON/XML. Distinguish offline tests from live/ARM acceptance.

Code-block coverage (as defined above) must reach at least 80%. During initial bootstrap without a measurable target-branch baseline, only this minimum applies; target-branch comparison is not applicable. An unexpectedly missing, stale or incomparable baseline in subsequent operation remains an error. Documentation-only PRs may retain the same coverage for the block metric as the target branch; decreases are forbidden. Other PRs must strictly improve the block metric below a 100% baseline; an exact 100% baseline accepts an exact 100% result. Use current commit IDs, comparable collection conditions and unrounded counter ratios. Missing PR measurements, invalid counts and zero denominators cannot pass. Microsoft.CodeCoverage reports, including the code-block breakdown, are the basis for verification; Cobertura line-rate, branch-rate or any Cobertura-derived value must never be used as or substituted for the block metric; missing or unreadable native block counters fail. Verify metric mapping and report format against real reports in P1.

**Coverage boundary decisions are complete:** Bootstrap requires at least 80% block coverage without a target-branch comparison. Documentation-only PRs may retain equal coverage. The exact-100% exception remains valid. Never fabricate a baseline or treat a missing PR report as success.

## Platform policy

Binding platform policy (2026-10-14): .NET version 10 or newer and Debian version 13 Trixie or newer. The initial library/example target framework is `net10.0`; the minimum architecture is ARM64 (`linux-arm64`), particularly NanoPi R76S. Add newer stable .NET/Debian releases to the verified compatibility matrix; previews are not automatically guaranteed. Visual Studio 2026 and SDK 10.0.303 are the verified local starting point; packages, extra ARM variants and deployment model remain undecided. Verify the library, actual package consumer and example on Debian 13/ARM64 with .NET 10. Distinguish platform requirements from verified compatibility; never claim runtime success without documented tests.

Sources (checked 2026-10-14): [.NET support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [.NET 10 OS matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md), [Debian releases](https://www.debian.org/releases/).

## Implementation phases

| Phase | Work and deliverable | Dependencies | Requirements |
|---|---|---|---|
| P0 – complete | Foundational decisions documented; details assigned to implementation phases | None | REQ-001–009, 054–056, 064, 086, 090–093, 102–106 |
| P1 | .NET structure, test foundation and CI design/pipeline | P0; implementation authorization, separate GitHub authorization | REQ-004–007, 009–011, 070–073, 078, 080–086, 090, 093 |
| P2 | Entities, unit/time normalization, real JSON/XML serializers and output XSDs | P1 | REQ-023 and 101–102 (export entity contracts only), 042–043 (shared time normalization), 050–057, 067–069, 074–079, 082, 105–108 |
| P4 | Shared geographic selection and both catalogs | P2 | REQ-020–024, 065–068, 100–102 |
| P5 | DWD observations as the first complete provider flow | P4 | REQ-030–032, 050–056, 082 |
| P6 | OsthessenWetter observations with timestamp-quality markers | P5; verified source access | REQ-040–046, 050–056, 082, 092 |
| P7 | MOSMIX forecasts with issue/validity times | P5; P2 forecast contract | REQ-051–055, 065–069, 100–108 |
| P8 | MQTT serializer and complete library integration | P4–P7 | REQ-008, 050, 060–069, 072–073, 082, 105 |
| P3 | CLI usage example, configuration, output and broker transport | P8; complete library acceptance | REQ-006–011, 030–031, 040, 064–069, 071–073, 103 |
| P9 | Debian/ARM64 acceptance, external workflow and release preparation | P3 | REQ-004–009, 080–086, 090–093 |

These are dependency phases, not calendar commitments. P6/P7 are functionally independent once prerequisites are met. First demonstrate a complete DWD library run with serializer contract tests; later MQTT integration uses the same result contract. P3 follows only after the library is complete.

### P0 – completed on 2026-10-14

P0 is complete by explicit project instruction. Architecture, platforms, NuGet primary product, constructor endpoints, time/rounding contract, output language, test tools and coverage boundaries are decided. Completion applies to preliminary planning, not implemented functionality.

| Remaining detailed work | Owning phase |
|---|---|
| Resolve latest stable dependencies, check licenses, pin SDK and prove coverage reports | P1 |
| Entities, XML namespaces, schema versioning and XSD generation | P2 |
| Verify source formats, element mappings, attribution and access conditions | corresponding source-adapter phase |
| CLI configuration, exit codes, deployment and example | P3, after library completion |
| Public NuGet identity, release versioning and publication procedure | P9, before publication |

ARM64 remains the minimum architecture; additional ARM32 support is a possible later extension and does not gate P1. These assigned details do not reopen P0. Implementation/runtime evidence remains pending until corresponding acceptance.

### P1 – Structure, tests and CI

The [P1 detailed plan](PHASE_P1_PLAN.md) specifies prerequisites, project boundaries, twelve work packages, twenty-four tests, coverage checks and acceptance. It is planning only and does not start implementation.

P1 establishes library/package/test/CI foundations with a technical package integration test. Domain library components follow in P2/P4–P8; implement the CLI example afterwards in P3. The CLI consumes a versioned package reference, verified using a local `.nupkg`. Keep the actual number of .NET projects small and justify it in P0/P1. Each executable class has a suitable interface; pure DTOs do not need one.

Plan package/API/consumer tests, SDK/package pinning, secret/build exclusions, reproducible restore, test projects, an initial meaningful Red/Green behavior test, and CI collection. No production retrieval or device changes yet.

CI runs builds, offline tests, machine-readable test/coverage results and readable reports. Measure PR and current target-branch commits comparably, record both IDs and compare unrounded block-coverage counter ratios. Apply REQ-086 including its exact-100% exception; missing reports or less than 80% fail. ARM64 cross-publish supplements CI but does not replace runtime tests. Public PRs receive no production secrets; executing untrusted PR code must not receive privileged publication rights.

**Acceptance:** Reproducible local build/tests, documented TDD cycle, resolved coverage bootstrap and accurate CI status. An unexecuted GitHub pipeline must not be described as passing on GitHub.

### P2 – Package entities and serialization

Detailed realization plan: [P2 implementation plan](PHASE_P2_PLAN.md). Its first step checks P1 completion, extracts the accepted foundation evidence and adjusts the plan before implementation.

Planned entity families: CDC/MOSMIX lists with their entries/search parameters, observations with typed values, forecast runs with steps/intervals/values, and quality/export-error metadata. Decide names/contracts before implementation. Mark all custom export types `[Serializable]`.

Write tests first for present/missing values, real zeros, pressure conversions/references, UTC without suffix, inferred dates, leading zeros and special characters. Use `System.Text.Json.JsonSerializer` and `System.Xml.Serialization.XmlSerializer` with the same entities. Verify nested graphs and full documents using real serializers, not only mocks.

**Acceptance:** Semantically equivalent valid documents, correctly omitted values, preserved timestamps/IDs and testable serializer services with interfaces/DI. No network needed yet.

### P4 – Coordinates and station selection

Shared coordinate/geographic logic supports decimal degrees/DMS, S/W, bounds and culture independence. Apply inclusive distance/absolute-altitude limits.

Keep CDC and MOSMIX adapters separate. Preserve IDs/metadata and determine product-specific availability without assuming complete instrumentation. Support alphanumeric MOSMIX IDs; missing altitudes must not bypass filters. Both lists have stable ordering and defined empty results.

**Acceptance:** Offline catalog fixtures, boundary/filter tests and full JSON/XML results for `--SelectDWD` and `--SelectMOSMIX`. Document explicitly enabled live verification separately.

### P5 – DWD observations

The first vertical slice combines station resolution, HTTP, CSV/ZIP parsing, missing/quality values, periods, normalization and export. Inject HTTP with bounded timeouts/downloads and controlled missing-product behavior. Add quantities only with verified availability.

**Acceptance:** `--ParseDWD` produces valid JSON/XML for an explicitly configured/supplied station. Missing station triggers neither HTTP nor MQTT; never substitute other stations. Offline tests cover headers/formats, wrong IDs, units, `-999` and different times. Record live-data quality separately.

### P6 – OsthessenWetter

Use the OsthessenWetter page structure as is, as encountered at implementation time. During implementation, document the implementation date, source URL and structural assumptions relevant to parsing. Support applies to this documented revision; later structural changes may require parser updates. Log detectable format deviations according to REQ-044; timestamp-quality and source-condition requirements remain applicable.

Test glob filters, multiple results, umlauts and collision-free sources using synthetic HTML fixtures. Date inference from retrieval and `Europe/Berlin` carries `estimated_date`. Handle midnight, DST gaps/ambiguity, stale and offline displays. Never relabel gusts/rain totals.

**Acceptance:** Export all matching stations traceably. No matches, format changes and unresolved times yield defined errors. Estimated times remain visible. Source access without clarified conditions is not production acceptance.

### P7 – MOSMIX forecasts

### Separate MOSMIX parsers and update frequency

Provide two distinct parsers with separate interfaces: `MosmixSParser` / `IMosmixSParser` for `MOSMIX_S`, and `MosmixLParser` / `IMosmixLParser` for `MOSMIX_L`. Both receive their own source URLs as constructor parameters and have independent DI configuration. Shared KML/KMZ helpers may be reused, but one switchable parser cannot replace the two public contracts. Missing data never triggers silent fallback to the other product.

| Product | DWD output frequency | Run times |
|---|---|---|
| `MOSMIX_S` | Hourly | A new product run each hour |
| `MOSMIX_L` | Four times daily | 03, 09, 15 and 21 UTC |

These describe provider cycles, not an internal scheduler or guaranteed file availability exactly at run time. Keep issue, retrieval and validity times separate. Read the actual forecast horizon from the file timeline; a new file does not prove all contained steps were recalculated. Define freshness limits and external retrieval plans per product.

The example keeps `--ParseMOSMIX` and additionally requires `--Product MOSMIX_S|MOSMIX_L` to select the library parser. Reject missing/unknown products before retrieval. `--SelectMOSMIX` keeps the existing geographic/altitude options and distinguishes known availability/elements per product; explicitly mark unknown availability. Output metadata, source identifiers and configured MQTT topics must distinguish S/L to prevent accidental retained-state replacement.

Source, checked 2026-10-14: [DWD procedure description, section 2.1.3/table 2.1](https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_verfahrenbeschreibung_gesamt.pdf?__blob=publicationFile&v=6). Recheck cycles before implementation/release; no parser is implemented yet.


The two separate product adapters handle KML/KMZ, namespaces, location identity, issue metadata, parameter series and forecast timeline. Missing-value/length tests prevent shifts. Normalize energy/rain periods only with justified semantics. Disable external entities/DTDs and test archive bounds.

**Acceptance:** `--ParseMOSMIX --Product MOSMIX_S|MOSMIX_L --MOSMIXID` returns a complete run with separate `issued_at`, `retrieved_at`, `valid_at`, never `measured_at`. Both exports are equivalent; test wrong IDs, stale runs, corrupt structure and missing parameters.

### P8 – MQTT serializer and complete library integration

Verify the generic, configurable-topic MQTT serializer without broker connections. Integrate all library parsers, output entities, JSON/XML/XSD contracts and DI through actual package consumption. Observe agreed topic, QoS and retain semantics.

**Acceptance:** Complete library functions for DWD observations, OsthessenWetter and both MOSMIX products verified through the package API. Independently consume the library without a CLI or broker. Begin P3 only after this library acceptance; example transport, broker acknowledgment tests and five-command integration belong to P3.

### P3 – CLI and export pipeline

Prerequisite: P8 and complete library acceptance. Implement the CLI solely as a consumer of the completed package, including example MQTT transport, QoS-1 acknowledgment handling and all five commands. Library-phase acceptance uses direct API tests; command spellings in those phases identify the corresponding library behavior and do not require an earlier CLI.

Accept exactly one command and validate options before retrieval. Resolve configuration/CLI deterministically. Missing DWD station/MOSMIX ID and invalid patterns/formats produce clear failures.

Prepare `--Output mqtt|json|xml` and `--OutputFile` under the contract. JSON/XML need no broker, emit one document and respect existing files. Atomic completion or documented failure handling prevents misleading partial files. Separate logs/output/status, and control cancellation/resources. Test unfinished providers only with test doubles; do not claim working live commands.

**Acceptance:** Process/command tests demonstrate single execution and termination, with no external side effects from invalid input. Test files, stdout, write errors, existing destinations and partial failure.

### P9 – ARM64 acceptance and operation

Run release artifacts on the documented Debian/ARM64 system: noninteractive execution, real serializers, UTF-8, UTC/source timezones, file permissions, exit codes, signals and cleanup. Report offline fixtures, live sources and test-broker results separately.

English guides cover configuration and cron/n8n invocation without installing internal scheduling. Explain topic permissions, estimated dates, source conditions, limitations, freshness and failures. Release artifacts include versions/checksums and traceable build/test evidence. The versioned NuGet package is the primary release artifact; CLI artifacts are examples only. Verify API compatibility, package contents and consumption without source checkout. Feed/GitHub publication or deployment requires explicit authorization.

**Acceptance:** Independent NuGet use, public library API, configurable-topic MQTT serialization and DI integration are primary product evidence; the CLI remains an example. ARM64 runtime evidence, successful functional acceptance, complete documentation and explicit limitations. Mocks or cross-publish do not replace target-system acceptance.

## Definition of done per change

- Identify requirement and interface/entity contract; no unrequested features.
- Demonstrate Red/Green/Refactor through meaningful behavior/regression tests and required MSTest/Moq/FluentAssertions setup/cleanup.
- Pass relevant builds/tests. Native block coverage meets 80% and PR comparison including the exact-100% exception where a PR exists under the approved bootstrap/documentation rules. Unresolved gates remain unresolved.
- All comments and canonical project documents are English and consistent. Preserve IDs, units and UTC contract.
- No secrets, production side effects or false acceptance claims. Identify target checks and omitted verification clearly.

## Next concrete step

P1.02 review is complete. P1.03 closed the SDK finding: global.json now pins SDK 10.0.402, with locked restore and a CLI Release build proven successful ([record](docs/p1/P1_03_BUILD_SETUP.md)). The Visual Studio/MSBuild NETSDK1141 build failure has been resolved (via `dotnet build-server shutdown` plus a full Visual Studio restart) and a subsequent IDE solution build succeeded. [P1.09](docs/p1/P1_09_LOCAL_CI.md#p109-acceptance-2026-10-19) subsequently verified clean-checkout reproducibility on Windows x64. Package versions remain unchanged; overall P1 acceptance is pending.

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


Historical initial-scaffolding verification on 2026-10-15: solution listing and restore succeeded; Release build completed with 0 warnings and 0 errors. Both projects present at that time had lock files. That initial empty-scaffolding result was not test/coverage evidence; later evidence is recorded in P1.04–P1.07.

P1.01 entry audit completed on 2026-10-15: [record and starting inventory](docs/p1/P1_01_BASELINE.md). Existing scaffolding is preserved; no new domain code or test/coverage result is claimed. P1.02 review has since completed; current follow-up is recorded below.


**P1.02 review (2026-10-15) complete:** [tooling/license/compatibility report](docs/p1/P1_02_TOOLING_REVIEW.md). All 16 test packages reviewed; six direct versions current, no reported NuGet vulnerability/deprecation findings.

**P1.03 (2026-10-15) complete; Visual Studio build limitation resolved:** [build setup record](docs/p1/P1_03_BUILD_SETUP.md). global.json pinned to SDK 10.0.402, locked restore and CLI Release build succeeded; build/secret exclusions in .gitignore are sufficient. The Visual Studio/MSBuild NETSDK1141 failure was resolved via `dotnet build-server shutdown` and a full Visual Studio restart; a subsequent IDE solution build succeeded. Clean-checkout reproducibility (no Git commit exists yet) remains outstanding.

**P1.04 (2026-10-16) complete:** [package consumer record](docs/p1/P1_04_PACKAGE_CONSUMER.md). Local package `WeatherParser.Parsing.0.1.0-initial.nupkg` built, project-local package source configured, isolated consumer `tests/WeatherParser.Package.Tests` with a pure PackageReference created and tested (2/2 tests passed).

**P1.05 complete:** [TDD baseline record](docs/p1/P1_05_TDD_BASELINE.md). Parser-endpoint URI validation, duplicate-name rejection and endpoint mapping were established through Red→Green→Refactor, with constructor injection, MSTest/Moq/FluentAssertions setup/cleanup and offline execution. The record reports 6 passing tests; its narrow real-DI checks were subsequently consolidated in P1.06.

**P1.06 complete:** [package/DI contract record](docs/p1/P1_06_DI_CONTRACTS.md). The public `AddWeatherParsing` entry point registers endpoint validators using DI abstractions in the library. Real `ServiceProvider` tests demonstrate successful resolution and validation, and detect absent or incomplete registrations. The record reports successful library/source-test/package-test builds and 8 passing unit tests after replacing the earlier duplicate DI tests. These are recorded completion results, not new test executions.

**P1.07 (2026-10-16) complete:** [coverage report contract record](docs/p1/P1_07_COVERAGE_CONTRACT.md). Microsoft.CodeCoverage raw report (`.coverage`) produced and converted with `dotnet-coverage` into XML/Cobertura format; code block breakdown per function and separate, traceable counter mapping per assembly (production code 63.33% block/50.00% line coverage; test code 97.93%/94.67%) proven. Coverage evaluation including the 80% threshold and exceptions remains assigned to P1.08.

**P1.08 (2026-10-19) complete and accepted by the user:** [acceptance record](docs/p1/P1_08_COVERAGE_EVALUATION.md#p108-acceptance-2026-10-19). The evaluator, executable runner, fixture tests and pinned conversion tool are accepted on the recorded Windows evidence. The user explicitly accepted the missing original Red-step evidence for P1.08 only; subsequent work retains the TDD requirement. Local CI and integrated baseline evidence remain P1.09; overall P1 acceptance is pending.

**P1.09 (2026-10-19) complete and accepted by the user:** [acceptance record](docs/p1/P1_09_LOCAL_CI.md#p109-acceptance-2026-10-19). `scripts/ci-local.ps1` runs the P1 pipeline from a fresh clone (locked restore, Release build, four test projects, pack, same-run package consumer, coverage conversion, two-pass baseline, gate) and exits with stage-specific codes; all seven fault injections exit as documented. The clean-clone runs found two defects (missing local package source, a line-ending-dependent test), both fixed. A two-pass run of a code change with unchanged coverage correctly ends with exit 90; the documentation-only candidate `ef9b9ac` passes the two-pass comparison against `0a2dabe` (exit 0). After review, `-BaselinePath` requires the baseline's sibling `collection-settings.json` and rejects incomparable settings (exit 85). The genuine initial bootstrap seed was established on `0a2dabe` (52/56 blocks, Release); later candidates use normal two-pass comparison. Windows x64 only.

**P1.10 (2026-10-19) complete and accepted by the user:** [acceptance record](docs/p1/P1_10_DEBIAN_ARM64.md#p110-acceptance-2026-10-19). Full two-pass CI run on the NanoPi R76S, Debian 13 ARM64: 162 tests passed, package consumer against the package built on the target, coverage conversion and gate ran on ARM64, gate PASSED with 54/56 (96.43%) against the target's 52/56. New script options `-RunSettings`, `-CollectOnly`, `-ResumeFrom` added to `scripts/ci-local.ps1`. `scripts/prepare-debian-arm64.sh` installs .NET SDK and PowerShell 7 into user directories. `docs/COMPATIBILITY.md` added with matrix for Debian 13 ARM64 support. Tool-resolver-cache isolation fix applied. RID-specific locked restore fails with NU1004; projects stay RID-neutral as decided by the user.

**P1.11 (2026-10-19) complete and accepted by the user:** [acceptance record](docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md#p111-acceptance-2026-10-19). Developer instructions in `CONTRIBUTING.md` cover all procedures with status; README reduced to mapping; consistency table added to AGENTS.md; Windows walkthrough passed; documentation checks completed; coverage gate classified as documentation-only (54/56 equal).

**P1.12 (2026-10-19) complete; P1 accepted by the user:** [acceptance record](docs/p1/P1_12_ACCEPTANCE.md#p1-acceptance-2026-10-19), [P2 handoff](docs/p1/P2_HANDOFF.md). The audit decision is ACCEPTED for the P1 foundation: candidate `6ac88e7`, two-pass run against the bootstrap seed `0a2dabe`, gate passed with 54/56 against 52/56, all 24 test IDs passed (T-P1-06, T-P1-13, T-P1-17 and T-P1-24 after review corrections) and the ARM64 runtime is proven at P1 scope. The P2 handoff is prepared; P2 has not started.
