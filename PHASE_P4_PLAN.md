# WeatherParser – P4 implementation plan

**Status (2026-10-20): P4.01 executed on 2026-10-21 (outcome READY, [record](docs/p4/P4_01_FOUNDATION_CHECK.md)); P4.02 complete on 2026-10-21: selection contract SC-01 to SC-23 approved by the user ([contract](docs/p4/P4_SELECTION_CONTRACT.md)); this plan and the step plans P4.03 to P4.12 reconciled with it (commit `5c7f1f2`, hosted CI run 39676704275 green on all three jobs; [P4 verification record](docs/p4/P4_VERIFICATION.md) section 2). P4.03 (coordinate parsing and search parameters) complete: `a3b79d7`, hosted CI run 39737759157 green ([verification](docs/p4/P4_VERIFICATION.md) section 3). P4.04 (geodesy and inclusive filters) complete: `abe6d64`, hosted CI run 39810906820 green (section 4). P4.05 (CDC station-list reader) complete: `0195b4f`, hosted CI run 39828777889 green (section 5). P4.06 (CDC availability derivation) complete: `643475f`, hosted CI run 39840675493 green (section 6). P4.07 (MOSMIX catalog and availability) complete: `0f29842`, hosted CI run 39905420541 green (section 7). P4.08 (catalog retrieval boundary) implemented (section 8).**
Refines the P4 section of [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) using the [P4 handoff](docs/p2/P4_HANDOFF.md); binding rules in [AGENTS.md](AGENTS.md) and [REQUIREMENTS.md](REQUIREMENTS.md).

## 1. First step: check P2 completion and adjust this plan

P4.01 is always the first execution step: verify P2 acceptance and merge from `docs/p2/P2_12_ACCEPTANCE.md` (sections 10 and 12) and the P4 handoff; extract the integration target SHA, package identity `WeatherParser.Parsing` `0.1.0-initial`, SDK/tool versions, the frozen export contract v1 and limitations; remeasure the native block coverage of the integration target under comparable conditions as the comparison baseline; update this plan with the extracted facts before P4.02. If P2 were incomplete: record the gap, do not implement P4.

### P4.01 extraction (2026-10-21)

Recorded in [P4_01_FOUNDATION_CHECK.md](docs/p4/P4_01_FOUNDATION_CHECK.md); extracted foundation:

| Field | Value | Evidence |
|---|---|---|
| Integration target | `b7da8288d8746bc6e034222c0467a8fcebba55b3` (develop head; P2 merge `74bf625` plus an AGENTS.md-only commit) | `git rev-parse origin/develop`, ancestry check |
| Package identity | `WeatherParser.Parsing` `0.1.0-initial`, unpublished | P4 handoff section 1 |
| SDK, runtime, coverage tools | SDK 10.0.402 (current servicing release, checked 2026-10-21), runtime 10.0.13, Microsoft.CodeCoverage and dotnet-coverage 18.11.0 | `global.json`, `DEPENDENCIES.md`, official release metadata |
| Export contract | v1 frozen; namespace `urn:weatherparser:export:v1`; C-01 to C-116; additive optional members only (C-03, C-105) | `docs/p2/P2_EXPORT_CONTRACT.md` |
| Helpers for P4 | `CdcStationList`, `MosmixLocationList`, `SearchParameters`, `IListEntryOrdering`, `IJsonExportSerializer`, `IXmlExportSerializer`, `AddWeatherParsing`, schema validation settings (C-104) | P4 handoff section 2 |
| P2 coverage at acceptance | 3035/3037 on Windows and the NanoPi R76S (candidate `bced7be`) | `docs/p2/P2_12_ACCEPTANCE.md` |
| Remeasured baseline | 3035/3037 native blocks on the integration target; every P4 code change must strictly improve on it | run `artifacts/ci/e5dfbc6-20261021T041105Z/` |
| Inherited limitations | with owners as listed in the P4.01 record section 4 | `docs/p2/P2_12_ACCEPTANCE.md` section 6 |

