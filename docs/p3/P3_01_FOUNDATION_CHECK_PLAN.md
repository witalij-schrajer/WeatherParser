# P3.01 – Foundation Check and Plan Adjustment: Implementation Plan

**Status: planned on 2026-10-29; accepted by the user on 2026-10-29 with the phase plan ("Accept the plans"); executed on 2026-10-29: complete, outcome READY ([record](P3_01_FOUNDATION_CHECK.md)).** Refines section 1 and the P3.01 row of section 5 of [PHASE_P3_PLAN.md](../../PHASE_P3_PLAN.md).

## 1. Goal and Scope
Goal: confirm that P8 is accepted and merged, classify every commit on `develop` since the P8 merge, extract the library foundation the CLI consumes, map every inherited item to a P3 step or a carried-forward owner, remeasure the native block-coverage baseline of the integration target on the first committed revision of the P3 branch, and adjust `PHASE_P3_PLAN.md` where the extraction contradicts it.
In scope: records recheck; commit classification; extraction table (package identity, SDK and tools, the consumed public API, error and notice IDs, the consumer registrations, contracts in force); inherited-item mapping; SDK servicing check; the coverage input of CL-19 (every uncovered block of the target and whether the `FileSystemMosmixEvidenceCache.DeleteTemporaryFile` block can be covered by a test without a library change); P3 branch, "P3 started" status passages and draft pull request; baseline run with `ci-local.ps1`; plan reconciliation; record `docs/p3/P3_01_FOUNDATION_CHECK.md` and the P3.01 section of `docs/p3/P3_VERIFICATION.md`; outcome READY or NOT_READY.
Out of scope with owner: dependency facts and the CLI contract → P3.02; production code and tests → P3.03 onwards.

