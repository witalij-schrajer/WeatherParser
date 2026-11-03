# P8.01 – Foundation Check and Plan Adjustment: Implementation Plan

**Status: planned on 2026-10-28; accepted by the user on 2026-10-28 with the phase plan; complete on 2026-10-28 with outcome READY ([record](../../p8/P8_01_FOUNDATION_CHECK.md)).** Refines section 1 and the P8.01 row of section 5 of [PHASE_P8_PLAN.md](../PHASE_P8_PLAN.md).

## 1. Goal and Scope
Goal: confirm that P7 is accepted and merged, classify every commit on `develop` since the P7 merge, extract the accepted foundation, map every inherited limitation to a P8 step or a carried-forward owner, remeasure the native block-coverage baseline of the integration target on the first committed revision of the P8 branch, and adjust `PHASE_P8_PLAN.md` where the extraction contradicts it.
In scope: records recheck; commit classification; extraction table; inherited-limitation mapping; SDK servicing check; P8 branch, "P8 started" status passages and draft pull request (on the user's instruction); baseline run with `ci-local.ps1`; plan reconciliation; record `docs/p8/P8_01_FOUNDATION_CHECK.md` and the P8.01 section of `docs/p8/P8_VERIFICATION.md`; outcome READY or NOT_READY.
Out of scope with owner: protocol facts and the MQTT message contract → P8.02; production code and tests → P8.03 onwards; CLI and broker transport → P3.

## 2. Preconditions and Inputs
- P7 accepted by the user on 2026-10-28 on candidate `2f1115b`; pull request #11 merged into `develop` on 2026-10-28 (merge commit `20b2ef0a62e8a2a85c7f1a28b7428cb45b8dbeec`). The post-merge documentation pull request #12 carries the P7 post-merge records and the P8 plans; its merge state is checked here.
- Since `20b2ef0`, `develop` contains pull request #13 (merge `ff40dc9`): the automatic Codex review workflow `.github/workflows/review.yml` with the briefs under `.github/review/`, the CI change "run the three CI jobs only when code changed" in `.github/workflows/ci.yml`, the AGENTS.md and CONTRIBUTING.md passages on both, `docs/ci/CODEX_REVIEW_WORKFLOW_VERIFICATION.md`, and one test added to `tests/WeatherParser.Parsing.Tests/ParserEndpointValidatorTests.cs` (the constructor null guard). It changes CI configuration and tests, not the library.
- The planning gate of PHASE_P8_PLAN.md section 5 is satisfied: the step plans P8.01 to P8.10 are written and Codex-reviewed to 0 findings (P8.03 onwards additionally wait for their reconciliation with the approved P8.02 contract).
- Inputs: `docs/p7/P7_12_ACCEPTANCE.md` (section 4), `docs/p7/P8_HANDOFF.md` (sections 1, 4 and 7), the limitation sections of the P2, P4, P5 and P6 acceptance records, `docs/p2/P2_EXPORT_CONTRACT.md`, `global.json`, `DEPENDENCIES.md`, `docs/COMPATIBILITY.md`, official .NET release metadata (URL and check date recorded).
- Required user instruction: creating the branch `features/develop/P8`, committing, pushing and opening the draft pull request.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Integration target | `20b2ef0` (P7 merge) / the `develop` head at branch creation | The `develop` head at branch creation (`git rev-parse origin/develop`); every commit since `20b2ef0` classified as documentation, CI configuration, tests or production code; a production-code change of the library since `20b2ef0` is reported to the user before P8.02 | Orchestrator, recorded | S1, S4 |
| Baseline expectation | 10246/10249 (P7 gate on `2f1115b`) / remeasured value | The remeasured value of S4 is binding; a deviation (for example from the test added by pull request #13) is recorded and explained from the report counters | Orchestrator, recorded | S4 |
| Inherited limitations | Map each to a P8 step and check / carry forward with owner | Mapping as in S2; an item without a mapping blocks READY | Orchestrator, recorded | S2 |
| Plan adjustments | Adjust `PHASE_P8_PLAN.md` where the extraction contradicts it / defer to P8.02 | Adjust in S5; contract-level changes stay with P8.02 | Orchestrator, recorded; contract changes User (P8.02) | S5 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p8/P8_01_FOUNDATION_CHECK.md` | Create | Records recheck, commit classification, extraction table, limitation mapping, SDK check, baseline run, adjustments, outcome |
| `docs/p8/P8_VERIFICATION.md` | Create | P8 verification record with the P8.01 section |
| `PHASE_P8_PLAN.md` | Change | Section 1 table "P8.01 extraction" and adjustments; status line |
| `README.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md` | Change | Phase-status passages "P8 started on <date> on the user's instruction" (README needs the package check of CONTRIBUTING.md section 4) |

## 5. Procedure
No slice changes behavior, so there is no Red step. The orchestrating agent writes every record and status text and runs every command; Codex reviews in S5.

### S1 Records recheck and commit classification
- Confirm acceptance and merge; `git merge-base --is-ancestor 20b2ef0 origin/develop`; `git log --oneline 20b2ef0..origin/develop` and `git diff --stat 20b2ef0 origin/develop`, each commit classified; `git diff 20b2ef0 origin/develop -- src/` must be empty, otherwise the change is described and reported; pull request #12 state recorded.

### S2 Extraction, limitation mapping and SDK check
- Extraction table: integration target, package identity `WeatherParser.Parsing` `0.1.0-initial`, SDK and runtime versions, coverage tools, the contract ranges (C-01 to C-127; SC-01 to SC-23; OC-01 to OC-14; OW-01 to OW-16; MF-01 to MF-18), the services P8 reuses (`IJsonExportSerializer`, `IXmlExportSerializer`, the five export roots, `AddWeatherParsing`, the package-consumer helpers `TestConsumerServices` and `ConsumerSelectionSupport`), the CI rules in force since pull request #13.
- Limitation mapping of every open item in `P7_12_ACCEPTANCE.md` section 4, `P8_HANDOFF.md` section 7 and every item inherited with owner P8 (the table of PHASE_P8_PLAN.md section 1 is the minimum): MQTT payloads and topics → P8.03 to P8.06; product-specific forecast topics and the complete retained run → P8.04, P8.06; OW-14 → P8.06, P8.08, owner the user; DI under load → P8.07 (MQ-12); registration consequence of the options types → P8.07, P8.08; schema compatibility note → P8.08, P9; builder and root-validation duplication, coarse XML cancellation, hosted ARM64 container, package reproducibility → carried with their owners; MOSMIX_S evidence-refresh CLI exposure → P3.
- SDK servicing status against the official .NET release metadata; the pinned SDK 10.0.402 stays unless the user decides an update.

### S3 Branch, status passages, seed commit, push and draft pull request
- Branch `features/develop/P8` from `origin/develop`; README package check; commit 1 = the "P8 started" passages and the P8.01 record draft; push; `gh pr create --draft --base develop`; Auto-fix on; pull-request number recorded. Because the seed commit changes `README.md`, which counts as code for the CI classification, the hosted CI jobs run on this push.

### S4 Baseline
- `pwsh ./scripts/ci-local.ps1 -Commit <seed-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`; record exit code, run directory, the native block counters of the target and of the seed, test counts and the hosted CI run of the seed push.

### S5 Plan reconciliation, record and review
- Add the table "P8.01 extraction" to PHASE_P8_PLAN.md section 1; adjust the plan where the extraction contradicts it; finish the record with outcome READY or NOT_READY; Codex five-angle review of the record and the plan changes until 0 findings; commit and push on the user's instruction.

## 6. Tests and Test IDs
| Step-local case | T-P8 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S4 | T-P8-16 (baseline) | REQ-085, REQ-086 | Remeasured native block baseline of the integration target |

## 7. Evidence to Record
- Commit classification with SHAs; extraction table; mapping table; SDK check URL and date; `ci-local.ps1` run directory, exit code and counters; hosted CI run ID; Codex rounds and findings

## 8. Risks and Limitations
- (a) Pull request #12 may still be open when P8 starts; the integration target is then the `develop` head without it, and its records are classified when it is merged
- (b) Documentation-only pushes no longer run the CI jobs; the seed commit runs them only because it changes `README.md`

## 9. Acceptance Criteria
- [ ] P7 acceptance and merge confirmed; every commit since `20b2ef0` classified
- [ ] Every inherited item mapped to a P8 step or a carried-forward owner
- [ ] Baseline remeasured and recorded on the seed commit
- [ ] Plan adjusted; record READY or NOT_READY; Codex 0 findings or reported
