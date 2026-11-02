# WeatherParser – P2 implementation plan

**Status (2026-10-20): P2 accepted by the user on 2026-10-20 ([P2 acceptance record](../p2/P2_12_ACCEPTANCE.md)); steps P2.01 to P2.11 are complete, and P2.12 is complete: pull request #3 was merged into `develop` on 2026-10-20 (merge commit `74bf625`); P2 is complete; P4 handoff: [P4_HANDOFF.md](../p2/P4_HANDOFF.md).** Original planning status (2026-10-19): planning only; P1 completion was checked and its accepted handoff incorporated below.

## 1. First step: check P1 completion, extract evidence and adjust this plan

**P2.01 is always the first execution step.** Read the [P1 detailed plan](PHASE_P1_PLAN.md), [P1 acceptance record](../p1/P1_12_ACCEPTANCE.md), [P2 handoff](../p1/P2_HANDOFF.md), and their linked completion records. Verify that P1.01–P1.12 and the whole P1 foundation are complete and accepted. A plan, existing file or passing intermediate run alone does not establish acceptance.

If P1 is incomplete, record the missing prerequisites and their owners, leave unavailable foundation fields unresolved, and keep P2 provisional. Do not implement P2, fabricate acceptance evidence or waive a failed gate. Planning may continue.

If P1 is complete, extract the accepted implementation and comparison SHAs, acceptance-record revision, package identity/hash, SDK/runtime/tool versions, commands, run artifacts, native coverage settings/counters, platform evidence and inherited limitations. **Update this plan with those facts before implementing P2:** replace stale assumptions, resolve affected dependencies and commands, identify inherited work and record the evidence behind each adjustment. Recheck this step when implementation begins if the repository or handoff has changed.

### Planning-time check and extracted foundation (2026-10-19)

P1.01–P1.12 and P1 are accepted in the current records. The planning-time check read those records and the retained final run's commit metadata, gate result, collection settings and package hash. This consumes existing evidence; it is not a rerun of the P1 audit or pipeline.

| Field | Extracted value / evidence |
|---|---|
| Acceptance/handoff document revision inspected | `2cbee612df1159c8d1a9415223d3a1db95f979ca`; later documentation commits are distinct from the tested implementation |
| Accepted implementation | `6ac88e783ce10fa6d676860ba831ee371abd9cdf` |
| Comparison target in the P1 acceptance run | `0a2dabeaa4b4baa836b87d7a4aa1a456e3dcb552`, the one genuine bootstrap seed |
| Local test package | `WeatherParser.Parsing`, `0.1.0-initial`; publication identity/feed remain P9 decisions |
| Accepted package SHA-512, base64 | `ld4iecLFargjHkaan1xitVHa/j5GuV5VanGuoCR2olCRDetWgY8h/dqNEF76Di5fLpnrQLpDqORsZK85oYd8jQ==` |
| Accepted package SHA-256, independently checked here | `819f64425820f739f222345f0a9d79caa899dabafedfcf036e72a0ea314022fb` |
| SDK / runtime | SDK `10.0.402`, runtime `10.0.13`, recorded separately; `global.json` pins the SDK with roll-forward disabled |
| Coverage toolchain | `Microsoft.CodeCoverage` and local `dotnet-coverage` `18.11.0`; collector `Code Coverage`; Release configuration |
| Run settings | `coverage.runsettings`, static managed instrumentation; SHA-256 `dc6f662e4f6faaa853c33ed5497f182a31527fd126961411a604b683ffcf05d2` |
| Exclusions | `*.Tests.dll`, `WeatherParser.Coverage*.dll`; no P2 logic may be hidden through additional exclusions |
| Accepted Windows evidence | `artifacts/ci/6ac88e7-20261019T213031Z/`; exit 0; current 54 covered / 2 uncovered, target 52 covered / 4 uncovered; all five gate rules passed |
| Test counts in that run | Parsing 15, coverage checker 99, coverage runner 56, package consumer 2: 172 passing tests; zero build warnings |
| Native metric | Product code blocks in `WeatherParser.Parsing.dll`; count ratios, not displayed percentages or Cobertura metrics |
| Platform evidence | [P1.10 record](../p1/P1_10_DEBIAN_ARM64.md) and [compatibility matrix](../COMPATIBILITY.md): actual Debian 13.3 ARM64 / NanoPi R76S foundation checks; final P1 tooling/test corrections were verified on Windows only, as qualified by P1.12 |
| Procedure owner | [CONTRIBUTING.md](../../CONTRIBUTING.md): restore/build §2, tests §3, package consumer §4, coverage §5, local CI §6, Debian/ARM64 §7 |

Adjustments made from this extraction: use the accepted package/SDK and existing CI, not the rejected draft's invented package/version or historical SDK 10.0.303; retain the current block-comparison policy; require fresh evidence for new P2 behavior; and consume P1.11's guide rather than repeating its audit. The accepted 54/56 is context, not a frozen future measurement. A later P2 comparison must remeasure its deliberate integration target under comparable conditions; never select the older 52/56 seed merely to make the comparison easier.

