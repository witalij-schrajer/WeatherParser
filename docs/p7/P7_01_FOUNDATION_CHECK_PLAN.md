# P7.01 – Foundation Check and Plan Adjustment: Implementation Plan

**Status: planned (2026-10-26); not started. P7 starts only on a separate instruction of the user.** Refines section 1 and the P7.01 row of section 5 of [PHASE_P7_PLAN.md](../../PHASE_P7_PLAN.md).

## 1. Goal and Scope
Goal: confirm that P6 is accepted and merged, extract the accepted foundation, map every inherited limitation to a P7 step or a carried-forward owner, remeasure the native block-coverage baseline of the integration target on the first committed revision of the P7 branch, and adjust `PHASE_P7_PLAN.md` where the extraction contradicts it.
In scope: records recheck; extraction table; inherited-limitation mapping; SDK servicing check; P7 branch, "P7 started" status passages and draft pull request (on the user's instruction); baseline run with `ci-local.ps1`; plan reconciliation; record `docs/p7/P7_01_FOUNDATION_CHECK.md` and the P7.01 section of `docs/p7/P7_VERIFICATION.md`; outcome READY or NOT_READY.
Out of scope with owner: source facts and the MOSMIX forecast contract → P7.02; production code and tests → P7.03 onwards; CLI → P3.

## 2. Preconditions and Inputs
- P6 accepted by the user on 2026-10-26 on candidate `12e7205`; pull request #9 merged into `develop` on 2026-10-26 (merge commit `de9d4ba3123dcd8a25b8cb0d8d42724f20748843`). The post-merge documentation pull request #10 carries the P6 post-merge records and the P7 plans; its merge state is checked here.
- The planning gate of PHASE_P7_PLAN.md section 5 is satisfied: all twelve step plans P7.01 to P7.12 are written and Codex-reviewed to 0 findings (P7.03 onwards additionally wait for their reconciliation with the approved P7.02 contract before they start).
- Inputs: `docs/p6/P6_11_ACCEPTANCE.md` (section 4), `docs/p6/P7_HANDOFF.md` (sections 1, 4 and 7), `docs/p5/P5_11_ACCEPTANCE.md` (section 4, the items inherited with owner P7), `docs/p4/P4_SELECTION_CONTRACT.md` (section 5), `docs/p6/P6_VERIFICATION.md`, `docs/p2/P2_EXPORT_CONTRACT.md`, `global.json`, `DEPENDENCIES.md`, `docs/COMPATIBILITY.md`, official .NET release metadata (URL and check date recorded).
- Required user instruction: creating the branch `features/develop/P7`, committing, pushing and opening the draft pull request.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Integration target | `de9d4ba` (P6 merge) / the `develop` head at branch creation after pull request #10 | The `develop` head at branch creation (`git rev-parse origin/develop`); it must be `de9d4ba` or a documentation-only successor; any production change since `de9d4ba` is reported to the user before P7.02 | Orchestrator, recorded | S3, S4 |
| Baseline expectation | 8486/8489 (P6 end state on `12e7205`) / remeasured value | The remeasured value of S4 is binding; a deviation is recorded and explained | Orchestrator, recorded | S4 |
| Inherited limitations | Map each to a P7 step and check / carry forward with owner | Mapping as in S2; any item without a mapping blocks READY | Orchestrator, recorded | S2 |
| Plan adjustments | Adjust `PHASE_P7_PLAN.md` where the extraction contradicts it / defer to P7.02 | Adjust in S5; contract-level changes stay with P7.02 | Orchestrator, recorded; contract changes User (P7.02) | S5 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p7/P7_01_FOUNDATION_CHECK.md` | Create | Records recheck, extraction table, limitation mapping, SDK check, baseline run, adjustments, outcome |
| `docs/p7/P7_VERIFICATION.md` | Create | P7 verification record with the P7.01 section |
| `PHASE_P7_PLAN.md` | Change | Section 1 table "P7.01 extraction" and adjustments; status line |
| `README.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md` | Change | Phase-status passages "P7 started on <date> on the user's instruction" (README needs the package check of CONTRIBUTING.md section 4) |

## 5. Procedure
No slice changes behavior, so there is no Red step. The orchestrating agent writes every record and status text and runs every command; Codex reviews in S5.

### S1 Records recheck
- Confirm acceptance and merge; `git merge-base --is-ancestor de9d4ba origin/develop`; `git log --oneline de9d4ba..origin/develop`, each commit classified as documentation-only or not; pull request #10 state recorded.

### S2 Extraction, limitation mapping and SDK check
- Extraction table: integration target, package identity, SDK and runtime versions, coverage tools, export contract version and decision range (C-01 to C-119, with C-66 to C-81 for forecast runs), the selection, observation and OsthessenWetter contract ranges, the helpers P7 reuses (P7 handoff sections 2 and 4: `ICatalogDownloader`, `IForecastRunBuilder`, `IMosmixLEvidenceReader`, `IMosmixSEvidenceRefresher`, the MOSMIX_L URI composition of SC-12, `MosmixKmzBuilder` in the tests), P6 coverage at acceptance.
- Limitation mapping of every open item in `P6_11_ACCEPTANCE.md` section 4 and `P7_HANDOFF.md` section 7, and of every item inherited with owner P7, at least:

| Inherited item | P7 disposition | Check |
|---|---|---|
| MOSMIX element verification and freshness per product (P4) | Done in P7 | P7.02 (MF-01, MF-09, MF-18), P7.05 (T-P7-23), P7.07 and P7.08 (T-P7-13) |
| Full MOSMIX parsing, time alignment and units; SC-20 re-verified against the schema (P4 section 5) | Done in P7 | P7.02 (MF-07), P7.04, P7.05 |
| Forecast-only quantities as additive contract decisions (P2) | Decided in P7 | P7.02 (MF-11), P7.03 |
| Builder and root-validation duplication | Guarded again in P7 | P7.03 and P7.09 equivalence tests through the real serializers |
| Coarse XML cancellation (C-10) | Carried forward, owner as recorded | Restated in the P7.12 limitations |
| Hosted `debian13-arm64` container is not target evidence | Kept separate | P7.11 records NanoPi evidence apart from hosted CI |
| Package not byte-reproducible | Carried forward, owner P9 | Restated in the P7.12 limitations |
| DI under load | Carried forward, owners P8 and P3 | Restated in the P7.12 limitations |
| OsthessenWetter source conditions (OW-14) | Unchanged, owner the user; P8 and P3 must not redistribute | Restated in the P7.12 limitations and the P8 handoff |
| Example-CLI exposure of the MOSMIX_S evidence refresh (SC-22) | Unchanged, owner the user and P3 | Restated in the P7.12 limitations |
| Every other open P4, P5 and P6 item | Its recorded owner | Restated in the P7.12 limitations |

- SDK servicing status against the official .NET release metadata.

### S3 Branch, status passages, seed commit, push and draft pull request
- Branch `features/develop/P7` from `origin/develop`; README package check; commit 1 = the "P7 started" passages and the P7.01 record draft; push; `gh pr create --draft --base develop`; Auto-fix on; pull-request number and CI run recorded.

### S4 Baseline run on the committed revision
- `pwsh ./scripts/ci-local.ps1 -Commit <commit-1-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`; equal coverage passes for the documentation-only candidate; both block counters, the gate result and its SHA-256, the package hash and the test counts recorded. The counters are the comparison value for the P7 code steps only while the pull-request base and `coverage.runsettings` are unchanged; both are checked before every reuse.

### S5 Plan reconciliation, review and record commit
- `PHASE_P7_PLAN.md` section 1 table "P7.01 extraction" and adjustments; Codex five-angle review of the uncommitted changes; confirmed findings fixed by the orchestrating agent; checkpoint commit when the user requests it; push; CI run recorded.

## 6. Tests and Test IDs
| Step-local case | T-P7 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S4 | T-P7-20 (baseline) | REQ-086 | Native block counters of the integration target remeasured on the committed revision; equal coverage accepted for the documentation-only candidate; unrounded counters |

## 7. Evidence to Record
- Commits since `de9d4ba` with classification; integration target SHA; extraction table with sources; limitation mapping
- SDK servicing check (URL, date, result)
- Branch, commit SHAs, pull-request number, CI run ID
- `ci-local.ps1` command, exit code, run directory, counters, gate result and SHA-256, package hash
- Plan adjustments; Codex rounds and dispositions; authorship

## 8. Risks and Limitations
- (a) `develop` moved beyond `de9d4ba` with production changes — reported; P7.02 waits for the user's decision
- (b) A newer SDK servicing release — recorded; an update is a separate user decision
- (c) Baseline differs from 8486/8489 — the remeasured value is binding and the difference explained
- (d) Pull request #10 not merged — the branch is created from the merged state only

## 9. Acceptance Criteria
- [ ] P6 acceptance and merge confirmed; every commit since `de9d4ba` classified
- [ ] Extraction table complete with sources
- [ ] Every inherited limitation mapped to a P7 step and check or carried forward with its owner
- [ ] SDK servicing status recorded
- [ ] Branch, seed commit, push and draft pull request done on the user's instruction; CI run recorded
- [ ] Baseline run on the committed revision passed; counters recorded
- [ ] `PHASE_P7_PLAN.md` section 1 updated; status passages consistent (AGENTS.md table)
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability reported to the user
- [ ] Outcome recorded (READY or NOT_READY with open items); no production code or test changed
