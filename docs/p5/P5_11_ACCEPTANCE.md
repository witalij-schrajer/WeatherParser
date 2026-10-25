# P5.11 – P5 Acceptance Record

**Status (2026-10-25): the evidence audit (section 5) accepts the implementation candidate `fd461c8`. P5 was accepted by the user on 2026-10-25 (section 6), and the status documents are updated (section 7). P5 is complete only once the user merges pull request #7 into `develop` (A11, section 8).** Plan: [P5_11_ACCEPTANCE_HANDOFF_PLAN.md](P5_11_ACCEPTANCE_HANDOFF_PLAN.md); handoff: [P6_HANDOFF.md](P6_HANDOFF.md).

## 1. Review identity (A1)

| Item | Value |
|---|---|
| Implementation candidate | `fd461c8cededddf53a039c31adad68a110c1c7c9` (`fd461c8`), branch `features/develop/P5`. It carries the fixes of the PR #7 review of `6492091` and replaces the first verification candidate `a87c7bb` ([P5.10 record](P5_10_INTEGRATED_VERIFICATION.md) section 9) |
| Pull request | #7 into `develop`, draft until A9 (section 8) |
| Integration target | PR base `1e604dc7b3f17826d19207cbec9a699e40e5a30f`; checked again on 2026-10-25 (`gh pr view 7` `baseRefOid` and `git rev-parse origin/develop` after `git fetch`): unchanged since P5.01 |
| Head before this record | `c3d49ae` (P5.10 rerun records); like this record's commit it changes documentation only, no production code, tests, build, CI, package or schema files |
| P5.10 outcome | READY for `a87c7bb` (record section 8) and, after the rerun of S1 to S3, for `fd461c8` (section 9); S4 not repeated |
| Package | `WeatherParser.Parsing` `0.1.0-initial`, not published |
| References | [P5_VERIFICATION.md](P5_VERIFICATION.md) section 10 and the P5.10 record name the candidate; the observation contract gains a verification reference to it with the A8 status updates |

## 2. Requirement and test evidence matrix (A2, T-P5-01 – T-P5-19)

Platform evidence for every row comes from the P5.10 rerun on candidate `fd461c8` ([P5.10 record](P5_10_INTEGRATED_VERIFICATION.md) section 9):
- Windows `ci-local.ps1`: exit 0.
- Hosted CI run 40170729670: three jobs green, SHA pair verified.
- NanoPi R76S run: exit 0, identical test outcomes.

Each run passed 1937 Parsing tests (the two live tests are not executed in standard runs) and 36 package-consumer tests, and its gate passed with 7491/7494 blocks against the baseline 6811/6814. The first runs on `a87c7bb` (record sections 4 to 6) had the same outcomes with 1928 Parsing tests and 7481/7484 blocks. The only exception is T-P5-18, whose single live run was on `a87c7bb` (see the row).

"Red" names the step whose record in [P5_VERIFICATION.md](P5_VERIFICATION.md) holds the Red → Green → Refactor evidence. "Characterization" marks tests that passed on their first run over behavior that already existed; each step record names them with their provenance.