Inherited limitations: hosted CI (`.github/workflows/ci.yml`) was added after P1 acceptance by commits `c1d3b43` and `b4df185` and is not part of the accepted P1 foundation; for pull requests into `develop` or `master` it compares against the PR base; its checks cannot be made required on the current GitHub plan; its first manual run against the seed `0a2dabe` passed all three jobs, as recorded in [CONTRIBUTING.md section 6](../../CONTRIBUTING.md#6-local-ci); the Debian 13 ARM64 job runs in a `debian:trixie` container on a GitHub-hosted ARM64 runner and does not replace actual NanoPi R76S target evidence; local and hosted runs are recorded separately; projects remain RID-neutral after the documented RID-specific locked-restore finding; the package embeds the commit ID; no P2 serializers, output schemas or provider behavior are proven by P1. P1.10's temporary target tools/work directory were cleaned up. Future ARM64 access, setup, installations and transfers need the applicable explicit authorization; past permissions and installed tools must not be assumed. Release/hardware acceptance remains P9.

**P2.01 revalidation (2026-10-20):** The revalidation of P2.01 concluded with outcome READY, as documented in the [P2.01 foundation check record](../p2/P2_01_FOUNDATION_CHECK.md). Every field in the section 1 extraction table matched the accepted P1 records, leaving the table unchanged. The new integration target `64e460b` was remeasured at 54/56, and P2.02 will commence upon a separate user instruction.

## 2. Goal, binding scope and exclusions

P2 supplies public export entities, shared time/unit/rounding behavior, real JSON/XML serialization, reproducible **output** XSDs, DI registration and independent package-consumer evidence. It implements the P2 scope in the [development plan](../../DEVELOPMENT_PLAN.md), subject to [AGENTS.md](../../AGENTS.md) and [REQUIREMENTS.md](../../REQUIREMENTS.md).

Binding coverage includes REQ-023, REQ-042–043, REQ-050–057, REQ-067–069, REQ-070–071, REQ-074–086 and REQ-101–108 where they define shared export contracts and tests. Source-specific retrieval/parsing portions stay with their provider phases. REQ-079 applies even though it appears outside the main numeric sequence in REQUIREMENTS.md.

**Ownership against DEVELOPMENT_PLAN.md and the P1 handoff:** P2 owns only the export entity contracts of REQ-023/REQ-101–102 (field shapes, ID types, ordering rule representation); P4 owns selection, filtering, availability derivation and the empty-result log notice. P2 owns the shared, source-independent time-normalization service for already-extracted source times (full timestamps, time-of-day with Europe/Berlin date inference, midnight and DST gap/overlap handling, quality markers) with synthetic tests; P6 owns extracting OsthessenWetter page times, applying that service and testing it with page fixtures; P5/P7 apply it to their sources. REQ-057 rounding and REQ-079 output XSDs are P2. The accepted P1 handoff stays unchanged as historical record; this plan and DEVELOPMENT_PLAN.md record the refinement. See [DEVELOPMENT_PLAN.md](../../DEVELOPMENT_PLAN.md) and [P2 handoff](../p1/P2_HANDOFF.md).

Out of scope: HTTP/provider parsers, catalog filtering/geodesy, DWD MOSMIX input-schema types/generation (REQ-112, P7), MQTT topic mapping/serializer orchestration (P8), broker transport and CLI/configuration/file/stdout/process behavior (P3), scheduling and publication. REQ-066 destination validation, overwrite protection and atomic file creation remain P3; P2 owns serialization completeness, error/cancellation behavior and caller-stream lifetime under REQ-069. No source retrieval is required to serialize either format.

Keep the existing library and test projects. Do not restore removed entity scaffolding wholesale, create empty placeholder projects or implement a CLI under the name of a schema tool. The phase order remains **P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9**.

## 3. Contract decisions before the affected implementation

Existing requirements are binding, not proposals. Record remaining choices in `docs/p2/P2_EXPORT_CONTRACT.md` before the affected Red test. Proposed names below are recommendations, not approved public contracts. Resolve routine design choices within the authorized scope; changes to binding contracts require an explicit decision and documentation.

| Decision | Recommendation / required resolution | Blocks |
|---|---|---|
| Envelope and roots | Define a typed multi-result envelope, distinct CDC/MOSMIX list, observation and forecast payloads, and typed errors. Specify exact roots, collection names and status semantics; never concatenate documents. Single observation datasets and single forecast runs are also standalone JSON (and XML) roots because P8 publishes one MQTT JSON message per dataset/run (REQ-062, REQ-075) and the chosen JSON shape is that payload contract (REQ-069). | P2.02, P2.08 |
| Version and XML namespace | Consider `schema_version: "1"` and `urn:weatherparser:export:v1`; decide exact literals and JSON/XML field/collection mapping. No invented external domain ownership. | P2.02, P2.09 |
| Evolution | Define additive/breaking change rules, unknown-field behavior and XML compatibility. Do not presume every additive field is compatible. | P2.02, P2.09 |
| Missing versus empty | Missing quantities are omitted; zero remains present. Empty selections are valid. Decide null/empty collection behavior and numeric omission without empty numeric elements or unintended `xsi:nil`. | P2.02–03, P2.08 |
| Time representation | Retain unambiguous UTC internally, using a reviewed `DateTimeOffset` or UTC-validated `DateTime` design. Prove the shared graph's wire mapping emits second precision without `Z`/offset in both serializers. Default serializer date handling is not sufficient evidence. | P2.02, P2.04 |
| Observation time semantics | Define the dataset-level `measured_at` meaning when readings differ; retain every quantity's own timestamp and quality markers without implying temporal alignment. | P2.06 |
| Quantity/quality/period/error shape | Specify strongly typed functional objects, nullability, pressure reference, source identity, averaging periods and stable error identifiers. Preserve required field names and distinguish timestamp quality from other quality information. | P2.03, P2.06–08 |
| Normalization boundary | Keep normalization and output rounding separate; prepare one functional graph used by both formats. Choose numeric representation/tolerances for conversion and exact midpoint behavior. | P2.05–08 |
| Ordering | Lists: distance then their own ID; forecasts: chronological. Define deterministic tie handling and other collection ordering without silently discarding distinct values/runs. | P2.03, P2.07–08 |
| Serializer service contract | Define supported root types, UTF-8 encoding/BOM policy, completion/error/cancellation semantics and caller-owned streams left open. Do not promise atomic writes to an arbitrary stream. Supported roots include the envelope, both list roots and the standalone observation dataset and forecast run roots. | P2.02, P2.08 |
| XSD delivery and generator | Decide schema location/versioning, generation approach and whether schemas ship in the package. Justify any small tool project and its solution/CI integration before adding it. | P2.09–10 |

For partial functional failures, the agreed envelope may contain successful results plus identifiable errors (REQ-068). A completed document containing errors is distinct from a failed serialization operation. P2 must not report serialization success after an exception or cancellation; P3 later maps functional failures to process exit codes.

## 4. Entity and behavior contracts

Every custom functional object in the export graph, including nested entries, source/location metadata, search parameters, quantities, quality, periods and errors, is strongly typed and explicitly `[Serializable]`. No anonymous objects, `dynamic` or untyped export dictionaries replace these contracts. Entities contain no services, loggers, connections or secrets and support the actual XML serializer's construction/property/collection requirements. Do not introduce BinaryFormatter.

| Graph | Required content and semantics |
|---|---|
| CDC station list | `schema_version`, source, UTC retrieval time, search parameters; entries with string `station_id`, name, latitude/longitude, `altitude_m`, `distance_km`, signed `altitude_difference_m`, `available_measurements`. Preserve leading zeros; sort distance then ID. |
| MOSMIX location list | Separate string `mosmix_id`, including alphanumeric IDs, and `available_forecast_elements` plus the required list/location/search metadata. Distinguish product availability and explicitly unknown availability; never equate MOSMIX and CDC IDs. |
| Observations | `schema_version`, source, location, dataset `measured_at`, `retrieved_at`, `timestamp_quality`, `measurements`; every exported quantity retains its own measurement timestamp and applicable quality/source-timezone markers. Different reading times remain distinct. |
| Forecast run | Separate dataset with `dataset_type: "forecast"`, source, schema version, `mosmix_id`, location, product, `issued_at`, `retrieved_at`, chronologically ordered `forecasts`; each step has `valid_at` and typed values. Interval quantities retain start/end and averaging meaning. No observation `measured_at` in the forecast graph, no mixing of runs or products. |
| Envelope/errors | One complete document with typed result collections and distinct typed error records; errors never masquerade as observations. An empty selection is valid; an observation dataset or forecast run with no usable values fails. |

Use the REQ-052 quantity names and units: `temperature_c`, `pressure_hpa`, `pressure_mmhg`, `humidity_percent`, `wind_direction_deg`, `wind_speed_kmh`, `illuminance_lux`, `solar_irradiance_w_m2`, `rain_rate_mm_h`. Forecasts reuse them only where meanings match. Additional quantities need documented names, units and semantics, not silent relabeling of probabilities, gusts, totals or energy.

Omit absent/invalid/nonfinite readings while preserving true zero and valid negative temperatures. Define field-specific checks from requirements or verified source meaning; do not invent a universal plausibility range or north for unknown calm-wind direction.

Time normalization accepts already extracted source time information, not provider documents. Convert full timestamps to UTC; never substitute retrieval time. Where only time of day exists, the permitted `Europe/Berlin` date inference uses retrieval context, midnight handling, `timestamp_quality: "estimated_date"` and `source_timezone: "Europe/Berlin"`. Complete unambiguous source timestamps use `source`. Ambiguous/nonexistent local times require explicit unambiguous evidence or a defined rejection; never select a DST interpretation arbitrarily. Preserve unambiguous UTC internally; export `YYYY-MM-DDTHH:mm:ss` exactly.

Pressure conversion uses `mmHg = hPa × 0.7500617` and `hPa = mmHg × 1.3332239`. Derive both outputs from the same unrounded original, retain `sensor` / `sea_level` / `unknown` reference, and document rounding tolerance. Round only for output: temperature 0.1 °C, pressure 1 hPa and 1 mmHg, humidity 1 percentage point, wind speed 1 km/h; midpoint ties go away from zero. Do not add a new precision rule for other quantities.

Never generically convert lux to W/m². Energy-to-mean-power and accumulation-to-mean-rate conversions require verified units and a known positive reference period; retain that period and averaging semantics. Reject unsupported/missing/zero-duration inputs rather than fabricate irradiance or rain intensity. Source-element mappings remain provider-phase work.

## 5. Ordered implementation packages

For each behavior slice: record the meaningful expected failing test (Red), implement minimally (Green), then refactor with tests green. Use MSTest, Moq and FluentAssertions. Construct subjects/shared dependencies in `[TestInitialize]`; verify all mocks through `MockRepository.VerifyAll()` in `[TestCleanup]`. Use real serializers in contract tests, with only external boundaries mocked. Pure DTOs need no artificial interface; every class with executable application/domain logic has a purpose-specific interface and constructor-injected dependencies.

Apply the [AGENTS.md AI-assisted development workflow](../../AGENTS.md#ai-assisted-development-user-decision-dated-2026-10-19) to every P2 step: the local `qwen3-coder:30b` model generates the Red tests, implementation, refactorings, generator code and documentation drafts from precise briefs; the orchestrating LLM writes the precise briefs and applies qwen's output, reviewing against AGENTS.md, REQUIREMENTS.md and this plan; the orchestrating LLM runs builds/tests itself and returns defects to qwen; exceptions apply when qwen is unavailable, user decisions are needed, recorded evidence or repository operations are required, or repeated precise briefs still fail. In those cases the orchestrating LLM does the work itself and states the reason in the step's record. Each step's record names qwen-generated and orchestrator-written/corrected parts.

| Step | Depends on | Deliverable and completion evidence |
|---|---|---|
| **P2.01 – Check P1, extract and revise** | None for checking; accepted P1 for implementation handoff | Execute section 1 first. If accepted, reconcile the extraction table, inherited limitations, target choice, procedures and this plan with current accepted evidence. Otherwise record NOT_READY prerequisites. Planning-time check is recorded above; revalidate before implementation. |
| P2.02 – Contract decisions and first serializer slice | P2.01 and explicit P2 implementation instruction | Record the relevant decisions in section 3. Begin with one minimal required typed graph and a Red test for real JSON/XML, omitted numeric value versus zero, UTC formatting and escaping. Implement only that slice and its injectable serializer services. No all-entity scaffold or completed-schema requirement at this point. |
| P2.03 – CDC/MOSMIX list graphs | P2.02 | Complete list entries, metadata/search parameters, independent IDs, availability and deterministic ordering. Real serializers cover populated and empty lists; selection/geodesy algorithms remain P4. |
| P2.04 – UTC and source-time normalization | P2.02 | Implement injectable time-normalization behavior for complete/inferred timestamps, midnight and DST rejection/evidence. Both formats preserve the agreed UTC lexical contract and quality markers. |
| P2.05 – Units, pressure and rounding | P2.02 | Meaningful conversion/rounding/omission tests precede interface-based services. Prove both pressure directions, same-original derivation, reference preservation, positive/negative ties and period-aware transformations. |
| P2.06 – Observation graphs | P2.02, P2.04–05 | Complete all required quantity, location/source and quality/period shapes. Prove per-quantity times, differing times, omitted readings, real zeros and invalid all-empty datasets using both real serializers. |
| P2.07 – Forecast graphs | P2.02, P2.04–05 | Complete separate run/step/interval contracts for S/L products, issue/retrieval/validity distinction and usable-values validation. Prove no `measured_at`, cross-run/product mixing or shifted time/value association in prepared synthetic graphs. Actual source-series parsing remains P7. |
| P2.08 – Full serializer/envelope contract | P2.03, P2.06–07 | Serialize every supported root and representative complete nested graph. Verify one document, deterministic ordering, equivalent meaning, typed errors, culture independence, UTF-8, cancellation/failures and agreed stream lifetime. Use the same prepared graph for both formats without a second retrieval or assembler. |
| P2.09 – Reproducible output XSDs | P2.08 and generator/delivery decision | Complete the feasibility and generation/negative-validation work in section 7. Produce deterministic versioned output schemas tied to entity mappings and prove valid exports and invalid documents are distinguished. |
| P2.10 – DI and actual package consumption | P2.08–09 | Extend central registration and independent package tests. Resolve serializer/normalization services through real DI from the same-run `.nupkg`, serialize representative graphs, and validate packaged/schema delivery as decided. Prove missing-dependency failures and independent consumer configurations. |
| P2.11 – Integrated coverage/platform verification | P2.03–10 | Execute section 8 for a committed candidate supplied by an authorized author. Preserve real test/TDD/schema/package/native coverage evidence and separate Windows and ARM64 outcomes. Failed/missing mandatory evidence yields NOT_READY. |
| P2.12 – Acceptance and P4 handoff | P2.11 | Write the requirement/test evidence matrix, limitations, contract/version decisions and explicit acceptance disposition. Hand P4 the accepted entities/helpers and downstream source-mapping responsibilities. Mark P2 complete only after acceptance. Mark pull request #3 ready for review, resolve review findings test-first, wait for the user's merge into `develop`; P2 complete only after acceptance and merge. |

Each step has a detailed implementation plan under `docs/p2/`; the step plans refine this phase plan and must not contradict it; they are plans only and contain no execution evidence.

- P2.01 – [Check P1, extract and revise](p2/P2_01_FOUNDATION_CHECK_PLAN.md)
- P2.02 – [Contract decisions and first serializer slice](p2/P2_02_CONTRACT_AND_FIRST_SLICE_PLAN.md)
- P2.03 – [CDC and MOSMIX list graphs](p2/P2_03_LIST_GRAPHS_PLAN.md)
- P2.04 – [UTC and source-time normalization](p2/P2_04_TIME_NORMALIZATION_PLAN.md)
- P2.05 – [Units, pressure and output rounding](p2/P2_05_UNITS_PRESSURE_ROUNDING_PLAN.md)
- P2.06 – [Observation graphs](p2/P2_06_OBSERVATION_GRAPHS_PLAN.md)
- P2.07 – [Forecast graphs](p2/P2_07_FORECAST_GRAPHS_PLAN.md)
- P2.08 – [Full serializer and envelope contract](p2/P2_08_SERIALIZER_ENVELOPE_PLAN.md)
- P2.09 – [Reproducible output XSDs](p2/P2_09_OUTPUT_SCHEMAS_PLAN.md)
- P2.10 – [DI and actual package consumption](p2/P2_10_DI_PACKAGE_CONSUMPTION_PLAN.md)
- P2.11 – [Integrated coverage and platform verification](p2/P2_11_INTEGRATED_VERIFICATION_PLAN.md)
- P2.12 – [Acceptance and P4 handoff](p2/P2_12_ACCEPTANCE_HANDOFF_PLAN.md)

Decisions marked OPEN or "Decided by: User" in the step plans are not approved contracts; they are recorded in `docs/p2/P2_EXPORT_CONTRACT.md` when the user decides them.

## 6. Requirement-to-test matrix

These new `T-P2-*` IDs are planned traceability identifiers, not executed results. At implementation, map each to actual test names, Red/Green evidence and run artifacts. Existing P1 IDs and all 84 binding requirement IDs remain unchanged.

| ID | Requirements | Required behavior / boundary evidence |
|---|---|---|
| T-P2-01 | REQ-074, REQ-076–077 | Traverse representative complete graphs: every nested custom export type explicitly `[Serializable]`, XML constructible, no services/secrets. |
| T-P2-02 | REQ-023, REQ-067–068 | CDC graph contains every required entry/list/search field; IDs retain leading zeros; signed altitude difference, distance/ID ordering and valid empty result. |
| T-P2-03 | REQ-101–102, REQ-067 | Independent alphanumeric/leading-zero MOSMIX IDs, elements/product availability including unknown, metadata and empty list; no substituted CDC IDs. |
| T-P2-04 | REQ-050, REQ-052–053 | All required observation quantities, field-specific validity, real zeros/negative temperatures; absent/nonfinite/invalid readings omitted and all-unusable dataset rejected. |
| T-P2-05 | REQ-051, REQ-067 | Every timestamp exactly UTC second precision without `Z`/offset in JSON/XML under `en-US` and `de-DE`; invariant numeric output. |
| T-P2-06 | REQ-042–043, REQ-056 | Full-source versus inferred date markers, timezone metadata and midnight context; no retrieval-time substitution. |
| T-P2-07 | REQ-042–043, REQ-082 | Europe/Berlin DST gap/overlap rejected without sufficient evidence; explicit unambiguous timestamps normalize correctly. |
| T-P2-08 | REQ-050, REQ-056 | Distinct timestamps/quality for individual readings survive both exports; dataset timestamp follows the recorded rule without hiding differences. |
| T-P2-09 | REQ-054 | Both pressure formulas use one authoritative unrounded input, retain reference and satisfy documented output tolerance; no silent reference conversion. |
| T-P2-10 | REQ-057 | Every specified output increment, positive/negative midpoint ties and conversion-before-rounding; no premature rounding or extra precision policy. |
| T-P2-11 | REQ-055, REQ-106 | No lux/power shortcut; correct known-period energy/rain transforms; missing/zero/invalid periods rejected; averaging metadata retained. |
| T-P2-12 | REQ-052, REQ-055, REQ-106 | Gust, mean wind, probability, accumulation, intensity and energy remain distinct; unsupported quantity substitutions rejected. |
| T-P2-13 | REQ-105, REQ-111 | Complete forecast metadata, product/location identity, ordered validity times and no observation `measured_at`; separate runs/products. |
| T-P2-14 | REQ-105–107 | Interval start/end and values remain associated in prepared graphs, unavailable values omitted without time shifting, no-usable-values run rejected. Source-reader alignment is separately tested in P7. |
| T-P2-15 | REQ-062, REQ-067–069, REQ-074–075 | Multi-result envelope and empty selection serialize as one complete document; typed partial errors distinguish failure from valid data. A standalone observation dataset and a standalone forecast run each serialize as one complete JSON document with the same field shape as inside the envelope. |
| T-P2-16 | REQ-067, REQ-075, REQ-077 | Parse real JSON/XML into semantic assertions for the same graph: IDs, metadata, units, times, quantities, omissions, zeros and order agree. |
| T-P2-17 | REQ-067, REQ-076–077 | Umlauts and XML-special characters round-trip through test readers; absent numbers do not become empty numeric elements or default zero. |
| T-P2-18 | REQ-068–069, REQ-071 | UTF-8 and agreed BOM/stream ownership; serialization/write exceptions, cancellation and unsupported roots propagate defined failures, never a success signal or guaranteed atomic arbitrary-stream write. |
| T-P2-19 | REQ-070–071, REQ-077–078 | Real DI resolves interface-based services; missing dependencies fail; two configured consumers do not leak configuration/state. |
| T-P2-20 | REQ-079 | Two clean generations from the same accepted entity mappings/toolchain produce identical named XSD files and checksums. |
| T-P2-21 | REQ-067, REQ-076, REQ-079 | Actual XML exports validate for every root; missing required fields, wrong namespaces and malformed timestamps fail the schema/contract checks. |
| T-P2-22 | REQ-079, REQ-083 | Compile the full local schema set and validate offline with external resolution/DTDs prohibited; include/import handling cannot fetch arbitrary resources. |
| T-P2-23 | REQ-074–079 | Version/null/empty/error mappings match the recorded contract; schema changes are deliberate and compatibility-tested, not an unrestricted `xs:any` escape. |
| T-P2-24 | REQ-012, REQ-077, REQ-082 | Independent consumer uses the same-run versioned package, not a ProjectReference; real DI/serializers and agreed schema delivery work through the public API. |
| T-P2-25 | REQ-082, REQ-086 | Exact package-content check, matching packed/restored hashes, only permitted consumer hash rewrite, other dependencies remain locked; README changes trigger repack verification. |
| T-P2-26 | REQ-080–083, REQ-085 | Meaningful Red→Green→Refactor evidence, required test setup/cleanup and offline behavior; no production source/broker dependence. |
| T-P2-27 | REQ-086 | Native block minimum plus valid target comparison, current/target identities/settings and exclusions; no new bootstrap or docs-only bypass for code/schema/package/tool changes. |
| T-P2-28 | REQ-005, REQ-017, REQ-084 | Actual P2 serializer/time/schema/package behavior on the supported target, separately from cross-build and Windows evidence; report missing checks explicitly. |

## 7. Output-schema generation and delivery

Before selecting a generator, test a minimal entity graph using the pinned .NET toolchain on the supported development/target platforms. Evaluate entity-mapping/export APIs or a reviewed, pinned alternative. Do not assume Windows-only `xsd.exe` is globally installed or portable. [XmlSchemaExporter documentation](https://learn.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlschemaexporter?view=net-10.0) labels that API as infrastructure; assess its suitability rather than treating its existence as a validated generator recommendation.

Entity XML mappings are authoritative. Specify root types, namespace/version, cardinalities, omission, collection mappings and timestamp lexical constraints. If generated output needs deterministic refinement for facets, make that refinement part of the reproducible generation procedure; do not maintain an independently drifting hand-written schema model. Schema validation and semantic tests must together reject invalid calendar/time values and enforce UTC interpretation where the suffix-free lexical form cannot express it alone.

Record generator source/version, exact command, SDK, mappings/configuration, stable file/namespace ordering, UTF-8/line endings and SHA-256 inventory. Run two clean generations and compare the complete output set. Compile the full vetted local schema set and validate actual exported documents for each root plus negative fixtures. Disable DTD/external entity resolution and network schema discovery; explicitly preload local includes/imports. Review [XmlSchemaSet.Add behavior](https://learn.microsoft.com/en-us/dotnet/api/system.xml.schema.xmlschemaset.add?view=net-10.0), including its separate resolver behavior. These references are spike inputs, not proof that a generator has been executed.

Keep outputs under the proposed `schemas/export/v1/`, separate from DWD MOSMIX input schemas. If a tool project is needed, justify its boundary, interfaces, tests, lock files and solution/CI integration. Extend the CI solution filter and test sequence to include any new tool tests; report tool coverage separately without excluding product normalization/serialization logic.

The current package check allows the library DLL, canonical README, license, nuspec and standard NuGet metadata. If XSDs are shipped, first decide exact versioned package paths and update the allowlist and package-boundary tests narrowly through TDD. If delivered separately, document how consumers obtain and match the correct schema version. Never broaden the package check to accept arbitrary contents.

## 8. Execution verification and evidence

Use the accepted [developer guide](../../CONTRIBUTING.md) for current commands. After P2.01 confirms the pinned toolchain/procedures, development checks may restore/build/test the product test project in locked mode. Run independent package tests only after packing and preparing the same-run local package according to the guide; a source-reference unit run is not package evidence.

For the full committed candidate, the current procedure is the following template; replace placeholders with resolved full SHAs before execution:

```powershell
pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings
```

The script checks committed state from a fresh clone. This planning request authorizes no commit; an authorized author must supply implementation checkpoints before clean-clone evidence can certify them. Record the candidate, real integration target and script revision; do not label working-tree tests as verification of uncommitted changes through CI. Use the accepted collect/resume workflow only when needed and with its provenance checks intact.

Require at least 80% native product block coverage **and** strict improvement in the unrounded ratio over a comparable below-100% integration target; exact 100% may remain exactly 100%. Documentation-only equality applies only when the entire actual diff qualifies. Changes to schema assets, projects, package contents, tools, coverage or CI do not receive a Markdown-only exemption. The one-time initial bootstrap was consumed in P1.09; no fresh bootstrap for P2, a lost baseline, a different platform or a failed comparison. A failed gate yields NOT_READY; meaningful tests, not metric manipulation, must resolve it.

The integration target is the PR base commit: for pull requests from `features/develop/P2` into `develop` or `master`, the hosted workflow (`.github/workflows/ci.yml`) passes it to `scripts/ci-local.ps1` as `-TargetRef`, and local runs use the same base commit deliberately. The base is currently `develop` at `64e460b`, whose product code equals the accepted `6ac88e7`; P2.01 remeasured it at 54/56, and each subsequent candidate will remeasure its own base rather than assuming the result. Each later candidate is compared with the then-current integration target, so the bar rises with every merged checkpoint. With a 54/56 target and N new blocks of which U remain uncovered (no change to the two existing uncovered blocks), the candidate passes only if (54+N−U)/(56+N) > 54/56, i.e. U < N/28 — fewer than about 3.6% of new blocks may be uncovered; covering the two existing uncovered blocks with meaningful tests relaxes this. Slice P2 work so each candidate's new logic is fully exercised by meaningful behavior tests; never add assertion-free tests, metric padding or exclusions.

Remeasure target/current using comparable SDK, collector/converter versions, configuration, RID, run-settings hash and exclusions. Preserve raw native reports and counter ratios; do not substitute line/branch metrics. Keep checker/generator tool evidence separate from the product gate and avoid double-counting the consumer's package assembly.

Verify Windows and actual Debian/ARM64 P2 behavior separately, using the deliberate candidate and a target measured on that platform. Do not relabel cross-publish, old P1 evidence or Windows runs as new ARM64 runtime success. Re-establish target tooling only within explicit authorization; if required platform checks cannot run, record the precise OPEN blocker and obtain an explicit acceptance disposition before claiming completion. Final release/hardware checks remain P9.

`README.md` is packed, and package metadata/assembly informational version embed the commit ID. Repack after relevant documentation/contract changes. A committed consumer lock cannot be made self-consistent with its own commit merely by refreshing the package hash. Use the accepted isolated-clone workflow: rewrite only the local WeatherParser package `contentHash`, retain all other locked dependencies, and verify packed/restored SHA-512 plus the exact package entries.

The future `docs/p2/P2_VERIFICATION.md` records each T-P2 row's test/run/commit and outcome, Red/Green evidence, SDK/runtime/tool versions, hosts, settings/exclusions, native counters/rule results, package hashes/contents, schema command/checksums/negative tests, actual platform checks, qwen/orchestrator authorship per step including the reason whenever qwen was not applicable, omitted checks and acceptance decision. Offline tests require no weather HTTP or brokers; initial dependency restore may still need NuGet network access. Model output and proposed test names never count as execution evidence.

## 9. Files and documentation maintenance

| Path | Action during execution |
|---|---|
| `PHASE_P2_PLAN.md` | Reconcile foundation extraction first, then maintain decisions, dependencies and plan changes. |
| `docs/p2/P2_EXPORT_CONTRACT.md` | Create the reviewed public field/type/JSON/XML/version/omission/error mapping and decision register. |
| `src/WeatherParser.Parsing/` | Extend the existing library incrementally with approved data contracts and interface-based behavior; no second library. |
| `tests/WeatherParser.Parsing.Tests/` | Extend meaningful offline normalization and real serializer/schema tests. |
| `tests/WeatherParser.Package.Tests/` | Extend real package-consumer/DI/schema-delivery tests; retain PackageReference isolation. |
| `schemas/export/v1/` and a justified `tools/` generator if needed | Create only after generator/delivery decisions and feasibility tests; integrate solution/filter/locks/CI as required. |
| `docs/p2/P2_VERIFICATION.md`, `docs/p2/P4_HANDOFF.md` | Create actual evidence/acceptance and downstream handoff records when executing; no empty completion records now. |
| `DEVELOPMENT_PLAN.md`, `CONTRIBUTING.md`, `DEPENDENCIES.md`, `docs/COMPATIBILITY.md`, `README.md` | Update only facts/procedures affected by executed work, respecting single-source ownership and packed README verification. |

Keep documentation, generated diagnostics and inline/block comments, XML documentation comments, script/configuration comments in English. Preserve the 84 requirement IDs, legal MIT text, historical inventories/checksums and others' working-tree changes. No application behavior, requirements, historical evidence, commit, push or publication is changed by preparing this plan.

## 10. P2 acceptance and P4 handoff checklist

Checked on 2026-10-20 against the [P2 acceptance record](../p2/P2_12_ACCEPTANCE.md#7-phase-plan-checklist-phase_p2_planmd-section-10); the pull-request item stays open until the merge into `develop`.

- [x] P2.01 has revalidated P1 completion, extracted its accepted data and adjusted this plan; implementation authorization is recorded.
- [x] Required contract decisions are recorded before their implementation and agree with binding requirements.
- [x] Typed, explicitly `[Serializable]` complete list/observation/forecast/error graphs preserve all required fields, nullability, IDs, metadata and separation.
- [x] Real JSON/XML tests prove equivalent meaning, exact UTC/culture behavior, omissions/zeros, escaping, ordering and complete documents.
- [x] Normalization tests prove per-quantity times, inference/DST handling, valid units/periods, pressure references and required output rounding.
- [x] Serializer failure/cancellation/stream behavior and interface-based DI, including independent consumers, are verified.
- [x] Deterministic output-XSD generation, complete local schema compilation and positive/negative real-export validation pass.
- [x] Same-run package consumption, frozen unrelated dependencies, hash identity and exact contents/schema delivery are verified.
- [x] Meaningful TDD evidence and every T-P2-01–T-P2-28 outcome are mapped to actual test names and artifacts.
- [x] Full candidate coverage minimum/comparison and provenance checks pass without another bootstrap or exclusion/bypass workaround.
- [x] New P2 platform behavior is verified, or any missing mandatory check has a named blocker and explicit acceptance disposition; no unsupported runtime claim is made.
- [x] The result record states actual SHAs, tool versions, test counts, package/schema hashes, qwen/orchestrator authorship per step, limitations and the explicit acceptance decision.
- [x] Pull request #3 marked ready for review after the status updates, review findings resolved, and the pull request merged into `develop` by the user (or on the user's explicit instruction) (done 2026-10-20: CI run 39495576541 succeeded, no review findings, merge commit `74bf625`).
- [x] P4 receives the accepted public contracts/helpers, schema/version mapping and source-specific validation/mapping responsibilities; plans and current status agree.

P4 implements coordinates/catalog selection against these contracts; P5–P7 own provider extraction and verified element mappings; P8 adds MQTT serialization/integration; P3 later adds the CLI and transport. P2 acceptance neither completes those phases nor permits publication.

## 11. Plan authorship and review

Local `qwen3-coder:30b` produced the initial draft and one correction draft. Both were reviewed against the repository and precise correction briefs. The correction draft still misplaced serializer responsibilities, invented foundation/package details and omitted the required first-step sequence and detailed evidence mapping. Under AGENTS.md's exception for repeated precise briefs that still fail, the orchestrating agent corrected/restructured this plan directly and extracted the subsequently accepted P1 evidence. No model-generated statement was treated as a test result. Preparing this document implements no P2 production code, tests or schemas.

Review revision (2026-10-20): four review findings were applied — standalone dataset/run roots as the shared MQTT JSON payload contract, the AI-assisted workflow in execution and records, ownership reconciliation with DEVELOPMENT_PLAN.md and the P1 handoff, and the coverage integration target with its strict-improvement consequence. qwen generated the added text from precise briefs and corrected its own defects in two further rounds. The orchestrating agent wrote the briefs, reviewed every block, and itself deleted one outdated target sentence that qwen kept after three precise briefs and fixed the resulting sentence fragment (AGENTS.md exception). The stale "no hosted CI" inherited limitation in section 1 was replaced by qwen text, corrected by qwen in two further rounds after review. The orchestrating agent wrote this record.

Step plans (2026-10-20): the twelve step plans in `docs/p2/` were drafted by qwen from the orchestrating agent's briefs and corrected by qwen in review rounds; each plan's status line records the corrections the orchestrating agent applied itself under the AGENTS.md exception, and qwen drafted the step-plan link list above.
