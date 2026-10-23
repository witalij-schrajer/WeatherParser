# P4.12 – P4 Acceptance Record

**Status (2026-10-23): evidence audit ACCEPTED (section 5); awaiting the user's acceptance decision (section 6). No phase-status document is changed before that decision.** Plan: [P4_12_ACCEPTANCE_HANDOFF_PLAN.md](P4_12_ACCEPTANCE_HANDOFF_PLAN.md); handoff: [P5_HANDOFF.md](P5_HANDOFF.md).

## 1. Review identity (A1)

| Item | Value |
|---|---|
| Implementation candidate | `98122e560370e9de781bd4e0e3ebe0aca270e3d8` (`98122e5`), branch `features/develop/P4` |
| Pull request | #5 into `develop`, draft |
| Integration target | PR base `b7da8288d8746bc6e034222c0467a8fcebba55b3`; checked again on 2026-10-23 (`gh pr view 5` `baseRefOid` and `git rev-parse origin/develop`): unchanged since the P4.11 verification, so no rerun is needed |
| Head at the time of this record | `e98b85c094f3bc0224bd91e3f7d45c754028d84c` (P4.11 completion record; documentation commits after the candidate do not change production code, tests, build, CI, package or schema files) |
| P4.11 outcome | READY ([P4.11 record](P4_11_INTEGRATED_VERIFICATION.md) section 8) |
| Package | `WeatherParser.Parsing` `0.1.0-initial`, not published |
| References | [P4_VERIFICATION.md](P4_VERIFICATION.md) section 11 and the P4.11 record name the candidate; the selection contract gains a verification reference to it with the A8 status updates |

## 2. Requirement and test evidence matrix (A2, T-P4-01 – T-P4-20)

Platform evidence for every row: the P4.11 runs on candidate `98122e5` — Windows `ci-local.ps1` (exit 0), hosted CI run 40023445539 (three jobs green, SHA pair verified) and the NanoPi R76S run (exit 0) — each with 1609 Parsing tests passed (the live test not executed in standard runs), 31 package-consumer tests passed and the gate passed with 6484/6487 blocks. "Red" names the step whose record holds the Red → Green → Refactor evidence; "characterization" marks tests that passed on their first run over existing behavior, recorded in the step record with their provenance.