No other plan adjustment was needed.

## 2. Goal, binding scope and exclusions

Goal: library services that select CDC observation stations and MOSMIX forecast locations around a reference location and return the P2 list roots, proven offline with synthetic fixtures and the real serializers.

In scope:
- coordinate parsing
- search-parameter validation
- geodesic distance and inclusive filters
- CDC station catalog reader
- CDC availability derivation
- MOSMIX location catalog reader
- per-product MOSMIX availability
- catalog retrieval boundary (HTTP behind an interface, constructor endpoints, bounded timeout/size, cancellation)
- selection services producing `CdcStationList`/`MosmixLocationList` with `SearchParameters`, source, UTC retrieval time, ordering through `IListEntryOrdering`, empty result with log notice
- DI registration and package-consumer evidence
- integrated coverage/platform verification
- acceptance and P5 handoff

Out of scope with owner:
- CLI options, output modes, files/stdout, exit codes → P3
- DWD observation retrieval/parsing → P5 (P4 retrieves and structurally checks the per-station `now` files only to establish availability, SC-19; full observation parsing and normalization stay in P5)
- OsthessenWetter → P6
- MOSMIX forecast parsing, KML/KMZ, input schema `pointforecast_dwd_extension_V1_0.xsd` → P7 (P4 retrieves and structurally checks MOSMIX_L files and the MOSMIX_S all-stations file only to establish availability, SC-20, SC-22; forecast interpretation, time alignment and units stay in P7)
- MQTT serializer/topics → P8
- publication → P9

No change of export contract v1 except additive optional members with a recorded decision (C-03).

## 3. Decisions before the affected implementation

Decisions are recorded in `docs/p4/P4_SELECTION_CONTRACT.md` (Proposed name) in P4.02, reviewed by Codex read-only (up to 10 rounds) and approved by the user before the affected Red test; source formats are taken only from official DWD documentation or the actual published files, with URL and check date recorded; P4.02 also records access conditions, attribution and retrieval-frequency guidance for every catalog and availability endpoint (REQ-092); unresolved restrictions block live retrieval.

All decisions below were approved on 2026-10-21 in [P4_SELECTION_CONTRACT.md](docs/p4/P4_SELECTION_CONTRACT.md) as SC-01 to SC-23.

