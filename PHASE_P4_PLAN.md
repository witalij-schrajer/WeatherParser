# WeatherParser – P4 implementation plan

**Status (2026-10-20): plan only; nothing is implemented or verified. P2 is complete (merge commit `74bf625`); P4 started on 2026-10-20 on the user's instruction.**
Refines the P4 section of [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) using the [P4 handoff](docs/p2/P4_HANDOFF.md); binding rules in [AGENTS.md](AGENTS.md) and [REQUIREMENTS.md](REQUIREMENTS.md).

## 1. First step: check P2 completion and adjust this plan

P4.01 is always the first execution step: verify P2 acceptance and merge from `docs/p2/P2_12_ACCEPTANCE.md` (sections 10 and 12) and the P4 handoff; extract the integration target SHA, package identity `WeatherParser.Parsing` `0.1.0-initial`, SDK/tool versions, the frozen export contract v1 and limitations; remeasure the native block coverage of the integration target under comparable conditions as the comparison baseline; update this plan with the extracted facts before P4.02. If P2 were incomplete: record the gap, do not implement P4.

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
- DWD observation retrieval/parsing → P5
- OsthessenWetter → P6
- MOSMIX forecast parsing, KML/KMZ, input schema `pointforecast_dwd_extension_V1_0.xsd` → P7
- MQTT serializer/topics → P8
- publication → P9

No change of export contract v1 except additive optional members with a recorded decision (C-03).

## 3. Decisions before the affected implementation

Decisions are recorded in `docs/p4/P4_SELECTION_CONTRACT.md` (Proposed name) in P4.02, reviewed by Codex read-only (up to 10 rounds) and approved by the user before the affected Red test; source formats are taken only from official DWD documentation or the actual published files, with URL and check date recorded; P4.02 also records access conditions, attribution and retrieval-frequency guidance for every catalog and availability endpoint (REQ-092); unresolved restrictions block live retrieval.