| T-P4 ID | Requirements | Tests | TDD evidence | Status |
|---|---|---|---|---|
| T-P4-01 | REQ-021, REQ-082 | `CoordinateParserTests` | Red, with recorded characterization, P4.03 (P4_VERIFICATION section 3) | PASSED |
| T-P4-02 | REQ-021, REQ-082 | `CoordinateParserDmsTests` | Red, with recorded characterization, P4.03 (section 3) | PASSED |
| T-P4-03 | REQ-020, REQ-022 | `SearchParameterFactoryTests`; parameter tests of `CdcStationSelectorLimitTests`, `MosmixLocationSelectorLimitTests` | Red, with recorded characterization, P4.03 and P4.09 (sections 3 and 9) | PASSED |
| T-P4-04 | REQ-022 | `DistanceCalculatorTests` (converging Vincenty reference pairs within 1 mm; spherical calculations and the near-antipodal fallback within 0.6 %, SC-06) | Red, P4.04 (section 4) | PASSED |
| T-P4-05 | REQ-022, REQ-082 | `SearchAreaFilterTests`, `SearchAreaFilterCompositionTests` | Red; `Evaluate_WithRealCalculator_LimitsAroundReturnedDistance` characterization, P4.04 (section 4) | PASSED |
| T-P4-06 | REQ-023, REQ-024, REQ-083 | `CdcStationCatalogReaderTests`, `CdcStationCatalogReaderMalformedTests`, `CdcStationCatalogReaderBoundaryTests` | Red; the 7 missing-value and boundary tests and further tests characterization, P4.05 (section 5) | PASSED |
| T-P4-07 | REQ-024 | `CdcAvailabilityDeriverSelectCandidatesTests`, `CdcAvailabilityDeriverDeriveTests`, `CdcNowEvidenceReaderTests` | Red, with recorded characterization, P4.06 (section 6) | PASSED |
| T-P4-08 | REQ-100 to REQ-102, REQ-082 | `MosmixLocationCatalogReaderTests`, `...LineTests`, `...MalformedTests`, `...NegativeBoundsTests`, `...NormalizationTests` | Red; zero and negative altitude normalization characterization, P4.07 (section 7) | PASSED |
| T-P4-09 | REQ-101, REQ-102 | `MosmixAvailabilityResolverTests`, the five `MosmixLEvidenceReader*Tests`, the four `FileSystemMosmixEvidenceCache*Tests`, `MosmixLocationListContractTests` (explicit `unknown`), `MosmixLocationSelectorTests` | Red, with recorded characterization, P4.07 and P4.09 (sections 7 and 9) | PASSED |
| T-P4-10 | REQ-014, REQ-102 | `MosmixCatalogEndpointsValidatorTests`, `CdcCatalogEndpointsValidatorTests`, `CatalogSourceConstructionTests`, `CatalogServiceArgumentTests`, the six `CatalogDownloader*Tests` | Red, with recorded characterization (4 S2 failure cases in P4.08), P4.07 and P4.08 (sections 7 and 8) | PASSED |
| T-P4-11 | REQ-023, REQ-068 | `CdcStationSelectorTests`, `CdcStationSelectorLimitTests`, `ComposedCdcPipelineTests` | Red, P4.09 (section 9); the composed test is characterization | PASSED |
| T-P4-12 | REQ-101, REQ-068 | `MosmixLocationSelectorTests`, `MosmixLocationSelectorLimitTests`, `ComposedMosmixPipelineTests` | Red, P4.09 (section 9); the composed tests are characterization | PASSED |
| T-P4-13 | REQ-067, REQ-068, REQ-082 | `SelectionExportContractTests`; package side `PackageSelectionEquivalenceTests` | Characterization, P4.09 S3 and P4.10 (sections 9 and 10) | PASSED |
| T-P4-14 | REQ-013, REQ-014, REQ-016 | `SelectionDependencyInjectionTests`, `SelectionDependencyInjectionRoutingTests`, `PackageSelectionTests`, `PackageMosmixEvidenceTests`, `PackageSelectionEquivalenceTests`; C-116 package evidence of the committed runs | Characterization, P4.10 (section 10; no DI, package or API defect found) | PASSED |
| T-P4-15 | REQ-085 | Red, Green and Refactor recorded per slice in P4.03 to P4.10 and P4.11 S0; characterization recorded with provenance | Sections 3 to 10; P4.11 record section 2 | PASSED |
| T-P4-16 | REQ-086 | Coverage gate: 6484/6487 (99.95 %) against the remeasured integration-target baseline 3035/3037 (99.93 %); minimum 80 %; strict improvement; unrounded native counters; comparable collection settings | P4.11 record sections 4 to 6 | PASSED |
| T-P4-17 | REQ-017, REQ-084 | All P4 tests and the gate on the NanoPi R76S, separate from Windows, identical outcomes | P4.11 record section 6 (user authorization 2026-10-23) | PASSED |
| T-P4-18 | REQ-083, REQ-092 | `LiveCheck_DwdSources_OnlyWhenExplicitlyEnabled` with `LiveCheckRunnerTests`, `LiveCheckRunnerFailureTests` | Red, P4.11 S0; one explicitly enabled live run (P4.11 record sections 2 and 7) | PASSED (optional row, run once) |
| T-P4-19 | REQ-101, REQ-102, REQ-083 | The four `FileSystemMosmixEvidenceCache*Tests`, the six `MosmixSEvidenceRefresher*Tests`, the index tests of the MOSMIX selector tests, `ComposedMosmixPipelineTests`, `PackageMosmixEvidenceTests`, the index cases of `LiveCheckRunnerTests` | Red, P4.07 and P4.08; selector and consumer parts P4.09 and P4.10 (sections 7 to 10) | PASSED |
| T-P4-20 | REQ-067, REQ-068, REQ-079 | `MosmixEvidenceTimestampTests`, `MosmixEvidenceTimestampWireTests`, `SchemaRefinementTests` (schema generator), `PackageEvidenceTimestampTests`, `PackageMosmixEvidenceTests` | Red, P4.07; consumer part P4.10 (sections 7 and 10) | PASSED |

