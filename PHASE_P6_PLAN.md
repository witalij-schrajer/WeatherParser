# Phase P6 – OsthessenWetter Observations: Implementation Plan

**Status: P6 started on 2026-10-25 on the user's instruction (branch `features/develop/P6` from `develop` `8d8079a`); P6.01 (foundation check) in progress. Planning (2026-10-25): phase plan and step plans P6.01 to P6.11 written and Codex-reviewed to 0 findings (section 10); the step plans are reconciled with the contract only after its approval in P6.02 (planning gate, section 5). The decisions in section 3 are proposals; they become binding only through the user's approval of the OsthessenWetter contract in P6.02.** This plan refines the P6 section of [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) on the foundation of the [P6 handoff](docs/p5/P6_HANDOFF.md). Planning input: [P6 planning check](docs/p6/research/P6_PLANNING_PAGE_CHECK.md) (page structure and source conditions as retrieved on 2026-10-25).

## 1. First step: check P5 completion and adjust this plan

P6.01 is always the first execution step:
- verify the P5 acceptance and merge from `docs/p5/P5_11_ACCEPTANCE.md` (sections 6 and 8) and the P6 handoff;
- record the integration target: the `develop` head at P6 branch creation, which must be the P5 merge `29fd5533449898f1b14513b11c65a0afa1cf049a` or a documentation-only successor (for example after the post-merge documentation pull request #8); every commit since `29fd553` is classified;
- extract the package identity `WeatherParser.Parsing` `0.1.0-initial`, the SDK and tool versions, the frozen export contract v1 (C-01 to C-119), the selection contract (SC-01 to SC-23), the observation contract (OC-01 to OC-14) and the inherited limitations with their owners;
- check the SDK servicing status;
- remeasure the native block coverage of the integration target (expected 7491/7494) with `ci-local.ps1` on the first committed revision of the P6 branch;
- record everything in `docs/p6/P6_01_FOUNDATION_CHECK.md` and adjust this plan where the extraction contradicts it. P6.01 adds a table "P6.01 extraction" to this section.

## 2. Goal, binding scope and exclusions

Goal: a library service that retrieves the configured OsthessenWetter page once, selects every station whose displayed name matches a consumer-supplied glob pattern and returns one P2 `observation_dataset` per matched station, with inferred dates marked `estimated_date`, proven offline with synthetic HTML fixtures and the real serializers.

In scope:
- the station name pattern of REQ-040 (case-insensitive glob with `*` and `?`, whole-name match without wildcards), validated before any request
- one bounded retrieval of the consumer-supplied page URL; an unavailable page fails with an actionable English diagnostic (REQ-046)
- reading the overview and the wind table of the documented page revision (REQ-045), with format deviations detected and logged (REQ-044)
- mapping of the displayed quantities that have a REQ-052 field, with verified units and periods; gusts and totals never relabelled (REQ-044)
- date inference from the retrieval time in `Europe/Berlin` through `ISourceTimeNormalizer.InferDate`, marked `estimated_date`, with midnight and DST handling (REQ-042, REQ-043, C-30, C-31)
- station identity, collision-free source IDs (REQ-041, C-50) and the location coordinates the export contract requires (C-51)
- every matched station processed; no match fails; offline stations detected (REQ-041, REQ-044)
- dataset assembly through `IObservationDatasetBuilder`, export equivalence and schema validation
- DI registration, README consumer guidance and package-consumer evidence
- clarification status of the source conditions (REQ-092)
- integrated coverage and platform verification, acceptance and P7 handoff

Out of scope with owner:
- the CLI option `--ParseOstHessen`, `StationNamePattern` as a CLI argument, configuration, exit codes and output modes → P3
- MQTT messages and stable topic IDs for each station (REQ-041 "individual messages", "collision-free topic IDs") → P8; P6 provides the collision-free `source` and `station_id` that P8 builds on
- MOSMIX forecasts → P7
- publication → P9
- the other sections of the page (daily extremes, snow line, precipitation statistics, diagrams, iframes) and the other pages of the site → not read
- the displayed quantities without a REQ-052 field (`Temp.-Trend`, `Tmax/Tmin`) → not exported; recorded in the contract (OW-06)
- contacting the site operator about the source conditions → the user (section 3, OW-14)

No change of export contract v1 except additive optional members with a recorded decision (C-03).

## 3. Decisions before the affected implementation

Decisions are recorded in `docs/p6/P6_OSTHESSEN_CONTRACT.md` (decision IDs `OW-01` to `OW-16`) in P6.02, reviewed by Codex read-only (up to 10 rounds) and approved by the user before the affected Red test. Source facts come only from the actual published page, retrieved and recorded with URL, date and SHA-256 (REQ-045); the planning check of 2026-10-25 is input, not the record. The rows below are planning proposals; the contract may refine them, and where it does, the approved contract wins.

| ID | Decision | Options / recommendation | Decided by | Blocks |
| --- | --- | --- | --- | --- |
| OW-01 | Supported page revision | The `/messwerte/` page as retrieved in P6.02: the overview table and the wind table identified by their header labels (planning check section 2); the implementation date, URL, SHA-256 and every structural assumption the parser relies on recorded; support applies to this revision (REQ-045) | User (P6.02) | P6.04 |
| OW-02 | HTML parsing | A: an HTML5-conformant parser package (candidates AngleSharp and HtmlAgilityPack, both MIT; licence, maintenance, .NET 10 compatibility and the latest stable version verified from official sources in P6.02, pinned in `Directory.Packages.props` with `DEPENDENCIES.md` updated); no scripts, no styles, no resource loading, no network access from the parser / B: an own bounded tokenizer. Recommended A: the page is not well-formed and uses legacy character references without semicolons (planning check), which only the HTML5 rules decode correctly; an own tokenizer would reimplement those rules | User (P6.02) | P6.04 |
| OW-03 | Station identity and source ID | The page has no IDs. A: `station_id` is a slug of the overview-table name: Unicode NFC, lower case invariant, `ä`→`ae`, `ö`→`oe`, `ü`→`ue`, `ß`→`ss`, every other run of characters outside `[a-z0-9]` → `-`, trimmed; `source` `osthessenwetter:<slug>` (C-50); the original name is kept in `location.name` (REQ-041); two names with the same slug fail the parse with `OSTHESSEN-STATION-ID-COLLISION` / B: the original name as `station_id`. Recommended A (stable, ASCII, collision-checked) | User (P6.02) | P6.05, P6.07 |
| OW-04 | Coordinates (C-51) | The page shows no coordinates, and C-51 requires them. A: consumer-supplied station metadata in the options: a list of entries with the original name (exact, after NFC), latitude, longitude and optional altitude, validated at construction; a matched station without an entry fails that station with `OSTHESSEN-STATION-METADATA-MISSING` (OW-08) / B: coordinates built into the library (rejected: unverified data, not traceable) / C: an export contract change that makes coordinates optional (rejected: not additive, C-03). Recommended A | User (P6.02) | P6.05, P6.07, P6.09 |
| OW-05 | Joining the two tables | The rows differ in order and in name spelling (`Roßbach`/`Rossbach`, a malformed reference that leaves a trailing `;`). A: the overview table is the station list and the pattern matches its names; a wind row is joined when exactly one wind row has the same join key (NFC, invariant case fold, `ß`→`ss`, trailing `;` and whitespace removed); no or several candidates → the wind readings of that station are omitted with notice `OSTHESSEN-WIND-ROW-UNMATCHED` / B: join by position (rejected: the order differs) | User (P6.02) | P6.04, P6.05 |
| OW-06 | Quantities, units and periods | Exported: `Temp.2m` → `Temperature` (°C, instantaneous, overview row time); `Luftfeuchte` → `RelativeHumidity` (%, overview row time); `Wind Ø 10 Min.` → `MeanWindSpeed` in `KilometresPerHour` with the period `[t − 10 min, t]` at the wind row time t. Not exported: `Spitzenböe` and both gust columns (`WindGust`, never wind speed, REQ-044); `Wind Ø 60 Min.` (one wind speed field; the 10-minute mean is kept consistent with DWD, OC-05); `Niederschlag` (A: omitted while its reference period is unverified, never exported as a rate, REQ-055 / B: `PrecipitationAccumulation` over `[local midnight, t]` if P6.02 can verify the period from the source); `Temp.-Trend`, `Tmax/Tmin` (no REQ-052 field). Units are read from the displayed unit suffix and checked; a different suffix is a format deviation. Recommended as stated with precipitation option A | User (P6.02) | P6.05 |
| OW-07 | Times | The displayed `HH:MM Uhr` per row → `TimeOnly`; `ISourceTimeNormalizer.InferDate(time, retrievedAtUtc)` → UTC with `timestamp_quality` `estimated_date` and `source_timezone` `Europe/Berlin` (REQ-042, C-30 midnight rule with its 5-minute tolerance, C-31 DST rejection, C-32 markers); the overview time applies to the overview readings, the wind time to the wind reading (REQ-056); a missing or unreadable time omits that row's readings with reason `MissingTime` (C-59) and notice; a DST rejection omits that row's readings with notice `OSTHESSEN-TIME-UNRESOLVED`; no freshness filter (REQ-043) | User (P6.02) | P6.05 |
| OW-08 | Public API and partial failure | A: `IOsthessenWetterParser.ParseAsync(string stationNamePattern, CancellationToken)` returns `OsthessenWetterParseResult` with `Datasets` (one per usable matched station, in page order) and `StationFailures` (original name, error ID, reason) for matched stations that produce no dataset (offline, metadata missing, no usable reading); no match → `OsthessenWetterException` `OSTHESSEN-PATTERN-NO-MATCH`; matches but no dataset → `OSTHESSEN-NO-USABLE-STATION` carrying the failures / B: `Task<IReadOnlyList<ObservationDataset>>` with failures only logged / C: all-or-nothing (any failed station fails the parse). Recommended A: every match stays visible to the consumer (REQ-041 "do not silently reduce"), and REQ-050 holds because a station without usable readings is never a dataset | User (P6.02) | P6.07 |
| OW-09 | Pattern grammar | `*` any number of characters, `?` exactly one character (one Unicode scalar after NFC), every other character literal (no regex, no character classes, no escapes); case-insensitive by invariant case folding; matched against the whole NFC name; empty, whitespace-only or longer than 128 characters → `OSTHESSEN-PATTERN-INVALID` before any request; matching takes at most O(pattern length × name length) steps, never exponential backtracking (with the 128-character pattern bound) | User (P6.02) | P6.03 |
| OW-10 | Retrieval and options | Consumer-supplied `OsthessenWetterEndpoints` (`Uri PageUri`, absolute HTTPS or HTTP without user information, validated at construction without retrieval; diagnostics name the endpoint without its query) and `OsthessenWetterOptions` (`PageDeadline` default 30 s, permitted (0, 1 h], and the station metadata of OW-04); one GET per parse through the bounded retrieval boundary (`ICatalogDownloader` with an additive `CatalogDeadlineKind.OsthessenWetter`, P4 and P5 behavior unchanged), at most 2 MiB, no retries (SC-09); any non-success status, a timeout or a transport failure → `OSTHESSEN-PAGE-UNAVAILABLE` with an actionable English message naming the endpoint and the status, never replacement data (REQ-046); deadline expiry → `OSTHESSEN-DEADLINE-EXCEEDED`; caller cancellation first | User (P6.02) | P6.06, P6.09 |
| OW-11 | Format deviations | Overview table not found, its header labels changed, or a row without the documented cells → `OSTHESSEN-FORMAT-CHANGED` (failure, REQ-044, REQ-045); wind table not found or changed → notice `OSTHESSEN-WIND-TABLE-MISSING`, wind readings omitted; an unreadable number or an unexpected unit in one cell → that reading omitted with notice `OSTHESSEN-VALUE-UNREADABLE`; numbers parsed culture-invariantly with `.` as the separator; page text is never logged beyond station names (the page can contain error text with host names, planning check section 2) | User (P6.02) | P6.04, P6.05 |
| OW-12 | Offline and stale stations | The status image title `Station offline` → the station fails with `OSTHESSEN-STATION-OFFLINE` (OW-08), even if values are shown; `Station online` with values is processed; any other or missing status → `OSTHESSEN-FORMAT-CHANGED`; stale displays are not filtered (REQ-043); a displayed time more than 5 minutes later than the local retrieval time is placed on the previous day by C-30, after the C-31 DST checks; older displays are not detectable (documented limitation) | User (P6.02) | P6.04, P6.07 |
| OW-13 | Error and notice IDs | `OsthessenWetterException` with `OSTHESSEN-PATTERN-INVALID`, `OSTHESSEN-PATTERN-NO-MATCH`, `OSTHESSEN-PAGE-UNAVAILABLE`, `OSTHESSEN-DEADLINE-EXCEEDED`, `OSTHESSEN-FORMAT-CHANGED`, `OSTHESSEN-STATION-ID-COLLISION`, `OSTHESSEN-NO-USABLE-STATION`, `OSTHESSEN-OPTIONS-INVALID`; station failure IDs `OSTHESSEN-STATION-OFFLINE`, `OSTHESSEN-STATION-METADATA-MISSING`, `OSTHESSEN-STATION-NO-READING`; notices `OSTHESSEN-WIND-TABLE-MISSING`, `OSTHESSEN-WIND-ROW-UNMATCHED`, `OSTHESSEN-VALUE-UNREADABLE`, `OSTHESSEN-TIME-UNRESOLVED`, `OBSERVATION-READING-OMITTED` (reused from OC-10) | User (P6.02) | P6.03–P6.07 |
| OW-14 | Source conditions (REQ-092) | Facts: no licence for the values; the imprint reserves the author's copyright and forbids reproduction in other publications without express consent; robots.txt does not restrict crawling; no request-frequency rule. Proposal: the library is implemented and tested with synthetic fixtures only; README states the conditions, the attribution "Osthessen-Wetter.de" and a project recommendation of at most one retrieval every 10 minutes, scheduled externally; production use and redistribution (for example MQTT) are not accepted until the user records the operator's consent or other clarified conditions. Whether and how to contact the operator is the user's decision | User (P6.02) | P6.09, P6.11 |
| OW-15 | Live verification | Optional, explicitly enabled (`WEATHERPARSER_LIVE_CHECK_MODE=osthessen`), one request, recorded separately (pattern of SC-16 and the P5 live check); run only with the user's authorization in P6.10; the retrieved page is not committed | User (P6.02) | P6.10 |
| OW-16 | Dataset values | `source` `osthessenwetter:<slug>`; `location` from the overview name and the OW-04 metadata; `retrieved_at` from the consumer's `TimeProvider`, read once per parse and truncated to whole seconds; dataset `measured_at` the latest exported reading (C-55); `timestamp_quality` `estimated_date` (C-56); no `quality` member (the page shows no quality code); export contract unchanged | User (P6.02) | P6.07, P6.08 |

Names marked (Proposed) in this plan and in the step plans (interfaces, folders, test helpers) are implementation names that no OW row fixes; the orchestrating agent decides them in the step and records them, as in P5.

## 4. Behavior contracts

| Area | Required behavior |
| --- | --- |
| Pattern | REQ-040, OW-09: validated first; glob semantics only; whole-name match; culture-independent; no request before validation |
| Retrieval | REQ-046, REQ-063, SC-09, OW-10: consumer endpoint validated at construction without retrieval; consumer `HttpClient`; bounded size and time; one request; no retries; unavailable page → actionable failure; caller cancellation, deadline and timeout classified in that precedence |
| Page reading | REQ-044, REQ-045, OW-01, OW-02, OW-11: the documented tables found by their labels; malformed markup and legacy character references decoded by HTML5 rules; deviations fail or are noticed as defined; no scripts or resources loaded; synthetic fixtures only |
| Station set | REQ-041, OW-03, OW-05, OW-08, OW-12: every matched station processed; no match fails; offline stations reported; collision-free IDs; original names with umlauts kept |
| Values | REQ-053, REQ-044, OW-06: zero is a real value; empty cells omitted; gusts and totals never relabelled; C-42 validation; every omission carries its reason |
| Units and periods | REQ-052, REQ-055, OW-06: displayed unit suffixes checked; wind speed in km/h with its 10-minute period; no rate from a total without a verified period; rounding only for output |
| Times | REQ-042, REQ-043, REQ-056, C-30 to C-32, OW-07: per-table row times; date inferred in `Europe/Berlin` and marked `estimated_date`; midnight rule; DST gaps and overlaps never resolved arbitrarily; no retrieval-time substitution; no freshness filter |
| Dataset | REQ-050, C-50 to C-60, OW-16: one dataset per usable station; never a dataset without usable readings |
| Export | REQ-067–069, C-64: the same dataset graph through both real serializers; valid against the v1 schema |
| Logging | REQ-044, REQ-072: stable notice and error IDs, UTC, the source and the station name; omitted readings with kind and reason; no page content, measurement values or secrets in logs |
| Source conditions | REQ-092, OW-14: documented; production acceptance only with clarified conditions |

Every class with executable logic has an interface and constructor injection; registration only in `AddWeatherParsing`; no global state. Every step that adds a public service interface registers it in `AddWeatherParsing` in the same step and keeps `ServiceRegistrationCompletenessTests` green; P6.09 verifies the integrated and the packaged consumption.

## 5. Ordered implementation packages

| Step | Depends on | Content |
| --- | --- | --- |
| P6.01 – Foundation check and plan adjustment | P5 merge | Section 1; remeasure the integration-target baseline; update this plan |
| P6.02 – Source facts and OsthessenWetter contract | P6.01 | Retrieve and record the page revision; verify the structure, units and source conditions; evaluate the HTML parser package; record OW-01 to OW-16 in `docs/p6/P6_OSTHESSEN_CONTRACT.md`; Codex review loop; user approval; reconcile the step plans P6.03 to P6.11; no production code |
| P6.03 – Station name pattern | P6.02 | `IStationNamePatternMatcher`/`StationNamePatternMatcher` (Proposed): validation and bounded-time (O(pattern × name)) glob matching per OW-09; registration |
| P6.04 – Page reading | P6.02 | The HTML parser dependency (OW-02) and `IOsthessenWetterPageReader`/`OsthessenWetterPageReader` (Proposed): the two tables → raw station rows (status, name, time text, cell texts) with format-deviation detection (OW-11, OW-12) and the join of OW-05; synthetic fixtures; registration |
| P6.05 – Quantity mapping, identity and times | P6.03, P6.04 | `IOsthessenWetterObservationMapper` (Proposed): rows → `ObservationReading` per OW-06 with unit checks, the wind period, `InferDate` times (OW-07), slugs and collisions (OW-03), metadata lookup (OW-04); registration |
| P6.06 – Retrieval boundary | P6.02 | `IOsthessenWetterPageSource` (Proposed) over `ICatalogDownloader`: one bounded request, the additive `CatalogDeadlineKind.OsthessenWetter`, unavailable-page failures, options validation (OW-10); registration |
| P6.07 – Parser service and dataset assembly | P6.03–P6.06 | `IOsthessenWetterParser`/`OsthessenWetterParser` (OW-08): validate, retrieve, read, match, map, build through `IObservationDatasetBuilder` with provider `osthessenwetter`, station failures, notices and omissions logged; deadline, cancellation and failure precedence at parser level; registration |
| P6.08 – Export equivalence and schema validation | P6.07 | The produced datasets through both real serializers: equivalence, schema validation, UTC forms, `estimated_date` markers, `source_timezone`, the wind period, umlauts |
| P6.09 – DI registration, README and package consumption | P6.08 | Integrated DI with consumer options; README section "OsthessenWetter observations (library)" (source conditions, attribution, retrieval guidance, error and notice IDs); package-consumer tests from the same-run `.nupkg` |
| P6.10 – Integrated coverage and platform verification | P6.03–P6.09 | Candidate through `ci-local.ps1` on Windows, hosted CI, NanoPi R76S (with authorization); optional separately enabled live parse (with authorization); READY/NOT_READY |
| P6.11 – Acceptance and P7 handoff | P6.10 | Evidence matrix T-P6-01 to T-P6-19, limitations including the source-condition status, user acceptance, status updates, P7 handoff; pull request ready for review, review fixes, merge by the user |

Step plans are stored as `docs/p6/P6_NN_<SHORT_NAME>_PLAN.md`; each is written by the orchestrating agent and reviewed by Codex before the step starts (user decision dated 2026-10-24: documentation is not generated by qwen). All eleven were written on 2026-10-25. Planning gate (PR #8 review of `80dc47b`): P6 execution readiness is NOT_READY until every step plan below is Codex-reviewed to 0 findings; P6.03 onwards additionally waits for the reconciliation of the step plans with the approved P6.02 contract. Each step plan names its prerequisites, its ordered Red → Green → Refactor slices, the affected files, its OW decisions, its T-P6 IDs, its exit criteria and the evidence to record. The P6.01 foundation record maps every open item of `docs/p5/P5_11_ACCEPTANCE.md` section 4 and `docs/p5/P6_HANDOFF.md` section 7 to a P6 step and check or to a carried-forward owner (P6.01 plan S2); an unmapped item blocks P6 implementation readiness.

- [P6.01](docs/p6/P6_01_FOUNDATION_CHECK_PLAN.md)
- [P6.02](docs/p6/P6_02_SOURCE_FACTS_AND_CONTRACT_PLAN.md)
- [P6.03](docs/p6/P6_03_STATION_NAME_PATTERN_PLAN.md)
- [P6.04](docs/p6/P6_04_PAGE_READING_PLAN.md)
- [P6.05](docs/p6/P6_05_QUANTITY_MAPPING_PLAN.md)
- [P6.06](docs/p6/P6_06_RETRIEVAL_BOUNDARY_PLAN.md)
- [P6.07](docs/p6/P6_07_PARSER_SERVICE_PLAN.md)
- [P6.08](docs/p6/P6_08_EXPORT_EQUIVALENCE_PLAN.md)
- [P6.09](docs/p6/P6_09_DI_README_PACKAGE_PLAN.md)
- [P6.10](docs/p6/P6_10_INTEGRATED_VERIFICATION_PLAN.md)
- [P6.11](docs/p6/P6_11_ACCEPTANCE_HANDOFF_PLAN.md)

## 6. Requirement-to-test matrix

| Test ID | Requirement IDs | Behavior and boundary | Step |
| --- | --- | --- | --- |
| T-P6-01 | REQ-040 | Pattern grammar: `*` (zero, one, many characters), `?` (exactly one), literal metacharacters of other syntaxes (`[`, `]`, `.`, `+`, `\`), case-insensitive with umlauts (`müs` matches `Müs`), whole-name match without wildcards (`Müs` does not match `Müs-Hohhütte`), NFC and NFD input equal; empty, whitespace-only and over-long patterns rejected with `OSTHESSEN-PATTERN-INVALID` before any request; adversarial patterns (`*a*a*a*…b`, `*` followed by a long literal suffix) complete within the O(pattern × name) bound | P6.03 |
| T-P6-02 | REQ-045, REQ-044 | Page reading: both tables found by their labels; rows with status, name, time and cells; legacy character references (`&szlig`, `&uumltte;`) decoded as a browser does; unclosed and stray tags tolerated; missing overview table, changed header labels or missing cells → `OSTHESSEN-FORMAT-CHANGED`; missing wind table → notice; other page sections never read | P6.04 |
| T-P6-03 | REQ-041 | Table join: names that differ only in `ß`/`ss`, case or a trailing `;` joined; rows in different order joined by name; no or two candidate wind rows → `OSTHESSEN-WIND-ROW-UNMATCHED`, overview readings kept | P6.04, P6.05 |
| T-P6-04 | REQ-052, REQ-053, REQ-057, REQ-044 | Quantities: temperature °C (negative and zero kept), humidity %, 10-minute mean wind km/h with its period; empty cells omitted; unreadable numbers and unexpected units omitted with notice; numbers only in the grammar `-?[0-9]+(\.[0-9]+)?`, so `NaN`, `Infinity`, exponents and thousands separators are unreadable; through the real builder: humidity 0 and 100 kept, below 0 and above 100 (including 100.1, never rounded first) omitted with `OutOfRange`, wind 0 kept and negative wind omitted (never clamped), a digit string that overflows `double` omitted as `NonFinite`, the other readings of the row kept; rounding only on the wire with ties away from zero (20.25 → 20.3, −20.25 → −20.3, wind 12.5 → 13 km/h); gusts, the 60-minute mean, precipitation, trend and extremes never exported as another quantity | P6.05 (mapping), P6.08 (wire) |
| T-P6-05 | REQ-042, REQ-043, REQ-056 | Times: `HH:MM Uhr` per table row; date inferred in `Europe/Berlin`; `timestamp_quality` `estimated_date` on every reading and on the dataset (C-56), `source_timezone` `Europe/Berlin` on every reading (C-53; the dataset has no such member in v1); overview and wind times kept separately; missing or unreadable time → `MissingTime` omission; no freshness filter; retrieval time never substituted | P6.05, P6.07 |
| T-P6-06 | REQ-042, REQ-043, REQ-082 | Midnight and DST: displayed 23:58 retrieved at 00:02 local → previous day; displayed 00:00 retrieved at 00:01 → same day; the 5-minute tolerance boundary on both sides; a time in the spring-forward gap and in the autumn overlap → `OSTHESSEN-TIME-UNRESOLVED`, never an arbitrary interpretation; retrieval instants on both sides of each transition | P6.05 |
| T-P6-07 | REQ-041, REQ-050 | Identity: slug rules with umlauts and `ß`; `source` `osthessenwetter:<slug>`; original name kept in `location.name`; two names with one slug → `OSTHESSEN-STATION-ID-COLLISION`; IDs stable across runs | P6.05, P6.07 |
| T-P6-08 | REQ-041, C-51 | Coordinates: matched station with metadata → location with coordinates and altitude or none (never zero); without metadata → station failure `OSTHESSEN-STATION-METADATA-MISSING`; invalid metadata (range, duplicate names, empty name) → `OSTHESSEN-OPTIONS-INVALID` at construction | P6.05, P6.06 |
| T-P6-09 | REQ-046, REQ-063 | Retrieval: endpoint validated at construction; `PageDeadline` default 30 s accepted, the smallest positive value and exactly 1 hour accepted, zero, negative, `Timeout.InfiniteTimeSpan`, one tick above 1 hour and `TimeSpan.MaxValue` rejected with `OSTHESSEN-OPTIONS-INVALID` at construction, before any timer or request; one request; size bound; no retries; HTTP 404, 500 and 503, timeout and transport failure → `OSTHESSEN-PAGE-UNAVAILABLE` with an actionable English message; deadline → `OSTHESSEN-DEADLINE-EXCEEDED`; caller cancellation first; no replacement data | P6.06, P6.07 |
| T-P6-10 | REQ-041, REQ-044 | Station set: one match, several matches (all returned, page order), no match → `OSTHESSEN-PATTERN-NO-MATCH`; offline station → `OSTHESSEN-STATION-OFFLINE` in the failures; matches but no dataset → `OSTHESSEN-NO-USABLE-STATION`; never reduced to the first match; one composed parse with usable stations before and after an offline and a metadata-missing station returns every usable dataset and every station failure in page order from exactly one retrieval | P6.07 |
| T-P6-11 | REQ-050, C-55, C-56 | Dataset assembly: provider `osthessenwetter`, location, `retrieved_at` from `TimeProvider`, dataset `measured_at` the latest reading, `timestamp_quality` `estimated_date`; a station without usable readings → `OSTHESSEN-STATION-NO-READING`, never a dataset | P6.07 |
| T-P6-12 | REQ-051, REQ-067, REQ-068, REQ-069, REQ-079 | Each dataset through both real serializers without another retrieval: equivalent, schema-valid, UTC forms, markers, period, umlauts escaped correctly in XML | P6.08 |
| T-P6-13 | REQ-013, REQ-014, REQ-016, REQ-078 | DI with consumer endpoints and options, independent configurations, same-run package consumer; every new doing class behind an interface with constructor injection (`ServiceRegistrationCompletenessTests`, `NoSharedStateTests`) | P6.09 (integration and package); P6.03 to P6.07 (registration of each new interface) |
| T-P6-14 | REQ-090, REQ-092 | README OsthessenWetter section: source conditions and their clarification status, attribution, retrieval guidance, coordinates configuration, error and notice IDs; package check | P6.09 |
| T-P6-15 | REQ-085 | Recorded Red → Green → Refactor evidence per slice | P6.03–P6.09 |
| T-P6-16 | REQ-086 | Native block coverage ≥ 80 % and strict improvement against the remeasured integration target | P6.10 |
| T-P6-17 | REQ-017, REQ-084 | P6 behavior on the NanoPi R76S, separate from Windows, including the `Europe/Berlin` time-zone data on Debian | P6.10 |
| T-P6-18 | REQ-083, REQ-092 | Optional live parse of one pattern, explicitly enabled and authorized, one request, recorded separately; the page not committed | P6.10 |
| T-P6-19 | REQ-044, REQ-072, REQ-082 | Diagnostics on a successful parse, a parse with station failures and a total rejection: every notice, station failure and omission logged with its stable ID, UTC, the source and the station name, omissions with kind and reason and before a rejection propagates; synthetic sensitive text in other page sections (an error message with a host and a user name) and an endpoint with a query string never appear in logs or exception messages; an endpoint with user information is rejected at construction with `OSTHESSEN-OPTIONS-INVALID`; no measurement values or secrets in logs | P6.06 (endpoint), P6.07 |

## 7. Execution verification and evidence

Records in `docs/p6/P6_VERIFICATION.md` per step: Red names and observed failures, commands, test counts, coverage counters, authorship, Codex review runs and findings. Commands from CONTRIBUTING.md sections 2–7: `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`, `pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`. Hosted CI results per push; NanoPi evidence separate from Windows; mocks and model output are no evidence of live or hardware success.

Branch `features/develop/P6` from `develop`; open the draft pull request into `develop` directly after the first pushed commit; push every further commit to it so hosted CI runs on every push; commits and pushes only on the user's request.

Per implementation step: qwen generates tests and code from briefs; the orchestrating agent reviews the diff, runs builds, tests and coverage; then the Codex CLI reviews the step read-only before the user is asked to commit; confirmed findings in code, tests, scripts and data are fixed by qwen test-first, confirmed findings in documentation by the orchestrating agent; rejected findings are reported with the reason. Step plans get their own Codex review before the step starts.

Fixtures: synthetic HTML written for the tests (station names, values and times invented or taken from public place names), reproducing only the structural features of the documented revision (planning check section 2); no copy of the retrieved page in the repository (copyright, OW-14; REQ-083).

## 8. Files and documentation maintenance

New records under `docs/p6/`; production code under `src/WeatherParser.Parsing/Observations/Osthessen/` (Proposed, namespace `WeatherParser.Parsing.Observations.Osthessen`); tests under `tests/WeatherParser.Parsing.Tests/Observations/Osthessen/` and `tests/WeatherParser.Package.Tests/`; synthetic fixtures under `tests/WeatherParser.Parsing.Tests/Fixtures/Osthessen/` (Proposed); README section "OsthessenWetter observations (library)" in P6.09. A new package dependency (OW-02) updates `Directory.Packages.props`, the lock files and `DEPENDENCIES.md` in the same change (AGENTS.md consistency table). Status documents follow actual progress: the status line of this plan is updated at every step completion; the phase-status passages of README.md, AGENTS.md, DEVELOPMENT_PLAN.md and PHASE_P1_PLAN.md are updated when P6 starts (P6.01), when P6.09 turns the README feature status of OsthessenWetter parsing from planned to implemented, at the acceptance (P6.11) and after the merge; implementation, verification, acceptance and merge statuses stay distinct.

## 9. P6 acceptance and P7 handoff checklist

- [ ] P6.01 foundation check recorded
- [ ] OsthessenWetter contract approved by the user after Codex review
- [ ] every mandatory T-P6 row has passing tests and recorded evidence; the optional live check T-P6-18 is either passed or recorded as skipped with its limitation
- [ ] pattern, retrieval, page reading and normalization match REQ-040–046 and REQ-050–056
- [ ] all matching stations exported traceably; no match, format changes and unresolved times yield defined errors; estimated times visible (DEVELOPMENT_PLAN.md P6 acceptance)
- [ ] the datasets serialize equivalently and validate against the v1 schema
- [ ] no export contract change without a recorded additive decision
- [ ] DI and same-run package consumption proven
- [ ] coverage gate passed on Windows and the NanoPi R76S, platform outcomes separate
- [ ] source-condition status recorded; production acceptance stated as open unless the user has recorded clarified conditions (REQ-092)
- [ ] limitations with owners
- [ ] user acceptance recorded
- [ ] status documents consistent
- [ ] pull request ready for review, review findings resolved test-first, merged into `develop` by the user
- [ ] P7 handoff written

## 10. Plan authorship and review

Written by the orchestrating agent on 2026-10-25 from the P6 handoff, REQUIREMENTS.md, DEVELOPMENT_PLAN.md, the export, selection and observation contracts, the repository code and the planning check of the page; not generated by qwen (user decision dated 2026-10-24). Committed for review on the user's instruction on 2026-10-25 (`80dc47b`) before any Codex review. On the user's instruction the phase plan alone (not the step plans) was then reviewed read-only by the OpenAI Codex CLI in the five-angle audit format, four rounds on 2026-10-25 with 4, 1, 1 and 0 findings. Every finding was verified by the orchestrating agent against the requirements, the contracts and the code, confirmed and fixed by the orchestrating agent:
- round 1: T-P6-10 lacked a composed parse mixing usable and failed stations; T-P6-05 required `source_timezone` on the dataset, which v1 does not have (C-49, C-53); OW-12 omitted the 5-minute tolerance of C-30 (`SourceTimeNormalizer` `MidnightTolerance`); no matrix row covered diagnostic content and sanitization (added T-P6-19, and OW-10 now rejects endpoints with user information);
- round 2: T-P6-09 lacked the `PageDeadline` boundary and invalid-value cases (the OC-09 pattern of P5);
- round 3: T-P6-04 lacked numeric validation and rounding cases through the real builder (REQ-053, REQ-057, C-42);
- round 4: 0 findings.

The step plans P6.01 to P6.11 were written by the orchestrating agent on 2026-10-25 after the PR #8 review of `80dc47b` (verdict REQUEST CHANGES: missing step plans and limitation mapping, plus the two contract corrections already fixed in `a77ee93`). The review's risk register is reflected in them: P6.05 is split into five slices with their own files and exit checks; OW-02 and OW-04 are resolved first in P6.02; the source-condition status is carried through P6.02, P6.09 and P6.11; coverage is checked per slice. Codex five-angle review of the eleven step plans together with the new phase-plan passages (one cross-plan review, read-only), four rounds on 2026-10-25 with 8, 5, 2 and 0 findings, every finding verified by the orchestrating agent against the plans, the contracts and the code, confirmed and fixed by the orchestrating agent:
- round 1: T-P6-04 builder-level cases missing in P6.05; P6.07 collision scope contradictory; the shared consumer setups missing from the slices that add mandatory options; inconsistent metadata-lookup name; the matching bound claimed linear although the two-pointer algorithm is O(pattern × name) (OW-09 and T-P6-01 restated); no supplementary-scalar `?` test; P6.07 interruption tests only during retrieval; P6.08 planned deliberately broken expectations as Red (now characterization, as in P5.08);
- round 2: one remaining "linear-time" passage; composed `MissingTime` and old-display cases in P6.05; offline-row-with-values and old-display composed tests in P6.07; P6.09 Red for already implemented behavior (now characterization, as in P5.09); P6.07 Green batched after three Red slices (now per slice);
- round 3: metadata validation at source construction untested in P6.06; `ConsumerBoundary.cs` and the two `LiveCheckRunner.cs` containers missing from the consumer-setup updates;
- round 4: 0 findings.

Pull-request review of `608497d` (2026-10-25): the original planning finding was closed; 2 new findings [P2], both confirmed by the orchestrating agent against `CatalogDownloader` (`BoundedStream.Read` blocks in the inner read) and `ServiceRegistrationCompletenessTests`, and fixed: the P6.04 reader consumes the HTTP body only asynchronously (`ReadAsync`, with an asynchronous in-memory copy if the parser package needs synchronous input), with stalled-body tests through the real downloader in P6.06 and composed in P6.07; P6.04 and P6.07 register their service in S1, and P6.06, which had the same sequencing issue, too. Codex five-angle review of the fix: 1 round, 0 findings.

With these reviews the first part of the planning gate (section 5) is met; the reconciliation with the approved P6.02 contract remains before P6.03.