| Decision | Required resolution / recommendation | Decided by | Blocks |
| --- | --- | --- | --- |
| CDC catalog source and format | Which official DWD CDC station description file(s) define station ID, name, coordinates, altitude and active period; exact column layout, encoding and ID width from the actual documented format (OPEN until verified in P4.02) | User (contract review) | P4.05–06 |
| CDC availability derivation | Which DWD products/metadata prove a quantity per station (REQ-024), how the REQ-052 names of C-21 map to products, and the rule for inactive stations (Recommended: exclude stations whose metadata shows no current data instead of listing them as suitable) | User (contract review) | P4.06 |
| MOSMIX catalog source and format | Official DWD MOSMIX location catalog: ID, name, coordinate and altitude format (for example degrees and minutes), encoding; missing altitude handling per REQ-102 (OPEN until verified) | User (contract review) | P4.07 |
| MOSMIX per-product availability | How availability per `MOSMIX_S`/`MOSMIX_L` and the verified element list are derived; Recommended default: `unknown` with an empty element list when no location-specific evidence exists (C-23) | User (contract review) | P4.07 |
| Geodetic method | Documented WGS84 method and tolerance (Recommended: a named, cited method with published reference distances for tests; OPEN between ellipsoidal and spherical) | User (contract review) | P4.04 |
| Coordinate grammar | Accepted decimal and DMS spellings (degree, prime/apostrophe, double prime/quote symbols, whitespace, hemisphere letters), sign/hemisphere conflicts, ranges; invariant culture | User (contract review) | P4.03 |
| Logging of notices | Empty-result and excluded-location notices: abstraction and package (Recommended: `Microsoft.Extensions.Logging.Abstractions` after version/license check, with stable notice IDs, written by the consumer's logger) | User (contract review) | P4.07, P4.09 |
| Retrieval boundary | HTTP abstraction (`HttpClient` from the consumer or `IHttpClientFactory`), endpoint parameters per catalog/product, timeout, maximum download size, retries (bounded, Recommended none in the library), cancellation and error types | User (contract review) | P4.08 |
| Catalog failure semantics | Fail or skip for malformed records, diagnostics, behavior when all records are invalid, and when availability metadata is missing or unreadable; a valid empty selection must stay distinguishable from retrieval, format and availability-derivation failures (P2 list roots carry no error collection) | User (contract review) | P4.05–09 |
| Public selection API | Interfaces, input type for search parameters, result types (Proposed `ICdcStationSelector`, `IMosmixLocationSelector` returning the P2 list roots), error/exception types | User (contract review) | P4.09–10 |
| List metadata and time source | The `source` literal of each list root (C-18 leaves the literals to P4), the time abstraction for `retrieved_at` (for example the BCL `TimeProvider` abstract class or a library interface), who registers it (library or consumer), when it is sampled during a selection, and its whole-second conversion for `ExportTimestamp.FromUtc` | User (contract review) | P4.06, P4.09 |
| Service registration under C-107/C-108 | How services with consumer-supplied endpoint `Uri` parameters are registered when C-107 forbids factory and instance registrations (for example a typed endpoint options class registered by the consumer, or a recorded additive amendment of C-107); every new public interface registered in the step that adds it | User (contract review) | P4.03–10 |
| Live verification | Whether and how an explicitly enabled live check against the real DWD catalogs is run and recorded separately from standard tests | User | P4.11 |

## 4. Behavior contracts

| Area | Required behavior |
| --- | --- |
| Coordinates | REQ-021: decimal and DMS, N/S/E/W, ranges, minutes/seconds 0 ≤ x < 60, nonfinite, wrong hemisphere and conflicting sign rejected, culture independent, English messages |
| Search parameters | REQ-020/022: altitude may be negative, both limits ≥ 0 and finite, values carried unchanged into `SearchParameters` |
| Distance and filters | REQ-022: documented WGS84 method, both limits inclusive, absolute altitude difference filtered separately, signed `altitude_difference_m` = entry minus reference |
| CDC catalog | REQ-023/024: IDs as strings with leading zeros, umlauts preserved, format per documentation, malformed records handled per the decided failure semantics, never guessed; a format failure never yields a successful empty list |
| CDC availability | REQ-024: only verified quantities in `available_measurements`, catalog membership no proof, inactive stations not suitable |
| MOSMIX catalog | REQ-100–102: own catalog and IDs, alphanumeric and leading-zero IDs, never a CDC ID, missing altitude never zero, unfilterable locations excluded with a notice, no CDC fallback |
| MOSMIX availability | C-23: one item per product, `unknown` explicit, elements only when `available` |
| Retrieval | REQ-014: endpoints as constructor `Uri` parameters validated without retrieval, no built-in or fallback URLs, bounded timeout and size, cancellation, failures surfaced with actionable English messages, no fabricated data |
| Lists | REQ-023/101: `schema_version`, source, UTC `retrieved_at` from an injected time source, converted to the whole-second representation that `ExportTimestamp.FromUtc` accepts (the conversion rule is decided in P4.02 and tested with a clock value that has fractional seconds), `SearchParameters`, ordering through `IListEntryOrdering`, valid empty list plus log notice, never a substitute |
| Exports | REQ-067–068: the same list graph serialized by both real serializers, valid against the v1 output schema |

Every class with executable logic has an interface and constructor injection; registration only in `AddWeatherParsing`; no global state.

## 5. Ordered implementation packages

| Step | Depends on | Content |
| --- | --- | --- |
| P4.01 – Foundation check and plan adjustment | P2 merge | Section 1; remeasure the integration-target coverage baseline; update this plan |
| P4.02 – Source facts and selection contract | P4.01 | Verify the CDC and MOSMIX catalog formats and availability sources from official DWD sources; record the section 3 decisions in `docs/p4/P4_SELECTION_CONTRACT.md`; Codex review loop; user approval; no production code |
| P4.03 – Coordinate parsing and search parameters | P4.02 | Coordinate parser and search-parameter validation service with interfaces, test-first; every step that adds a public service interface registers it in `AddWeatherParsing` in the same step and keeps `ServiceRegistrationCompletenessTests` green |
| P4.04 – Geodesy and inclusive filters | P4.02–03 | Distance calculator with the decided method and reference values; inclusive distance/altitude filter; altitude difference |
| P4.05 – CDC station catalog reader | P4.02 | Reader for the documented CDC catalog format from local streams with synthetic fixtures; IDs, names, coordinates, altitudes, active periods; malformed input |
| P4.06 – CDC availability derivation | P4.05 | Derive `available_measurements` and inactive-station handling from the decided product metadata |
| P4.07 – MOSMIX location catalog and product availability | P4.02, P4.04 | Reader for the documented MOSMIX catalog; coordinate/altitude normalization; missing-altitude exclusion with notice; per-product availability with explicit `unknown` |
| P4.08 – Catalog retrieval boundary | P4.05, P4.07 | Injectable retrieval with constructor endpoints, endpoint validation, bounded timeout/size, cancellation and error handling; tests with a mocked HTTP handler, offline |
| P4.09 – Selection services and list assembly | P4.03–08 | `CdcStationList`/`MosmixLocationList` assembly with search parameters, source, UTC retrieval time, ordering, empty result and notice; real JSON/XML and schema validation |
| P4.10 – DI registration and package consumption | P4.09 | Integrated DI configuration with consumer-supplied endpoints and independent configurations; package-consumer tests from the same-run `.nupkg` |
| P4.11 – Integrated coverage and platform verification | P4.03–10 | Committed candidate through `scripts/ci-local.ps1` on Windows, hosted CI, NanoPi R76S evidence (with authorization); optional separately enabled live check; READY/NOT_READY |
| P4.12 – Acceptance and P5 handoff | P4.11 | Evidence matrix, limitations, user acceptance, status updates, P5 handoff; pull request ready for review, review fixes, merge by the user |

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
| T-P4-07 | REQ-024 | CDC availability from product metadata; membership no proof; inactive stations not suitable | P4.06 |
| T-P4-08 | REQ-100–102, REQ-082 | MOSMIX catalog: alphanumeric and leading-zero IDs, coordinate normalization, missing altitude excluded with notice | P4.07 |
| T-P4-09 | REQ-101–102 | Per-product availability, explicit `unknown`, no CDC fallback | P4.07 |
| T-P4-10 | REQ-014, REQ-102 | Constructor endpoints validated without retrieval, no fallback, bounded timeout/size, cancellation, surfaced failures | P4.08 |
| T-P4-11 | REQ-023, REQ-068 | CDC list assembly: metadata, search parameters, ordering, empty list with notice, no substitute; retrieval time with fractional seconds | P4.09 |
| T-P4-12 | REQ-101, REQ-068 | MOSMIX list assembly: same rules | P4.09 |
| T-P4-13 | REQ-067–068, REQ-082 | Both lists through real JSON/XML serializers, equivalence, schema-valid, leading zeros, umlauts | P4.09 |
| T-P4-14 | REQ-013, REQ-014, REQ-016 | DI with consumer endpoints, independent configurations, same-run package consumer | P4.10 |
| T-P4-15 | REQ-085 | Recorded Red → Green → Refactor evidence per slice | P4.03–10 |
| T-P4-16 | REQ-086 | Native block coverage ≥ 80 %; strict improvement against the remeasured integration target below 100 %, exact 100 % accepted only at an exact 100 % baseline; unrounded native counter ratios | P4.11 |
| T-P4-17 | REQ-017, REQ-084 | P4 behavior on the NanoPi R76S (Debian 13 ARM64), separate from Windows | P4.11 |
| T-P4-18 | REQ-083, REQ-092 | Optional live catalog check, explicitly enabled and recorded separately | P4.11 |

## 7. Execution verification and evidence

Records in `docs/p4/P4_VERIFICATION.md` per step (Red names and observed failures, commands, test counts, coverage counters, authorship, Codex review runs and findings); commands from CONTRIBUTING.md sections 2–7 (`dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`, `pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`); hosted CI results per push; NanoPi evidence separate from Windows; mocks and model output are no evidence of live or hardware success.

Branch `features/develop/P4` from `develop`; open the draft pull request into `develop` directly after the first pushed commit; push every further commit to it so hosted CI runs on every push; commits and pushes only on the user's request.
Per implementation step: qwen generates tests and code from briefs; the orchestrating agent reviews the diff, runs builds, tests and coverage; then the Codex CLI reviews the step read-only before the user is asked to commit; confirmed findings are fixed by qwen test-first, rejected findings are reported with the reason. Step plans get their own Codex review before the step starts.

## 8. Files and documentation maintenance

New records under `docs/p4/`; production code under `src/WeatherParser.Parsing/<Folder>/` (Proposed folders `Geography/`, `Catalogs/`, `Selection/`); tests under `tests/WeatherParser.Parsing.Tests/` and `tests/WeatherParser.Package.Tests/`; synthetic fixtures only (no unchecked third-party copies); AGENTS.md consistency table applies (new package → `DEPENDENCIES.md`, `Directory.Packages.props` and lock files; README changes → package check of CONTRIBUTING.md section 4); public API and DI usage documented in README.md; phase status changes update the README status summary, `PHASE_P1_PLAN.md`, `DEVELOPMENT_PLAN.md` and this plan; a README change needs the CONTRIBUTING.md section 4 package check and the local CI script run on the committed revision.

## 9. P4 acceptance and P5 handoff checklist

- [ ] P4.01 foundation check recorded
- [ ] selection contract approved by the user after Codex review
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

## 10. Plan authorship and review

Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI; findings verified by the orchestrating agent and fixed by qwen; the first Codex review round reported 7 findings, including deferred DI registration and missing checkpoints, which were fixed in one round of fixes and two correction rounds, followed by a second Codex review reporting 0 findings. The plan was later changed by the cross-plan Codex review of all P4 plans (rounds 5, 2, 0 findings): a "List metadata and time source" decision row was added to section 3 and the section 9 checklist now treats the live check T-P4-18 as optional (passed or recorded as skipped). Nothing is implemented by this plan.
