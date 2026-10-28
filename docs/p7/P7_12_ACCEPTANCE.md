# P7.12 – P7 Acceptance Record

**Status (2026-10-28): the evidence audit (section 5) accepts the implementation candidate `a36d15a`. The user's acceptance (A7, section 6) is pending; the pull request stays a draft until the user instructs otherwise (A9); P7 is complete only once the user merges pull request #11 (A11).** Plan: [P7_12_ACCEPTANCE_HANDOFF_PLAN.md](P7_12_ACCEPTANCE_HANDOFF_PLAN.md); handoff: [P8_HANDOFF.md](P8_HANDOFF.md).

## 1. Review identity (A1)

| Item | Value |
|---|---|
| Implementation candidate | `a36d15a538e6ddc9cdf01d47b2ff45073a36cd95` (`a36d15a`), branch `features/develop/P7`: the P7.11 S0 commit (test-support code only; the library is unchanged since the P7.08 commit `0c3d39e`) |
| Pull request | [#11](https://github.com/witalij-schrajer/WeatherParser/pull/11) into `develop`, draft until A9 (section 8) |
| Integration target | PR base `8009f1c413ead6b150f2c71bec8f669e81be5558`; checked on 2026-10-28 (`gh pr view 11` `baseRefOid` and `git rev-parse origin/develop` after `git fetch`): unchanged since P7.01 |
| Head before this record | `14b7696` (the P7.11 record and status passages, documentation only on top of `a36d15a`); this record and the P8 handoff change documentation only (no production code, tests, build, CI, package or schema files) |
| Verification | [P7.11 record](P7_11_INTEGRATED_VERIFICATION.md): READY on `a36d15a` on Windows, hosted CI run 40615010706 and the NanoPi R76S, with the MOSMIX_S resource check on the target within the approved thresholds and one authorized live parse per product |

## 2. Requirement and test evidence matrix (A2, T-P7-01 – T-P7-23)

Platform evidence for every row: the P7.11 runs on candidate `a36d15a` ([P7.11 record](P7_11_INTEGRATED_VERIFICATION.md) sections 3 to 7): Windows `ci-local.ps1` exit 0, hosted CI run 40615010706 with three green jobs, and the NanoPi R76S run with identical test outcomes. Each run passed 2935 Parsing tests (the 7 opt-in live and resource tests are not executed in standard runs) and 47 package-consumer tests, and its gate passed with 10228/10231 blocks against the baseline 8486/8489. The only uncovered blocks are the three of the baseline.

"Red" names the step whose section in [P7_VERIFICATION.md](P7_VERIFICATION.md) holds the Red → Green → Refactor evidence; "Characterization" marks tests over behavior implemented test-first in an earlier step or over existing library behavior, recorded as first-run passes; "after Green" marks planned tests added without a Red (P7.06 deviation, section 4).

| T-P7 ID | Requirements | Tests | TDD evidence | Status |
|---|---|---|---|---|
| T-P7-01 | REQ-103, C-69 | `MosmixLParserTests`, `MosmixSParserTests` (invalid IDs rejected before any request or timer, with one sanitized rejection log that never contains the ID's content), `MosmixLForecastSourceIdTests` | Red, P7.07 and P7.08 (section 7, 8); Red, P7.06 Codex round 2 (section 6) | PASSED |
| T-P7-02 | REQ-014, REQ-016, REQ-109 | `MosmixForecastOptionsTests` (endpoint rules, deadline and `MaxRunAge` bounds on both sides), option-bound tests of both parser fixtures, `MosmixDependencyInjectionTests` (`MOSMIX-OPTIONS-INVALID` on resolution) | Red, P7.06 S1; Red, P7.07 and P7.08; characterization, P7.10 | PASSED |
| T-P7-03 | REQ-104, REQ-063, REQ-108 | `MosmixForecastSourceTests`, `MosmixForecastSourceRetrievalTests`, `CatalogDownloaderRequestTimeoutTests`, `MosmixForecastSourcePrecedenceTests`, the parser deadline and cancellation tests | Red, P7.06 S2 and S3 and its Codex rounds 1 and 2; the one-request, transport-failure, streamed-bound and stall cases added after Green (P7.06 deviation) | PASSED |
| T-P7-04 | REQ-108, REQ-082 | `MosmixForecastDocumentReaderArchiveTests` (members, bounds on both sides in both forms, DTD, external entity, external references never followed, CRC-32 with the check value `CBF43926`, asynchronous input, cancellation) | Red, P7.04 (section 4) | PASSED |
| T-P7-05 | REQ-104, REQ-107, REQ-112 | `MosmixForecastDocumentReaderStructureTests` (namespaces, product ID, issue time, timeline, placemark identity, element names, token grammar and counts, retained bounds, coordinates, location name, optional schema elements, `dwd:ReferencedModel` bounds) | Red, P7.04 and its Codex round 1 | PASSED |
| T-P7-06 | REQ-104, REQ-107, MF-06 | `MosmixForecastDocumentReaderAllStationsTests` (first, middle, last, absent, duplicates before and after, corrupted member after the placemark, bounded memory with a readiness barrier), `MosmixProductIndependenceTests`; the offline resource check of P7.11 on the target | Red, P7.04 and P7.08; P7.11 S3 | PASSED |
| T-P7-07 | REQ-105, MF-08 | reader coordinate and name tests (three-part coordinates required, ranges on both sides, umlauts and punctuation kept), `MosmixElementMapperTests` (location) | Red, P7.04 (Codex round 1 withdrew the optional altitude) and P7.05 | PASSED |
| T-P7-08 | REQ-052, REQ-053, REQ-054, REQ-057, REQ-106, MF-10, MF-12 | `MosmixUnitScalerTests` (exact decimal scaling), `MosmixElementMapperTests`, `MosmixComposedMappingTests` (zeros kept, out-of-range omitted and not clamped, direction `-0.000001` and `360.000001`, precision kept until output rounding, both pressure outputs from the unrounded value) | Red, P7.05; three precision and boundary cases added after Green (P7.05 Codex round 1) | PASSED |
| T-P7-09 | REQ-055, REQ-106, MF-10, MF-13 | `MosmixElementMapperTests` and `MosmixComposedMappingTests` (`Rad1h` mean W/m² over `[valid_at − 1 h, valid_at]`, `RR1c` mean mm/h with its period, zero totals kept; no rate without a verified period) | Red, P7.05 | PASSED |
| T-P7-10 | REQ-044, REQ-055, REQ-106, MF-10, MF-11 | `ForecastOnlyMembersBuilderTests`, `ForecastOnlyMembersExportTests`, `ForecastRunBuilderTests.Build_GustSeries_IsNotWrittenAsWindSpeed`, `MosmixComposedMappingTests` (gust maximum, precipitation rate and sum, probabilities not exported) | Red, P7.03 S3 (section 3) and P7.05 | PASSED |
| T-P7-11 | REQ-107, C-77, MF-12 | `MosmixComposedMappingTests` (leading, middle and trailing missing tokens omitted at their own `valid_at`, later values unshifted, the rejection with all omissions), `MosmixLParserRunTests` and `MosmixSParserRunTests` (absent and not-exported elements, `MOSMIX-NO-USABLE-VALUE`), `ForecastRunRejectionDiagnosticsTests` | Red, P7.03 S1, P7.05, P7.07, P7.08 | PASSED |
| T-P7-12 | REQ-051, REQ-105, REQ-110, C-71, MF-13 | `MosmixLParserRunTests` and `MosmixSParserRunTests` (`retrieved_at` sampled once after the read, whole seconds, no `measured_at` on the wire), `MosmixComposedMappingTests` (horizon of 1 and 247 steps, first period before the issue time kept), `MosmixExportContractTests` (UTC forms) | Red, P7.05, P7.07, P7.08; characterization, P7.09 | PASSED |
| T-P7-13 | REQ-104, REQ-110, MF-09 | `ForecastRunFreshnessMarkerTests`, `MosmixLParserRunTests` and `MosmixSParserRunTests` (age at the limit fresh, one second above stale with the `MOSMIX-RUN-STALE` notice, negative age fresh), `MosmixExportContractTests` (stale marker per product with its own limit) | Red, P7.03 S2, P7.07, P7.08; characterization, P7.09 | PASSED |
| T-P7-14 | REQ-105, REQ-107, REQ-111, C-68, C-75 | `MosmixLParserRunTests`, `MosmixSParserRunTests`, `ComposedMosmixLPipelineTests`, `ComposedMosmixSPipelineTests` (`dwd-mosmix-l:<id>`, `dwd-mosmix-s:<id>`, one run per parse), `MosmixExportContractTests` (IDs and sources) | Red, P7.07 and P7.08; characterization, P7.09 | PASSED |
| T-P7-15 | REQ-109, REQ-016 | `MosmixProductIndependenceTests` (both parsers in one container, own endpoints and options, deadline isolation, no fallback), `MosmixDependencyInjectionTests`, `MosmixExportContractTests` (one request for the product and none for the other) | Red, P7.08; characterization, P7.09 and P7.10 | PASSED |
| T-P7-16 | REQ-051, REQ-067, REQ-068, REQ-069, REQ-077, REQ-079 | `MosmixExportContractTests` (34 cases over both products through the container's real serializers: equivalence, schema validity of every XML document, UTC forms, periods, omitted values, zeros, leading-zero IDs, umlauts and markup escaped in XML, determinism), `ForecastRunFreshnessMarkerTests`, `ForecastOnlyMembersExportTests` | Red, P7.03; characterization, P7.09 (section 9) | PASSED |
| T-P7-17 | REQ-072, REQ-082, MF-02, MF-15 | `MosmixLParserDiagnosticsTests`, `MosmixSParserDiagnosticsTests` (every entry with ID, UTC, product, MOSMIX ID and the sanitized endpoint; no values, file text or query), `ForecastRunRejectionDiagnosticsTests`, the source sanitization tests of P7.06 | Red, P7.03 S1, P7.06 Codex round 1, P7.07 and its Codex round 1, P7.08 | PASSED |
| T-P7-18 | REQ-012, REQ-013, REQ-016, REQ-078 | `MosmixDependencyInjectionTests`, `PackageMosmixForecastTests` (package consumer, 47 tests), `ServiceRegistrationCompletenessTests`, `NoSharedStateTests` (runtime Red before every registration in P7.04 to P7.08) | Characterization, P7.10 (section 10); registration Red in each step | PASSED |
| T-P7-19 | REQ-090, REQ-092, REQ-110, MF-16 | README section "MOSMIX forecasts (library)" (source paths, element table, cycles and freshness, retrieval guidance, CC BY 4.0 attribution, IDs, schema compatibility, logging note); package checks after every README change | P7.10 and its Codex rounds 1 and 2; P7.11 record section 8 (package check after the status corrections) | PASSED |
| T-P7-20 | REQ-085, REQ-086 | step records of P7.03 to P7.11; the gate on every step commit and on the candidate | sections 3 to 11 of the verification record; the P7.06 deviation (section 4 of this record) | PASSED with the recorded P7.06 deviation |
| T-P7-21 | REQ-017, REQ-084, REQ-005 | NanoPi R76S run (ci-local exit 0, identical outcomes) and the MOSMIX_S resource check on the target: 58,710 ms of the 300,000 ms deadline, process peak 143,413,248 bytes of the 268,435,456-byte threshold | P7.11 S3 | PASSED |
| T-P7-22 | REQ-083, MF-17 | `LiveCheck_MosmixL_OnlyWhenExplicitlyEnabled` and `LiveCheck_MosmixS_OnlyWhenExplicitlyEnabled` (one authorized run each), `LiveCheckMosmixRunnerTests`, `ResourceCheckRunnerTests` | Red, P7.11 S0; live runs P7.11 S4 | PASSED (single snapshot per product on Windows, 2026-10-28) |
| T-P7-23 | REQ-102, MF-18 | `MosmixElementVocabularyTests` (the 137 defined names, the 114 MOSMIX_L and 40 MOSMIX_S P4 vocabulary names, every mapped element defined and present) | Red, P7.05; the vocabulary check recorded in the contract (MF-18: no mismatch, no P4 amendment) | PASSED |

## 3. Contract summary (A3)

- MF-01 to MF-18 approved by the user on 2026-10-26 ([contract](P7_FORECAST_CONTRACT.md) section 8). Each row is implemented in the step its row names (P7.03 to P7.11) and traced in section 2; MF-18 found no vocabulary mismatch, so no P4 amendment was proposed.
- Export contract v1, additive decisions approved by the user on 2026-10-26 and implemented in P7.03 ([P2 export contract](../p2/P2_EXPORT_CONTRACT.md#16-p7-decisions-additive-forecast-members-and-builder-amendments) section 16): C-120 `run_age_exceeded` after `forecasts` (MF-09); C-121 `IntervalAggregation.Maximum` and `.Sum`; C-122 `wind_gust_kmh` and C-123 `precipitation_amount_mm` after `rain_rate_mm_h` (MF-11); C-124 to C-126 the builder and graph-validation amendments; C-127 `ForecastRunRejectedException.OmittedValues` (MF-02). Regenerated schema `schemas/export/v1/weatherparser-export-v1.xsd`, SHA-256 `6288f1a7dad2909477f9e104621bd8fd04706e09e115742832efba8e2628a0d2` (also verified byte-identical in the packed package by every package check since). Documents that carry the new members validate only against the regenerated schema (C-105 consequence, README).
- Additive library changes: the MOSMIX forecast types listed in the [P8 handoff](P8_HANDOFF.md) section 2; `CatalogRequest.RequestTimeout` and the two additive `CatalogDeadlineKind` members mapped per product (existing kinds unchanged and re-tested); the registration consequence that every consumer of `AddWeatherParsing` registers `MosmixLForecastEndpoints`, `MosmixLForecastOptions`, `MosmixSForecastEndpoints` and `MosmixSForecastOptions` (README).

## 4. Limitations and open items (A4)

| Item | Owner |
|---|---|
| **P7.06 TDD deviation**: several cases of the step plan's S1 and S3 Red sets (one request without retry, transport failure, the streamed MOSMIX_L size bound, the MOSMIX_L stall, the request-timeout carriage, the registration with independent options) were added after Green without a Red; no Red was manufactured afterwards; the tests pass and are part of every run since ([verification record](P7_VERIFICATION.md) section 6) | User: disposition (accept as recorded, or request a remedy); until then P7.06 reads "implemented and verified; completion pending" |
| MOSMIX_S costs one all-stations download per parse (35,966,201 bytes on 2026-10-28; 8,967 ms parse on Windows, 58,710 ms for the synthetic workload on the NanoPi R76S); the compressed archive is held in memory while the member is streamed; the 5-minute deadline is also the request bound (MF-05) | Recorded; external scheduling at most once per hourly run (README) |
| The first interval period may start before `issued_at` (MF-13, recorded, not corrected) | Recorded decision |
| The CRC-32 member check runs on the forecast paths only; the P4 MOSMIX_S evidence refresher is unchanged (P7.04) | Recorded; a P4 amendment only by a new user decision |
| Elements not exported in v1: probabilities, longer totals and gusts, weather codes, cloud cover, visibility, sunshine; no humidity derived from the dew point (MF-10); every unmapped element is reported once per parse as `MOSMIX-ELEMENT-NOT-EXPORTED` (107 for MOSMIX_L and 33 for MOSMIX_S in the live runs) | Recorded decision; extensions are new export decisions |
| `pressure_reference` is `unknown` for MOSMIX pressure until a DWD source states the reduction level (MF-10) | Recorded; revisit with a source statement |
| Registration consequence: consumers that do not use MOSMIX forecasts still register the four option and endpoint types; option failures surface on first resolution, not at `ValidateOnBuild` (README logging note) | Recorded (README); optional registration only by a new decision |
| Schema compatibility: consumers validating against the committed v1 schema of P2 must take the regenerated schema (C-105); exhaustive switches over `IntervalAggregation` must handle `maximum` and `sum` (C-121) | Consumers (README) |
| The live parses are single snapshots per product on Windows (2026-10-28), not on the target; the live MOSMIX_S peak of 240,820,224 bytes was measured in a test host that also buffered the download and is informative only | Recorded limitation (MF-17) |
| The target resource measurement is one run of the synthetic workload (same token count as the real file, smaller compressed size) on an idle device | Recorded limitation; the readiness thresholds of MF-05 and MF-07 stay as approved |
| Example CLI `--SelectMOSMIX`, `--ParseMOSMIX --Product`, configuration, exit codes | P3 |
| MQTT payloads and topics for forecast runs (product-specific sources of C-68, complete retained run of REQ-062) | P8 |
| Inherited: builders and root validation implement the same rules separately (equivalence tests guard it); coarse XML cancellation (C-10); hosted `debian13-arm64` is a container, target evidence only from the NanoPi R76S; the package is not byte-reproducible across builds; DI under load; the MOSMIX_S evidence-refresh CLI exposure; the OsthessenWetter source conditions (OW-14: no redistribution of OsthessenWetter values until clarified) | P8, P3, P9 and the user as in the [P6.11 acceptance record](../p6/P6_11_ACCEPTANCE.md) section 4, carried forward |

## 5. Audit decision (A5)

**ACCEPTED** (orchestrating agent's evidence audit, 2026-10-28, for candidate `a36d15a`):
- every T-P7 row is PASSED with tests and run evidence for `a36d15a`; the live row T-P7-22 is a single authorized snapshot per product; T-P7-20 carries the recorded P7.06 deviation, which the audit reports rather than hides;
- the P7.11 outcome is READY with no open defect, and the target measurement stays within the approved MOSMIX_S deadline and memory threshold;
- the coverage gate passed with 10228/10231 against 8486/8489 on Windows, in hosted CI and on the NanoPi R76S;
- every contract decision is approved and implemented, the export contract changes are additive with the regenerated schema recorded, and each limitation has an owner.

This audit decision is not the user's acceptance. The P7.06 deviation is the one item whose disposition the audit leaves to the user explicitly.

## 6. User decision (A7)

Pending. The user decides on the evidence of this record, the [P7.11 record](P7_11_INTEGRATED_VERIFICATION.md) and the [verification record](P7_VERIFICATION.md): accept on candidate `a36d15a`, accept with amendments (for example a disposition of the P7.06 deviation), or not accept. The wording and date of the decision will be recorded here.

## 7. Status updates (A8)

Status passages read "P7.11 READY on candidate `a36d15a`; not yet accepted or merged — P7.12 follows" since the P7.11 commit `14b7696` (AGENTS.md, README.md, DEVELOPMENT_PLAN.md, PHASE_P1_PLAN.md, PHASE_P7_PLAN.md). After A7 they change to "P7 accepted by the user on <date> on candidate `a36d15a`; complete only once the user merges pull request #11", with the README package check and a `ci-local.ps1` run on the committed revision recorded here.

## 8. Ready for review, review and merge (A9 to A11)

- **A9**: pending the user's instruction; the pull request is a draft.
- **A10**: review findings will be verified and fixed test-first; a production change makes a new candidate and repeats P7.11 S1 to S3.
- **A11**: the user merges; the merge SHA and the P8 integration target will be recorded here and in the post-merge documentation pull request, as after P4, P5 and P6, if the user decides so.

## 9. Authorship and review

- The orchestrating agent wrote this record, the P8 handoff and the status passages from the recorded evidence and ran every check named here.
- Codex reviewed them read-only; the rounds, findings and dispositions are recorded in section 10.

## 10. Codex review rounds
- Round 1 (`codex_p712_r1.md`, outside the repository): 2 findings (P3), both confirmed and fixed: T-P7-01 misdescribed the rejection log (the parsers emit one sanitized `MOSMIX-ID-INVALID` log before throwing, never the ID's content); the C-120 to C-127 reference named section 11 of the P2 export contract instead of section 16.
- Round 2 (`codex_p712_r2.md`): 0 findings. Codex rounds for this record and the P8 handoff: 2/0.
