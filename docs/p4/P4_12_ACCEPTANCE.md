# P4.12 – P4 Acceptance Record

**Status (2026-10-24): the evidence audit (section 5) accepts the new implementation candidate `b6bc47f`, which carries the A10 review fixes (section 8.1). P4.11 S1 to S3 were rerun on it with outcome READY, and S4 was not repeated (SC-16). A2, A5 and the P5 handoff (A6) are refreshed. P4 was accepted by the user on 2026-10-24 (section 6), and the status documents are updated (section 7). P4 is complete: pull request #5 merged into `develop` on 2026-10-24 (merge commit `95bcb1e`, section 8.3).** Plan: [P4_12_ACCEPTANCE_HANDOFF_PLAN.md](../development/p4/P4_12_ACCEPTANCE_HANDOFF_PLAN.md); handoff: [P5_HANDOFF.md](P5_HANDOFF.md).

## 1. Review identity (A1)

| Item | Value |
|---|---|
| Implementation candidate | `b6bc47f89147c05bf04e06cf0f87c1497bc651f3` (`b6bc47f`), branch `features/develop/P4`: the A10 review fixes (section 8.1). It replaces the first candidate `98122e5`, whose evidence was the basis of the first version of this record |
| Pull request | #5 into `develop`; marked ready for review by the user on 2026-10-23T21:40:30Z (section 8) |
| Integration target | PR base `b7da8288d8746bc6e034222c0467a8fcebba55b3`; checked again on 2026-10-24 (`gh pr view 5` `baseRefOid` and `git rev-parse origin/develop` after `git fetch`): unchanged since the P4.11 verification |
| Head at the time of this record | `b6bc47f89147c05bf04e06cf0f87c1497bc651f3`; the documentation commit carrying this refresh changes no production code, tests, build, CI, package or schema files |
| P4.11 outcome | READY for `98122e5` ([P4.11 record](P4_11_INTEGRATED_VERIFICATION.md) section 8) and, after the rerun of S1 to S3, for `b6bc47f` (section 9); S4 not repeated (SC-16) |
| Package | `WeatherParser.Parsing` `0.1.0-initial`, not published |
| References | [P4_VERIFICATION.md](P4_VERIFICATION.md) section 11 and the P4.11 record name the candidate; the selection contract gains a verification reference to it with the A8 status updates |

## 2. Requirement and test evidence matrix (A2, T-P4-01 – T-P4-20)

Platform evidence for every row comes from the P4.11 rerun on candidate `b6bc47f` ([P4.11 record](P4_11_INTEGRATED_VERIFICATION.md) section 9):
- Windows `ci-local.ps1`: exit 0.
- Hosted CI run 40041773136: three jobs green, SHA pair verified.
- NanoPi R76S run: exit 0.