## 2. Preconditions and Inputs
- P8 accepted by the user on 2026-10-29 on candidate `ce166eb`; pull request #14 merged into `develop` on 2026-10-29 (merge commit `21b72106b3fd422c1a04020dd1d4288e6f99df74`). The post-merge documentation pull request #15 carries the P8 post-merge records and the P3 plans; its merge state is checked here.
- The planning gate of PHASE_P3_PLAN.md section 5 is satisfied: the phase plan and the step plans P3.01 to P3.12 are reviewed to 0 findings and accepted by the user.
- Inputs: `docs/p8/P8_10_ACCEPTANCE.md` (sections 4, 6 to 8), `docs/p8/P3_HANDOFF.md`, the limitation sections of the P2 and P4 to P7 acceptance records, the README sections of the library (consumer registrations, error and notice IDs), `global.json`, `DEPENDENCIES.md`, `docs/COMPATIBILITY.md`, the coverage report of the baseline run, official .NET release metadata (URL and check date recorded).
- Required user instruction: starting P3 (creating the branch `features/develop/P3`, committing, pushing and opening the draft pull request).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Integration target | `21b7210` (P8 merge) / the `develop` head at branch creation | The `develop` head at branch creation (`git rev-parse origin/develop`); every commit since `21b7210` classified; a production-code change of the library since `21b7210` is reported to the user before P3.02 | Orchestrator, recorded | S1, S4 |
| Baseline expectation | 11102/11103 (measured on `9dfa7ca` for target `21b7210`) / remeasured value | The remeasured value of S4 is binding; a deviation is recorded and explained from the report counters | Orchestrator, recorded | S4 |
| Inherited items | Map each to a P3 step and check / carry forward with owner | Mapping as in S2; an item without a mapping blocks READY | Orchestrator, recorded | S2 |
| Plan adjustments | Adjust `PHASE_P3_PLAN.md` where the extraction contradicts it / defer to P3.02 | Adjust in S5; contract-level changes stay with P3.02 | Orchestrator, recorded; contract changes User (P3.02) | S5 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p3/P3_01_FOUNDATION_CHECK.md` | Create | Records recheck, commit classification, extraction table, item mapping, SDK check, baseline run and uncovered blocks, adjustments, outcome |
| `docs/p3/P3_VERIFICATION.md` | Create | P3 verification record with the P3.01 section |
| `PHASE_P3_PLAN.md` | Change | Section 1 table "P3.01 extraction" and adjustments; status line |
| `README.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md` | Change | Phase-status passages "P3 started on <date> on the user's instruction" (README needs the package check of CONTRIBUTING.md section 4) |

## 5. Procedure
No slice changes behavior, so there is no Red step. The orchestrating agent writes every record and status text and runs every command.

### S1 Records recheck and commit classification
- Confirm acceptance and merge; `git merge-base --is-ancestor 21b7210 origin/develop`; `git log --oneline 21b7210..origin/develop` and `git diff --stat 21b7210 origin/develop`, each commit classified; `git diff 21b7210 origin/develop -- src/` must be empty, otherwise the change is described and reported; pull request #15 state recorded.

### S2 Extraction, item mapping and SDK check
- Extraction table: integration target, package identity `WeatherParser.Parsing` `0.1.0-initial`, SDK and runtime versions, coverage tools, the contract ranges (C-01 to C-127; SC-01 to SC-23; OC-01 to OC-14; OW-01 to OW-16; MF-01 to MF-18; MQ-01 to MQ-15).
- The consumed public API, read from the source and the README, not from memory: `ICdcStationSelector`, `IMosmixLocationSelector`, `IMosmixSEvidenceRefresher`, `IDwdObservationParser`, `IOsthessenWetterParser` (result with datasets and station failures), `IMosmixLParser`, `IMosmixSParser` (result with run, omissions, freshness), `ICoordinateParser`, `IJsonExportSerializer`, `IXmlExportSerializer`, `IMqttMessageSerializer`, `AddWeatherParsing`; the thirteen consumer registrations; every public exception type with its `ErrorId` values and every notice ID (the input of CL-10).
- Item mapping of every open item in `P8_10_ACCEPTANCE.md` section 4, `P3_HANDOFF.md` section 7 and every item inherited with owner P3 (the table of PHASE_P3_PLAN.md section 1 is the minimum).
- SDK servicing status against the official .NET release metadata; the pinned SDK 10.0.402 stays unless the user decides an update.

### S3 Branch, status passages, seed commit, push and draft pull request
- Branch `features/develop/P3` from `origin/develop`; README package check; commit 1 = the "P3 started" passages and the P3.01 record draft; push; `gh pr create --draft --base develop`; Auto-fix on; pull-request number recorded. The seed commit changes `README.md`, which counts as code for the CI classification, so the hosted CI jobs run on this push.

### S4 Baseline and coverage input
- `pwsh ./scripts/ci-local.ps1 -Commit <seed-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`; record exit code, run directory, the native block counters of the target and of the seed, test counts and the hosted CI run of the seed push.
- From the target's coverage report: list every uncovered block of `WeatherParser.Parsing.dll` with its method; for `FileSystemMosmixEvidenceCache.DeleteTemporaryFile`, read the code path and record whether a test can reach the `catch` through the public or internal API without changing library code (for example a deletion failure on the platform), with the reason; the result is the input of CL-19 option B.

### S5 Plan reconciliation, record and review
- Add the table "P3.01 extraction" to PHASE_P3_PLAN.md section 1; adjust the plan where the extraction contradicts it (for example a library error ID that the CL-10 list must pass through); finish the record with outcome READY or NOT_READY; Codex five-angle review of the record and the plan changes until 0 findings; commit and push to the phase pull request.

## 6. Tests and Test IDs
| Step-local case | T-P3 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S4 | T-P3-20 (baseline) | REQ-085, REQ-086 | Remeasured native block baseline of the integration target and its uncovered blocks |

## 7. Evidence to Record
- Commit classification with SHAs; extraction table; mapping table; SDK check URL and date; `ci-local.ps1` run directory, exit code and counters; the uncovered-block list and the `DeleteTemporaryFile` assessment; hosted CI run ID; Codex rounds and findings

## 8. Risks and Limitations
- (a) Pull request #15 may still be open when P3 starts; the integration target is then the `develop` head without it, and its records are classified when it is merged
- (b) The consumed API list is read from the source; a member the README names but the source lacks (or the reverse) is a documentation defect reported to the user, not silently corrected in the CLI

## 9. Acceptance Criteria
- [ ] P8 acceptance and merge confirmed; every commit since `21b7210` classified
- [ ] Every inherited item mapped to a P3 step or a carried-forward owner
- [ ] Consumed API, error IDs and notice IDs extracted from the source
- [ ] Baseline remeasured and recorded on the seed commit; uncovered blocks listed and assessed
- [ ] Plan adjusted; record READY or NOT_READY; Codex 0 findings or reported