| Decision | Required resolution / recommendation | Decided by | Blocks |
| --- | --- | --- | --- |
| CDC catalog source and format | Union of the four 10-minute `now` station lists (air temperature, wind, precipitation, solar), fixed-width ISO-8859-1 layout with verified positions (SC-02) | User, approved 2026-10-21 | P4.05–06 |
| CDC availability derivation | List pre-filter on from/to dates (UTC assumption, one-day tolerance), then, only for stations inside the search area, the station's current `now` data file per product; a quantity counts only with a usable value (P2 `IQuantityValidator`) within [reference instant − 3 h, reference instant + 10 min] (SC-03, SC-19) | User, approved 2026-10-21 | P4.06 |
| MOSMIX catalog source and format | ISO-8859-1, header token check, IDs `^[A-Z0-9]{4,5}$`, coordinates in degrees and minutes, elevation −500..9000 m, missing elevation → excluded with notice (SC-04) | User, approved 2026-10-21 | P4.07 |
| MOSMIX per-product availability | MOSMIX_L (option A) from each in-area location's own file; MOSMIX_S from a cached evidence index of the all-stations file, built by a separate refresh operation (`IMosmixSEvidenceRefresher`) whose timing is controlled externally, for example by n8n or cron (not a dependency); selections only read the index; a stale index is used and marked (`MOSMIX-S-EVIDENCE-STALE`); a missing index fails (`MOSMIX-S-EVIDENCE-MISSING`) (SC-05, SC-20, SC-22) | User, approved 2026-10-21 | P4.07 |
| Geodetic method | Vincenty (WGS84) with spherical fallback and a spherical pre-filter in the filter; GeographicLib reference values recorded before the P4.04 Red test (SC-06) | User, approved 2026-10-21 | P4.04 |
| Coordinate grammar | Decimal `^[+-]?\d{1,3}(\.\d{1,20})?$` and DMS `D° M′ [S″] H` with the accepted symbols, hemisphere rules and ranges of SC-07; invariant culture; failures `COORDINATE-INVALID` | User, approved 2026-10-21 | P4.03 |
| Logging of notices | `Microsoft.Extensions.Logging.Abstractions` 10.0.13 with both DI packages raised to 10.0.13 in P4.07, the first step whose service logs (the MOSMIX catalog reader's missing-altitude notice); the P4.05 and P4.06 readers return diagnostics without logging and the P4.09 selectors log them; services take `ILogger<T>`, the consumer registers logging; notice IDs of SC-17 (SC-08) | User, approved 2026-10-21 | P4.07, P4.09 |
| Retrieval boundary | Consumer-registered `HttpClient`, no library handler; 30 s per request, 8 MiB / 1 MiB / 64 MiB size bounds, no retries, classification precedence, sanitized messages (SC-09) | User, approved 2026-10-21 | P4.08 |
| Catalog failure semantics | Malformed records skipped with `CATALOG-RECORD-SKIPPED`; format failure for header mismatch, empty stream, lines over 2048 characters or no valid record; exclusions are not failures; any list, catalog or non-404 per-station failure fails the selection; valid empty selection with one `SELECTION-EMPTY` (SC-10) | User, approved 2026-10-21 | P4.05–09 |
| Public selection API | Interfaces of SC-11 including `ICdcNowEvidenceReader`, `IMosmixLEvidenceReader`, `ICdcStationSelector` and `IMosmixLocationSelector`; one validation policy for factory and selectors with snapshot at entry; every public exception exposes `ErrorId` (SC-11, SC-17) | User, approved 2026-10-21 | P4.09–10 |
| List metadata and time source | `source` = `dwd:cdc-10min-now` and `dwd:mosmix-stationskatalog`; the consumer registers the BCL `TimeProvider`; the reference instant is read once per selection, evidence download times separately; `retrieved_at` truncated to whole seconds (SC-13) | User, approved 2026-10-21 | P4.06, P4.09 |
| Service registration under C-107/C-108 | C-107 unchanged; the consumer registers `CdcCatalogEndpoints` (eight URIs and limits) and `MosmixCatalogEndpoints` (catalog, MOSMIX_L directory, limits, MOSMIX_S settings), an `HttpClient`, a `TimeProvider` and logging; URI composition rules; validation at construction (SC-12) | User, approved 2026-10-21 | P4.03–10 |
| Live verification | Optional and explicitly enabled (P4.11 S0); the requests of one CDC and one MOSMIX selection with a small search radius, bounded by SC-18; recorded separately (SC-15) | User, approved 2026-10-21 | P4.11 |
| Per-station request limits | MaxEvidenceCandidates (default 100) and SelectionDeadline (default 120 s), at most 4 parallel requests (SC-18) | User, approved 2026-10-21 | P4.08, P4.09 |
| MOSMIX_S evidence cache and refresh | Separate operation, externally triggered, no scheduler, SC-22 | User, approved 2026-10-21 | P4.07, P4.08 |
| Evidence timestamps in the result | Additive optional members `evidence_retrieved_at` and `evidence_issued_at` on `MosmixProductAvailability`, the schema generator extended for optional timestamps, the v1 schema regenerated (SC-23, C-117 to C-119) | User, approved 2026-10-21 | P4.07, P4.09 |

## 4. Behavior contracts

| Area | Required behavior |
| --- | --- |
| Coordinates | REQ-021: decimal and DMS, N/S/E/W, ranges, minutes/seconds 0 ≤ x < 60, nonfinite, wrong hemisphere and conflicting sign rejected, culture independent, English messages |
| Search parameters | REQ-020/022: altitude may be negative, both limits ≥ 0 and finite, values carried unchanged into `SearchParameters` |
| Distance and filters | REQ-022: documented WGS84 method, both limits inclusive, absolute altitude difference filtered separately, signed `altitude_difference_m` = entry minus reference |
| CDC catalog | REQ-023/024: IDs as strings with leading zeros, umlauts preserved, format per documentation, malformed records handled per the decided failure semantics, never guessed; a format failure never yields a successful empty list |
| CDC availability | REQ-024: only quantities with a usable value in the station's current `now` data (SC-03, SC-19) appear in `available_measurements`; catalog membership no proof; stations without a candidate product or without an available quantity are inactive and never entries |
| MOSMIX catalog | REQ-100–102: own catalog and IDs, alphanumeric and leading-zero IDs, never a CDC ID, missing altitude never zero, unfilterable locations excluded with a notice, no CDC fallback |
| MOSMIX availability | C-23: one item per product, `unknown` explicit, elements only when `available`; MOSMIX_L from each in-area location's file (SC-05 option A, SC-20); MOSMIX_S from the externally refreshed evidence index, a stale index used and marked (SC-22); `evidence_retrieved_at` and `evidence_issued_at` whenever evidence was used (SC-23) |
| Retrieval | REQ-014: endpoints in consumer-registered options validated at construction without retrieval, no built-in or fallback URLs; consumer `HttpClient`; size and time bounds, classification precedence and sanitized messages (SC-09, SC-12); per-station requests only inside the search area, at most `MaxEvidenceCandidates` candidates, 4 in parallel, within `SelectionDeadline` (SC-18) |
| Lists | REQ-023/101: `schema_version`, `source` literals (SC-13), UTC `retrieved_at` from the reference instant of the consumer's `TimeProvider` truncated to whole seconds, `SearchParameters` from the validated snapshot (SC-11), ordering through `IListEntryOrdering`, valid empty list plus one `SELECTION-EMPTY` notice, never a substitute |
| Exports | REQ-067–068: the same list graph serialized by both real serializers, valid against the v1 output schema |

Every class with executable logic has an interface and constructor injection; registration only in `AddWeatherParsing`; no global state.

## 5. Ordered implementation packages

| Step | Depends on | Content |
| --- | --- | --- |
| P4.01 – Foundation check and plan adjustment | P2 merge | Section 1; remeasure the integration-target coverage baseline; update this plan |
| P4.02 – Source facts and selection contract | P4.01 | Verify the CDC and MOSMIX catalog formats and availability sources from official DWD sources; record the section 3 decisions in `docs/p4/P4_SELECTION_CONTRACT.md`; Codex review loop; user approval; no production code |
| P4.03 – Coordinate parsing and search parameters | P4.02 | Coordinate parser and search-parameter validation service with interfaces, test-first; every step that adds a public service interface registers it in `AddWeatherParsing` in the same step and keeps `ServiceRegistrationCompletenessTests` green |
| P4.04 – Geodesy and inclusive filters | P4.02–03 | Distance calculator with the decided method and reference values; inclusive distance/altitude filter; altitude difference |
| P4.05 – CDC station catalog reader | P4.02 | Reader of one CDC station list with the verified SC-02 layout from local streams with synthetic fixtures (SC-14): header check, field grammars, missing altitude, malformed and duplicate records, 2048-character line limit, format failures (SC-10) |
| P4.06 – CDC availability derivation | P4.05 | Union and conflicts of the four lists, date pre-filter (SC-02, SC-03 stage 1), `ICdcNowEvidenceReader` (SC-19) and the usable-value derivation (SC-03 stage 2) |
| P4.07 – MOSMIX location catalog and product availability | P4.02, P4.04, P4.05 | Export members C-117 to C-119 with generator change and schema regeneration (SC-23); MOSMIX catalog reader (SC-04); logging package and DI version raise (SC-08); `MosmixCatalogEndpoints` with all its URIs, limits and MOSMIX_S settings, their validation at construction and the URI composition rules (SC-12, SC-18, SC-22), needed by the cache store; `IMosmixLEvidenceReader` (SC-20); MOSMIX_S evidence index and cache store (SC-22 read and write side); availability resolver (SC-05) |
| P4.08 – Catalog retrieval boundary | P4.05–P4.07 | Downloader with SC-09 bounds and classification; `CdcCatalogEndpoints` with its eight URIs and limits, validated with the P4.07 rules, and the `ErrorId` of `ParserEndpointValidationException` (SC-12, SC-17); per-station evidence downloads with HTTP 404 semantics; `IMosmixSEvidenceRefresher` (SC-22 download and stream parse); catalog sources; offline tests with a mocked HTTP handler |
| P4.09 – Selection services and list assembly | P4.03–08 | Selectors with snapshot validation (SC-11), reference instant and evidence times (SC-13), candidate cap, deadline and parallelism (SC-18), evidence orchestration, notices and diagnostics, `CdcStationList`/`MosmixLocationList` assembly with evidence timestamps; real JSON/XML and schema validation |
| P4.10 – DI registration and package consumption | P4.09 | Integrated DI configuration with consumer-registered options and independent configurations; README consumer guidance (SC-16 attribution, IP note, retrieval guidance, refresh scheduling example with n8n or cron, transport settings); package-consumer tests from the same-run `.nupkg` including the refresh and the new export members |
| P4.11 – Integrated coverage and platform verification | P4.03–10 | Committed candidate through `scripts/ci-local.ps1` on Windows, hosted CI, NanoPi R76S evidence (with authorization); optional separately enabled live check; READY/NOT_READY |
| P4.12 – Acceptance and P5 handoff | P4.11 | Evidence matrix, limitations, user acceptance, status updates, P5 handoff including the SC-21 reuse obligations (P5 builds on the CDC evidence reader's archive and format handling; P7 on the MOSMIX_L reader's secure KMZ and XML handling and the MOSMIX_S streaming reader); pull request ready for review, review fixes, merge by the user |

Step plans are stored as `docs/p4/P4_NN_<SHORT_NAME>_PLAN.md`; each is drafted by qwen and reviewed by Codex before the step starts.

- [P4.01](docs/p4/P4_01_FOUNDATION_CHECK_PLAN.md)
- [P4.02](docs/p4/P4_02_SOURCE_FACTS_AND_CONTRACT_PLAN.md)
- [P4.03](docs/p4/P4_03_COORDINATES_PLAN.md)
- [P4.04](docs/p4/P4_04_GEODESY_FILTERS_PLAN.md)
- [P4.05](docs/p4/P4_05_CDC_CATALOG_PLAN.md)
- [P4.06](docs/p4/P4_06_CDC_AVAILABILITY_PLAN.md)
- [P4.07](docs/p4/P4_07_MOSMIX_CATALOG_PLAN.md)
- [P4.08](docs/p4/P4_08_RETRIEVAL_BOUNDARY_PLAN.md)
- [P4.09](docs/p4/P4_09_SELECTION_SERVICES_PLAN.md)
- [P4.10](docs/p4/P4_10_DI_PACKAGE_CONSUMPTION_PLAN.md)
- [P4.11](docs/p4/P4_11_INTEGRATED_VERIFICATION_PLAN.md)
- [P4.12](docs/p4/P4_12_ACCEPTANCE_HANDOFF_PLAN.md)

## 6. Requirement-to-test matrix

| Test ID | Requirement IDs | Behavior and boundary | Step |
| --- | --- | --- | --- |
| T-P4-01 | REQ-021, REQ-082 | Decimal coordinates: bounds −90/+90 and −180/+180 inclusive, out of range, nonfinite, culture independence | P4.03 |
| T-P4-02 | REQ-021, REQ-082 | DMS with N/S/E/W, minutes/seconds 0 ≤ x < 60, wrong hemisphere, conflicting signs, `50° 33′ 18″ N`, `9° 40′ 26″ E` | P4.03 |
| T-P4-03 | REQ-020, REQ-022 | Search parameters: negative reference altitude allowed, limits ≥ 0 and finite | P4.03 |
| T-P4-04 | REQ-022 | Distance against published reference values within the decided tolerance | P4.04 |
| T-P4-05 | REQ-022, REQ-082 | Inclusive limits at the exact boundary, altitude filtered separately, signed altitude difference | P4.04 |
| T-P4-06 | REQ-023, REQ-024, REQ-083 | CDC catalog: leading-zero IDs, umlauts, documented format, malformed records handled per the decided failure semantics, never guessed; a format failure never yields a successful empty list | P4.05 |
| T-P4-07 | REQ-024 | CDC availability from product metadata; membership no proof; inactive stations not suitable; usable-value rule (SC-03) | P4.06 |
| T-P4-08 | REQ-100–102, REQ-082 | MOSMIX catalog: alphanumeric and leading-zero IDs, coordinate normalization, missing altitude excluded with notice | P4.07 |
| T-P4-09 | REQ-101–102 | Per-product availability, explicit `unknown`, no CDC fallback; MOSMIX_L files, MOSMIX_S index (SC-22), evidence timestamps (SC-23) | P4.07 |
| T-P4-10 | REQ-014, REQ-102 | Constructor endpoints validated without retrieval, no fallback, bounded timeout/size, cancellation, surfaced failures | P4.07 (MOSMIX options validation), P4.08 |
| T-P4-11 | REQ-023, REQ-068 | CDC list assembly: metadata, search parameters, ordering, empty list with notice, no substitute; retrieval time with fractional seconds | P4.09 |
| T-P4-12 | REQ-101, REQ-068 | MOSMIX list assembly: same rules | P4.09 |
| T-P4-13 | REQ-067–068, REQ-082 | Both lists through real JSON/XML serializers, equivalence, schema-valid, leading zeros, umlauts | P4.09 |
| T-P4-14 | REQ-013, REQ-014, REQ-016 | DI with consumer endpoints, independent configurations, same-run package consumer | P4.10 |
| T-P4-15 | REQ-085 | Recorded Red → Green → Refactor evidence per slice | P4.03–10 |
| T-P4-16 | REQ-086 | Native block coverage ≥ 80 %; strict improvement against the remeasured integration target below 100 %, exact 100 % accepted only at an exact 100 % baseline; unrounded native counter ratios | P4.11 |
| T-P4-17 | REQ-017, REQ-084 | P4 behavior on the NanoPi R76S (Debian 13 ARM64), separate from Windows | P4.11 |
| T-P4-18 | REQ-083, REQ-092 | Optional live catalog check, explicitly enabled and recorded separately | P4.11 |
| T-P4-19 | REQ-101, REQ-102, REQ-083 | MOSMIX_S evidence refresh and index: download and stream parse bounds, all-stations identity rules, source binding, atomic replacement, missing and stale index handling (SC-22) | P4.07, P4.08 (P4.09 selector index check and stale notice, P4.10 DI and package consumer) |
| T-P4-20 | REQ-067, REQ-068, REQ-079 | Additive export members `evidence_retrieved_at` and `evidence_issued_at` (C-117 to C-119): generator, regenerated v1 schema, real serializers, package consumer | P4.07, P4.10 |

## 7. Execution verification and evidence

Records in `docs/p4/P4_VERIFICATION.md` per step (Red names and observed failures, commands, test counts, coverage counters, authorship, Codex review runs and findings); commands from CONTRIBUTING.md sections 2–7 (`dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`, `pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`); hosted CI results per push; NanoPi evidence separate from Windows; mocks and model output are no evidence of live or hardware success.

Branch `features/develop/P4` from `develop`; open the draft pull request into `develop` directly after the first pushed commit; push every further commit to it so hosted CI runs on every push; commits and pushes only on the user's request.
Per implementation step: qwen generates tests and code from briefs; the orchestrating agent reviews the diff, runs builds, tests and coverage; then the Codex CLI reviews the step read-only before the user is asked to commit; confirmed findings are fixed by qwen test-first, rejected findings are reported with the reason. Step plans get their own Codex review before the step starts.

## 8. Files and documentation maintenance

New records under `docs/p4/`; production code under `src/WeatherParser.Parsing/<Folder>/` (Proposed folders `Geography/`, `Catalogs/`, `Selection/`); tests under `tests/WeatherParser.Parsing.Tests/` and `tests/WeatherParser.Package.Tests/`; synthetic fixtures only (no unchecked third-party copies); AGENTS.md consistency table applies (new package → `DEPENDENCIES.md`, `Directory.Packages.props` and lock files; README changes → package check of CONTRIBUTING.md section 4); public API and DI usage documented in README.md; phase status changes update the README status summary, `PHASE_P1_PLAN.md`, `DEVELOPMENT_PLAN.md` and this plan; a README change needs the CONTRIBUTING.md section 4 package check and the local CI script run on the committed revision. The contract record `docs/p4/P4_SELECTION_CONTRACT.md` and the research manifest under `docs/p4/research/` are added. Changes to existing P2 code are allowed only where the contract requires them (`src/WeatherParser.Parsing/Export/MosmixProductAvailability.cs`, `tools/WeatherParser.SchemaGeneration/`, the regenerated `schemas/export/v1/weatherparser-export-v1.xsd`, `ParserEndpointValidationException` gaining `ErrorId`), with the AGENTS.md consistency table (new package and DI version raise → `Directory.Packages.props`, lock files, `DEPENDENCIES.md`).

## 9. P4 acceptance and P5 handoff checklist

- [x] P4.01 foundation check recorded (2026-10-21, outcome READY)
- [x] selection contract approved by the user after Codex review
- [ ] every mandatory T-P4 row has passing tests and recorded evidence; the optional live check T-P4-18 is either passed or recorded as skipped with its limitation
- [ ] coordinate, filter, catalog and availability behavior matches REQ-020–024 and REQ-100–102
- [ ] both list roots serialize equivalently and validate against the v1 schema
- [ ] no export contract change without a recorded additive decision
- [ ] DI and same-run package consumption proven
- [ ] coverage gate passed on Windows and the NanoPi R76S, platform outcomes separate
- [ ] limitations with owners
- [ ] user acceptance recorded
- [ ] status documents consistent
- [ ] pull request ready for review, review findings resolved test-first, merged into `develop` by the user
- [ ] P5 handoff written
- [ ] P5 handoff records the SC-21 reuse obligations for P5 and P7

## 10. Plan authorship and review

Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI; findings verified by the orchestrating agent and fixed by qwen; the first Codex review round reported 7 findings, including deferred DI registration and missing checkpoints, which were fixed in one round of fixes and two correction rounds, followed by a second Codex review reporting 0 findings. The plan was later changed by the cross-plan Codex review of all P4 plans (rounds 5, 2, 0 findings): a "List metadata and time source" decision row was added to section 3 and the section 9 checklist now treats the live check T-P4-18 as optional (passed or recorded as skipped). Nothing is implemented by this plan.

On 2026-10-21 (P4.02 S6) qwen3-coder:30b reconciled this plan with the approved selection contract from the orchestrating agent's change list; the result was reviewed by the OpenAI Codex CLI in the five-angle format (rounds 3, 2, 1, 0 findings; round 3 ran on an unchanged file after an applier failure and repeated a round-2 finding). The orchestrating agent removed an editing artifact and a duplicated status line itself. The cross-plan review of all P4 plans (rounds 4, 3, 1, 1, 0) then added P4.05 to the P4.07 dependencies and named the contributing steps in the T-P4-10 and T-P4-19 rows.