Each run passed 1662 Parsing tests (the live test is not executed in standard runs) and 31 package-consumer tests, and its gate passed with 6811/6814 blocks. The first runs on `98122e5` (record sections 4 to 6) had the same outcomes with 1609 Parsing tests and 6484/6487 blocks. The only exception is T-P4-18, whose single live run was on `98122e5` (see the row). "Red" names the step whose record holds the Red → Green → Refactor evidence; "characterization" marks tests that passed on their first run over existing behavior, recorded in the step record with their provenance.

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
| T-P4-09 | REQ-101, REQ-102 | `MosmixAvailabilityResolverTests`, the five `MosmixLEvidenceReader*Tests`, the five `FileSystemMosmixEvidenceCache*Tests`, `MosmixLocationListContractTests` (explicit `unknown`), `MosmixLocationSelectorTests` | Red, with recorded characterization, P4.07 and P4.09 (sections 7 and 9) | PASSED |
| T-P4-10 | REQ-014, REQ-102 | `MosmixCatalogEndpointsValidatorTests`, `CdcCatalogEndpointsValidatorTests`, `CatalogSourceConstructionTests`, `CatalogServiceArgumentTests`, the six `CatalogDownloader*Tests` | Red, with recorded characterization (4 S2 failure cases in P4.08), P4.07 and P4.08 (sections 7 and 8) | PASSED |
| T-P4-11 | REQ-023, REQ-068 | `CdcStationSelectorTests`, `CdcStationSelectorLimitTests`, `ComposedCdcPipelineTests` | Red, P4.09 (section 9); the composed test is characterization | PASSED |
| T-P4-12 | REQ-101, REQ-068 | `MosmixLocationSelectorTests`, `MosmixLocationSelectorLimitTests` (including the A10 deadline and caller-cancellation cases of the index read), `ComposedMosmixPipelineTests` | Red, P4.09 (section 9) and A10 (this record, section 8.1); the composed tests are characterization | PASSED |
| T-P4-13 | REQ-067, REQ-068, REQ-082 | `SelectionExportContractTests`; package side `PackageSelectionEquivalenceTests` | Characterization, P4.09 S3 and P4.10 (sections 9 and 10) | PASSED |
| T-P4-14 | REQ-013, REQ-014, REQ-016 | `SelectionDependencyInjectionTests`, `SelectionDependencyInjectionRoutingTests`, `PackageSelectionTests`, `PackageMosmixEvidenceTests`, `PackageSelectionEquivalenceTests`; C-116 package evidence of the committed runs | Characterization, P4.10 (section 10; no DI, package or API defect found) | PASSED |
| T-P4-15 | REQ-085 | Red, Green and Refactor recorded per slice in P4.03 to P4.10 and P4.11 S0; characterization recorded with provenance | Sections 3 to 10; P4.11 record section 2 | PASSED |
| T-P4-16 | REQ-086 | Coverage gate: 6811/6814 (99.96 %) against the remeasured integration-target baseline 3035/3037 (99.93 %); minimum 80 %; strict improvement; unrounded native counters; comparable collection settings | P4.11 record section 9 (first candidate: sections 4 to 6) | PASSED |
| T-P4-17 | REQ-017, REQ-084 | All P4 tests and the gate on the NanoPi R76S, separate from Windows, identical outcomes | P4.11 record section 9 (user instruction 2026-10-24), first candidate section 6 (user authorization 2026-10-23) | PASSED |
| T-P4-18 | REQ-083, REQ-092 | `LiveCheck_DwdSources_OnlyWhenExplicitlyEnabled` with `LiveCheckRunnerTests`, `LiveCheckRunnerFailureTests` | Red, P4.11 S0; one explicitly enabled live run on `98122e5` (P4.11 record sections 2 and 7). Not repeated for `b6bc47f` (SC-16). The changed MOSMIX_S parser built the same index offline from a stored real DWD file (record section 9) | PASSED (optional row, run once on `98122e5`) |
| T-P4-19 | REQ-101, REQ-102, REQ-083 | The five `FileSystemMosmixEvidenceCache*Tests`, the eight `MosmixSEvidenceRefresher*Tests`, the index tests of the MOSMIX selector tests, `ComposedMosmixPipelineTests`, `PackageMosmixEvidenceTests`, the index cases of `LiveCheckRunnerTests` | Red, P4.07 and P4.08; selector and consumer parts P4.09 and P4.10 (sections 7 to 10); streaming bounds, concurrency and index-read deadline A10 (this record, section 8.1) | PASSED |
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
| The live check is a single snapshot of 2026-10-23 on Windows, not on the target, and is not repeated. It ran on candidate `98122e5`, so the A10 changes to the MOSMIX_S refresher and the index read have no live run. The refresher change is covered offline against a stored real DWD file (P4.11 record section 9) | Recorded limitation (SC-16) |
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

