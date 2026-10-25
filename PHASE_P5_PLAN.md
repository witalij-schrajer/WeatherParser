# Phase P5 – DWD Observations: Implementation Plan

**Status: planning complete (2026-10-24, record in section 10); P5 started on 2026-10-24 on the user's instruction (branch `features/develop/P5` from `develop` `1e604dc`); P5.01 (foundation check) executed with outcome READY ([record](docs/p5/P5_01_FOUNDATION_CHECK.md)), checkpoint `ca76114`; P5.02: observation contract OC-01 to OC-14 approved by the user on 2026-10-24 ([contract](docs/p5/P5_OBSERVATION_CONTRACT.md)), this plan and the step plans P5.03 to P5.11 reconciled with it; P5.03 to P5.09 (station resolution, file reading, quantity mapping, retrieval boundary, parser service, export equivalence, DI, README and package consumption) complete ([record](docs/p5/P5_VERIFICATION.md) sections 3 to 9); P5.10 (integrated verification) complete with outcome READY on candidate `a87c7bb` ([P5.10 record](docs/p5/P5_10_INTEGRATED_VERIFICATION.md)); P5.11 (acceptance) needs the user's decision.** This plan refines the P5 section of [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) on the foundation of the [P5 handoff](docs/p4/P5_HANDOFF.md).

## 1. First step: check P4 completion and adjust this plan

P5.01 is always the first execution step: verify the P4 acceptance and merge from `docs/p4/P4_12_ACCEPTANCE.md` (sections 6 and 8.3) and the P5 handoff; extract the integration target SHA `95bcb1e3287cae275323cc3bf202337de1942d88`, the package identity `WeatherParser.Parsing` `0.1.0-initial`, the SDK and tool versions, the frozen export contract v1 (C-01 to C-119), the selection contract (SC-01 to SC-23) and the inherited limitations with their owners; check the SDK servicing status; remeasure the native block coverage of the integration target (expected 6811/6814) with `ci-local.ps1` on the first committed revision of the P5 branch; record everything in `docs/p5/P5_01_FOUNDATION_CHECK.md`; adjust this plan where the extraction contradicts it. P5.01 adds a table "P5.01 extraction" to this section when it runs.

P5.01 extraction (2026-10-24, [record](docs/p5/P5_01_FOUNDATION_CHECK.md)):