| T-P5 ID | Requirements | Tests | TDD evidence | Status |
|---|---|---|---|---|
| T-P5-01 | REQ-030, REQ-031 | `DwdStationResolverTests` (ID grammar cases), `DwdStationExceptionTests` | Red, P5.03 (section 3) | PASSED |
| T-P5-02 | REQ-031, REQ-023 | `DwdStationResolverTests` (unknown station, metadata, products, conflict) | Red, P5.03 (section 3) | PASSED |
| T-P5-03 | REQ-032, REQ-083 | `CdcNowEvidenceReaderColumnsTests`; the P4 reader tests unchanged | Red, P5.04 (section 4) | PASSED |
| T-P5-04 | REQ-032, REQ-053 | `DwdObservationMapperTests` | Red, P5.05 (section 5) | PASSED |
| T-P5-05 | REQ-052, REQ-054, REQ-057 | `DwdObservationMapperTests`, `DwdObservationExportContractTests` (wire values), `PackageDwdObservationTests` (packaged wire values) | Red, P5.05; characterization, P5.08, P5.09 and the PR #7 review fix (P5.10 record section 9) | PASSED |
| T-P5-06 | REQ-055 | `DwdObservationMapperPeriodTests` (including the period-start boundary of the PR #7 review fix), `DwdObservationExportContractTests`, `PackageDwdObservationTests` (full station) | Red, P5.05 and the PR #7 review fix; characterization, P5.08 and the package case | PASSED |
| T-P5-07 | REQ-053 | `DwdObservationMapperTests` (calm and zero direction), `DwdObservationParserLoggingTests`, `ComposedDwdObservationPipelineTests`, `DwdObservationExportContractTests` (all calm, all zero direction) | Red, P5.05 and P5.07; characterization, P5.08 (sections 5, 7 and 8) | PASSED |
| T-P5-08 | REQ-032 | `DwdObservationMapperTests` (quality token), `DwdObservationExportContractTests` (quality per measurement) | Red, P5.05; characterization, P5.08 | PASSED |
| T-P5-09 | REQ-043, REQ-051, REQ-056 | `DwdObservationMapperTimeTests`, `DwdObservationParserTests` | Red, P5.05 and P5.07 (sections 5 and 7) | PASSED |
| T-P5-10 | REQ-014, REQ-063 | `DwdObservationSourceTests`, `DwdObservationSourceDeadlineTests`, `CdcRequestComposerTests`, `CatalogDownloaderObservationDeadlineTests`, `CdcCatalogEndpointsValidatorTests`, `DwdObservationParserDeadlineTests`, `DwdObservationParserFailureTests` | Red, P5.06 and P5.07 (sections 6 and 7) | PASSED |
| T-P5-11 | REQ-050, REQ-031 | `DwdObservationParserTests`, `ObservationDatasetBuilderTests`, `ComposedDwdObservationPipelineTests` | Red, P5.07 (section 7) | PASSED |
| T-P5-12 | REQ-051, REQ-067, REQ-068, REQ-069, REQ-079 | `DwdObservationExportContractTests`, `ObservationExportEquivalenceTests`, `AllRootsEquivalenceTests` | Characterization, P5.08 (section 8; shared helper extracted with the P2 tests unchanged) | PASSED |
| T-P5-13 | REQ-013, REQ-014, REQ-016, REQ-078 | `DwdObservationDependencyInjectionTests`, `PackageDwdObservationTests`, `ServiceRegistrationCompletenessTests`, `NoSharedStateTests`; C-116 package evidence of the committed runs | Characterization, P5.09 (section 9) and the PR #7 review fix; the registration of each new interface with its step (P5.03, P5.05 to P5.07) | PASSED |
| T-P5-14 | REQ-090, REQ-092 | README section "DWD observations (library)"; package check (CONTRIBUTING.md section 4) | P5.09 (section 9), Codex 3, 1, 0 findings | PASSED |
| T-P5-15 | REQ-085 | Red, Green and Refactor recorded per slice in P5.03 to P5.09, P5.10 S0 and the PR #7 review fix; characterization recorded with provenance | Sections 3 to 9; P5.10 record sections 2 and 9 | PASSED |
| T-P5-16 | REQ-086 | Coverage gate: 7491/7494 (99.96 %) against the remeasured integration-target baseline 6811/6814 (99.96 %); minimum 80 %; strict improvement on unrounded native counters; comparable collection settings | P5.10 record section 9 (first candidate: sections 4 to 6) | PASSED |
| T-P5-17 | REQ-017, REQ-084 | All P5 tests and the gate on the NanoPi R76S, separate from Windows, identical outcomes | P5.10 record section 9 and section 6 (user authorization 2026-10-25) | PASSED |
| T-P5-18 | REQ-083, REQ-092 | `LiveCheck_DwdObservation_OnlyWhenExplicitlyEnabled` with the offline `LiveCheckObservationRunnerTests` | Red, P5.10 S0; one explicitly enabled live run of station 01526 on `a87c7bb` (P5.10 record sections 2 and 7). Not repeated for `fd461c8`: the fix only rejects timestamps before 0001-01-01 00:10, which live data cannot reach | PASSED (optional row, run once on `a87c7bb`) |
| T-P5-19 | REQ-044, REQ-072 | `DwdObservationParserLoggingTests`, `DwdObservationParserFailureTests` | Red, P5.07 (section 7) | PASSED |

No row is FAILED or OPEN; no acceptance criterion was amended.

## 3. Contract summary (A3)

- **Observation contract**: [P5_OBSERVATION_CONTRACT.md](P5_OBSERVATION_CONTRACT.md), rows OC-01 to OC-14, approved by the user on 2026-10-24 with OC-02 option A, OC-05 `RWS_10` option A, OC-06 option B and OC-12 option A:
  - OC-01 station ID grammar
  - OC-02 station existence and metadata
  - OC-03 products to retrieve and missing products
  - OC-04 value selection and times
  - OC-05 units, references and periods
  - OC-06 calm wind
  - OC-07 quality code
  - OC-08 public API
  - OC-09 options, registration and deadline
  - OC-10 error and notice IDs
  - OC-11 request bound
  - OC-12 source conditions and retrieval guidance
  - OC-13 live verification
  - OC-14 shared reader extension
- **Export contract**: [P2_EXPORT_CONTRACT.md](../p2/P2_EXPORT_CONTRACT.md) v1 is unchanged by P5. No file under `docs/p2`, `schemas` or `src/WeatherParser.Parsing/Export` changed between `1e604dc` and `fd461c8`. The DWD observation dataset uses the existing `ObservationDataset` root, its measurement members and the v1 schema. Namespace `urn:weatherparser:export:v1` and `schema_version` `"1"` are unchanged.
- **Additive library API of P5**: none of it is an export entity.
  - `IDwdObservationParser` / `DwdObservationParser`, `DwdObservationOptions`, `DwdObservationNoticeIds`
  - `IDwdStationResolver` / `DwdStationResolver` with `DwdResolvedStation`
  - `IDwdObservationMapper` / `DwdObservationMapper` with `DwdMappingResult`, `DwdMappingOmission` and `DwdMappingOmissionReason`
  - `IDwdObservationSource` / `DwdObservationSource`
  - `ICdcRequestComposer` / `CdcRequestComposer` with `CdcListRequestTarget` and `CdcArchiveRequestTarget`
  - `DwdStationException`, `ObservationLimitException`
  - additive members: `CatalogDeadlineKind.Observation`, `ICdcCatalogEndpointsValidator.ValidateEndpoints`, `CdcNowEvidenceRow.QualityLevel` and all data columns in `Values` (OC-14), `ObservationDatasetRejectedException.OmittedReadings`

## 4. Limitations and open items (A4)

| Item | Owner |
|---|---|
| The `RWS_10` time stamp is instrument dependent (with a Pluvio Ott gauge the physical interval ends about 5 minutes earlier) and is not corrected; the exported period is the nominal one (OC-05 option A, README) | Recorded limitation; a correction needs a new user decision |
| Calm wind and zero direction: the direction is omitted when `FF_10` or `DD_10` is 0, because DWD documents no calm marker; this is a project assumption (OC-06 option B) | Recorded assumption; revisit if DWD documents a calm marker (user decision) |
| The quality control of the `now` data is not yet completed and values may still change; `quality` passes the `QN` token unchanged (OC-07) | Consumers (README guidance) |
| No freshness filter: a station that stopped reporting still yields its newest values with their own `measured_at` (OC-04) | Consumers evaluate `measured_at` (README) |
| The live parse is a single snapshot of 2026-10-25 on Windows on candidate `a87c7bb`, not on the target and not repeated for `fd461c8` (OC-13). Its station 01526 reported no pressure, so the pressure path has synthetic evidence only | Recorded limitation |
| Scheduling (at most one parse per station every 10 minutes is a project recommendation, OC-12) | External orchestration (cron, n8n), not a dependency |
| Example CLI `--ParseDWD`, configuration, exit codes, CLI cancellation and timeout bounds | P3 |
| MQTT payloads and topics for observations | P8 |
| Inherited from P4: DI under load (P5 verifies singleton registration, independent containers and build validation, not concurrent load) | P8, P3 |
| Hosted `debian13-arm64` is a container; target evidence exists only for the NanoPi R76S; release hardware acceptance | P9 (REQ-084) |
| The packed package is not byte-reproducible across builds; C-116 compares packed and restored hashes within one run | Recorded; revisit with publication (P9) if required |
| Several rows rely on characterization evidence over existing behavior (section 2) | Recorded; no rework planned |
| Publication, package ID, feed, release | P9 |
| Inherited from P4: MOSMIX element verification, freshness per product and full MOSMIX parsing | P7 |
| Inherited from P4: example-CLI exposure of the MOSMIX_S evidence refresh (needs a requirement change) | User, P3 (SC-22) |
| Inherited from P4: the selection live check is a single snapshot of 2026-10-23 (SC-16) | Recorded limitation, carried forward |
| Inherited from P2: coarse XML cancellation (C-10) | Accepted limitation, carried forward |
| Inherited from P2: builders and root validation implement the same rules separately; P5 now uses `IObservationDatasetBuilder`, and the equivalence tests guard divergence | P6, P7, carried forward |
| Inherited from P2: forecast-only quantities (gusts, probabilities, totals) are not yet in the export contract | P7 (additive contract decisions) |
| Inherited from P2: the `de_DE.UTF-8` system locale is not installed on the target; culture checks use ICU data | Recorded platform fact, carried forward |
| P1 limitations ([P1 acceptance record](../p1/P1_12_ACCEPTANCE.md)) | Unchanged |

## 5. Audit decision (A5)

**ACCEPTED** (orchestrating agent's evidence audit, 2026-10-25, for candidate `fd461c8`):
- Every T-P5 row is PASSED with tests and run evidence for `fd461c8`. The exception is T-P5-18, the optional live row, whose single run was on `a87c7bb`.
- The P5.10 rerun reported READY with no open defect.
- The coverage gate passed with 7491/7494 against 6811/6814 on Windows (packed SHA-512 `H3eh/g0+Ip6jMDvoYOzEqalfXN/T80+4Ukjxa8Y0+c+/98sz2trIVwG1PXDryKJupM/YVuKO/eKP/Qr/g2km3Q==`) and on the NanoPi R76S (packed SHA-512 `pTeA9PGxJRxg5tG5lx53YuMDsdCN2kj5tJqQuIT/fpaqWSJRm3rUhm/qWBb6/gdXUzfEESOjFi6OMaHvgZHL/Q==`).
- Hosted CI run 40170729670 is green with the SHA pair verified.
- Test counts per platform: Parsing 1937 passed (1939 with the two skipped live tests), Coverage 99, Coverage.Runner 56, SchemaGeneration 46, package consumer 36.
- Both findings of the PR #7 review of `6492091` are fixed and answered (P5.10 record section 9). The earlier documentation review of `248d270` was resolved by `485d492` and approved on `485d492`.
- Every contract decision is approved, and each limitation has an owner.

This audit decision is not the user's acceptance.

## 6. User decision (A7)

**P5 accepted by the user on 2026-10-25 on candidate `fd461c8cededddf53a039c31adad68a110c1c7c9`.**

The user's message in chat (2026-10-25): "I accept P5 (DWD observations) on candidate fd461c8. Go ahead with P5.11: acceptance record, status updates, mark PR #7 ready for review."

- **Order**: the user decided on the basis of the P5.10 record and its rerun section 9, which were presented before the decision. This record and the P6 handoff were written afterwards, deviating from the plan's order (A2 to A6 before A7). They compile the same evidence and change no verification result, contract claim, limitation or acceptance criterion.
- **Effect**: the acceptance amends no acceptance criterion; the limitations of section 4 stay open with their owners. Under the PR workflow, P5 is complete only once the user merges pull request #7 into `develop` (A11).

## 7. Status updates after acceptance (A8)

Status passages updated on 2026-10-25:
- `AGENTS.md`, `README.md`: the phase-status line and a dated line.
- `DEVELOPMENT_PLAN.md`: the phase-status line.
- `PHASE_P1_PLAN.md`: an update sentence.
- `PHASE_P5_PLAN.md`: the status line, and the section 9 checklist ticked except the pull-request and merge item.
- `P5_OBSERVATION_CONTRACT.md`: the status line with the verification reference to `fd461c8`.
- `P5_VERIFICATION.md`: the status line and section 11.
- `P5_11_ACCEPTANCE_HANDOFF_PLAN.md`: the status line.

Every passage says that P5 is complete only once the user merges pull request #7 into `develop`. The README change triggers the package check (AGENTS.md consistency table).

The working-tree checks and the Codex review rounds are recorded in [P5_VERIFICATION.md](P5_VERIFICATION.md) section 11. The checkpoint commit, its `ci-local.ps1` run and the push are recorded there once executed.

## 8. Ready for review, review and merge (A9 to A11)

- **A9 (ready for review)**: on the user's instruction of 2026-10-25, after the A8 push (plan section 3). The `ready_for_review` CI run will be recorded in [P5_VERIFICATION.md](P5_VERIFICATION.md) section 11 once it has run.
- **A10 (review)**: further review comments are handled by the new-candidate rule of the plan.
- **A11 (merge)**: the user's. The home of the post-merge records is decided by the user at A11.

## 9. Authorship and review

- The orchestrating agent wrote this record, the P6 handoff and the status passages (documentation, AGENTS.md user decision of 2026-10-24) from the recorded evidence and ran every check.
- Codex reviewed them read-only (rounds in [P5_VERIFICATION.md](P5_VERIFICATION.md) section 11).