**ACCEPTED** (orchestrating agent's evidence audit, refreshed 2026-10-24 for candidate `b6bc47f`):
- Every T-P4 row is PASSED with tests and run evidence for `b6bc47f`. The exception is T-P4-18, the optional live row, whose single run was on `98122e5` (SC-16; offline supplement in P4.11 record section 9).
- The P4.11 rerun reported READY with no open defect.
- The coverage gate passed with 6811/6814 against 3035/3037 on Windows (packed SHA-512 `dCCn3uSuMMvrF1eT6hAudmi8isancaeohI9aDhsT4fj6Ya5RBz28cdDG11LRLuXDYCoOt/YezbVFvFD6plAsTQ==`) and on the NanoPi R76S (packed SHA-512 `ccLueUXv0KDJf0rBASUzSnWEgGfGi/pE3Bes2eDeFUvUeYF2Hpq3+ddgHyVWINK8EPgy/A9gK5cRYXPQHr7KMQ==`).
- Hosted CI run 40041773136 is green with the SHA pair verified.
- Test counts per platform: Parsing 1662 passed (1663 with the skipped live test), Coverage 99, Coverage.Runner 56, SchemaGeneration 46, package consumer 31.
- All three A10 review comments are fixed and answered (section 8.1).
- Every contract decision is approved, and each limitation has an owner.

This audit decision is not the user's acceptance. The first audit (2026-10-23) accepted candidate `98122e5` with 6484/6487 blocks and hosted CI run 40023445539; this refresh supersedes it.

## 6. User decision (A7)

**P4 accepted by the user on 2026-10-24 on candidate `b6bc47f89147c05bf04e06cf0f87c1497bc651f3`.**

The user's message in chat (2026-10-24): "I accept P4 on candidate b6bc47f. Record the acceptance (A7), update the status documents (A8) and record the review outcome; then mark PR #5 ready for review (A9)."

The acceptance follows the refreshed evidence audit (section 5) and the review outcome of section 8.2. It amends no acceptance criterion; the limitations of section 4 stay open with their owners. Under the PR workflow, P4 is complete only once the user merges pull request #5 into `develop` (A11).

## 7. Status updates after acceptance (A8)

Status passages updated on 2026-10-24:
- `AGENTS.md`: status line and a dated line.
- `README.md`: status line and a dated line.
- `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md`: status note.
- `PHASE_P4_PLAN.md`: status line, and the section 9 checklist ticked except the pull-request and merge item.
- `P4_SELECTION_CONTRACT.md`: status line with the verification reference to `b6bc47f`.
- `P4_VERIFICATION.md`: status line and section 12.
- `P4_12_ACCEPTANCE_HANDOFF_PLAN.md`: status line.

Every passage says that P4 is complete only once the user merges pull request #5 into `develop`. After the merge the passages were changed to "P4 complete" (section 8.3).

Checks on the working tree (HEAD `13ca95e` plus these changes; Windows, SDK 10.0.402):
- README package check (CONTRIBUTING.md section 4): README.md, LICENSE and the export schema are identical in the package, the restored hash equals the packed hash, and the package consumer passed 31/31. The consumer lock was not committed.
- `dotnet build WeatherParser.Ci.slnf -c Release --no-incremental`: 0 warnings, 0 errors.
- `dotnet test WeatherParser.Ci.slnf -c Release --no-build`: Parsing 1662 passed (1 skipped: the live test), SchemaGeneration 46, Coverage 99, Coverage.Runner 56.
- Pre-commit coverage (CONTRIBUTING.md section 5, full P4 diff against `b7da828`): 6811/6814 above 3035/3037, exit 0 (`evaluate.json` SHA-256 `2c995e7e6352f7cc37f9c45398100c0b11db5171eb270196c7d147a23f55e881`).

Codex five-angle review of the uncommitted record, handoff and status changes:
- Round 1: 2 findings (P3), both confirmed and fixed by qwen:
  - the review provenance in section 8.2 (the comments came from review 5634766709 on `c864650`, not from a review on `b6bc47f`);
  - the status date of `P4_VERIFICATION.md`.
- Own finding, fixed in the same request: section 8.1 dated the replies 2026-10-24 instead of 2026-10-23 (UTC).
- Round 2: 0 findings.
- Round 3, after this section and `P4_VERIFICATION.md` section 12 were written: 0 findings.

Authorship:
- qwen3-coder:30b drafted the status passages from the agent's facts. Two defective blocks were corrected by a second qwen request: one replaced the P2 sentence of `PHASE_P1_PLAN.md` instead of keeping it, and one combined non-adjacent parts of a line.
- The agent wrote sections 6, 7 and 8.2 and section 12 of `P4_VERIFICATION.md`, fixed a link wording in the `P4_VERIFICATION.md` status line and an unbalanced bold marker in this record's status line, and ran every check.

Checkpoint, on the user's request of 2026-10-24:
- Commit: `7cfc87ba7d24a244bf63d44b2b5db73bbab69d0d`.
- `pwsh ./scripts/ci-local.ps1 -Commit 7cfc87ba7d24a244bf63d44b2b5db73bbab69d0d -TargetRef b7da8288d8746bc6e034222c0467a8fcebba55b3 -RunSettings coverage.runsettings`:
  - exit 0, 2026-10-24T04:02:05Z to 04:02:58Z, run directory artifacts/ci/7cfc87b-20261024T040206Z (gitignored);
  - commits.json: commit `7cfc87b...`, targetCommit `b7da828...`, two-pass;
  - Parsing 1662 of 1663 passed (live test not executed), SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 31;
  - gate passed with 6811/6814 (gate-result.json SHA-256 `2c995e7e6352f7cc37f9c45398100c0b11db5171eb270196c7d147a23f55e881`);
  - packed SHA-512 `W1rhtMzEvyjCL/bYpAeuJ/uiSPzgkrinb3QZRp3tM3ya9Aou6kUhchLmFAFK87ae3KbkgUU5sOndBK0Yu/z2hQ==`; consumer lock contentHash line rewritten (one line).
- Pushed to pull request #5.
- This record's corrections of the ready-for-review statements (section 8, A9) follow in a second documentation commit. Its hosted CI run and the merge go into the post-merge checkpoint (A11).

## 8. Ready for review, review and merge (A9 to A11)

A9 (ready for review): the user marked pull request #5 ready for review on 2026-10-23T21:40:30Z (GitHub timeline event `ready_for_review`), before the A10 review. That was earlier than the plan's default timing (after A8, plan section 3), by the user's own action. The `ready_for_review` CI run is 40028480931 on head `c864650` (created 2026-10-23T21:40:31Z), which succeeded. When `gh pr ready 5` was run on 2026-10-24 after the A8 push, GitHub reported the pull request as already ready for review. Every later push ran CI again (`synchronize`). A11 (merge) is the user's.

### 8.1 A10 review round 1 (PR #5, 2026-10-23 to 2026-10-24)

The review of head `c864650` raised three inline comments. All three were confirmed and fixed test-first. The fixes change production code, so they make a **new implementation candidate**: the fix commit that adds this section. That commit is `b6bc47f`. Under A10 it needed a P4.11 S1–S4 rerun and refreshed A2, A5 and A6 before the user's acceptance decision. All three were done on 2026-10-24 (P4.11 record section 9; sections 2 and 5 of this record; the P5 handoff). The P4.11 live check (SC-16) stays a single run on `98122e5`.

| Comment | Finding | Fix | Red evidence |
|---|---|---|---|
| 4364483129 [Major] | `XNode.ReadFrom` materialized each placemark (and the product definition) before any bound applied (SC-20, SC-22) | `MosmixSEvidenceRefresher` parses the member as a forward-only stream. Text is read in bounded chunks (`kml:name`, `dwd:ProductID`, `dwd:IssueTime` at most 256 characters; forecast tokens at most 17 characters, scanned incrementally). A byte-level markup guard bounds every tag, CDATA section, comment and processing instruction to 1 MiB and the element nesting to 32. It accepts only UTF-8 (optional byte order mark) or ISO-8859-1 members that start with markup. A bounded name table limits the distinct XML names to 1,048,576 characters. Non-element children of containers and processing instructions are ignored, as `XElement.Elements()` and `XElement.Value` did | `MosmixSEvidenceRefresherMemoryBoundTests`: 836 MB and about 300 MB allocated against the 64 MiB bound before the fix; further cases from the Codex rounds below |
| 4364483133 [Major] | `SelectionDeadline` did not cover the evidence-index read (SC-18) | `MosmixLocationSelector` passes a token linked from the caller's token and the deadline to `ReadAsync`. Caller cancellation keeps precedence (SC-09) | `SelectAsync_DeadlineExpiresWhileIndexReadWaits_FailsWithSelectionDeadlineExceeded` hung until its 10 s test timeout |
| 4364483136 [Minor] | Replacing the index failed while a reader held it open | Readers open with `FileShare.Read \| FileShare.Delete`. An existing index is replaced with `File.Replace`, a first index is moved into place. `FileShare.Delete` alone, as suggested, was not enough: `File.Move(overwrite: true)` was still denied on Windows (.NET 10.0.13 probe) | `FileSystemMosmixEvidenceCacheConcurrencyTests`: `EVIDENCE-CACHE-FAILED` before the fix |

Codex reviewed the fix diff (five-angle audit) in seven rounds. Each confirmed finding got a Red test before the fix.

| Round | Findings | Disposition |
|---|---|---|
| 1 | CDATA materialized whole; `kml:name` bounded before trimming | Confirmed, fixed (markup guard; trim-aware bounded reader) |
| 2 | Internal whitespace deleted beyond the text bound; UTF-16 members bypass the byte guard; distinct names accumulate in the name table | Confirmed, fixed (whitespace runs kept as one space; encoding start check; bounded name table) |
| — | Own finding: the member's first construct was not bounded | Confirmed, fixed (`OversizedFirstConstructWithoutDeclaration_RejectedByTheMarkupBound`) |
| 3 | Unbounded nesting depth inside skipped subtrees | Confirmed, fixed (depth 32 accepted, 33 rejected) |
| 4 | Processing instructions rejected (compatibility regression) | Confirmed, fixed (`IgnoreProcessingInstructions`) |
| 5 | Significant whitespace (`xml:space="preserve"`) and text between container children rejected | Confirmed, fixed (`MoveToChildElement`; 50 MB text between children stays below 64 MiB) |
| 6, 7 | 0 | — |

The orchestrating agent also found that its local pre-commit coverage script had measured stale Release binaries after Debug-only builds. The script now builds Release first. The fresh measurement exposed one unreachable block in `MoveToChildElement`, which was removed.

Evidence on the working tree that `b6bc47f` records (Windows, SDK 10.0.402):
- `WeatherParser.Ci.slnf` build: 0 warnings.
- Parsing tests: 1662 passed, 1 skipped (the guarded live check).
- Pre-commit coverage gate: block coverage 6811/6814 against baseline 3035/3037 at `b7da828`, exit 0 (`evaluate.json` SHA-256 `2c995e7e6352f7cc37f9c45398100c0b11db5171eb270196c7d147a23f55e881`). The only uncovered library blocks are the three known ones: the `ParserEndpointValidator` constructor (2) and `FileSystemMosmixEvidenceCache.DeleteTemporaryFile` (1).
- Local package check: packed files identical, restored hash equals packed hash, package consumer 31 passed; the consumer lock was not committed.

Hosted CI run 40041773136 on `b6bc47f` succeeded on all three jobs (P4.11 record section 9). Replies to the three comments were posted on 2026-10-23 between 23:49:28Z and 23:49:31Z (UTC). A check one hour later found no new comments or reviews.

Authorship of the fix round:
- qwen3-coder:30b generated the tests and most production changes.
- The orchestrating agent made these changes by hand, each after failed briefs or as a mechanical edit:
  - restored dropped `using` directives;
  - moved the `pendingWhitespace` declaration;
  - removed an unused field and an always-true condition from the markup guard;
  - added the one-property `IgnoreProcessingInstructions` setting;
  - wrote this record.

### 8.2 A10 review round 2 (head `13ca95e`, 2026-10-24)

- Source: the reviewer's result, which the user posted in chat on 2026-10-24. It is not posted on GitHub. The three comments of section 8.1 were raised in review 5634766709 on `c864650` (2026-10-23T21:43:03Z); the only later GitHub review objects are the empty wrappers of the three replies, on `b6bc47f`.
- Scope: pull request #5 at `13ca95e`, the documentation checkpoint of the refresh, reviewed from all five angles.
- Findings: Critical none, Major none, Minor none. All three earlier issues are fixed and resolved. Since the previous review only five documentation files changed, and their verification records match the reviewed implementation.
- Verdict: ready to merge from a code-review perspective, pending the CI run of the head. The reviewer did not rerun tests locally and judged the hardware evidence from the committed records.
- The pending run, 40059685762 on `13ca95e` (pull_request), succeeded on windows-x64, ubuntu-x64 and debian13-arm64.
- The check "all review comments resolved or answered; review approval recorded" (A10) is met for this round.

### 8.3 A11 merge and post-merge checkpoint (2026-10-24)

- Merge: the user merged pull request #5 into `develop` on 2026-10-24T04:09:56Z (`gh pr view 5`: state MERGED, merged by witalij-schrajer). Merge commit `95bcb1e3287cae275323cc3bf202337de1942d88`, head `337a47ab8f2031d4396c818830ec2727d0c3ff49`, base before the merge `b7da828`; `git rev-parse origin/develop` equals the merge commit.
- Final head CI: run 40060895171 on `337a47a` (pull_request) succeeded on windows-x64, ubuntu-x64 and debian13-arm64. Run 40060809682 on `7cfc87b` was cancelled by the workflow's `cancel-in-progress` concurrency when `337a47a` was pushed; `7cfc87b` is covered by the `ci-local.ps1` run of section 7 and by run 40060895171 on its successor. The workflow has no `push` trigger, so no run exists for the merge commit itself.
- Home of the post-merge records (plan section 3, decided by the user on 2026-10-24): a separate documentation pull request from branch `features/develop/P4-post-merge` into `develop`; P5 starts only on a separate instruction.
- Post-merge updates: phase status "P4 complete" in `AGENTS.md`, `README.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md`, `PHASE_P4_PLAN.md` (status and the last checklist item), the selection contract, `P4_VERIFICATION.md` and the P4.12 plan; the P5 handoff integration target is the merge commit `95bcb1e`.
- P4 is complete (A7 and A11).
- Authorship: the orchestrating agent wrote this checkpoint (plan role for A11: orchestrating agent). It also moved the "Authorship of the fix round" block, which the section 8.2 insertion had left below 8.2, back to the end of section 8.1. Checks: the README package check passed on the working tree (README.md, LICENSE and the export schema identical in the package, consumer 31/31). Codex five-angle review of this checkpoint: round 1, 0 findings.
- Checkpoint commit `38bd19860ba862dc5eb84132f8df0de2ec8037d7`, made on the user's request of 2026-10-24.
- `pwsh ./scripts/ci-local.ps1 -Commit 38bd19860ba862dc5eb84132f8df0de2ec8037d7 -TargetRef 95bcb1e3287cae275323cc3bf202337de1942d88 -RunSettings coverage.runsettings`:
  - exit 0, 2026-10-24T04:14:29Z to 04:15:26Z, run directory artifacts/ci/38bd198-20261024T041429Z (gitignored);
  - commits.json: commit `38bd198...`, targetCommit `95bcb1e...`, two-pass;
  - Parsing 1662 of 1663 passed (live test not executed), SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 31; target pass Parsing 1662 of 1663;
  - gate passed with all five rules true: 6811/6814 is at or above the merged-`develop` baseline 6811/6814 (documentation-only change, equal coverage allowed); gate-result.json SHA-256 `1b780e0423f1b781e33a70d4874da4fed0670531e5090a0a6cf8a575c8c1b3f4`;
  - packed SHA-512 `F6/Kpn5FBrEBVAnK/Q0v08jEuJsyUHlPkpOgUtWcTWGzA0TpaBZyfBRKl8ilXDHUxaubbL5Nc4t2F9tnCTwCpQ==`; consumer lock contentHash line rewritten (one line).
- This result is recorded in a second documentation commit on the same branch. The hosted CI run of the documentation pull request and its merge by the user stay outside this record.

## 9. Authorship

Refresh of 2026-10-24 (A10):
- The orchestrating agent ran the P4.11 rerun, extracted every value and wrote the refreshed sections 1, 2, 4, 5 and 8.1 of this record.
- qwen3-coder:30b drafted section 9 of the P4.11 record from the agent's facts.
- The agent updated by hand the value lines of the P5 handoff and its paragraph on the streaming-reader bounds, `docs/COMPATIBILITY.md` and `P4_VERIFICATION.md` section 11. These are value updates and evidence statements, which AGENTS.md lets the orchestrating agent write.
- The agent also corrected the SDK version in section 8.1 (10.0.402, not 10.0.303).
- Codex reviewed the refresh (five-angle audit, read-only): round 1, 0 findings.

The orchestrating agent wrote this record from its verified evidence (step records, P4.11 runs and artifacts, the T-P4 mapping); qwen3-coder:30b drafted the [P5 handoff](P5_HANDOFF.md) from the orchestrating agent's facts, which the agent checked against the code (every cited type name) and the plans. Codex review of the draft: round 1, 4 findings, all confirmed and fixed — inherited P2 limitations now carried forward item by item with owners (record and handoff), characterization evidence named per step in the matrix (agent), API and schema links in the handoff (qwen added them; the agent corrected the Markdown syntax) and the coverage rule stated as the ratio rule of REQ-086 (the agent, after qwen did not apply it). Round 2: 0 findings.