| Field | Value |
| --- | --- |
| Integration target | `1e604dc7b3f17826d19207cbec9a699e40e5a30f` (`develop` head at branch creation; P4 merge `95bcb1e` plus the documentation-only pull request #6; no production change since `95bcb1e`) |
| Package identity | `WeatherParser.Parsing` `0.1.0-initial`, unpublished |
| SDK / runtime | SDK 10.0.402 (latest 10.0 SDK on 2026-10-24), runtime 10.0.13 |
| Coverage tools | Microsoft.CodeCoverage 18.11.0, dotnet-coverage 18.11.0 |
| Contracts | Export contract v1 C-01 to C-119 (frozen; additive optional members only, C-03); selection contract SC-01 to SC-23 |
| Baseline | 6811/6814 native blocks of `WeatherParser.Parsing.dll`, remeasured for target `1e604dc` on the seed commit `483f6d5` (`ci-local.ps1` exit 0); valid while the pull-request base and `coverage.runsettings` are unchanged |
| Branch and pull request | `features/develop/P5`, draft pull request #7 into `develop` |
| Adjustment | The integration target is `1e604dc` instead of `95bcb1e`; no other plan change |

## 2. Goal, binding scope and exclusions

Goal: a library service that parses the DWD 10-minute `now` observations of one explicitly resolved CDC station into the P2 `observation_dataset` root, proven offline with synthetic fixtures and the real serializers, reusing the P4 catalog, retrieval and evidence-reader components (SC-21).

In scope:
- station resolution against the four CDC station lists; never a substitute station
- bounded retrieval of the station's per-product `now` archives with controlled missing-product behavior
- reading the rows of all four products, including `QN`, through the shared `ICdcNowEvidenceReader` component
- mapping of the SC-03 quantities with verified units, pressure reference, averaging periods, missing markers and quality codes
- the latest usable value per quantity with its own measurement time
- dataset assembly through `IObservationDatasetBuilder`
- export equivalence and schema validation
- DI registration, README consumer guidance and package-consumer evidence
- integrated coverage and platform verification
- acceptance and P6 handoff

Out of scope with owner:
- CLI options, configuration precedence (`StationID` versus the configured reference station), output modes, exit codes → P3
- OsthessenWetter → P6
- MOSMIX forecasts → P7
- MQTT → P8
- publication → P9
- `illuminance_lux` (no DWD source) and the DWD columns `TM5_10`, `TD_10`, `RWS_DAU_10`, `RWS_IND_10`, `DS_10`, `SD_10`, `LS_10` (no REQ-052 field), and `SLA_10`, `SLO_10` (documented for wind but absent from the `now` files) → not exported; recorded in the observation contract (OC-05)
- the `historical` and `recent` DWD directories → not used

No change of export contract v1 except additive optional members with a recorded decision (C-03).

## 3. Decisions before the affected implementation

Decisions are recorded in [`docs/p5/P5_OBSERVATION_CONTRACT.md`](docs/p5/P5_OBSERVATION_CONTRACT.md) (decision IDs `OC-01` to `OC-14`) in P5.02, reviewed by Codex read-only (up to 10 rounds) and approved by the user before the affected Red test. Source facts come only from official DWD documentation or the actual published files, with URL and check date recorded. P5.02 also records access conditions, attribution and retrieval-frequency guidance (REQ-092).

**Approved on 2026-10-24 (P5.02 S4):** the user approved OC-01 to OC-14 with OC-02 option A, OC-05 `RWS_10` option A, OC-06 option B and OC-12 option A. The approved contract is binding; the rows below are the planning proposals, now marked with their OC row. Where the contract refines a row, the contract wins: OC-02 (a cross-list conflict raises `STATION-METADATA-CONFLICT`), OC-05 (`RWS_10` keeps the nominal period; the instrument-dependent time stamp is a documented limitation; `SLA_10` and `SLO_10` not exported), OC-06 (`DD_10` omitted when `FF_10` is 0 or `DD_10` is 0), OC-10 (`STATION-METADATA-CONFLICT` added), OC-12 (at most one parse per station every 10 minutes; DWD documents an update frequency of "< 1 h"), and the alternatives and source facts of the contract. Names still marked (Proposed) in this plan and the step plans (for example `IDwdStationResolver`, `IDwdObservationMapper`, `IDwdObservationSource`, `ICdcRequestComposer`, folders and test helpers) are implementation names that no OC row fixes; the orchestrating agent decides them in the step and records them, as P4 did.

| Decision | Options / recommendation | Decided by | Blocks |
| --- | --- | --- | --- |
| Station ID grammar | Exactly five ASCII digits as in the lists, `\A[0-9]{5}\z` (the policy of `CdcStationCatalogReader` and `CdcNowEvidenceReader`; `^\d{5}$` would accept Unicode digits and a trailing newline) / 1 to 5 digits left-padded. Recommended: exactly five digits, no padding (REQ-031); failure `STATION-ID-INVALID` (approved, OC-01) before any request | User (P5.02) | P5.03 |
| Station existence and metadata | The ID is resolved against the union of the four station lists (SC-02), which the observation source (P5.06) reads under the observation deadline; the resolver works on the list records and makes no request itself; a station in no list → `STATION-UNKNOWN` (approved, OC-02), no `now` request; `location` from the list record (name, coordinates, altitude or none, leading zeros kept). Decided (OC-02 option A): a cross-list conflict logs `CDC-STATION-CONFLICT` and fails the parse with `STATION-METADATA-CONFLICT` before any `now` request | User (P5.02) | P5.03 |
| Products to retrieve | For each product whose list contains the station, the station's `now` archive; a product whose list lacks the station is not requested; HTTP 404 → notice `OBSERVATION-PRODUCT-MISSING` (approved, OC-03) and the product contributes nothing; any other retrieval or format failure fails the parse (REQ-031: no data of another station, no fabricated values). Recommended as stated | User (P5.02) | P5.06 |
| Value selection and times | Per quantity the most recent row with a usable value (SC-03 usability); its `MESS_DATUM` becomes `measured_at` through `ISourceTimeNormalizer.NormalizeComplete` (UTC, quality `source`); no freshness filter and no retrieval-time substitution (REQ-043); dataset `measured_at` per C-55. Recommended as stated | User (P5.02) | P5.05, P5.07 |
| Units, references and periods | `TT_10` °C; `RF_10` %; `PP_10` hPa with `pressure_reference` `sensor` (air pressure at station altitude); `FF_10` m/s (`WindSpeedUnit.MetresPerSecond`, C-46) and `DD_10` degrees (C-43), both means over the previous 10 minutes and therefore carrying the period `[MESS_DATUM − 10 min, MESS_DATUM]` on `ObservationReading.Period` (exported per C-53); `RWS_10` mm as `PrecipitationAccumulation` over the same period; `GS_10` J/cm² × 10000 → J/m² as `RadiationEnergy` over the same period (C-45); `TT_10` is instantaneous and carries no period; `RF_10` and `PP_10` carry no period because DWD documents none for them. Verified against the DWD descriptions in P5.02 and approved as OC-05 (`RWS_10` option A: nominal period, no instrument correction) | User (P5.02) | P5.05 |
| Calm wind | Decided (OC-06 option B): omit `DD_10` when `FF_10` of the same row is 0 or when `DD_10` is 0 (REQ-053, C-43); DWD documents no calm marker, the data write north as 360, so `DD_10` = 0 is treated as no reliable direction (recorded project assumption) | User (P5.02) | P5.05 |
| Quality code | `quality` = the `QN` value as a string (`"1"`, `"2"`, `"3"`), the source quality code unchanged as C-53 requires; a prefixed or translated token would change the meaning of a v1 member and is out of scope (C-03); meaning documented in README | User (P5.02) | P5.04, P5.05 |
| Public API | Approved (OC-08): `IDwdObservationParser`/`DwdObservationParser` with `Task<ObservationDataset> ParseAsync(string stationId, CancellationToken cancellationToken)`; notices and omitted readings logged through `ILogger<DwdObservationParser>`; no usable reading → `ObservationDatasetRejectedException` (C-60). Unselected alternative: a result type carrying the notices | User (P5.02) | P5.07 |
| Options and registration | Approved (OC-09): new `DwdObservationOptions` (`ObservationDeadline`, default 120 s) registered by the consumer beside the existing `CdcCatalogEndpoints`, whose lists and `now` directories are reused / unselected alternative: the deadline added to `CdcCatalogEndpoints`; validation at construction (SC-12): `ObservationDeadline` permitted greater than zero and at most 1 hour (the SC-18 range); `Timeout.InfiniteTimeSpan`, zero, negative values, values above the maximum and `TimeSpan.MaxValue` throw `OBSERVATION-LIMIT-INVALID` at construction, before any timer is created or any request made (a `CancellationTokenSource` delay above about 49.7 days throws `ArgumentOutOfRangeException` and an infinite delay never fires; both reproduced on .NET 10 on 2026-10-24). Deadline adaptation: an additive `CatalogDeadlineKind.Observation` that `CatalogDownloader` maps to `ObservationLimitException` `OBSERVATION-DEADLINE-EXCEEDED`; the observation source builds its list and archive requests with that kind, so expiry during list or archive retrieval is never reported as `SELECTION-DEADLINE-EXCEEDED`; the P4 `ICdcCatalogSource` and the `Selection` behavior stay unchanged | User (P5.02) | P5.06, P5.09 |
| Error and notice IDs | Approved (OC-10): `DwdStationException` `STATION-ID-INVALID`, `STATION-UNKNOWN`, `STATION-METADATA-CONFLICT` (OC-02); `ObservationLimitException` `OBSERVATION-DEADLINE-EXCEEDED` and `OBSERVATION-LIMIT-INVALID` (invalid options at construction); an additive `IReadOnlyList<OmittedReading> OmittedReadings` property on the P2 type `ObservationDatasetRejectedException` (empty by default, filled by the builder at the rejection site; a library change, not an export-contract change) so that the parser logs every omission before the rejection propagates; reuse of the `CatalogRetrievalException` IDs and of `CatalogFormatException` `CDC-EVIDENCE-INVALID`; notices `OBSERVATION-PRODUCT-MISSING`, `OBSERVATION-READING-OMITTED` (kind and reason), `CDC-STATION-CONFLICT`, `CATALOG-RECORD-SKIPPED` | User (P5.02) | P5.03–P5.07 |
| Request bound | At most eight requests per parse (four lists, at most four archives), at most 4 in parallel (SC-18), all within `ObservationDeadline`; no retries (SC-09) | User (P5.02) | P5.06 |
| Source conditions and retrieval guidance | CC BY 4.0 attribution "Deutscher Wetterdienst (DWD)", IP-retention note, README recommendation for consumers: external scheduling (for example n8n or cron); the `now/` data are updated at a frequency of "< 1 h" (DWD descriptions); decided (OC-12 option A): at most one parse per station every 10 minutes, a project recommendation, not a provider rule (REQ-092) | User (P5.02); documented in P5.09 | P5.09 |
| Live verification | Optional, explicitly enabled, one parse of one real station within the request bound, recorded separately (pattern of SC-15 and SC-16) | User (P5.02) | P5.10 |
| Shared reader extension | Extend `ICdcNowEvidenceReader` additively so that it exposes `QN` and all product columns, P4 callers unchanged / a second reader. Recommended: the additive extension (SC-21) | Orchestrator, recorded in P5.02 | P5.04 |

## 4. Behavior contracts

| Area | Required behavior |
| --- | --- |
| Station resolution | REQ-030/031: ID grammar checked first; existence in the station lists; metadata (name, coordinates, altitude or none) from the lists; never a substitute station; no request before validation |
| Retrieval | REQ-014, REQ-063, SC-09, SC-18: consumer endpoints validated at construction without retrieval; consumer `HttpClient`; bounded size and time; no retries; HTTP 404 controlled as a missing product; caller cancellation, deadline and timeout classified in that precedence |
| File reading | SC-19 extended: all product columns and `QN` exposed; the SC-19 bounds and grammar kept; archives read as in-memory streams; synthetic fixtures only |
| Values | REQ-032/053: −999 missing whatever its spelling; zero is a real value; C-42 validation; every omission carries its reason |
| Units and periods | REQ-052/054/055/057, C-39/40/45/46: verified source units; both pressure outputs from one unrounded original with the `sensor` reference; m/s converted to km/h; accumulation and energy converted only with their 10-minute period; rounding only for output |
| Times | REQ-043/051/056: per-quantity UTC `measured_at` from `MESS_DATUM`; differing times per quantity preserved; no retrieval-time substitution; no freshness filter |
| Dataset | REQ-050, C-50 to C-60: `source` `dwd:<id>`; `location` from the lists; `retrieved_at` from the consumer's `TimeProvider`, read once per parse and truncated to whole seconds; dataset `measured_at` the latest exported reading; `timestamp_quality` `source`; no usable reading → rejection, never a success without readings |
| Export | REQ-067–069, C-64: the same dataset graph serialized by both real serializers; valid against the v1 output schema |
| Logging | REQ-072, REQ-044: stable notice IDs, UTC, station ID; omitted readings logged with kind and reason; no secrets |

Every class with executable logic has an interface and constructor injection; registration only in `AddWeatherParsing`; no global state. Every step that adds a public service interface registers it in `AddWeatherParsing` in the same step and keeps `ServiceRegistrationCompletenessTests` green; P5.09 verifies the integrated and the packaged consumption.

## 5. Ordered implementation packages

| Step | Depends on | Content |
| --- | --- | --- |
| P5.01 – Foundation check and plan adjustment | P4 merge | Section 1; remeasure the integration-target baseline; update this plan |
| P5.02 – Source facts and observation contract | P5.01 | Re-verify the four product descriptions, units, `QN`, `-999`, file names and the 404 behavior from official DWD sources; record the section 3 decisions in `docs/p5/P5_OBSERVATION_CONTRACT.md`; Codex review loop; user approval; reconcile the step plans P5.03 to P5.11; no production code |
| P5.03 – Station resolution | P5.02 | `IDwdStationResolver`/`DwdStationResolver` (Proposed): ID grammar (`\A[0-9]{5}\z`), lookup in the station-list records supplied by the caller (read by P5.06), `ObservationLocation` assembly, products per station, failures before any `now` request; no HTTP in this step; registration in `AddWeatherParsing` |
| P5.04 – Observation file reading | P5.02 | Additive extension of the shared `ICdcNowEvidenceReader` component for all product columns and `QN` under the SC-19 bounds (SC-21); synthetic fixtures for `tu`, `ff`, `rr` and `sd`; P4 callers unchanged |
| P5.05 – Quantity mapping, units, periods and quality | P5.04 | `IDwdObservationMapper`/`DwdObservationMapper` (Proposed): rows → `ObservationReading` per SC-03 kind with units, pressure reference, the 10-minute periods of wind, precipitation and radiation, the `GS_10` unit conversion, missing markers, calm wind, `QN` quality, the latest usable value per quantity and UTC times through `ISourceTimeNormalizer`; registration in `AddWeatherParsing` |
| P5.06 – Observation retrieval boundary | P5.03, P5.04 | `IDwdObservationSource`/`DwdObservationSource` (Proposed) over `ICatalogDownloader`: the four station lists and the archive URIs from the `now` directories, 404 as a missing product, size bounds, the additive `CatalogDeadlineKind.Observation` mapped to `OBSERVATION-DEADLINE-EXCEEDED` (P4 behavior unchanged), parallelism, `DwdObservationOptions` validation; expiry during list and archive retrieval and simultaneous caller cancellation tested; registration in `AddWeatherParsing` |
| P5.07 – Parser service and dataset assembly | P5.03–P5.06 | `IDwdObservationParser`/`DwdObservationParser` (approved, OC-08): resolve, retrieve, map, build through `IObservationDatasetBuilder` with provider `dwd`, `retrieved_at` from `TimeProvider`, notices and omissions logged with stable IDs, rejection without usable readings; deadline, cancellation and failure precedence verified at parser level; registration in `AddWeatherParsing` |
| P5.08 – Export equivalence and schema validation | P5.07 | The produced `ObservationDataset` through both real serializers: equivalence, schema validation, UTC forms, omitted members, periods and quality on the wire |
| P5.09 – DI registration, README and package consumption | P5.08 | Integrated DI with consumer options; README section for observation parsing (attribution, IP note, retrieval guidance, error and notice IDs); package-consumer tests from the same-run `.nupkg` |
| P5.10 – Integrated coverage and platform verification | P5.03–P5.09 | Candidate through `ci-local.ps1` on Windows, hosted CI, NanoPi R76S (with authorization); optional separately enabled live parse; READY/NOT_READY |
| P5.11 – Acceptance and P6 handoff | P5.10 | Evidence matrix T-P5-01 to T-P5-19, limitations, user acceptance, status updates, P6 handoff; pull request ready for review, review fixes, merge by the user |

Step plans are stored as `docs/p5/P5_NN_<SHORT_NAME>_PLAN.md`; each is written by the orchestrating agent and reviewed by Codex before the step starts (user decision dated 2026-10-24: documentation is not generated by qwen).

- [P5.01](docs/p5/P5_01_FOUNDATION_CHECK_PLAN.md)
- [P5.02](docs/p5/P5_02_SOURCE_FACTS_AND_CONTRACT_PLAN.md)
- [P5.03](docs/p5/P5_03_STATION_RESOLUTION_PLAN.md)
- [P5.04](docs/p5/P5_04_OBSERVATION_FILE_READING_PLAN.md)
- [P5.05](docs/p5/P5_05_QUANTITY_MAPPING_PLAN.md)
- [P5.06](docs/p5/P5_06_RETRIEVAL_BOUNDARY_PLAN.md)
- [P5.07](docs/p5/P5_07_PARSER_SERVICE_PLAN.md)
- [P5.08](docs/p5/P5_08_EXPORT_EQUIVALENCE_PLAN.md)
- [P5.09](docs/p5/P5_09_DI_README_PACKAGE_PLAN.md)
- [P5.10](docs/p5/P5_10_INTEGRATED_VERIFICATION_PLAN.md)
- [P5.11](docs/p5/P5_11_ACCEPTANCE_HANDOFF_PLAN.md)

## 6. Requirement-to-test matrix

| Test ID | Requirement IDs | Behavior and boundary | Step |
| --- | --- | --- | --- |
| T-P5-01 | REQ-030, REQ-031 | Station ID grammar `\A[0-9]{5}\z`: five ASCII digits accepted; empty, whitespace, non-digit, Unicode-digit, trailing-newline, four- and six-digit inputs rejected with `STATION-ID-INVALID` before any request | P5.03 |
| T-P5-02 | REQ-031, REQ-023 | Station existence and metadata from the lists: unknown ID → `STATION-UNKNOWN` without a `now` request; name with umlauts, coordinates, altitude or none (never zero), leading zeros kept; products per station; cross-list conflict → `STATION-METADATA-CONFLICT` (OC-02) | P5.03 |
| T-P5-03 | REQ-032, REQ-083 | Reading all four products: every column and `QN` exposed; SC-19 bounds and grammar kept; P4 callers unchanged; synthetic fixtures | P5.04 |
| T-P5-04 | REQ-032, REQ-053 | Missing marker −999 in every spelling omitted; genuine zeros kept; non-finite and out-of-range values omitted with their reason (C-42) | P5.05 |
| T-P5-05 | REQ-052, REQ-054, REQ-057 | Units: °C, %, hPa with the `sensor` reference and both pressure outputs from one original, m/s → km/h, degrees; rounding only on the wire | P5.05 (mapping), P5.08 (wire) |
| T-P5-06 | REQ-055 | `RWS_10` and `GS_10` carry the 10-minute period and convert only with it; J/cm² → J/m² before C-45; `FF_10` and `DD_10` carry the same period without conversion; every period exported with start and end through both serializers | P5.05, P5.08 |
| T-P5-07 | REQ-053 | Calm wind: direction omitted when `FF_10` is 0 or `DD_10` is 0 (OC-06); never an invented north; when every non-missing direction row is excluded, no direction is exported and exactly one `OBSERVATION-READING-OMITTED` with the actual reason is logged, on success and before a rejection | P5.05 (mapping), P5.07 (logging, composed), P5.08 (wire) |
| T-P5-08 | REQ-032 | `QN` mapped to `quality` as the unchanged trimmed token (OC-07) for every exported value | P5.05 (mapping), P5.08 (wire) |
| T-P5-09 | REQ-043, REQ-051, REQ-056 | Latest usable value per quantity with its own UTC `measured_at` in the `YYYY-MM-DDTHH:mm:ss` form; differing times per quantity preserved; no freshness filter; retrieval time never substituted | P5.05, P5.07 |
| T-P5-10 | REQ-014, REQ-063 | Retrieval: consumer endpoints validated at construction; bounded size and timeout; no retries; 404 → missing-product notice; other failures fail the parse; deadline expiry during list and during archive retrieval → `OBSERVATION-DEADLINE-EXCEEDED` (never `SELECTION-DEADLINE-EXCEEDED`), caller cancellation taking precedence; at most eight requests | P5.06, P5.07 |
| T-P5-11 | REQ-050, REQ-031 | Dataset assembly: `source` `dwd:<id>`, location, `retrieved_at` from `TimeProvider`, dataset `measured_at` the latest, `timestamp_quality` `source`; no usable reading → rejection; never another station's data | P5.07 |
| T-P5-12 | REQ-051, REQ-067, REQ-068, REQ-069, REQ-079 | The same dataset through both real serializers without another retrieval: equivalent, schema-valid, UTC forms, omitted members, periods and quality | P5.08 |
| T-P5-13 | REQ-013, REQ-014, REQ-016, REQ-078 | DI with consumer options, independent configurations, same-run package consumer; every new doing class behind an interface with constructor injection (`ServiceRegistrationCompletenessTests`, `NoSharedStateTests`) | P5.09 (integration and package); P5.03, P5.05, P5.06, P5.07 (registration of each new interface) |
| T-P5-14 | REQ-090, REQ-092 | README observation section: attribution, IP note, retrieval guidance, error and notice IDs; package check | P5.09 |
| T-P5-15 | REQ-085 | Recorded Red → Green → Refactor evidence per slice | P5.03–P5.09 |
| T-P5-16 | REQ-086 | Native block coverage ≥ 80 % and strict improvement against the remeasured integration target | P5.10 |
| T-P5-17 | REQ-017, REQ-084 | P5 behavior on the NanoPi R76S, separate from Windows | P5.10 |
| T-P5-18 | REQ-083, REQ-092 | Optional live parse of one station, explicitly enabled, recorded separately | P5.10 |
| T-P5-19 | REQ-044, REQ-072 | Diagnostics: missing products, omitted readings (kind and reason, from the builder and from the mapper's OC-06 omissions), station conflicts and skipped records logged with stable IDs, UTC and the station ID; format deviations surface as failures, never as substituted values; no secrets in logs | P5.07 |

## 7. Execution verification and evidence

Records in `docs/p5/P5_VERIFICATION.md` per step: Red names and observed failures, commands, test counts, coverage counters, authorship, Codex review runs and findings. Commands from CONTRIBUTING.md sections 2–7: `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`, `pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`. Hosted CI results per push; NanoPi evidence separate from Windows; mocks and model output are no evidence of live or hardware success.

Branch `features/develop/P5` from `develop`; open the draft pull request into `develop` directly after the first pushed commit; push every further commit to it so hosted CI runs on every push; commits and pushes only on the user's request.

Per implementation step: qwen generates tests and code from briefs; the orchestrating agent reviews the diff, runs builds, tests and coverage; then the Codex CLI reviews the step read-only before the user is asked to commit; confirmed findings in code, tests, scripts and data are fixed by qwen test-first, confirmed findings in documentation (plans, the observation contract, records, README and status text) by the orchestrating agent; rejected findings are reported with the reason. Step plans get their own Codex review before the step starts.

## 8. Files and documentation maintenance

New records under `docs/p5/`; production code under `src/WeatherParser.Parsing/<Folder>/` (Proposed folder `Observations/Dwd/`, namespace `WeatherParser.Parsing.Observations.Dwd`, beside the existing `Observations/`); tests under `tests/WeatherParser.Parsing.Tests/` and `tests/WeatherParser.Package.Tests/`; synthetic fixtures under `tests/WeatherParser.Parsing.Tests/Fixtures/CdcNow/` (Proposed); README section "DWD observations (library)" in P5.09; the AGENTS.md consistency table applies. Status documents follow actual progress: the status line of this plan is updated at every step completion; the phase-status passages of README.md, AGENTS.md, DEVELOPMENT_PLAN.md and PHASE_P1_PLAN.md are updated when P5 starts (P5.01), when P5.09 turns the README feature status of DWD observation parsing from planned to implemented, at the acceptance (P5.11) and after the merge; implementation, verification, acceptance and merge statuses stay distinct.

## 9. P5 acceptance and P6 handoff checklist

- [ ] P5.01 foundation check recorded
- [ ] observation contract approved by the user after Codex review
- [ ] every mandatory T-P5 row has passing tests and recorded evidence; the optional live check T-P5-18 is either passed or recorded as skipped with its limitation
- [ ] station resolution, retrieval, parsing and normalization match REQ-030–032, REQ-043–044 and REQ-050–057
- [ ] the dataset serializes equivalently and validates against the v1 schema
- [ ] no export contract change without a recorded additive decision
- [ ] DI and same-run package consumption proven
- [ ] coverage gate passed on Windows and the NanoPi R76S, platform outcomes separate
- [ ] limitations with owners
- [ ] user acceptance recorded
- [ ] status documents consistent
- [ ] pull request ready for review, review findings resolved test-first, merged into `develop` by the user
- [ ] P6 handoff written
- [ ] P6 handoff records what P6 reuses (`ISourceTimeNormalizer.InferDate`, `IObservationDatasetBuilder`, the real serializers) and the open items

## 10. Plan authorship and review

Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief (step list, decisions, matrix and facts); reviewed read-only by the OpenAI Codex CLI in the five-angle audit format; findings verified by the orchestrating agent and fixed by qwen. Review rounds and findings (2026-10-24): this plan 6, 1, 0; the step plans P5.01 3, 0; P5.02 2, 0; P5.03 2, 0; P5.04 2, 2, 0; P5.05 2, 1, 0, 0; P5.06 5, 2, 0; P5.07 4, 2, 3, 0; P5.08 1, 0; P5.09 3, 0; P5.10 2, 1, 0; P5.11 2, 0; cross-plan review of all twelve documents 3, 0. Every finding was verified by the orchestrating agent against the code, the contracts and the plans and confirmed. Authorship note: for every document qwen reproduced the orchestrating agent's brief (step list, decisions, slices, tests, matrix) verbatim instead of transforming it, so the content is the orchestrating agent's design; the agent rendered the slice fields of the step plans into the P4 format with a mechanical script and, where qwen's edit blocks did not match the file text (backticks or capitalization), applied the described corrections itself: the P5.11 matrix range, the P5.04 boundary-test names, the P5.07 Green lines, the ObservationDatasetBuilder artifact row and the S3 and T-P5-11 wording of P5.07, the P5.05 S1 Green line, the P5.03 ValidateId signature and the P5.02 OC-07 row. The P5.03 interface gained `ValidateId` for consistency with P5.07 after the individual reviews.

User decision dated 2026-10-24, after these rounds: qwen is removed from the documentation pipeline and remains the code generation engine (AGENTS.md "AI-assisted development"). From then on the orchestrating agent writes and corrects plans, the observation contract, records, handoffs, README and status text; Codex reviews them read-only. The step plans P5.01 to P5.11 were amended accordingly in the same change (the drafting lines of their slices; no other content changed).

Pull-request review of PR #6 at `9b5a8ca` (2026-10-24, review 5637364332 and the summary comment): 1 finding [Major], confirmed by the orchestrating agent against `CdcCatalogEndpointsValidator` (SC-18 range) and a .NET 10 probe (`new CancellationTokenSource(TimeSpan.MaxValue, TimeProvider.System)` throws `ArgumentOutOfRangeException`; `Timeout.InfiniteTimeSpan` is accepted and never fires): the OC-09 row and the P5.06 S1 slice named no finite range for `ObservationDeadline`. Fixed by the orchestrating agent: the range and the rejected values are now part of the OC-09 proposal (this plan section 3, P5.02), the P5.06 decisions, S1 Red tests (both accepted boundaries, one tick above the maximum, infinite and `TimeSpan.MaxValue`), S1 Green, the matrix row and the acceptance checklist, and P5.07 notes that the timer delay is finite by that validation. Codex five-angle review of the fix diff before the commit: 1 round, 0 findings.

P5 planning completed on 2026-10-24 (user instruction after the fix): the phase plan and the eleven step plans P5.01 to P5.11 are final at commit `9da193a` on pull request #6 (branch `features/develop/P4-post-merge`); every individual Codex review, the cross-plan review and the review of the pull-request fix ended with 0 findings, the one pull-request review finding is fixed and answered, and hosted CI for `9da193a` succeeded on windows-x64, ubuntu-x64 and debian13-arm64. Open at completion: the merge of pull request #6 by the user; the 14 Proposed decisions OC-01 to OC-14, which the user approves in P5.02; a possible re-review of `9da193a`, whose findings would be handled as further review rounds under P5.11 A10 rules before the merge. P5 execution (P5.01 first) starts only on the user's separate instruction.