Characterization evidence recorded in the step records (tests that passed on their first run over behavior that already existed, with their provenance): P4.03 — the three `CoordinateFormatExceptionTests` and the three `SearchParameterExceptionTests` (types complete in the scaffold) and 12 coordinate rejection tests (section 3); P4.04 — `Evaluate_WithRealCalculator_LimitsAroundReturnedDistance` (section 4); P4.05 — the six `CatalogFormatExceptionTests`, the 7 missing-value and boundary tests of S2, 16 further reader tests and the header/dash-line length test (section 5); P4.06 — the coverage-gap tests for bounds, malformed headers and rows and argument checks added in S4 (section 6); P4.07 — the zero and negative altitude normalization tests, `Read_LeavesTheCallersStreamOpenOnSuccessAndOnFailure` and the review-added token-count and negative-coordinate tests (section 7); P4.08 — four S2 failure cases (section 8); P4.09 — the composed pipeline and S3 export contract tests (section 9); P4.10 — all DI and package scenarios (section 10); P4.11 S0 — the budget-handler and redirect tests (P4.11 record section 2).

No row is FAILED or OPEN; no acceptance criterion was amended.

## 3. Contract summary (A3)

- Selection contract: [P4_SELECTION_CONTRACT.md](P4_SELECTION_CONTRACT.md), rows SC-01 to SC-23 approved by the user on 2026-10-21 (SC-05 with MOSMIX_L option A; `MosmixSEvidenceMaxAge` without default): [SC-01 to SC-23 in the decision register](P4_SELECTION_CONTRACT.md#4-decision-register) — record and scope (SC-01), CDC catalog (SC-02), CDC availability (SC-03), MOSMIX catalog (SC-04), MOSMIX product availability (SC-05), geodetic method (SC-06), coordinate grammar (SC-07), logging of notices (SC-08), retrieval boundary (SC-09), catalog failure semantics (SC-10), public selection API (SC-11), service registration (SC-12), list metadata and time source (SC-13), fixtures (SC-14), live verification (SC-15), source conditions and attribution (SC-16), error and notice IDs (SC-17), per-station request bound (SC-18), CDC evidence files (SC-19), MOSMIX_L evidence files (SC-20), reader reuse (SC-21), MOSMIX_S evidence cache (SC-22), evidence timestamps (SC-23).
- Export contract: [P2_EXPORT_CONTRACT.md](../p2/P2_EXPORT_CONTRACT.md) v1 is unchanged except for the additive decisions C-117 to C-119 (two optional evidence timestamps `evidence_retrieved_at` and `evidence_issued_at` on `MosmixProductAvailability`, the `DateTime?` companion recognition of the schema generator and the regenerated v1 schema), allowed after the v1 freeze by C-03 and C-105. Namespace `urn:weatherparser:export:v1` and `schema_version` `"1"` are unchanged.

## 4. Limitations and open items (A4)

| Item | Owner |
|---|---|
| Unit conversions, quality codes and full observation parsing for the SC-03 quantities | P5 ([P5 handoff](P5_HANDOFF.md)) |
| Reader reuse: P5 builds on `ICdcNowEvidenceReader` (SC-19); P7 builds on `IMosmixLEvidenceReader` (SC-20) and the streaming reader of the MOSMIX_S refresh (SC-22) | P5, P7 (SC-21) |
| MOSMIX element verification, freshness per product, full MOSMIX parsing (SC-20 checks only structure, identity and element presence) | P7 |
| Example-CLI exposure of the MOSMIX_S evidence refresh (needs a requirement change and a CLI contract decision) | User, P3 (SC-22) |
| The live check is a single snapshot of 2026-10-23 on Windows, not on the target, and is not repeated | Recorded limitation (SC-16) |
| Hosted `debian13-arm64` is a container; target evidence exists only for the NanoPi R76S; release hardware acceptance | P9 (REQ-084) |
| The packed package is not byte-reproducible across builds; C-116 compares packed and restored hashes within one run | Recorded; revisit with publication (P9) if required |
| Several rows rely on characterization evidence over existing behavior (section 2) | Recorded; no rework planned |
| The local `develop` branch of the developer machine is stale (`74bf625`); verification used the PR base | Recorded fact |
| DI under load, MQTT payloads and topics, CLI, configuration, exit codes, broker transport | P8, P3 |
| Publication, package ID, feed, release | P9 |
| Inherited from P2: coarse XML cancellation (before serialization and during the copy, C-10) | Accepted limitation, carried forward; revisit if a later phase needs finer cancellation |
| Inherited from P2: builders and root validation implement the same rules separately; equivalence tests guard divergence | P5 to P7 (when the parsers use the builders), carried forward |
| Inherited from P2: forecast-only quantities (gusts, probabilities, totals) are not yet in the export contract | P7 (additive contract decisions), carried forward |
| Inherited from P2: `de_DE.UTF-8` system locale not installed on the target; culture checks use ICU data | Recorded platform fact, carried forward |
| Inherited from P2: coordinate parsing, geodesy, catalogs and availability derivation | Completed by P4 (this record) |
| Inherited from P2: provider extraction and source mappings; MQTT; CLI; publication | P5 to P7; P8; P3; P9 (rows above) |
| P1 limitations ([P1 acceptance record](../p1/P1_12_ACCEPTANCE.md)) | Unchanged |

## 5. Audit decision (A5)

**ACCEPTED** (orchestrating agent's evidence audit, 2026-10-23): every T-P4 row is PASSED with tests and run evidence for candidate `98122e5`; P4.11 reported READY with no open defect; the coverage gate passed with 6484/6487 against 3035/3037 on Windows (Windows packed SHA-512 `7P7sYMqKWtEJXRIUZUYeASFJAZFhtd1rnrwgzOqSrmY/3kb9j+tYGx0b3txYLpnDgbuWHeoWw1S+BGbZoz21VA==`) and on the NanoPi R76S (packed SHA-512 `hKlt6WlOi/PQwjoQtGl6TxYF8zU+8ZcCkLsAHmvtwSmKau6ml2/WWOrzWx+pCpF7qutWuqzhwuglNFZe9H5ttw==`), hosted CI run 40023445539 is green with the SHA pair verified; test counts per platform: Parsing 1609 passed (1610 with the skipped live test), Coverage 99, Coverage.Runner 56, SchemaGeneration 46, package consumer 31; every contract decision is approved; each limitation has an owner. This audit decision is not the user's acceptance.

## 6. User decision (A7)

Pending.

## 7. Status updates after acceptance (A8)

Pending.

## 8. Ready for review, review and merge (A9 to A11)

Pending.

## 9. Authorship

The orchestrating agent wrote this record from its verified evidence (step records, P4.11 runs and artifacts, the T-P4 mapping); qwen3-coder:30b drafted the [P5 handoff](P5_HANDOFF.md) from the orchestrating agent's facts, which the agent checked against the code (every cited type name) and the plans. Codex review of the draft: round 1, 4 findings, all confirmed and fixed — inherited P2 limitations now carried forward item by item with owners (record and handoff), characterization evidence named per step in the matrix (agent), API and schema links in the handoff (qwen added them; the agent corrected the Markdown syntax) and the coverage rule stated as the ratio rule of REQ-086 (the agent, after qwen did not apply it). Round 2: 0 findings.
